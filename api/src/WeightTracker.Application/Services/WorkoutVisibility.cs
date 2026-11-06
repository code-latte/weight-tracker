using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides which workouts a caller is entitled to see. Every read that depends on that scope goes through it,
/// so the rule cannot drift between the workout list and the reads that join other data to it.
/// </summary>
/// <remarks>
/// An admin sees every workout. A trainer sees the workouts they own or prescribe. An athlete sees the workouts
/// they own, except the prescribed drafts their trainer has not published yet.
/// </remarks>
public class WorkoutVisibility
{
    private readonly IWorkoutRepository _workouts;

    public WorkoutVisibility(IWorkoutRepository workouts)
    {
        _workouts = workouts;
    }

    /// <param name="callerId">Identifier of the caller.</param>
    /// <param name="callerRole">Role of the caller.</param>
    /// <exception cref="UnauthorizedOperationException">When the caller's role may not list workouts.</exception>
    public async Task<IReadOnlyList<Workout>> VisibleTo(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        var scoped = callerRole switch
        {
            Role.ADMIN => await _workouts.GetAll(cancellationToken),
            Role.TRAINER => PublishedToCaller(await _workouts.GetAllByUserOrTrainerId(callerId, cancellationToken), callerId),
            Role.USER => PublishedToCaller(await _workouts.GetAllByUserId(callerId, cancellationToken), callerId),
            _ => throw new UnauthorizedOperationException("GET /workouts"),
        };

        return scoped.ToList();
    }

    /// <summary>
    /// Whether the caller may see one workout. Anyone but an admin is refused a prescribed draft of which they are
    /// the athlete, since the trainer has not published it yet.
    /// </summary>
    public bool CanSee(Workout workout, Id callerId, Role callerRole)
    {
        var inScope = callerRole switch
        {
            Role.ADMIN => true,
            Role.TRAINER => workout.UserId == callerId || workout.TrainerId == callerId,
            Role.USER => workout.UserId == callerId,
            _ => false,
        };

        return inScope && (callerRole == Role.ADMIN || workout.IsHiddenFrom(callerId) == false);
    }

    /// <summary>
    /// Leaves out the prescribed drafts of which the caller is the athlete: their trainer has not published them yet.
    /// </summary>
    private static IEnumerable<Workout> PublishedToCaller(IEnumerable<Workout> workouts, Id callerId)
        => workouts.Where(workout => workout.IsHiddenFrom(callerId) == false);
}
