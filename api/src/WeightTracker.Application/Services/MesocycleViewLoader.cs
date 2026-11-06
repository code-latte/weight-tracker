using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Joins mesocycles with their trainer and athlete in a single lookup. Shared by every use case that returns
/// mesocycles; orchestration only.
/// </summary>
public class MesocycleViewLoader
{
    private IUserRepository _users;

    public MesocycleViewLoader(IUserRepository users)
    {
        _users = users;
    }

    /// <exception cref="InvalidOperationException">When a referenced user does not exist.</exception>
    public async Task<MesocycleView> Load(Mesocycle mesocycle, CancellationToken cancellationToken)
        => (await Load([mesocycle], cancellationToken))[0];

    /// <returns>One view per mesocycle, in the given order.</returns>
    /// <exception cref="InvalidOperationException">When a referenced user does not exist.</exception>
    public async Task<IReadOnlyList<MesocycleView>> Load(IEnumerable<Mesocycle> mesocycles, CancellationToken cancellationToken)
    {
        var all = mesocycles.ToList();

        if (all.Count == 0)
            return [];

        var userIds = all.SelectMany(mesocycle => new[] { mesocycle.TrainerId, mesocycle.AthleteId })
            .Distinct()
            .ToList();

        var users = (await _users.GetAllByIds(userIds, cancellationToken)).ToDictionary(user => user.Id);

        return all
            .Select(mesocycle => new MesocycleView(
                mesocycle, Find(users, mesocycle.TrainerId), Find(users, mesocycle.AthleteId)))
            .ToList();
    }

    // Foreign keys keep every referenced user in place, so a missing one is a data-integrity failure
    private static User Find(Dictionary<Id, User> users, Id userId)
        => users.TryGetValue(userId, out var user)
            ? user
            : throw new InvalidOperationException($"User '{userId}' referenced by a mesocycle does not exist");
}
