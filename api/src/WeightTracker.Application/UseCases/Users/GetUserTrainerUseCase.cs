using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// Finds the current trainer of one user: the trainer of their active trainer-athlete relationship.
/// A user may ask only about themselves; an admin may ask about anyone.
/// </summary>
/// <remarks>
/// The caller is checked before the user is looked up, so a user probing other ids never learns which exist.
/// </remarks>
public class GetUserTrainerUseCase
{
    private readonly IUserRepository _users;

    private readonly ITrainerAthleteRepository _trainerAthletes;

    public GetUserTrainerUseCase(IUserRepository users, ITrainerAthleteRepository trainerAthletes)
    {
        _users = users;
        _trainerAthletes = trainerAthletes;
    }

    /// <returns>The trainer, or <c>null</c> when the user has no active trainer or cannot be an athlete.</returns>
    /// <exception cref="UnauthorizedOperationException">When the caller is neither an admin nor that user with the user role.</exception>
    /// <exception cref="UserNotFoundException">When no user has the given id.</exception>
    public async Task<User?> Execute(Id callerId, Role callerRole, Id userId, CancellationToken cancellationToken)
    {
        var isSelf = callerRole == Role.USER && callerId == userId;

        if (callerRole != Role.ADMIN && isSelf == false)
            throw new UnauthorizedOperationException($"GET /users/{userId}/trainer");

        var user = await _users.GetById(userId, cancellationToken)
            ?? throw new UserNotFoundException(userId);

        // Only users can be athletes, so trainers and admins never have a trainer
        if (user.Role != Role.USER)
            return null;

        var relationship = (await _trainerAthletes.GetAllActiveByUserIds([userId], cancellationToken)).FirstOrDefault();

        if (relationship is null)
            return null;

        return await _users.GetById(relationship.TrainerId, cancellationToken);
    }
}
