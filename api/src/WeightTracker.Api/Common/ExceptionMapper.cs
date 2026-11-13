using Microsoft.AspNetCore.Mvc;
using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Api.Common;

/// <summary>
/// Turns the custom exceptions raised by the Domain and Application layers into HTTP responses.
/// </summary>
/// <remarks>
/// Anything that is not a declared custom exception resolves to a 500, as does any custom
/// Application exception without an explicit mapping, since those signal a server-side fault
/// rather than bad caller input.
/// </remarks>
public class ExceptionMapper
{
    private readonly ILogger<ExceptionMapper> _logger;

    public ExceptionMapper(ILogger<ExceptionMapper> logger)
    {
        _logger = logger;
    }

    public IActionResult Map(Exception exception)
    {
        return exception switch
        {
            InvalidEmailException
            or InvalidIdException
            or InvalidPersonNameException
            or InvalidExerciseNameException
            or InvalidUrlException
            or InvalidMuscleException
            or InvalidLanguageCodeException
            or InvalidFreeExerciseDbIdException
            or DuplicateExerciseTranslationException
            or InvalidEffortException
            or InvalidWorkoutStatusException
            or InvalidPasswordException
            or InvalidPasswordHashException
            or InvalidUserActivationStateException
            or InvalidTrainerAthleteStateException
            or InvalidTrainerInvitationStateException
            or TrainerCannotBeWorkoutOwnerException
            or InvalidSuperSeriesIdException
            or SuperSeriesRequiresTwoExercisesException
            or DuplicateExerciseInSuperSeriesException
            or SuperSeriesSetCountMismatchException
            or InvalidWorkoutAssignmentException
            or InvalidSetPrescriptionException
            or InvalidTimeInMinutesException
            or InvalidRepCountException
            or InvalidDurationInSecondsException
            or SetResultRequiredException
            or InvalidWeightException
            or WorkoutExerciseRequiresSetsException
            or ExerciseIdMismatchException
            or InvalidMesocycleNameException
            or InvalidMesocycleObjectiveException
            or InvalidDurationInWeeksException
            or InvalidWeekNumberException
            or InvalidTrainingWeekTypeException
            or InvalidMesocycleAssignmentException
            or InvalidWorkoutTemplateTrainerException
            or WorkoutWithoutTrainerException
            or IncompleteMesocyclePlacementException
            or MissingWorkoutDateException
            or InvalidHistoryLimitException
            or PasswordNotStrongEnoughException => Problem(
                StatusCodes.Status400BadRequest,
                exception.Message
            ),

            InvalidCredentialsException or InvalidRefreshTokenException or AuthenticatedUserNotFoundException => Problem(
                StatusCodes.Status401Unauthorized,
                exception.Message
            ),

            UserDeactivatedException => Problem(
                StatusCodes.Status403Forbidden,
                exception.Message,
                UserDeactivatedException.Code
            ),

            UnauthorizedOperationException or AdminSignupNotAllowedException
                or InvalidInvitationTokenException or SystemExerciseNotEditableException => Problem(
                StatusCodes.Status403Forbidden,
                exception.Message
            ),

            UserNotFoundException or ExerciseNotInWorkoutException or ExerciseNotFoundException
                or SuperSeriesNotFoundException or WorkoutNotFoundException
                or WorkoutExerciseNotInWorkoutException or TrainerAthleteNotFoundException
                or TrainerInvitationNotFoundException or MesocycleWeekNotFoundException
                or MesocycleNotFoundException or WorkoutTemplateNotFoundException
                or SetRecordNotFoundException => Problem(
                StatusCodes.Status404NotFound,
                exception.Message
            ),

            RefreshTokenAlreadyRevokedException or EmailAlreadyExistException
                or ExerciseAlreadyInWorkoutException or ExerciseAlreadyInSuperSeriesException
                or InvalidWorkoutStatusTransitionException or WorkoutLockedException
                or WorkoutExerciseOrderMismatchException or SetRecordOrderMismatchException or UserAlreadyActiveException
                or UserAlreadyDeactivatedException or AthleteAlreadyHasTrainerException
                or InvalidTrainerAthleteRoleException or InvalidTrainerAthleteStatusTransitionException
                or PendingInvitationAlreadyExistsException or InvalidTrainerInvitationStatusTransitionException
                or TrainerInvitationExpiredException or ExerciseArchivedException
                or ExerciseNotUsableInWorkoutException or MesocycleTrainerIsAthleteException
                or MesocycleWeekOutOfRangeException or DuplicateMesocycleWeekException
                or MesocycleDurationTooShortException or WorkoutPlacementLockedException
                or WorkoutDeletionLockedException
                or IncompleteWorkoutPlacementException or MesocycleAthleteMismatchException
                or MesocycleTrainerMismatchException or MesocycleWeekHasWorkoutsException
                or WorkoutResultsLockedException or SetRecordNotAllowedForTargetException
                or WorkoutReviewNotAllowedException or WorkoutResultsChangedException
                or WorkoutConcurrencyConflictException => Problem(
                StatusCodes.Status409Conflict,
                exception.Message
            ),

            // Observations are generated by the system, never from caller input, so breaking their rules is a server fault
            InvalidWorkoutObservationException or InvalidObservationThresholdsException => InternalServerError(exception),

            // Any other domain rule breach is still a caller problem
            DomainException => Problem(StatusCodes.Status400BadRequest, exception.Message),

            _ => InternalServerError(exception),
        };
    }

    private IActionResult InternalServerError(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception while serving the request");

        return Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
    }

    /// <param name="code">
    /// Stable, machine-readable reason, added as the <c>code</c> member of the body. Only set for the
    /// failures a client must tell apart from others with the same status.
    /// </param>
    private static IActionResult Problem(int statusCode, string detail, string? code = null)
    {
        var problem = new ProblemDetails { Status = statusCode, Detail = detail };

        if (code is not null)
            problem.Extensions["code"] = code;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
        };
    }
}
