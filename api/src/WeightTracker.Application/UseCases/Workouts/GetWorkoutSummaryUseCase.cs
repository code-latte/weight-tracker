using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Reads the stats of one workout the caller is entitled to see: sets in target, volume, and per exercise the top set
/// against the top set of its previous execution.
/// </summary>
/// <remarks>
/// The visibility rule is the one of <see cref="GetWorkoutByIdUseCase"/>, kept in <see cref="WorkoutVisibility"/>, and
/// it is applied before anything else is read. The previous executions are the owner's earlier finished workouts the
/// caller may see: for an admin or the owner that is all of them, for a trainer who does not own the workout only the
/// ones they prescribed, so a trainer never reads an athlete's own sessions through it. The sums and the ranking are the
/// domain rules of <see cref="SetMetrics"/>, <see cref="TopSetComparer"/> and <see cref="WorkoutExercise.SetsInTarget"/>.
/// </remarks>
public class GetWorkoutSummaryUseCase
{
    private readonly IWorkoutRepository _workouts;

    private readonly WorkoutVisibility _visibility;

    private readonly IExercisePreviousExecutionQuery _previousExecutions;

    public GetWorkoutSummaryUseCase(
        IWorkoutRepository workouts,
        WorkoutVisibility visibility,
        IExercisePreviousExecutionQuery previousExecutions)
    {
        _workouts = workouts;
        _visibility = visibility;
        _previousExecutions = previousExecutions;
    }

    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not see the workout.</exception>
    public async Task<WorkoutSummaryView> Execute(Id callerId, Role callerRole, Id workoutId, CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        if (_visibility.CanSee(workout, callerId, callerRole) == false)
        {
            throw new UnauthorizedOperationException($"GET /workouts/{workoutId}/summary");
        }

        var previous = await LoadPreviousExecutions(workout, callerId, callerRole, cancellationToken);

        var exercises = workout.Exercises
            .Select(entry => Summarize(entry, previous.GetValueOrDefault(entry.ExerciseId)))
            .ToList();

        var volumes = exercises.Where(exercise => exercise.Volume is not null).Select(exercise => exercise.Volume!.Value).ToList();

        return new WorkoutSummaryView(
            workout.Id,
            new SetsInTargetView(exercises.Sum(exercise => exercise.SetsInTarget.InTarget), exercises.Sum(exercise => exercise.SetsInTarget.Evaluated)),
            volumes.Count == 0 ? null : volumes.Sum(),
            exercises);
    }

    // The owner's earlier sessions. A trainer who does not own the workout only gets the ones they prescribed
    private async Task<IReadOnlyDictionary<Id, ExercisePreviousExecutionView>> LoadPreviousExecutions(
        Workout workout, Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        var exerciseIds = workout.Exercises.Select(entry => entry.ExerciseId).ToList();

        if (exerciseIds.Count == 0)
            return new Dictionary<Id, ExercisePreviousExecutionView>();

        var prescribedBy = callerRole == Role.ADMIN || callerId == workout.UserId ? null : callerId;

        var executions = await _previousExecutions.Get(
            workout.UserId, exerciseIds, DateOnly.FromDateTime(workout.WorkoutDate), workout.Id, prescribedBy, cancellationToken);

        return executions.ToDictionary(execution => execution.ExerciseId);
    }

    private static ExerciseSummaryView Summarize(WorkoutExercise entry, ExercisePreviousExecutionView? previous)
    {
        var (inTarget, evaluated) = entry.SetsInTarget();

        var candidates = entry.SetRecords
            .Select((record, position) => new TopSetCandidate(record.Result, record.Weight, position))
            .ToList();

        var volumes = entry.SetRecords
            .Select(record => SetMetrics.Volume(record.Result, record.Weight))
            .Where(volume => volume is not null)
            .Select(volume => volume!.Value)
            .ToList();

        return new ExerciseSummaryView(
            entry.Id,
            entry.ExerciseId,
            new SetsInTargetView(inTarget, evaluated),
            volumes.Count == 0 ? null : volumes.Sum(),
            ToView(TopSetComparer.Best(candidates)),
            previous is null ? null : new PreviousExecutionView(previous.WorkoutId, previous.WorkoutDate, ToView(TopSetOf(previous))!));
    }

    private static TopSetCandidate? TopSetOf(ExercisePreviousExecutionView previous)
        => TopSetComparer.Best(previous.Sets.Select((set, position) => new TopSetCandidate(set.ToSetResult(), set.ToWeight(), position)));

    private static LastPerformanceSetView? ToView(TopSetCandidate? set)
        => set is null ? null : LastPerformanceSetViewMapping.ToView(set.Result, set.Weight);
}
