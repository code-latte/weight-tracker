using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Reads the observations and highlights generated when a workout was completed.
/// </summary>
/// <remarks>
/// The visibility rule is the one of <see cref="GetWorkoutSummaryUseCase"/>, kept in <see cref="WorkoutVisibility"/>, and
/// it is applied before anything else is read. A visible workout with no observations (not completed, without trainer, or
/// completed before observations existed) gives empty lists, not an error.
/// </remarks>
public class GetWorkoutObservationsUseCase
{
    private readonly IWorkoutRepository _workouts;

    private readonly WorkoutVisibility _visibility;

    private readonly IWorkoutObservationsRepository _observations;

    public GetWorkoutObservationsUseCase(
        IWorkoutRepository workouts,
        WorkoutVisibility visibility,
        IWorkoutObservationsRepository observations)
    {
        _workouts = workouts;
        _visibility = visibility;
        _observations = observations;
    }

    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not see the workout.</exception>
    public async Task<WorkoutObservationsView> Execute(Id callerId, Role callerRole, Id workoutId, CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        if (_visibility.CanSee(workout, callerId, callerRole) == false)
        {
            throw new UnauthorizedOperationException($"GET /workouts/{workoutId}/observations");
        }

        var stored = await _observations.GetByWorkoutId(workoutId, cancellationToken);

        if (stored is null)
            return new WorkoutObservationsView([], []);

        var positions = workout.Exercises
            .Select((entry, index) => (entry.ExerciseId, index))
            .ToDictionary(entry => entry.ExerciseId, entry => entry.index);

        // An observation of an exercise since removed from the workout goes last
        var observations = stored.Observations
            .OrderBy(observation => observation.ExerciseId is null ? -1 : positions.GetValueOrDefault(observation.ExerciseId, int.MaxValue))
            .ThenBy(observation => observation.Code)
            .Select(observation => new WorkoutObservationView(
                observation.Id, observation.Code, observation.Severity, observation.ExerciseId, observation.Priority, observation.Evidence))
            .ToList();

        var highlights = stored.Highlights
            .Select(observation => new WorkoutHighlightView(observation.Id, observation.HighlightRank!.Value, observation.Priority!.Value))
            .ToList();

        return new WorkoutObservationsView(observations, highlights);
    }
}
