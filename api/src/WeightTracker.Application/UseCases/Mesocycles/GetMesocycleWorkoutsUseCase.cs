using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Mesocycles;

/// <summary>
/// Lists the workouts placed in one mesocycle, across all its weeks, provided the caller is entitled to see the
/// mesocycle.
/// </summary>
/// <remarks>
/// The mesocycle is checked first with the rule of <see cref="GetMesocycleByIdUseCase"/>, so a caller who may not
/// see it is refused instead of getting an empty list. The workouts are then narrowed with the rule of
/// <see cref="GetAllWorkoutsUseCase"/>, so this read never shows a workout the caller could not list there.
/// </remarks>
public class GetMesocycleWorkoutsUseCase
{
    private readonly IMesocycleRepository _mesocycles;

    private readonly MesocycleVisibility _mesocycleVisibility;

    private readonly IWorkoutRepository _workouts;

    private readonly WorkoutVisibility _workoutVisibility;

    private readonly WorkoutListLoader _lists;

    public GetMesocycleWorkoutsUseCase(
        IMesocycleRepository mesocycles,
        MesocycleVisibility mesocycleVisibility,
        IWorkoutRepository workouts,
        WorkoutVisibility workoutVisibility,
        WorkoutListLoader lists
    )
    {
        _mesocycles = mesocycles;
        _mesocycleVisibility = mesocycleVisibility;
        _workouts = workouts;
        _workoutVisibility = workoutVisibility;
        _lists = lists;
    }

    /// <returns>The visible workouts of the mesocycle, by week number and then workout date.</returns>
    /// <exception cref="MesocycleNotFoundException">When no mesocycle has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not see the mesocycle.</exception>
    /// <exception cref="InvalidOperationException">When an owner or trainer of a listed workout does not exist.</exception>
    public async Task<WorkoutListDTO> Execute(Id callerId, Role callerRole, Id mesocycleId, CancellationToken cancellationToken)
    {
        var mesocycle = await _mesocycles.GetById(mesocycleId, cancellationToken)
            ?? throw new MesocycleNotFoundException(mesocycleId);

        if (_mesocycleVisibility.CanSee(mesocycle, callerId, callerRole) == false)
            throw new UnauthorizedOperationException($"GET /mesocycles/{mesocycleId}/workouts");

        var placed = await _workouts.GetAllByMesocycleId(mesocycleId, cancellationToken);

        var workouts = placed
            .Where(workout => _workoutVisibility.CanSee(workout, callerId, callerRole))
            .OrderBy(workout => workout.MesocycleWeekNumber!.Value)
            .ThenBy(workout => workout.WorkoutDate)
            .ToList();

        return await _lists.Load(workouts, cancellationToken);
    }
}
