using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Mesocycles;

/// <summary>
/// Lists the mesocycles the caller is entitled to see, latest start date first.
/// </summary>
public class GetAllMesocyclesUseCase
{
    private readonly MesocycleVisibility _visibility;

    private readonly MesocycleViewLoader _views;

    public GetAllMesocyclesUseCase(MesocycleVisibility visibility, MesocycleViewLoader views)
    {
        _visibility = visibility;
        _views = views;
    }

    /// <exception cref="UnauthorizedOperationException">When the caller's role may not list mesocycles.</exception>
    public async Task<IReadOnlyList<MesocycleView>> Execute(Id callerId, Role callerRole, CancellationToken cancellationToken)
    {
        var mesocycles = await _visibility.VisibleTo(callerId, callerRole, cancellationToken);

        return await _views.Load(mesocycles, cancellationToken);
    }
}
