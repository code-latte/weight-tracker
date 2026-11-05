using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using WeightTracker.Api.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.AthleteInvitations;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Workouts;
using Xunit;

namespace WeightTracker.Integration.Common;

/// <summary>
/// Needs neither a database nor a host. It lives in this project because it is the one that
/// references the Api layer. Covers the status of every exception the exercises endpoints can
/// raise, and of the Workout aggregate rules, including the ones no HTTP request can reach.
/// </summary>
public class ExceptionMapperTests
{
    private static int StatusOf(Exception exception)
    {
        var mapper = new ExceptionMapper(NullLogger<ExceptionMapper>.Instance);

        return ((ObjectResult)mapper.Map(exception)).StatusCode!.Value;
    }

    [Fact]
    public void Map_ShouldReturnUnauthorizedWhenTheAuthenticatedUserNoLongerExists()
    {
        // Arrange
        var exception = new AuthenticatedUserNotFoundException();

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidExerciseName()
    {
        // Arrange
        var exception = new InvalidExerciseNameException("ab");

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidUrl()
    {
        // Arrange
        var exception = new InvalidUrlException("not a url");

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    public static TheoryData<Exception> InvalidExerciseInputs() => new()
    {
        new InvalidMuscleException(99),
        new InvalidLanguageCodeException("spa"),
        new InvalidFreeExerciseDbIdException(" "),
        new DuplicateExerciseTranslationException("es"),
    };

    [Theory]
    [MemberData(nameof(InvalidExerciseInputs))]
    public void Map_ShouldReturnBadRequestForInvalidExerciseInput(Exception exception)
    {
        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnNotFoundForAnUnknownExercise()
    {
        // Arrange
        var exception = new ExerciseNotFoundException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Map_ShouldReturnForbiddenForASystemExerciseThatCannotBeChanged()
    {
        // Arrange
        var exception = new SystemExerciseNotEditableException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void Map_ShouldReturnConflictForAnArchivedExercise()
    {
        // Arrange
        var exception = new ExerciseArchivedException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Map_ShouldReturnConflictForAnExerciseThatCannotBeUsedInTheWorkout()
    {
        // Arrange
        var exception = new ExerciseNotUsableInWorkoutException(Id.New(), Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidTimeInMinutes()
    {
        // Arrange
        var exception = new InvalidTimeInMinutesException("minutes must be greater than 0, got 0");

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidEffort()
    {
        // Arrange
        var exception = new InvalidEffortException(99);

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnConflictForAnExerciseAlreadyInTheWorkout()
    {
        // Arrange
        var exception = new ExerciseAlreadyInWorkoutException(Id.New(), Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Map_ShouldReturnNotFoundForAnExerciseNotInTheWorkout()
    {
        // Arrange
        var exception = new ExerciseNotInWorkoutException(Id.New(), Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidSuperSeriesId()
    {
        // Arrange
        var exception = new InvalidSuperSeriesIdException(0);

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForASuperSeriesWithTooFewExercises()
    {
        // Arrange
        var exception = new SuperSeriesRequiresTwoExercisesException(1);

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForADuplicateExerciseInASuperSeries()
    {
        // Arrange
        var exception = new DuplicateExerciseInSuperSeriesException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForASuperSeriesSetCountMismatch()
    {
        // Arrange
        var exception = new SuperSeriesSetCountMismatchException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidWorkoutAssignment()
    {
        // Arrange
        var exception = new InvalidWorkoutAssignmentException("Only an admin can set the trainer of a workout");

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnNotFoundForAnUnknownSuperSeries()
    {
        // Arrange
        var exception = new SuperSeriesNotFoundException(SuperSeriesId.From(1), Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Map_ShouldReturnNotFoundForAnUnknownWorkout()
    {
        // Arrange
        var exception = new WorkoutNotFoundException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Map_ShouldReturnNotFoundForAWorkoutExerciseNotInTheWorkout()
    {
        // Arrange
        var exception = new WorkoutExerciseNotInWorkoutException(Id.New(), Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Map_ShouldReturnConflictForAnExerciseAlreadyInASuperSeries()
    {
        // Arrange
        var exception = new ExerciseAlreadyInSuperSeriesException(Id.New(), Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidWorkoutStatus()
    {
        // Arrange
        var exception = new InvalidWorkoutStatusException(99);

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnConflictForAnInvalidWorkoutStatusTransition()
    {
        // Arrange
        var exception = new InvalidWorkoutStatusTransitionException(WorkoutStatus.DRAFT, WorkoutStatus.FINISHED);

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Map_ShouldReturnConflictForALockedWorkout()
    {
        // Arrange
        var exception = new WorkoutLockedException(Id.New(), WorkoutStatus.STARTED);

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Map_ShouldReturnConflictForAMismatchingExerciseOrder()
    {
        // Arrange
        var exception = new WorkoutExerciseOrderMismatchException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Map_ShouldReturnConflictForAnAlreadyActiveUser()
    {
        // Arrange
        var exception = new UserAlreadyActiveException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Map_ShouldReturnConflictForAnAlreadyDeactivatedUser()
    {
        // Arrange
        var exception = new UserAlreadyDeactivatedException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void Map_ShouldReturnForbiddenWithACodeForADeactivatedUser()
    {
        // Arrange
        var mapper = new ExceptionMapper(NullLogger<ExceptionMapper>.Instance);

        // Act
        var result = (ObjectResult)mapper.Map(new UserDeactivatedException());

        // Assert
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ((ProblemDetails)result.Value!).Extensions["code"].Should().Be("USER_DEACTIVATED");
    }

    [Fact]
    public void Map_ShouldAddNoCodeToOtherProblems()
    {
        // Arrange
        var mapper = new ExceptionMapper(NullLogger<ExceptionMapper>.Instance);

        // Act
        var result = (ObjectResult)mapper.Map(new InvalidCredentialsException());

        // Assert
        ((ProblemDetails)result.Value!).Extensions.Should().NotContainKey("code");
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidUserActivationState()
    {
        // Arrange
        var exception = new InvalidUserActivationStateException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    public static TheoryData<Exception, int> TrainerAthleteFailures() => new()
    {
        { new TrainerAthleteNotFoundException(Id.New(), Id.New()), StatusCodes.Status404NotFound },
        { new AthleteAlreadyHasTrainerException(Id.New()), StatusCodes.Status409Conflict },
        { new InvalidTrainerAthleteRoleException("wrong role"), StatusCodes.Status409Conflict },
        {
            new InvalidTrainerAthleteStatusTransitionException(TrainerAthleteStatus.ENDED, TrainerAthleteStatus.ENDED),
            StatusCodes.Status409Conflict
        },
    };

    [Theory]
    [MemberData(nameof(TrainerAthleteFailures))]
    public void Map_ShouldMapEveryTrainerAthleteFailure(Exception exception, int expected)
    {
        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(expected);
    }

    public static TheoryData<Exception, int> TrainerInvitationFailures() => new()
    {
        { new TrainerInvitationNotFoundException(Id.New()), StatusCodes.Status404NotFound },
        {
            new PendingInvitationAlreadyExistsException(Email.From("athlete@weighttracker.test")),
            StatusCodes.Status409Conflict
        },
        { new InvalidInvitationTokenException(), StatusCodes.Status403Forbidden },
        {
            new InvalidTrainerInvitationStatusTransitionException(
                TrainerInvitationStatus.ACCEPTED, TrainerInvitationStatus.REJECTED),
            StatusCodes.Status409Conflict
        },
        { new TrainerInvitationExpiredException(Id.New()), StatusCodes.Status409Conflict },
    };

    [Theory]
    [MemberData(nameof(TrainerInvitationFailures))]
    public void Map_ShouldMapEveryTrainerInvitationFailure(Exception exception, int expected)
    {
        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(expected);
    }

    public static TheoryData<Exception> InvalidTrainerAggregateStates() => new()
    {
        new InvalidTrainerAthleteStateException(Id.New()),
        new InvalidTrainerInvitationStateException(Id.New(), "it must expire after it is created"),
    };

    [Theory]
    [MemberData(nameof(InvalidTrainerAggregateStates))]
    public void Map_ShouldReturnBadRequestForAnInvalidTrainerAggregateState(Exception exception)
    {
        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    public static TheoryData<Exception, int> MesocycleFailures() => new()
    {
        { new InvalidMesocycleNameException("blank"), StatusCodes.Status400BadRequest },
        { new InvalidMesocycleObjectiveException("blank"), StatusCodes.Status400BadRequest },
        { new InvalidDurationInWeeksException(53, 1, 52), StatusCodes.Status400BadRequest },
        { new InvalidWeekNumberException(0), StatusCodes.Status400BadRequest },
        { new InvalidTrainingWeekTypeException(99), StatusCodes.Status400BadRequest },
        { new MesocycleWeekNotFoundException(WeekNumber.From(3), Id.New()), StatusCodes.Status404NotFound },
        { new MesocycleTrainerIsAthleteException(Id.New()), StatusCodes.Status409Conflict },
        { new MesocycleWeekOutOfRangeException(WeekNumber.From(5), 4, Id.New()), StatusCodes.Status409Conflict },
        { new DuplicateMesocycleWeekException(WeekNumber.From(2), Id.New()), StatusCodes.Status409Conflict },
        { new MesocycleDurationTooShortException(3, WeekNumber.From(4), Id.New()), StatusCodes.Status409Conflict },
        { new WorkoutPlacementLockedException(Id.New(), WorkoutStatus.STARTED), StatusCodes.Status409Conflict },
        { new IncompleteWorkoutPlacementException(Id.New()), StatusCodes.Status409Conflict },
    };

    [Theory]
    [MemberData(nameof(MesocycleFailures))]
    public void Map_ShouldMapEveryMesocycleAndPlacementFailure(Exception exception, int expected)
    {
        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(expected);
    }
}
