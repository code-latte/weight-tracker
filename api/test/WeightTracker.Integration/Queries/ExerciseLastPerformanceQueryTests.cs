using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Seed;
using WeightTracker.Integration.Bootstrap;
using Xunit;

namespace WeightTracker.Integration.Queries;

/// <summary>
/// Covers the read projection of what a user did the last time they finished a workout with an exercise. Workouts are
/// arranged through the workout repository because this tier tests the query itself, one workout at a time.
/// </summary>
public class ExerciseLastPerformanceQueryTests : IntegrationTest
{
    public ExerciseLastPerformanceQueryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly Id User = Id.From(SeedData.UserId);

    private static readonly Id Trainer = Id.From(SeedData.TrainerId);

    private static readonly Id PressBanca = Id.From(SeedData.PressBancaId);

    private static readonly Id Sentadilla = Id.From(SeedData.SentadillaId);

    private static readonly Id Plancha = Id.From(SeedData.PlanchaId);

    private static readonly DateTime RecordedAt = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

    private static SetResult Reps(int value) => new SetResult.Reps(RepCount.From(value));

    private static SetResult Seconds(int value) => new SetResult.Duration(DurationInSeconds.From(value));

    private static readonly Func<Workout, Workout> Started = workout =>
    {
        workout.ChangeStatus(WorkoutStatus.PUBLISHED, RecordedAt);
        workout.ChangeStatus(WorkoutStatus.STARTED, RecordedAt);

        return workout;
    };

    /// <summary>A started workout of the user on the given day holding every given exercise prescribed by reps.</summary>
    private static Workout AStartedWorkout(Id owner, DateTime day, params Id[] exerciseIds)
    {
        var workout = Workout.Create(owner, null, WorkoutName.From("Leg day"), null, day);

        foreach (var exerciseId in exerciseIds)
            workout.AddExercise(exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))]);

        return Started(workout);
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

    private Task<ExerciseLastPerformanceView?> LastPerformance(Id userId, Id exerciseId)
        => Factory.WithScope(services =>
            services.GetRequiredService<IExerciseLastPerformanceQuery>().Get(userId, exerciseId, CancellationToken.None));

    [Fact]
    public async Task Get_ShouldReturnNullWhenTheExerciseWasNeverDone()
    {
        // Arrange
        await Save(Completed(AStartedWorkout(User, new DateTime(2026, 11, 3), Sentadilla)));

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance.Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldReturnTheSetsOfTheLatestFinishedWorkoutInPositionOrder()
    {
        // Arrange
        var earlier = AStartedWorkout(User, new DateTime(2026, 11, 3), PressBanca);
        earlier.AddSetRecord(PressBanca, Reps(5), Weight.From(50), RecordedAt);
        var latest = AStartedWorkout(User, new DateTime(2026, 11, 10), PressBanca);
        latest.AddSetRecord(PressBanca, Reps(10), Weight.From(60), RecordedAt);
        latest.AddSetRecord(PressBanca, Reps(8), Weight.From(62.5m), RecordedAt.AddMinutes(3));
        latest.AddSetRecord(PressBanca, Reps(6), Weight.From(65), RecordedAt.AddMinutes(6));
        await Save(Completed(latest));
        await Save(Completed(earlier));

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance.Should().BeEquivalentTo(new ExerciseLastPerformanceView(
            new DateOnly(2026, 11, 10),
            Effort.MID,
            [
                new LastPerformanceSetView(10, null, 60m),
                new LastPerformanceSetView(8, null, 62.5m),
                new LastPerformanceSetView(6, null, 65m),
            ]), options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Get_ShouldReturnTheEffortPrescribedInTheLatestWorkout()
    {
        // Arrange
        var earlier = Workout.Create(User, null, WorkoutName.From("Leg day"), null, new DateTime(2026, 11, 3));
        earlier.AddExercise(PressBanca, Effort.LOW, [new SetPrescription(3, new SetTarget.Reps(10))]);
        Started(earlier);
        earlier.AddSetRecord(PressBanca, Reps(10), Weight.From(40), RecordedAt);
        await Save(Completed(earlier));
        var latest = Workout.Create(User, null, WorkoutName.From("Leg day"), null, new DateTime(2026, 11, 10));
        latest.AddExercise(PressBanca, Effort.HIGH, [new SetPrescription(3, new SetTarget.Reps(10))]);
        Started(latest);
        latest.AddSetRecord(PressBanca, Reps(10), Weight.From(60), RecordedAt);
        await Save(Completed(latest));

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance!.Effort.Should().Be(Effort.HIGH);
    }

    [Fact]
    public async Task Get_ShouldBreakATieOnTheDateWithTheNewestWorkout()
    {
        // Arrange
        var first = AStartedWorkout(User, new DateTime(2026, 11, 10), PressBanca);
        first.AddSetRecord(PressBanca, Reps(5), Weight.From(50), RecordedAt);
        await Save(Completed(first));
        await Task.Delay(5);
        var second = AStartedWorkout(User, new DateTime(2026, 11, 10), PressBanca);
        second.AddSetRecord(PressBanca, Reps(9), Weight.From(70), RecordedAt);
        await Save(Completed(second));

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance!.Sets.Should().Equal(new LastPerformanceSetView(9, null, 70m));
    }

    [Fact]
    public async Task Get_ShouldCountAReviewedWorkout()
    {
        // Arrange
        var workout = Workout.Create(User, Trainer, WorkoutName.From("Leg day"), null, new DateTime(2026, 11, 10));
        workout.AddExercise(PressBanca, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))]);
        Started(workout);
        workout.AddSetRecord(PressBanca, Reps(10), Weight.From(60), RecordedAt);
        workout.ChangeStatus(WorkoutStatus.COMPLETED, RecordedAt);
        workout.MarkReviewed(Trainer, RecordedAt.AddHours(1), workout.ResultsUpdatedAt);
        await Save(workout);

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance!.Sets.Should().Equal(new LastPerformanceSetView(10, null, 60m));
    }

    [Fact]
    public async Task Get_ShouldIgnoreUnfinishedWorkouts()
    {
        // Arrange
        var finished = AStartedWorkout(User, new DateTime(2026, 11, 3), PressBanca);
        finished.AddSetRecord(PressBanca, Reps(5), Weight.From(50), RecordedAt);
        await Save(Completed(finished));
        var inProgress = AStartedWorkout(User, new DateTime(2026, 11, 12), PressBanca);
        inProgress.AddSetRecord(PressBanca, Reps(12), Weight.From(80), RecordedAt);
        await Save(inProgress);

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance!.WorkoutDate.Should().Be(new DateOnly(2026, 11, 3));
        performance.Sets.Should().Equal(new LastPerformanceSetView(5, null, 50m));
    }

    [Fact]
    public async Task Get_ShouldReturnNullWhenTheExerciseWasOnlyDoneInAnUnfinishedWorkout()
    {
        // Arrange
        var inProgress = AStartedWorkout(User, new DateTime(2026, 11, 12), PressBanca);
        inProgress.AddSetRecord(PressBanca, Reps(12), Weight.From(80), RecordedAt);
        await Save(inProgress);

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance.Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldSkipAFinishedWorkoutWithoutRecordsForTheExercise()
    {
        // Arrange
        var earlier = AStartedWorkout(User, new DateTime(2026, 11, 3), PressBanca);
        earlier.AddSetRecord(PressBanca, Reps(5), Weight.From(50), RecordedAt);
        await Save(Completed(earlier));
        var latestWithoutRecords = AStartedWorkout(User, new DateTime(2026, 11, 10), PressBanca, Sentadilla);
        latestWithoutRecords.AddSetRecord(Sentadilla, Reps(10), Weight.From(100), RecordedAt);
        await Save(Completed(latestWithoutRecords));

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance!.WorkoutDate.Should().Be(new DateOnly(2026, 11, 3));
        performance.Sets.Should().Equal(new LastPerformanceSetView(5, null, 50m));
    }

    [Fact]
    public async Task Get_ShouldNotMixInSetsOfOtherExercisesOfTheSameWorkout()
    {
        // Arrange
        var workout = AStartedWorkout(User, new DateTime(2026, 11, 10), PressBanca, Sentadilla);
        workout.AddSetRecord(PressBanca, Reps(10), Weight.From(60), RecordedAt);
        workout.AddSetRecord(Sentadilla, Reps(12), Weight.From(100), RecordedAt.AddMinutes(10));
        await Save(Completed(workout));

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance!.Sets.Should().Equal(new LastPerformanceSetView(10, null, 60m));
    }

    [Fact]
    public async Task Get_ShouldExcludeTheWorkoutsOfOtherUsers()
    {
        // Arrange
        var mine = AStartedWorkout(User, new DateTime(2026, 11, 3), PressBanca);
        mine.AddSetRecord(PressBanca, Reps(5), Weight.From(50), RecordedAt);
        await Save(Completed(mine));
        var theirs = AStartedWorkout(Trainer, new DateTime(2026, 11, 10), PressBanca);
        theirs.AddSetRecord(PressBanca, Reps(15), Weight.From(90), RecordedAt);
        await Save(Completed(theirs));

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance!.WorkoutDate.Should().Be(new DateOnly(2026, 11, 3));
        performance.Sets.Should().Equal(new LastPerformanceSetView(5, null, 50m));
    }

    [Fact]
    public async Task Get_ShouldReturnAMissingWeightAsNullAndAZeroWeightAsZero()
    {
        // Arrange
        var workout = AStartedWorkout(User, new DateTime(2026, 11, 10), PressBanca);
        workout.AddSetRecord(PressBanca, Reps(10), null, RecordedAt);
        workout.AddSetRecord(PressBanca, Reps(12), Weight.From(0), RecordedAt.AddMinutes(3));
        await Save(Completed(workout));

        // Act
        var performance = await LastPerformance(User, PressBanca);

        // Assert
        performance!.Sets.Should().Equal(
            new LastPerformanceSetView(10, null, null),
            new LastPerformanceSetView(12, null, 0m));
    }

    [Fact]
    public async Task Get_ShouldReturnATimedSetWithItsSecondsAndNoReps()
    {
        // Arrange
        var workout = Workout.Create(User, null, WorkoutName.From("Core"), null, new DateTime(2026, 11, 10));
        workout.AddExercise(Plancha, Effort.LOW, [new SetPrescription(3, new SetTarget.Duration(45))]);
        Started(workout);
        workout.AddSetRecord(Plancha, Seconds(45), Weight.From(10), RecordedAt);
        await Save(Completed(workout));

        // Act
        var performance = await LastPerformance(User, Plancha);

        // Assert
        performance!.Sets.Should().Equal(new LastPerformanceSetView(null, 45, 10m));
    }
}
