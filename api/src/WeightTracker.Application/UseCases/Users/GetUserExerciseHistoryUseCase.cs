using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// Reads how a user's performance of one exercise evolves: their latest finished sessions of it, newest first, each
/// with its max weight, volume and estimated one-rep max.
/// </summary>
/// <remarks>
/// A regular user may only read their own history, which is checked before anything is looked up so a refused call
/// discloses nothing. A trainer reads their own, or for anyone else only the sessions they prescribed; an admin reads
/// every session of anyone. The numbers are the domain rules of <see cref="SetMetrics"/>, applied here to the raw values
/// the read projection returns, so the volume and the estimate are defined once.
/// </remarks>
public class GetUserExerciseHistoryUseCase
{
    public const int DefaultLimit = 10;

    public const int MinLimit = 1;

    public const int MaxLimit = 50;

    private readonly IUserRepository _users;

    private readonly IExerciseRepository _exercises;

    private readonly ExerciseVisibility _visibility;

    private readonly IExerciseHistoryQuery _history;

    public GetUserExerciseHistoryUseCase(
        IUserRepository users,
        IExerciseRepository exercises,
        ExerciseVisibility visibility,
        IExerciseHistoryQuery history)
    {
        _users = users;
        _exercises = exercises;
        _visibility = visibility;
        _history = history;
    }

    /// <param name="limit">Number of sessions to return, <see cref="DefaultLimit"/> when <c>null</c>.</param>
    /// <returns>The sessions, newest first, empty when the user never finished a workout with records for the exercise.</returns>
    /// <exception cref="InvalidHistoryLimitException">When the limit is outside <see cref="MinLimit"/> to <see cref="MaxLimit"/>.</exception>
    /// <exception cref="UnauthorizedOperationException">When a regular user asks for another user.</exception>
    /// <exception cref="UserNotFoundException">When no user has the given id.</exception>
    /// <exception cref="ExerciseNotFoundException">When no exercise has the given id, or the caller may not see it.</exception>
    public async Task<IReadOnlyList<ExerciseHistorySessionView>> Execute(
        Id callerId, Role callerRole, Id userId, Id exerciseId, int? limit, CancellationToken cancellationToken)
    {
        var requested = limit ?? DefaultLimit;

        if (requested < MinLimit || requested > MaxLimit)
            throw new InvalidHistoryLimitException(requested, MinLimit, MaxLimit);

        if (callerRole == Role.USER && callerId != userId)
            throw new UnauthorizedOperationException($"GET /users/{userId}/exercises/{exerciseId}/history");

        _ = await _users.GetById(userId, cancellationToken)
            ?? throw new UserNotFoundException(userId);

        var exercise = await _exercises.GetById(exerciseId, cancellationToken)
            ?? throw new ExerciseNotFoundException(exerciseId);

        await _visibility.EnsureVisible(exercise, callerId, callerRole, cancellationToken);

        // A trainer reading someone else's sessions only gets the ones they prescribed
        var prescribedBy = callerRole == Role.ADMIN || callerId == userId ? null : callerId;

        var sessions = await _history.Get(userId, exerciseId, prescribedBy, requested, cancellationToken);

        return sessions.Select(Summarize).ToList();
    }

    private static ExerciseHistorySessionView Summarize(ExerciseSessionView session)
    {
        var sets = session.Sets.Select(set => (Result: set.ToSetResult(), Weight: set.ToWeight())).ToList();

        var volumes = sets.Select(set => SetMetrics.Volume(set.Result, set.Weight)).Where(volume => volume is not null).Select(volume => volume!.Value).ToList();
        var estimates = sets.Select(set => SetMetrics.EstimatedOneRepMax(set.Result, set.Weight)).Where(estimate => estimate is not null).Select(estimate => estimate!.Value).ToList();
        var weights = sets.Where(set => set.Weight is not null).Select(set => set.Weight!.Value).ToList();

        return new ExerciseHistorySessionView(
            session.WorkoutId,
            session.WorkoutDate,
            weights.Count == 0 ? null : weights.Max(),
            volumes.Count == 0 ? null : volumes.Sum(),
            estimates.Count == 0 ? null : estimates.Max());
    }
}
