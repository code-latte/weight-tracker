using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.AthleteInvitations;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Joins invitations with the users they refer to in a single lookup. Shared by every use case that returns
/// invitations; orchestration only.
/// </summary>
public class InvitationViewLoader
{
    private IUserRepository _users;

    public InvitationViewLoader(IUserRepository users)
    {
        _users = users;
    }

    /// <exception cref="UserNotFoundException">When a referenced user does not exist.</exception>
    public async Task<InvitationView> Load(TrainerInvitation invitation, CancellationToken cancellationToken)
        => (await Load([invitation], cancellationToken))[0];

    /// <returns>One view per invitation, in the given order.</returns>
    /// <exception cref="UserNotFoundException">When a referenced user does not exist.</exception>
    public async Task<IReadOnlyList<InvitationView>> Load(
        IEnumerable<TrainerInvitation> invitations, CancellationToken cancellationToken)
    {
        var all = invitations.ToList();

        var userIds = all.Select(invitation => invitation.TrainerId)
            .Concat(all.Where(invitation => invitation.UserId is not null).Select(invitation => invitation.UserId!))
            .Distinct()
            .ToList();

        var users = (await _users.GetAllByIds(userIds, cancellationToken)).ToDictionary(user => user.Id);

        return all
            .Select(invitation => new InvitationView(
                invitation,
                Find(users, invitation.TrainerId),
                invitation.UserId is null ? null : Find(users, invitation.UserId)))
            .ToList();
    }

    private static User Find(Dictionary<Id, User> users, Id userId)
        => users.TryGetValue(userId, out var user) ? user : throw new UserNotFoundException(userId);
}
