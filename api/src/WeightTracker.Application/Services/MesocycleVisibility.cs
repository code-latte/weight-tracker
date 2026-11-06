using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides which mesocycles a caller is entitled to see. The list and the single read both go through it,
/// so the rule cannot drift between them.
/// </summary>
/// <remarks>
/// An admin sees every mesocycle. A trainer sees the mesocycles they prepare and a user the ones planned for them.
/// Access depends only on being the trainer or the athlete, not on an active trainer–athlete relationship.
/// </remarks>
public class MesocycleVisibility
{
    private readonly IMesocycleRepository _mesocycles;

    public MesocycleVisibility(IMesocycleRepository mesocycles)
    {
        _mesocycles = mesocycles;
    }

    /// <exception cref="UnauthorizedOperationException">When the caller's role may not list mesocycles.</exception>
    public async Task<IReadOnlyList<Mesocycle>> VisibleTo(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        var scoped = callerRole switch
        {
            Role.ADMIN => await _mesocycles.GetAll(cancellationToken),
            Role.TRAINER => await _mesocycles.GetAllByTrainerId(callerId, cancellationToken),
            Role.USER => await _mesocycles.GetAllByAthleteId(callerId, cancellationToken),
            _ => throw new UnauthorizedOperationException("GET /mesocycles"),
        };

        return scoped.ToList();
    }

    public bool CanSee(Mesocycle mesocycle, Id callerId, Role callerRole)
        => callerRole switch
        {
            Role.ADMIN => true,
            Role.TRAINER => mesocycle.TrainerId == callerId,
            Role.USER => mesocycle.AthleteId == callerId,
            _ => false,
        };
}
