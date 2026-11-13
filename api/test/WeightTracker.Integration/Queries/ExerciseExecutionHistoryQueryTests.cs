using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Workouts;
using WeightTracker.Domain.Workouts.Observations;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Queries;

/// <summary>
/// Covers the read projection that feeds the observation rules: earlier sessions with the same trainer, and the records
/// over every comparable session. Workouts are arranged through the workout repository because this tier tests the query
/// itself, one workout at a time.
/// </summary>
public class ExerciseExecutionHistoryQueryTests : IntegrationTest
{
    public ExerciseExecutionHistoryQueryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly Id User = Id.From(SeedData.UserId);

    private static readonly Id Trainer = Id.From(SeedData.TrainerId);

    private static readonly Id Admin = Id.From(SeedData.AdminId);

    private static readonly Id PressBanca = Id.From(SeedData.PressBancaId);

    private static readonly Id Sentadilla = Id.From(SeedData.SentadillaId);

    private static readonly Id Plancha = Id.From(SeedData.PlanchaId);

    private static readonly DateTime RecordedAt = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly AnchorDate = new(2026, 11, 10);

    private static SetResult Reps(int value) => new SetResult.Reps(RepCount.From(value));

    private static Workout AStartedWorkout(
        Id owner, Id? trainer, DateTime day, Id exerciseId, SetPrescription prescription, bool perSide = false, int? weekNumber = null)
    {
        var workout = Workout.Create(owner, trainer, WorkoutName.From("Leg day"), null, day);
        workout.AddExercise(exerciseId, Effort.MID, [prescription], perSide);

        // The placement is locked once the workout starts
        if (weekNumber is not null)
            workout.LinkToMesocycle(Id.From(SeedData.MesocycleId), WeekNumber.From(weekNumber.Value));

        workout.ChangeStatus(WorkoutStatus.PUBLISHED, RecordedAt);
        workout.ChangeStatus(WorkoutStatus.STARTED, RecordedAt);

        return workout;
    }

    private Task Save(Workout workout)
        => Factory.WithScope(async services =>
        {
            await services.GetRequiredService<IWorkoutRepository>().Save(workout, CancellationToken.None);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    /// <summary>A completed workout prescribed by the trainer with 3 sets of 10, holding one recorded set per given (reps, weight) pair.</summary>
    private async Task<Workout> AFinished(
        DateTime day, Id exerciseId, (int Reps, decimal Weight)[] sets, Id? owner = null, Id? trainer = null,
        bool perSide = false, int? weekNumber = null, bool noTrainer = false)
    {
        var workout = AStartedWorkout(
            owner ?? User, noTrainer ? null : trainer ?? Trainer, day, exerciseId, new SetPrescription(3, new SetTarget.Reps(10)), perSide, weekNumber);

        foreach (var (reps, weight) in sets)
            workout.AddSetRecord(exerciseId, Reps(reps), Weight.From(weight), RecordedAt);

        workout.ChangeStatus(WorkoutStatus.COMPLETED, RecordedAt);
        await Save(workout);

        return workout;
    }

    private Task<ExecutionHistoryView> History(Id[] exerciseIds, Id? anchor = null, int depth = 6, Id? owner = null, Id? trainer = null)
        => Factory.WithScope(services =>
            services.GetRequiredService<IExerciseExecutionHistoryQuery>()
                .Get(owner ?? User, trainer ?? Trainer, anchor ?? Id.New(), AnchorDate, exerciseIds, depth, CancellationToken.None));

    [Fact]
    public async Task Get_ShouldReturnTheEarlierSessionsNewestFirstWithTargetsAndSets()
    {
        // Arrange
        await AFinished(new DateTime(2026, 11, 3), PressBanca, [(10, 50m), (10, 50m), (8, 50m)]);
        var latest = await AFinished(new DateTime(2026, 11, 6), PressBanca, [(10, 60m), (9, 60m)], perSide: true);
        await AFinished(new DateTime(2026, 11, 1), PressBanca, [(5, 40m)]);

        // Act
        var history = await History([PressBanca]);

        // Assert
        var executions = history.Exercises[PressBanca].Executions;
        executions.Select(execution => execution.WorkoutDate).Should().Equal(
            new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 1));

        executions[0].WorkoutId.Should().Be(latest.Id);
        executions[0].PerSide.Should().BeTrue();
        executions[0].WeekType.Should().BeNull();
        executions[0].Targets.Should().Equal(Enumerable.Repeat<SetTarget>(new SetTarget.Reps(10), 3));
        executions[0].Sets.Should().Equal(
            new ExecutionSet(Reps(10), Weight.From(60m)),
            new ExecutionSet(Reps(9), Weight.From(60m)));
        executions[1].PerSide.Should().BeFalse();
    }

    [Fact]
    public async Task Get_ShouldExpandARepsRangeAndKeepATimedSet()
    {
        // Arrange
        var squat = AStartedWorkout(User, Trainer, new DateTime(2026, 11, 5), Sentadilla, new SetPrescription(2, new SetTarget.RepsRange(8, 12)));
        squat.AddSetRecord(Sentadilla, Reps(10), null, RecordedAt);
        squat.ChangeStatus(WorkoutStatus.COMPLETED, RecordedAt);
        await Save(squat);

        var plank = AStartedWorkout(User, Trainer, new DateTime(2026, 11, 5), Plancha, new SetPrescription(1, new SetTarget.Duration(40)));
        plank.AddSetRecord(Plancha, new SetResult.Duration(DurationInSeconds.From(45)), Weight.From(10m), RecordedAt);
        plank.ChangeStatus(WorkoutStatus.COMPLETED, RecordedAt);
        await Save(plank);

        // Act
        var history = await History([Sentadilla, Plancha]);

        // Assert
        var squatExecution = history.Exercises[Sentadilla].Executions.Single();
        squatExecution.Targets.Should().Equal(new SetTarget.RepsRange(8, 12), new SetTarget.RepsRange(8, 12));
        squatExecution.Sets.Single().Weight.Should().BeNull();

        var plankExecution = history.Exercises[Plancha].Executions.Single();
        plankExecution.Kind.Should().Be(ResultKind.DURATION);
        plankExecution.Sets.Single().Result.Should().Be(new SetResult.Duration(DurationInSeconds.From(45)));
    }

    [Fact]
    public async Task Get_ShouldLeaveOutAnExerciseThatWasNeverDone()
    {
        // Arrange
        await AFinished(new DateTime(2026, 11, 6), PressBanca, [(10, 60m)]);

        // Act
        var history = await History([PressBanca, Sentadilla]);

        // Assert
        history.Exercises.Keys.Should().Equal(PressBanca);
    }

    [Fact]
    public async Task Get_ShouldExcludeSessionsOfAnotherTrainerSelfCreatedOnesAndAnotherAthlete()
    {
        // Arrange
        var mine = await AFinished(new DateTime(2026, 11, 6), PressBanca, [(10, 60m)]);
        await AFinished(new DateTime(2026, 11, 5), PressBanca, [(10, 70m)], trainer: Admin);
        await AFinished(new DateTime(2026, 11, 4), PressBanca, [(10, 80m)], noTrainer: true);
        await AFinished(new DateTime(2026, 11, 3), PressBanca, [(10, 90m)], owner: Trainer, trainer: Admin);

        // Act
        var history = await History([PressBanca]);

        // Assert
        history.Exercises[PressBanca].Executions.Select(execution => execution.WorkoutId).Should().Equal(mine.Id);
        history.Exercises[PressBanca].RecordFor(ResultKind.REPS, false)!.MaxWeightKg.Should().Be(60m);
    }

    [Fact]
    public async Task Get_ShouldExcludeUnfinishedLaterAndTheAnchorSessionsAndSessionsWithoutRecords()
    {
        // Arrange
        var earlier = await AFinished(new DateTime(2026, 11, 6), PressBanca, [(10, 60m)]);
        await AFinished(new DateTime(2026, 11, 12), PressBanca, [(10, 100m)]);
        var anchor = await AFinished(new DateTime(2026, 11, 10), PressBanca, [(10, 100m)]);

        var unfinished = AStartedWorkout(User, Trainer, new DateTime(2026, 11, 8), PressBanca, new SetPrescription(3, new SetTarget.Reps(10)));
        unfinished.AddSetRecord(PressBanca, Reps(10), Weight.From(100m), RecordedAt);
        await Save(unfinished);

        var empty = AStartedWorkout(User, Trainer, new DateTime(2026, 11, 7), PressBanca, new SetPrescription(3, new SetTarget.Reps(10)));
        empty.ChangeStatus(WorkoutStatus.COMPLETED, RecordedAt);
        await Save(empty);

        // Act
        var history = await History([PressBanca], anchor.Id);

        // Assert
        history.Exercises[PressBanca].Executions.Select(execution => execution.WorkoutId).Should().Equal(earlier.Id);
        history.Exercises[PressBanca].RecordFor(ResultKind.REPS, false)!.MaxWeightKg.Should().Be(60m);
    }

    [Fact]
    public async Task Get_ShouldLoadOnlyTheLatestSessionsUpToTheDepth()
    {
        // Arrange
        await AFinished(new DateTime(2026, 11, 1), PressBanca, [(10, 40m)]);
        await AFinished(new DateTime(2026, 11, 2), PressBanca, [(10, 50m)]);
        await AFinished(new DateTime(2026, 11, 3), PressBanca, [(10, 60m)]);

        // Act
        var history = await History([PressBanca], depth: 2);

        // Assert
        history.Exercises[PressBanca].Executions.Select(execution => execution.WorkoutDate).Should().Equal(
            new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 2));
    }

    [Fact]
    public async Task Get_ShouldComputeTheRecordsOverSessionsBeyondTheDepth()
    {
        // Arrange
        await AFinished(new DateTime(2026, 11, 1), PressBanca, [(12, 120m), (8, 100m)]);
        await AFinished(new DateTime(2026, 11, 2), PressBanca, [(10, 80m), (6, 100m)]);
        await AFinished(new DateTime(2026, 11, 3), PressBanca, [(10, 60m)]);

        // Act
        var history = await History([PressBanca], depth: 1);

        // Assert
        history.Exercises[PressBanca].Executions.Should().ContainSingle();
        var record = history.Exercises[PressBanca].RecordFor(ResultKind.REPS, false)!;
        record.MaxWeightKg.Should().Be(120m);
        record.BestReps.Should().BeEquivalentTo(
            [new RepsAtWeight(120m, 12), new RepsAtWeight(100m, 8), new RepsAtWeight(80m, 10), new RepsAtWeight(60m, 10)]);
    }

    [Fact]
    public async Task Get_ShouldKeepRecordsApartByPerSideSetting()
    {
        // Arrange
        await AFinished(new DateTime(2026, 11, 1), PressBanca, [(10, 100m)]);
        await AFinished(new DateTime(2026, 11, 2), PressBanca, [(10, 30m)], perSide: true);

        // Act
        var history = await History([PressBanca]);

        // Assert
        history.Exercises[PressBanca].RecordFor(ResultKind.REPS, false)!.MaxWeightKg.Should().Be(100m);
        history.Exercises[PressBanca].RecordFor(ResultKind.REPS, true)!.MaxWeightKg.Should().Be(30m);
    }

    [Fact]
    public async Task Get_ShouldReportTheWeekTypeOfEachSessionAndLeaveLightWeeksOutOfTheRecords()
    {
        // Arrange
        await AFinished(new DateTime(2026, 10, 7), PressBanca, [(10, 100m)], weekNumber: 1);
        await AFinished(new DateTime(2026, 10, 28), PressBanca, [(10, 150m)], weekNumber: 4);

        // Act
        var history = await History([PressBanca]);

        // Assert
        history.Exercises[PressBanca].Executions.Select(execution => execution.WeekType).Should().Equal(
            TrainingWeekType.DELOAD, TrainingWeekType.ADAPTATION);
        history.Exercises[PressBanca].RecordFor(ResultKind.REPS, false)!.MaxWeightKg.Should().Be(100m);
    }

    [Theory]
    [InlineData(4, TrainingWeekType.DELOAD)]
    [InlineData(2, TrainingWeekType.ACCUMULATION)]
    public async Task Get_ShouldReturnTheWeekTypeOfTheAnchorWorkout(int weekNumber, TrainingWeekType expected)
    {
        // Arrange
        var anchor = await AFinished(new DateTime(2026, 11, 10), PressBanca, [(10, 100m)], weekNumber: weekNumber);

        // Act
        var history = await History([PressBanca], anchor.Id);

        // Assert
        history.WeekType.Should().Be(expected);
    }

    [Fact]
    public async Task Get_ShouldReturnNoWeekTypeForAnAnchorOutsideAMesocycle()
    {
        // Arrange
        var anchor = await AFinished(new DateTime(2026, 11, 10), PressBanca, [(10, 100m)]);

        // Act
        var history = await History([PressBanca], anchor.Id);

        // Assert
        history.WeekType.Should().BeNull();
    }
}
