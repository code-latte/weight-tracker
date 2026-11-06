using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Mesocycles;

/// <summary>
/// Reads one mesocycle with its weeks, provided the caller is entitled to see it.
/// </summary>
/// <remarks>
/// The visibility rule is the one of <see cref="GetAllMesocyclesUseCase"/>: admins see every mesocycle, trainers
/// the ones they prepare, users the ones planned for them.
/// </remarks>
public class GetMesocycleByIdUseCase
{
    private readonly IMesocycleRepository _mesocycles;

    private readonly MesocycleVisibility _visibility;

    private readonly MesocycleViewLoader _views;

    public GetMesocycleByIdUseCase(
        IMesocycleRepository mesocycles,
        MesocycleVisibility visibility,
        MesocycleViewLoader views
    )
    {
        _mesocycles = mesocycles;
        _visibility = visibility;
        _views = views;
    }

    /// <exception cref="MesocycleNotFoundException">When no mesocycle has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not see the mesocycle.</exception>
    public async Task<MesocycleView> Execute(Id callerId, Role callerRole, Id mesocycleId, CancellationToken cancellationToken)
    {
        var mesocycle = await _mesocycles.GetById(mesocycleId, cancellationToken)
            ?? throw new MesocycleNotFoundException(mesocycleId);

        if (_visibility.CanSee(mesocycle, callerId, callerRole) == false)
            throw new UnauthorizedOperationException($"GET /mesocycles/{mesocycleId}");

        return await _views.Load(mesocycle, cancellationToken);
    }
}
