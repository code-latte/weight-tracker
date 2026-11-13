using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Domain.Workouts.Observations;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Repositories;

/// <summary>
/// Covers persistence of the WorkoutObservations aggregate: one row per observation, replaced as a whole, with the
/// constraints the migration declares.
/// </summary>
public class WorkoutObservationsRepositoryTests : IntegrationTest
{
    public WorkoutObservationsRepositoryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly DateTime CreatedAt = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);

    private static readonly Id PressBanca = Id.From(SeedData.PressBancaId);

    private static readonly Id Sentadilla = Id.From(SeedData.SentadillaId);

    private Task<bool> Write(Func<IWorkoutObservationsRepository, Task> action)
        => Factory.WithScope(async services =>
        {
            await action(services.GetRequiredService<IWorkoutObservationsRepository>());
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    private async Task<Workout> StoredWorkout()
    {
        var workout = Workout.Create(
            Id.From(SeedData.UserId), Id.From(SeedData.TrainerId), WorkoutName.From("Leg day"), null, new DateTime(2026, 11, 2));

        await Factory.WithScope(async services =>
        {
            await services.GetRequiredService<IWorkoutRepository>().Save(workout, CancellationToken.None);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

        return workout;
    }

    private Task<WorkoutObservations?> Read(Id workoutId)
        => Factory.WithScope(services =>
            services.GetRequiredService<IWorkoutObservationsRepository>().GetByWorkoutId(workoutId, CancellationToken.None));

    private Task<List<WorkoutObservationEntity>> Rows()
        => Factory.WithScope(services =>
            services.GetRequiredService<WeightTrackerDbContext>().WorkoutObservations.AsNoTracking().ToListAsync());

    private static WorkoutObservation AnObservation(
        Id? exerciseId, ObservationCode code, HighlightPriority? priority = null, ObservationEvidence? evidence = null)
        => WorkoutObservation.Create(exerciseId, code, ObservationSeverity.INFO, priority, evidence ?? ObservationEvidence.Empty, CreatedAt);

    [Fact]
    public async Task Save_ShouldPersistEveryObservationWithItsEvidenceAndHighlights()
    {
        // Arrange
        var workout = await StoredWorkout();
        var record = AnObservation(
            PressBanca, ObservationCode.WEIGHT_PERSONAL_RECORD, HighlightPriority.MEDIUM,
            ObservationEvidence.Empty.With("previousMaxKg", 120m).With("currentMaxKg", 130m).With("changePercent", 8.33m));
        var trend = AnObservation(
            Sentadilla, ObservationCode.WEIGHT_DOWNTREND, HighlightPriority.HIGH,
            ObservationEvidence.Empty.With("previousWeightsKg", [110m, 105m, 100m]).With("currentWeightKg", 95m));
        var incomplete = AnObservation(null, ObservationCode.WORKOUT_INCOMPLETE);
        var observations = WorkoutObservations.Create(workout.Id, [record, trend, incomplete], [trend.Id, record.Id]);

        await Write(repository => repository.Save(observations, CancellationToken.None));

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored.Should().NotBeNull();
        stored!.IsNew.Should().BeFalse();
        stored.Observations.Should().HaveCount(3);
        stored.Highlights.Select(observation => observation.Id).Should().Equal(trend.Id, record.Id);
        stored.Observations.Single(observation => observation.Id == record.Id).Evidence.Should().Be(record.Evidence);
        stored.Observations.Single(observation => observation.Id == trend.Id).Evidence.Numbers("previousWeightsKg").Should().Equal(110m, 105m, 100m);
        stored.Observations.Single(observation => observation.Id == incomplete.Id).ExerciseId.Should().BeNull();
    }

    [Fact]
    public async Task GetByWorkoutId_ShouldReturnNullWhenNothingWasGenerated()
    {
        // Arrange
        var workout = await StoredWorkout();

        // Act
        var stored = await Read(workout.Id);

        // Assert
        stored.Should().BeNull();
    }

    [Fact]
    public async Task Save_ShouldReplaceTheStoredObservationsAsAWhole()
    {
        // Arrange
        var workout = await StoredWorkout();
        var first = WorkoutObservations.Create(
            workout.Id,
            [AnObservation(PressBanca, ObservationCode.WEIGHT_INCREASE, HighlightPriority.MEDIUM), AnObservation(null, ObservationCode.WORKOUT_INCOMPLETE)],
            []);
        var firstWithHighlight = first.Observations.First();
        await Write(repository => repository.Save(first, CancellationToken.None));

        var loaded = (await Read(workout.Id))!;
        var replacement = AnObservation(PressBanca, ObservationCode.WEIGHT_INCREASE, HighlightPriority.MEDIUM);
        var other = AnObservation(null, ObservationCode.WORKOUT_INCOMPLETE);
        loaded.Replace([replacement, other], [replacement.Id]);

        // Act
        await Write(repository => repository.Save(loaded, CancellationToken.None));

        // Assert
        var rows = await Rows();
        rows.Select(row => row.Id).Should().BeEquivalentTo([replacement.Id.Value, other.Id.Value]);
        rows.Should().NotContain(row => row.Id == firstWithHighlight.Id.Value);
        rows.Single(row => row.Id == replacement.Id.Value).HighlightRank.Should().Be(1);
    }

    [Fact]
    public async Task Save_ShouldLeaveNoRowsWhenTheReplacementIsEmpty()
    {
        // Arrange
        var workout = await StoredWorkout();
        await Write(repository => repository.Save(
            WorkoutObservations.Create(workout.Id, [AnObservation(null, ObservationCode.WORKOUT_INCOMPLETE)], []),
            CancellationToken.None));

        var loaded = (await Read(workout.Id))!;
        loaded.Replace([], []);

        // Act
        await Write(repository => repository.Save(loaded, CancellationToken.None));

        // Assert
        (await Rows()).Should().BeEmpty();
        (await Read(workout.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Save_ShouldKeepTheObservationsOfOtherWorkouts()
    {
        // Arrange
        var first = await StoredWorkout();
        var second = await StoredWorkout();
        await Write(repository => repository.Save(
            WorkoutObservations.Create(first.Id, [AnObservation(null, ObservationCode.WORKOUT_INCOMPLETE)], []), CancellationToken.None));
        await Write(repository => repository.Save(
            WorkoutObservations.Create(second.Id, [AnObservation(null, ObservationCode.WORKOUT_INCOMPLETE)], []), CancellationToken.None));

        var loaded = (await Read(first.Id))!;
        loaded.Replace([AnObservation(PressBanca, ObservationCode.WEIGHT_INCREASE)], []);

        // Act
        await Write(repository => repository.Save(loaded, CancellationToken.None));

        // Assert
        (await Read(second.Id))!.Observations.Should().ContainSingle().Which.Code.Should().Be(ObservationCode.WORKOUT_INCOMPLETE);
        (await Read(first.Id))!.Observations.Should().ContainSingle().Which.Code.Should().Be(ObservationCode.WEIGHT_INCREASE);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheObservationsOfADeletedWorkout()
    {
        // Arrange
        var workout = await StoredWorkout();
        await Write(repository => repository.Save(
            WorkoutObservations.Create(workout.Id, [AnObservation(null, ObservationCode.WORKOUT_INCOMPLETE)], []), CancellationToken.None));

        // Act
        await Factory.WithScope(async services =>
        {
            var context = services.GetRequiredService<WeightTrackerDbContext>();
            context.Workouts.Remove(await context.Workouts.SingleAsync(stored => stored.Id == workout.Id.Value));
            await context.SaveChangesAsync();

            return true;
        });

        // Assert
        (await Rows()).Should().BeEmpty();
    }

    [Fact]
    public async Task Constraints_ShouldRejectTwoRowsWithTheSameCodeForTheSameExercise()
    {
        // Arrange
        var workout = await StoredWorkout();

        // Act
        var act = () => Factory.WithScope(async services =>
        {
            var context = services.GetRequiredService<WeightTrackerDbContext>();
            context.WorkoutObservations.AddRange(Row(workout.Id, PressBanca), Row(workout.Id, PressBanca));
            await context.SaveChangesAsync();

            return true;
        });

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>().Where(exception => exception.InnerException is PostgresException);
    }

    [Fact]
    public async Task Constraints_ShouldRejectTwoWorkoutLevelRowsWithTheSameCode()
    {
        // Arrange
        var workout = await StoredWorkout();

        // Act
        var act = () => Factory.WithScope(async services =>
        {
            var context = services.GetRequiredService<WeightTrackerDbContext>();
            context.WorkoutObservations.AddRange(Row(workout.Id, null), Row(workout.Id, null));
            await context.SaveChangesAsync();

            return true;
        });

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task Constraints_ShouldRejectAHighlightRankOutsideOneToThree(int rank)
    {
        // Arrange
        var workout = await StoredWorkout();

        // Act
        var act = () => Factory.WithScope(async services =>
        {
            var context = services.GetRequiredService<WeightTrackerDbContext>();
            var row = Row(workout.Id, PressBanca);
            row.HighlightRank = rank;
            context.WorkoutObservations.Add(row);
            await context.SaveChangesAsync();

            return true;
        });

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Constraints_ShouldRejectTheSameHighlightRankTwiceInOneWorkout()
    {
        // Arrange
        var workout = await StoredWorkout();

        // Act
        var act = () => Factory.WithScope(async services =>
        {
            var context = services.GetRequiredService<WeightTrackerDbContext>();
            var first = Row(workout.Id, PressBanca);
            var second = Row(workout.Id, Sentadilla);
            first.HighlightRank = 1;
            second.HighlightRank = 1;
            context.WorkoutObservations.AddRange(first, second);
            await context.SaveChangesAsync();

            return true;
        });

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private static WorkoutObservationEntity Row(Id workoutId, Id? exerciseId)
        => new()
        {
            Id = Guid.NewGuid(),
            WorkoutId = workoutId.Value,
            ExerciseId = exerciseId?.Value,
            Code = "WEIGHT_INCREASE",
            Severity = 1,
            Data = "{}",
            CreatedAt = CreatedAt,
        };
}
