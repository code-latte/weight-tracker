using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// Covers persistence of the Mesocycle aggregate across its two tables. No endpoint reaches it yet.
/// </summary>
public class MesocycleRepositoryTests : IntegrationTest
{
    public MesocycleRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly MesocycleObjective WeekObjective = MesocycleObjective.From("Week objective");

    private static Mesocycle AMesocycle(Guid athleteId)
        => Mesocycle.Create(
            Id.From(SeedData.TrainerId), Id.From(athleteId), MesocycleName.From("Strength block"),
            MesocycleObjective.From("Peak strength"), new DateTime(2026, 11, 2), DurationInWeeks.From(4));

    private Task<Mesocycle?> Read(Id mesocycleId)
        => Factory.WithScope(services =>
            services.GetRequiredService<IMesocycleRepository>().GetById(mesocycleId, CancellationToken.None));

    private Task<bool> Write(Func<IMesocycleRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<IMesocycleRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    private async Task Update(Id mesocycleId, Action<Mesocycle> change)
        => await Write(async repository =>
        {
            var stored = await repository.GetById(mesocycleId, CancellationToken.None);
            change(stored!);
            await repository.Save(stored!, CancellationToken.None);
        });

    [Fact]
    public async Task GetById_ShouldRestoreTheSeededMesocycleWithItsSparseWeeks()
    {
        // Arrange
        var mesocycleId = Id.From(SeedData.MesocycleId);

        // Act
        var mesocycle = await Read(mesocycleId);

        // Assert
        mesocycle.Should().NotBeNull();
        mesocycle!.IsNew.Should().BeFalse();
        mesocycle.TrainerId.Should().Be(Id.From(SeedData.TrainerId));
        mesocycle.AthleteId.Should().Be(Id.From(SeedData.UserId));
        mesocycle.DurationInWeeks.Value.Should().Be(SeedData.MesocycleDurationInWeeks);
        mesocycle.Weeks.Select(week => (week.WeekNumber.Value, week.TrainingWeekType))
            .Should().Equal((1, TrainingWeekType.ADAPTATION), (2, TrainingWeekType.ACCUMULATION), (4, TrainingWeekType.DELOAD));
    }

    [Fact]
    public async Task GetById_ShouldReturnNullForAnUnknownId()
    {
        // Arrange
        var unknown = Id.New();

        // Act
        var mesocycle = await Read(unknown);

        // Assert
        mesocycle.Should().BeNull();
    }

    [Fact]
    public async Task Save_ShouldPersistANewMesocycleWithItsWeeks()
    {
        // Arrange
        var original = AMesocycle(SeedData.UserId);
        original.DefineWeek(WeekNumber.From(3), TrainingWeekType.INTENSIFICATION, MesocycleObjective.From("Heavy triples"));
        original.DefineWeek(WeekNumber.From(1), TrainingWeekType.ADAPTATION, WeekObjective);

        await Write(repository => repository.Save(original, CancellationToken.None));

        // Act
        var restored = await Read(original.Id);

        // Assert
        restored.Should().NotBeNull();
        restored!.Name.Should().Be(original.Name);
        restored.Objective.Should().Be(original.Objective);
        restored.StartDate.Should().Be(original.StartDate);
        restored.DurationInWeeks.Should().Be(original.DurationInWeeks);
        restored.Weeks.Select(week => (week.Id, week.WeekNumber, week.TrainingWeekType, week.Objective))
            .Should().Equal(original.Weeks.Select(week => (week.Id, week.WeekNumber, week.TrainingWeekType, week.Objective)));
    }

    [Fact]
    public async Task Save_ShouldUpdateAStoredMesocycleInPlace()
    {
        // Arrange
        var mesocycleId = Id.From(SeedData.MesocycleId);
        var objective = MesocycleObjective.From("Back off");

        await Update(mesocycleId, mesocycle =>
        {
            mesocycle.Rename(MesocycleName.From("Renamed block"));
            mesocycle.Reschedule(new DateTime(2026, 10, 5));
            mesocycle.DefineWeek(WeekNumber.From(1), TrainingWeekType.RECOVERY, objective);
            mesocycle.RemoveWeek(WeekNumber.From(4));
            mesocycle.ChangeDuration(DurationInWeeks.From(3));
        });

        // Act
        var restored = await Read(mesocycleId);

        // Assert
        restored!.Id.Should().Be(mesocycleId);
        restored.Name.Value.Should().Be("Renamed block");
        restored.StartDate.Should().Be(new DateTime(2026, 10, 5));
        restored.DurationInWeeks.Value.Should().Be(3);
        restored.Weeks.Select(week => week.WeekNumber.Value).Should().Equal(1, 2);
        var first = restored.Weeks.First();
        first.Id.Should().Be(Id.From(SeedData.MesocycleAdaptationWeekId));
        first.TrainingWeekType.Should().Be(TrainingWeekType.RECOVERY);
        first.Objective.Should().Be(objective);
        (await Factory.WithScope(services => services.GetRequiredService<WeightTrackerDbContext>()
            .Mesocycles.CountAsync()))
            .Should().Be(1);
    }

    [Fact]
    public async Task Save_ShouldAllowRemovingAndDefiningTheSameWeekInOneSave()
    {
        // Arrange
        var mesocycleId = Id.From(SeedData.MesocycleId);

        await Update(mesocycleId, mesocycle =>
        {
            mesocycle.RemoveWeek(WeekNumber.From(2));
            mesocycle.DefineWeek(WeekNumber.From(2), TrainingWeekType.INTENSIFICATION, WeekObjective);
        });

        // Act
        var restored = await Read(mesocycleId);

        // Assert
        var second = restored!.Weeks.Single(week => week.WeekNumber == WeekNumber.From(2));
        second.Id.Should().NotBe(Id.From(SeedData.MesocycleAccumulationWeekId));
        second.TrainingWeekType.Should().Be(TrainingWeekType.INTENSIFICATION);
    }

    [Fact]
    public async Task Save_ShouldRejectAnAthleteThatDoesNotExist()
    {
        // Arrange
        var mesocycle = AMesocycle(Id.New().Value);

        // Act
        var act = () => Write(repository => repository.Save(mesocycle, CancellationToken.None));

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
        (await Read(mesocycle.Id)).Should().BeNull();
    }
}
