using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class CreateWorkoutUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutAssignmentValidator _assignment;

    private MesocyclePlacementValidator _placement;

    private IUnitOfWork _unitOfWork;

    public CreateWorkoutUseCase(
        IWorkoutRepository workouts,
        WorkoutAssignmentValidator assignment,
        MesocyclePlacementValidator placement,
        IUnitOfWork unitOfWork
    )
    {
        _workouts = workouts;
        _assignment = assignment;
        _placement = placement;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates a workout. A regular user creates it for themselves; a trainer prescribes it to a regular
    /// user; an admin creates it for any other user and may assign a trainer.
    /// </summary>
    /// <param name="userId">Requested owner. Optional for a user, required for a trainer or an admin.</param>
    /// <param name="trainerId">Requested trainer. Only an admin may send it.</param>
    /// <param name="description">Optional description, <c>null</c> when the workout has none.</param>
    /// <param name="workoutDate">Day of a standalone workout. Ignored when the workout is placed, since its mesocycle week dates it.</param>
    /// <param name="mesocycleId">Mesocycle to place the workout in, <c>null</c> for a standalone workout. Sent with <paramref name="mesocycleWeekNumber"/>.</param>
    /// <param name="mesocycleWeekNumber">Week of the mesocycle to place the workout in, <c>null</c> for a standalone workout.</param>
    /// <returns>The created workout.</returns>
    /// <exception cref="InvalidWorkoutAssignmentException">When the requested owner or trainer is not allowed for the caller's role, or is deactivated.</exception>
    /// <exception cref="UserNotFoundException">When the requested owner does not exist.</exception>
    /// <exception cref="UnauthorizedOperationException">When a trainer names an owner who is not a regular user.</exception>
    /// <exception cref="TrainerCannotBeWorkoutOwnerException">When the trainer is the owner.</exception>
    /// <exception cref="IncompleteMesocyclePlacementException">When only one of the mesocycle and the week is sent.</exception>
    /// <exception cref="MissingWorkoutDateException">When the workout is not placed and no date is sent.</exception>
    /// <exception cref="MesocycleNotFoundException">When the mesocycle does not exist.</exception>
    /// <exception cref="MesocycleAthleteMismatchException">When the mesocycle is for another athlete.</exception>
    /// <exception cref="MesocycleTrainerMismatchException">When the workout's trainer does not prepare the mesocycle.</exception>
    /// <exception cref="MesocycleWeekOutOfRangeException">When the week is beyond the mesocycle's duration.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id? userId,
        Id? trainerId,
        WorkoutName name,
        WorkoutDescription? description,
        DateTime? workoutDate,
        Id? mesocycleId,
        WeekNumber? mesocycleWeekNumber,
        CancellationToken cancellationToken)
    {
        MesocyclePlacementValidator.EnsureComplete(mesocycleId, mesocycleWeekNumber);
        MesocyclePlacementValidator.EnsureDateOrPlacement(mesocycleId, workoutDate);

        var (ownerId, prescriberId) = ResolveAssignment(callerId, callerRole, userId, trainerId);

        if (callerRole != Role.USER)
        {
            var owner = await _assignment.EnsureOwnerExists(ownerId, cancellationToken);

            if (callerRole == Role.TRAINER && owner.Role != Role.USER)
                throw new UnauthorizedOperationException("POST /workouts");

            _assignment.EnsureCanBeAssigned(owner);
        }

        if (callerRole == Role.ADMIN && prescriberId is not null)
            await _assignment.EnsureCanTrain(prescriberId, cancellationToken);

        if (mesocycleId is not null)
            await _placement.EnsureCanPlace(
                ownerId, prescriberId, mesocycleId, mesocycleWeekNumber!, callerId, callerRole, "POST /workouts", cancellationToken);

        var date = await _placement.ResolveWorkoutDate(mesocycleId, mesocycleWeekNumber, workoutDate, cancellationToken);

        var workout = Workout.Create(ownerId, prescriberId, name, description, date);

        // A new workout is a draft, so placing it is never refused by the lock
        if (mesocycleId is not null)
            workout.LinkToMesocycle(mesocycleId, mesocycleWeekNumber!);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }

    /// <summary>
    /// Decides owner and trainer from the caller's role, before any lookup.
    /// </summary>
    private static (Id OwnerId, Id? TrainerId) ResolveAssignment(Id callerId, Role callerRole, Id? userId, Id? trainerId)
    {
        switch (callerRole)
        {
            case Role.USER:
                if (trainerId is not null)
                    throw new InvalidWorkoutAssignmentException("Only an admin can set the trainer of a workout");

                if (userId is not null && userId != callerId)
                    throw new InvalidWorkoutAssignmentException("A user can only create workouts for themselves");

                return (callerId, null);

            case Role.TRAINER:
                if (trainerId is not null)
                    throw new InvalidWorkoutAssignmentException("Only an admin can set the trainer of a workout");

                return (RequireOtherOwner(callerId, userId), callerId);

            case Role.ADMIN:
                return (RequireOtherOwner(callerId, userId), trainerId);

            default:
                throw new UnauthorizedOperationException("POST /workouts");
        }
    }

    private static Id RequireOtherOwner(Id callerId, Id? userId)
    {
        if (userId is null)
            throw new InvalidWorkoutAssignmentException("The user the workout is for must be specified");

        if (userId == callerId)
            throw new InvalidWorkoutAssignmentException("Trainers and admins cannot create workouts for themselves");

        return userId;
    }
}
