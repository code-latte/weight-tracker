using FluentAssertions;
using Moq;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Unit.Application.Services;

public class WorkoutListLoaderTests
{
    private static readonly WorkoutName Name = WorkoutName.From("Leg day");

    private readonly Mock<IExerciseRepository> _exercises = new(MockBehavior.Strict);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);

    private WorkoutListLoader ALoader() => new(_exercises.Object, _users.Object);

    private static Workout AWorkout(Id ownerId, Id? trainerId, params Id[] exerciseIds) =>
        Workout.Rehydrate(
            Id.New(), ownerId, trainerId, Name, null, new DateTime(2026, 10, 1), WorkoutStatus.PUBLISHED,
            exerciseIds.Select(exerciseId => WorkoutExercise.Rehydrate(
                Id.New(), exerciseId, Effort.MID, [new SetPrescription(3, new SetTarget.Reps(10))])));

    private static Exercise AnExercise(Id id) =>
        Exercise.Rehydrate(id, null, null, Muscles.CHEST, ExerciseTypes.STRENGTH, []);

    private static User AUser(Id id) => User.Rehydrate(
        id, Email.From($"{id.Value}@weighttracker.test"), PersonName.From("Ada"), PersonName.From("Lovelace"),
        new DateOnly(1988, 5, 12), Role.USER, PasswordHash.From("stored-hash"), new DateTime(2026, 1, 1), true, null);

    [Fact]
    public async Task ShouldReadOnlyTheReferencedExercisesAndUsersAndKeepTheOrder()
    {
        // Arrange
        var ownerId = Id.New();
        var trainerId = Id.New();
        var exerciseId = Id.New();
        var prescribed = AWorkout(ownerId, trainerId, exerciseId);
        var selfPlanned = AWorkout(ownerId, null, exerciseId);
        IEnumerable<Id>? askedExercises = null;
        IEnumerable<Id>? askedUsers = null;
        _exercises
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<Id> ids, CancellationToken _) => askedExercises = ids.ToList())
            .ReturnsAsync([AnExercise(exerciseId)]);
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .Callback((IEnumerable<Id> ids, CancellationToken _) => askedUsers = ids.ToList())
            .ReturnsAsync((IEnumerable<Id> ids, CancellationToken _) => ids.Select(AUser).ToList());

        // Act
        var result = await ALoader().Load([selfPlanned, prescribed], CancellationToken.None);

        // Assert
        result.Workouts.Should().Equal(selfPlanned, prescribed);
        askedExercises.Should().BeEquivalentTo([exerciseId]);
        askedUsers.Should().BeEquivalentTo([ownerId, trainerId]);
        result.Exercises.Keys.Should().BeEquivalentTo([exerciseId]);
        result.Users.Keys.Should().BeEquivalentTo([ownerId, trainerId]);
    }

    [Fact]
    public async Task MissingUser_ShouldFailInsteadOfReturningAPartialList()
    {
        // Arrange
        var workout = AWorkout(Id.New(), null);
        _exercises
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _users
            .Setup(repository => repository.GetAllByIds(It.IsAny<IEnumerable<Id>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var act = async () => await ALoader().Load([workout], CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
