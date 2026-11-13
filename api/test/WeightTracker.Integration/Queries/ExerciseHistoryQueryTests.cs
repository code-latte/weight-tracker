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
/// Covers the read projection of a user's latest finished sessions of an exercise. Workouts are arranged through the
/// workout repository because this tier tests the query itself, one workout at a time.
/// </summary>
public class ExerciseHistoryQueryTests : IntegrationTest
{
    public ExerciseHistoryQueryTests(ApiFactory factory) : base(factory)
    { }

    private static readonly Id User = Id.From(SeedData.UserId);

    private static readonly Id Trainer = Id.From(SeedData.TrainerId);

    private static readonly Id PressBanca = Id.From(SeedData.PressBancaId);

    private static readonly Id Sentadilla = Id.From(SeedData.SentadillaId);

    private static readonly DateTime RecordedAt = new(2026, 11, 7, 10, 0, 0, DateTimeKind.Utc);

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

    /// <summary>A finished workout holding one recorded set of the exercise, saved.</summary>
    private async Task<Workout> AFinishedWorkout(DateTime day, Id exerciseId, int reps, decimal weight, Id? trainer = null, Id? owner = null)
    {
        var workout = AStartedWorkout(owner ?? User, trainer, day, exerciseId);
        workout.AddSetRecord(exerciseId, Reps(reps), Weight.From(weight), RecordedAt);
        await Save(Completed(workout));

        return workout;
    }

    private Task<IReadOnlyList<ExerciseSessionView>> History(Id exerciseId, int limit = 10, Id? prescribedBy = null, Id? userId = null)
        => Factory.WithScope(services =>
            services.GetRequiredService<IExerciseHistoryQuery>().Get(userId ?? User, exerciseId, prescribedBy, limit, CancellationToken.None));

    [Fact]
    public async Task Get_ShouldReturnAnEmptyListWhenTheExerciseWasNeverDone()
    {
        // Arrange
        await AFinishedWorkout(new DateTime(2026, 11, 3), Sentadilla, 5, 100m);

        // Act
        var sessions = await History(PressBanca);

        // Assert
        sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ShouldReturnTheSessionsNewestFirstWithTheirSets()
    {
        // Arrange
        var oldest = await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);
        var newest = await AFinishedWorkout(new DateTime(2026, 11, 10), PressBanca, 8, 60m);
        var middle = await AFinishedWorkout(new DateTime(2026, 11, 6), PressBanca, 6, 55m);

        // Act
        var sessions = await History(PressBanca);

        // Assert
        sessions.Select(session => session.WorkoutId).Should().Equal(newest.Id, middle.Id, oldest.Id);
        sessions.Select(session => session.WorkoutDate).Should().Equal(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 3));
        sessions[0].Sets.Should().Equal(new LastPerformanceSetView(8, null, 60m));
    }

    [Fact]
    public async Task Get_ShouldKeepOnlyTheLatestSessionsUpToTheLimit()
    {
        // Arrange
        await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);
        var middle = await AFinishedWorkout(new DateTime(2026, 11, 6), PressBanca, 6, 55m);
        var newest = await AFinishedWorkout(new DateTime(2026, 11, 10), PressBanca, 8, 60m);

        // Act
        var sessions = await History(PressBanca, limit: 2);

        // Assert
        sessions.Select(session => session.WorkoutId).Should().Equal(newest.Id, middle.Id);
    }

    [Fact]
    public async Task Get_ShouldBreakATieOnTheDateWithTheNewestWorkout()
    {
        // Arrange
        var first = await AFinishedWorkout(new DateTime(2026, 11, 10), PressBanca, 5, 50m);
        await Task.Delay(5);
        var second = await AFinishedWorkout(new DateTime(2026, 11, 10), PressBanca, 9, 70m);

        // Act
        var sessions = await History(PressBanca);

        // Assert
        sessions.Select(session => session.WorkoutId).Should().Equal(second.Id, first.Id);
    }

    [Fact]
    public async Task Get_ShouldIgnoreUnfinishedWorkouts()
    {
        // Arrange
        var finished = await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);
        var inProgress = AStartedWorkout(User, null, new DateTime(2026, 11, 12), PressBanca);
        inProgress.AddSetRecord(PressBanca, Reps(12), Weight.From(80), RecordedAt);
        await Save(inProgress);

        // Act
        var sessions = await History(PressBanca);

        // Assert
        sessions.Select(session => session.WorkoutId).Should().Equal(finished.Id);
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
        var sessions = await History(PressBanca);

        // Assert
        sessions.Select(session => session.WorkoutId).Should().Equal(workout.Id);
    }

    [Fact]
    public async Task Get_ShouldSkipAFinishedWorkoutWithoutRecordsForTheExercise()
    {
        // Arrange
        var withRecords = await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);
        var withoutRecords = AStartedWorkout(User, null, new DateTime(2026, 11, 8), PressBanca, Sentadilla);
        withoutRecords.AddSetRecord(Sentadilla, Reps(10), Weight.From(100), RecordedAt);
        await Save(Completed(withoutRecords));

        // Act
        var sessions = await History(PressBanca);

        // Assert
        sessions.Select(session => session.WorkoutId).Should().Equal(withRecords.Id);
    }

    [Fact]
    public async Task Get_ShouldExcludeTheWorkoutsOfOtherUsers()
    {
        // Arrange
        var mine = await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m);
        await AFinishedWorkout(new DateTime(2026, 11, 8), PressBanca, 15, 90m, owner: Trainer);

        // Act
        var sessions = await History(PressBanca);

        // Assert
        sessions.Select(session => session.WorkoutId).Should().Equal(mine.Id);
    }

    [Fact]
    public async Task Get_ShouldReadOnlyTheWorkoutsPrescribedByTheGivenTrainer()
    {
        // Arrange
        var prescribed = await AFinishedWorkout(new DateTime(2026, 11, 3), PressBanca, 5, 50m, trainer: Trainer);
        var own = await AFinishedWorkout(new DateTime(2026, 11, 8), PressBanca, 9, 70m);

        // Act
        var scoped = await History(PressBanca, prescribedBy: Trainer);
        var unscoped = await History(PressBanca);

        // Assert
        scoped.Select(session => session.WorkoutId).Should().Equal(prescribed.Id);
        unscoped.Select(session => session.WorkoutId).Should().Equal(own.Id, prescribed.Id);
    }

    [Fact]
    public async Task Get_ShouldReturnTheSetsOfEachSessionInPositionOrderWithoutMixingExercises()
    {
        // Arrange
        var workout = AStartedWorkout(User, null, new DateTime(2026, 11, 6), PressBanca, Sentadilla);
        workout.AddSetRecord(PressBanca, Reps(10), Weight.From(60), RecordedAt);
        workout.AddSetRecord(Sentadilla, Reps(12), Weight.From(100), RecordedAt.AddMinutes(1));
        workout.AddSetRecord(PressBanca, Reps(8), null, RecordedAt.AddMinutes(3));
        workout.AddSetRecord(PressBanca, Reps(6), Weight.From(0), RecordedAt.AddMinutes(6));
        await Save(Completed(workout));

        // Act
        var sessions = await History(PressBanca);

        // Assert
        sessions.Should().ContainSingle();
        sessions[0].Sets.Should().Equal(
            new LastPerformanceSetView(10, null, 60m),
            new LastPerformanceSetView(8, null, null),
            new LastPerformanceSetView(6, null, 0m));
    }
}
