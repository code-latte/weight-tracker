using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Mesocycles;

/// <summary>
/// Lists the mesocycles planned for one athlete, latest start date first. A trainer gets the ones they prepare for
/// that athlete, an admin all of them. Regular users cannot use it, not even for themselves.
/// </summary>
public class GetUserMesocyclesUseCase
{
    private readonly IMesocycleRepository _mesocycles;

    private readonly IUserRepository _users;

    private readonly MesocycleViewLoader _views;

    public GetUserMesocyclesUseCase(
        IMesocycleRepository mesocycles,
        IUserRepository users,
        MesocycleViewLoader views
    )
    {
        _mesocycles = mesocycles;
        _users = users;
        _views = views;
    }

    /// <exception cref="UnauthorizedOperationException">When the caller is neither a trainer nor an admin.</exception>
    /// <exception cref="UserNotFoundException">When no user has the given id.</exception>
    public async Task<IReadOnlyList<MesocycleView>> Execute(
        Id callerId, Role callerRole, Id userId, CancellationToken cancellationToken)
    {
        if (callerRole is not (Role.TRAINER or Role.ADMIN))
            throw new UnauthorizedOperationException($"GET /users/{userId}/mesocycles");

        _ = await _users.GetById(userId, cancellationToken)
            ?? throw new UserNotFoundException(userId);

        var mesocycles = callerRole == Role.ADMIN
            ? await _mesocycles.GetAllByAthleteId(userId, cancellationToken)
            : await _mesocycles.GetAllByTrainerIdAndAthleteId(callerId, userId, cancellationToken);

        return await _views.Load(mesocycles, cancellationToken);
    }
}
