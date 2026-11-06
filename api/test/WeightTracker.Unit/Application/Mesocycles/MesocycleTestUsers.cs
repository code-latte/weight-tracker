using Moq;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;

namespace WeightTracker.Unit.Application.Mesocycles;

/// <summary>
/// Users and mesocycles shared by the mesocycle use case tests, plus a user repository that answers for them.
/// </summary>
internal static class MesocycleTestUsers
{
    public static readonly DateTime Now = new(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);

    public static User AStoredUser(Role role, bool isActive = true) => User.Rehydrate(
        Id.New(), Email.From($"{Guid.NewGuid():N}@weighttracker.test"), PersonName.From("Name"),
        PersonName.From("Surname"), new DateOnly(1988, 5, 12), role, PasswordHash.From("stored-hash"),
        Now.AddDays(-30), isActive, isActive ? null : Now.AddDays(-1));

    public static Mesocycle AStoredMesocycle(Id trainerId, Id athleteId, int durationInWeeks = 4) => Mesocycle.Rehydrate(
        Id.New(), trainerId, athleteId, MesocycleName.From("Block"), MesocycleObjective.From("Objective"),
        new DateTime(2026, 11, 9), DurationInWeeks.From(durationInWeeks), []);

    /// <summary>
    /// A loose user repository that resolves the given users by id, alone or in bulk.
    /// </summary>
    public static Mock<IUserRepository> UsersHolding(params User[] users)
    {
        var repository = new Mock<IUserRepository>();

        foreach (var user in users)
            repository.Setup(stored => stored.GetById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        repository
            .Setup(stored => stored.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Id> ids, CancellationToken _) => users.Where(user => ids.Contains(user.Id)).ToList());

        return repository;
    }
}
