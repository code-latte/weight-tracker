using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Queries;

/// <summary>
/// Covers the read projection of the previous time a user did each exercise, before a given workout. Workouts are
/// arranged through the workout repository because this tier tests the query itself, one workout at a time.
/// </summary>
public class ExercisePreviousExecutionQueryTests : IntegrationTest
{
    public ExercisePreviousExecutionQueryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly Id User = Id.From(SeedData.UserId);

    private static readonly Id Trainer = Id.From(SeedData.TrainerId);

    private static readonly Id PressBanca = Id.From(SeedData.PressBancaId);

    private static readonly Id Sentadilla = Id.From(SeedData.SentadillaId);

    private static readonly DateTime RecordedAt = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly AnchorDate = new(2026, 11, 10);

    private static SetResult Reps(int value) => new SetResult.Reps(RepCount.From(value));

    /// <summary>A started workout on the given day, optionally prescribed by a trainer, holding every given exercise prescribed by reps.</summary>
    private static Workout AStartedWorkout(Id owner, Id? trainer, DateTime day, params Id[] exerciseIds)
    {
        var workout = Workout.Create(owner, trainer, WorkoutName.From("Leg day"), null, day);

        foreach (var exerciseId in exerciseIds)
            workout.AddExercise(exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))]);

        workout.ChangeStatus(WorkoutStatus.PUBLISHED, RecordedAt);
        workout.ChangeStatus(WorkoutStatus.STARTED, RecordedAt);

        return workout;
    }

    private static Workout Completed(Workout workout)
    {
        workout.ChangeStatus(WorkoutStatus.COMPLETED, RecordedAt);

        return workout;
    }

    private Task Save(Workout workout)
        => Factory.WithScope(async services =>
        {
            await services.GetRequiredService<IWorkoutRepository>().Save(workout, CancellationToken.None);
            await services.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken.None);

            return true;
        });

    /// <summary>A finished workout of the user holding one recorded set of the exercise, saved.</summary>
    private async Task<Workout> AFinishedWorkout(DateTime day, Id exerciseId, int reps, decimal weight, Id? trainer = null, Id? owner = null)
    {
        var workout = AStartedWorkout(owner ?? User, trainer, day, exerciseId);
        workout.AddSetRecord(exerciseId, Reps(reps), Weight.From(weight), RecordedAt);
        await Save(Completed(workout));

        return workout;
    }

    private Task<IReadOnlyList<ExercisePreviousExecutionView>> Previous(
        Id[] exerciseIds, DateOnly beforeDate, Id? beforeWorkoutId = null, Id? prescribedBy = null, Id? owner = null)
        => Factory.WithScope(services =>
            services.GetRequiredService<IExercisePreviousExecutionQuery>()
                .Get(owner ?? User, exerciseIds, beforeDate, beforeWorkoutId ?? Id.New(), prescribedBy, CancellationToken.None));

    [Fact]
    public async Task Get_ShouldReturnTheLatestFinishedWorkoutBeforeTheAnchor()
    {
        // Arrange
        await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);
        var latest = await AFinishedWorkout(new DateTime(2026, 11, 6), PressBanca, 8, 60m);

        // Act
        var previous = await Previous([PressBanca], AnchorDate);

        // Assert
        previous.Should().ContainSingle();
        previous[0].ExerciseId.Should().Be(PressBanca);
        previous[0].WorkoutId.Should().Be(latest.Id);
        previous[0].WorkoutDate.Should().Be(new DateOnly(2026, 11, 6));
        previous[0].Sets.Should().Equal(new LastPerformanceSetView(8, null, 60m));
    }

    [Fact]
    public async Task Get_ShouldExcludeWorkoutsOnOrAfterTheAnchorDate()
    {
        // Arrange
        var before = await AFinishedWorkout(new DateTime(2026, 11, 6), PressBanca, 8, 60m);
        await AFinishedWorkout(new DateTime(2026, 11, 12), PressBanca, 12, 90m);

        // Act
        var previous = await Previous([PressBanca], AnchorDate);

        // Assert
        previous.Should().ContainSingle();
        previous[0].WorkoutId.Should().Be(before.Id);
    }

    [Fact]
    public async Task Get_ShouldCountAWorkoutOfTheSameDayCreatedBeforeTheAnchor()
    {
        // Arrange
        var first = await AFinishedWorkout(new DateTime(2026, 11, 10), PressBanca, 5, 50m);
        await Task.Delay(5);
        var anchor = await AFinishedWorkout(new DateTime(2026, 11, 10), PressBanca, 9, 70m);

        // Act
        var previous = await Previous([PressBanca], AnchorDate, anchor.Id);

        // Assert
        previous.Should().ContainSingle();
        previous[0].WorkoutId.Should().Be(first.Id);
    }

    [Fact]
    public async Task Get_ShouldExcludeAWorkoutOfTheSameDayCreatedAfterTheAnchor()
    {
        // Arrange
        var anchor = await AFinishedWorkout(new DateTime(2026, 11, 10), PressBanca, 5, 50m);
        await Task.Delay(5);
        await AFinishedWorkout(new DateTime(2026, 11, 10), PressBanca, 9, 70m);

        // Act
        var previous = await Previous([PressBanca], AnchorDate, anchor.Id);

        // Assert
        previous.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldIgnoreUnfinishedWorkouts()
    {
        // Arrange
        var finished = await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);
        var inProgress = AStartedWorkout(User, null, new DateTime(2026, 11, 8), PressBanca);
        inProgress.AddSetRecord(PressBanca, Reps(12), Weight.From(80), RecordedAt);
        await Save(inProgress);

        // Act
        var previous = await Previous([PressBanca], AnchorDate);

        // Assert
        previous.Should().ContainSingle();
        previous[0].WorkoutId.Should().Be(finished.Id);
    }

    [Fact]
    public async Task Get_ShouldCountAReviewedWorkout()
    {
        // Arrange
        var workout = AStartedWorkout(User, Trainer, new DateTime(2026, 11, 6), PressBanca);
        workout.AddSetRecord(PressBanca, Reps(10), Weight.From(60), RecordedAt);
        workout.ChangeStatus(WorkoutStatus.COMPLETED, RecordedAt);
        workout.MarkReviewed(Trainer, RecordedAt.AddHours(1), workout.ResultsUpdatedAt);
        await Save(workout);

        // Act
        var previous = await Previous([PressBanca], AnchorDate);

        // Assert
        previous.Should().ContainSingle();
        previous[0].WorkoutId.Should().Be(workout.Id);
    }

    [Fact]
    public async Task Get_ShouldSkipAFinishedWorkoutWithoutRecordsForTheExercise()
    {
        // Arrange
        var earlier = await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);
        var withoutRecords = AStartedWorkout(User, null, new DateTime(2026, 11, 8), PressBanca, Sentadilla);
        withoutRecords.AddSetRecord(Sentadilla, Reps(10), Weight.From(100), RecordedAt);
        await Save(Completed(withoutRecords));

        // Act
        var previous = await Previous([PressBanca], AnchorDate);

        // Assert
        previous.Should().ContainSingle();
        previous[0].WorkoutId.Should().Be(earlier.Id);
    }

    [Fact]
    public async Task Get_ShouldExcludeTheWorkoutsOfOtherOwners()
    {
        // Arrange
        var mine = await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);
        await AFinishedWorkout(new DateTime(2026, 11, 8), PressBanca, 15, 90m, owner: Trainer);

        // Act
        var previous = await Previous([PressBanca], AnchorDate);

        // Assert
        previous.Should().ContainSingle();
        previous[0].WorkoutId.Should().Be(mine.Id);
    }

    [Fact]
    public async Task Get_ShouldReadOnlyTheWorkoutsPrescribedByTheGivenTrainer()
    {
        // Arrange
        var prescribed = await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m, trainer: Trainer);
        var own = await AFinishedWorkout(new DateTime(2026, 11, 8), PressBanca, 9, 70m);

        // Act
        var scoped = await Previous([PressBanca], AnchorDate, prescribedBy: Trainer);
        var unscoped = await Previous([PressBanca], AnchorDate);

        // Assert
        scoped.Should().ContainSingle();
        scoped[0].WorkoutId.Should().Be(prescribed.Id);
        unscoped.Should().ContainSingle();
        unscoped[0].WorkoutId.Should().Be(own.Id);
    }

    [Fact]
    public async Task Get_ShouldReturnSetsInPositionOrder()
    {
        // Arrange
        var workout = AStartedWorkout(User, null, new DateTime(2026, 11, 6), PressBanca);
        workout.AddSetRecord(PressBanca, Reps(10), Weight.From(60), RecordedAt);
        workout.AddSetRecord(PressBanca, Reps(8), Weight.From(62.5m), RecordedAt.AddMinutes(3));
        workout.AddSetRecord(PressBanca, Reps(6), null, RecordedAt.AddMinutes(6));
        await Save(Completed(workout));

        // Act
        var previous = await Previous([PressBanca], AnchorDate);

        // Assert
        previous[0].Sets.Should().Equal(
            new LastPerformanceSetView(10, null, 60m),
            new LastPerformanceSetView(8, null, 62.5m),
            new LastPerformanceSetView(6, null, null));
    }

    [Fact]
    public async Task Get_ShouldResolveEachExerciseToItsOwnWorkoutAndLeaveOutTheOnesNeverDone()
    {
        // Arrange
        var pressWorkout = await AFinishedWorkout(new DateTime(2026, 11, 8), PressBanca, 8, 60m);
        var squatWorkout = await AFinishedWorkout(new DateTime(2026, 11, 3), Sentadilla, 5, 100m);
        var neverDone = Id.From(SeedData.PlanchaId);

        // Act
        var previous = await Previous([PressBanca, Sentadilla, neverDone], AnchorDate);

        // Assert
        previous.Should().HaveCount(2);
        previous.Single(view => view.ExerciseId == PressBanca).WorkoutId.Should().Be(pressWorkout.Id);
        previous.Single(view => view.ExerciseId == Sentadilla).WorkoutId.Should().Be(squatWorkout.Id);
    }

    [Fact]
    public async Task Get_ShouldReturnNothingWhenNoExerciseIsRequested()
    {
        // Arrange
        await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);

        // Act
        var previous = await Previous([], AnchorDate);

        // Assert
        previous.Should().BeEmpty();
    }
}
