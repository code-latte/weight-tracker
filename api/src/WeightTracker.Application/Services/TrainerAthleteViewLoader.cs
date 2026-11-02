using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.TrainerAthletes;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Joins trainer–athlete relationships with their trainer and athlete in a single lookup. Shared by every use case
/// that returns relationships; orchestration only.
/// </summary>
public class TrainerAthleteViewLoader
{
    private IUserRepository _users;

    public TrainerAthleteViewLoader(IUserRepository users)
    {
        _users = users;
    }

    /// <exception cref="UserNotFoundException">When a referenced user does not exist.</exception>
    public async Task<TrainerAthleteView> Load(TrainerAthlete relationship, CancellationToken cancellationToken)
        => (await Load([relationship], cancellationToken))[0];

    /// <returns>One view per relationship, in the given order.</returns>
    /// <exception cref="UserNotFoundException">When a referenced user does not exist.</exception>
    public async Task<IReadOnlyList<TrainerAthleteView>> Load(
        IEnumerable<TrainerAthlete> relationships, CancellationToken cancellationToken)
    {
        var all = relationships.ToList();

        var userIds = all.SelectMany(relationship => new[] { relationship.TrainerId, relationship.UserId })
            .Distinct()
            .ToList();

        var users = (await _users.GetAllByIds(userIds, cancellationToken)).ToDictionary(user => user.Id);

        return all
            .Select(relationship => new TrainerAthleteView(
                relationship, Find(users, relationship.TrainerId), Find(users, relationship.UserId)))
            .ToList();
    }

    private static User Find(Dictionary<Id, User> users, Id userId)
        => users.TryGetValue(userId, out var user) ? user : throw new UserNotFoundException(userId);
}
