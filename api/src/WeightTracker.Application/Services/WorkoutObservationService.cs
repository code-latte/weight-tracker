using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts.Observations;

namespace WeightTracker.Application.Services;

/// <summary>
/// Generates and stores the observations and highlights of a completed workout. Only workouts with a prescribing trainer
/// get observations; for the others nothing is generated and any stored ones are cleared.
/// </summary>
/// <remarks>
/// It runs after the completion was committed, as the reaction to the completion event. It is the entry point of its own
/// operation, so it commits its own unit of work once, when everything it stages is ready.
/// </remarks>
public class WorkoutObservationService
{
    private readonly IWorkoutRepository _workouts;

    private readonly IExerciseExecutionHistoryQuery _history;

    private readonly IWorkoutObservationsRepository _observations;

    private readonly WorkoutObservationGenerator _generator;

    private readonly ObservationThresholds _thresholds;

    private readonly IUnitOfWork _unitOfWork;

    private readonly IClock _clock;

    public WorkoutObservationService(
        IWorkoutRepository workouts,
        IExerciseExecutionHistoryQuery history,
        IWorkoutObservationsRepository observations,
        WorkoutObservationGenerator generator,
        ObservationThresholds thresholds,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _workouts = workouts;
        _history = history;
        _observations = observations;
        _generator = generator;
        _thresholds = thresholds;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Replaces the observations of the workout with the ones its current content gives. A workout that never had
    /// observations and gets none stores nothing.
    /// </summary>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    public async Task Generate(Id workoutId, CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        var generated = new GeneratedObservations([], []);

        if (workout.TrainerId is { } trainerId && workout.Exercises.Count > 0)
        {
            var history = await _history.Get(
                workout.UserId,
                trainerId,
                workout.Id,
                DateOnly.FromDateTime(workout.WorkoutDate),
                workout.Exercises.Select(entry => entry.ExerciseId).ToList(),
                _thresholds.HistoryDepth,
                cancellationToken);

            generated = _generator.Generate(workout, history.WeekType, history.Exercises, _clock.UtcNow);
        }

        var stored = await _observations.GetByWorkoutId(workout.Id, cancellationToken);

        if (stored is null && generated.Observations.Count == 0)
            return;

        if (stored is null)
        {
            stored = WorkoutObservations.Create(workout.Id, generated.Observations, generated.Highlights);
        }
        else
        {
            stored.Replace(generated.Observations, generated.Highlights);
        }

        await _observations.Save(stored, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
