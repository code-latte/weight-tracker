using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Workouts;

/// <summary>
/// Lists the workouts the caller is entitled to see.
/// </summary>
/// <remarks>
/// The caller is passed explicitly instead of through an ambient accessor, so the
/// Application layer stays free of request-scoped infrastructure.
/// </remarks>
public class GetAllWorkoutsUseCase
{
    private WorkoutVisibility _visibility;

    private WorkoutListLoader _lists;

    public GetAllWorkoutsUseCase(
        WorkoutVisibility visibility,
        WorkoutListLoader lists
    )
    {
        _visibility = visibility;
        _lists = lists;
    }

    /// <param name="standaloneOnly">Keeps only the workouts that are not placed in a mesocycle.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller's role may not list workouts.</exception>
    /// <exception cref="InvalidOperationException">When an owner or trainer of a listed workout does not exist.</exception>
    public async Task<WorkoutListDTO> Execute(Id callerId, Role callerRole, bool standaloneOnly, CancellationToken cancellationToken)
    {
        var visible = await _visibility.VisibleTo(callerId, callerRole, cancellationToken);

        // Narrows what the caller may already see, so the filter never widens visibility
        var workouts = standaloneOnly
            ? visible.Where(workout => workout.MesocycleId is null).ToList()
            : visible;

        return await _lists.Load(workouts, cancellationToken);
    }
}
