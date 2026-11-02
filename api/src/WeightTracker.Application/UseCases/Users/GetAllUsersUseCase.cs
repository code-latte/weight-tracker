using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// Lists the users the caller is entitled to see, each with their current trainer.
/// </summary>
/// <remarks>
/// The caller is passed explicitly instead of through an ambient accessor, so the
/// Application layer stays free of request-scoped infrastructure.
/// </remarks>
public class GetAllUsersUseCase
{
    private IUserRepository _users;

    private ITrainerAthleteRepository _trainerAthletes;

    public GetAllUsersUseCase(IUserRepository users, ITrainerAthleteRepository trainerAthletes)
    {
        _users = users;
        _trainerAthletes = trainerAthletes;
    }

    /// <summary>
    /// An administrator gets every user, whatever their role. Trainers list their athletes through
    /// <c>GET /trainer-athletes</c> instead.
    /// </summary>
    /// <returns>
    /// Every user in repository order, with the trainer of their active relationship when they are an athlete.
    /// </returns>
    /// <exception cref="UnauthorizedOperationException">When the caller is not an administrator.</exception>
    public async Task<IReadOnlyList<UserWithTrainerView>> Execute(Role callerRole, CancellationToken cancellationToken)
    {
        if (callerRole != Role.ADMIN)
            throw new UnauthorizedOperationException("GET /users");

        var users = (await _users.GetAll(cancellationToken)).ToList();

        var athleteIds = users.Where(user => user.Role == Role.USER).Select(user => user.Id).ToList();

        var trainerIdByAthleteId = (await _trainerAthletes.GetAllActiveByUserIds(athleteIds, cancellationToken))
            .ToDictionary(relationship => relationship.UserId, relationship => relationship.TrainerId);

        var trainerIds = trainerIdByAthleteId.Values.Distinct().ToList();

        var trainers = trainerIds.Count == 0
            ? new Dictionary<Id, User>()
            : (await _users.GetAllByIds(trainerIds, cancellationToken)).ToDictionary(trainer => trainer.Id);

        return users
            .Select(user => new UserWithTrainerView(
                user,
                user.Role == Role.USER
                    && trainerIdByAthleteId.TryGetValue(user.Id, out var trainerId)
                    && trainers.TryGetValue(trainerId, out var trainer)
                    ? trainer
                    : null))
            .ToList();
    }
}
