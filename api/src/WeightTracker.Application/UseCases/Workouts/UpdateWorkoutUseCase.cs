using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class UpdateWorkoutUseCase
{
    private const string Operation = "PUT /workouts/{id}";

    private IWorkoutRepository _workouts;

    private WorkoutEditAuthorizer _authorizer;

    private WorkoutAssignmentValidator _assignment;

    private MesocyclePlacementValidator _placement;

    private IUnitOfWork _unitOfWork;

    public UpdateWorkoutUseCase(
        IWorkoutRepository workouts,
        WorkoutEditAuthorizer authorizer,
        WorkoutAssignmentValidator assignment,
        MesocyclePlacementValidator placement,
        IUnitOfWork unitOfWork
    )
    {
        _workouts = workouts;
        _authorizer = authorizer;
        _assignment = assignment;
        _placement = placement;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Replaces the name, description and day of a workout, its owner and trainer, and its mesocycle placement.
    /// Changing the details is an edit; changing the owner or the trainer is reserved to admins; placing it in a
    /// mesocycle is reserved to the mesocycle's trainer and admins. A placed workout takes the date of its week on
    /// every update, even when the placement is unchanged. Status and exercises are untouched.
    /// </summary>
    /// <param name="userId">Owner the workout must have. A non-admin must send the current one.</param>
    /// <param name="trainerId">Trainer the workout must have, <c>null</c> for none. A non-admin must send the current one.</param>
    /// <param name="description">New description, <c>null</c> to clear it.</param>
    /// <param name="workoutDate">New day of a standalone workout. Ignored when the workout stays or becomes placed, since its mesocycle week dates it.</param>
    /// <param name="mesocycleId">Mesocycle the workout must be placed in, <c>null</c> together with <paramref name="mesocycleWeekNumber"/> to leave it standalone.</param>
    /// <param name="mesocycleWeekNumber">Week of the mesocycle the workout must be placed in, <c>null</c> to leave it standalone.</param>
    /// <returns>The updated workout.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not edit the workout, or a non-admin changes its owner or trainer.</exception>
    /// <exception cref="WorkoutLockedException">When the caller may edit the workout but it has already started.</exception>
    /// <exception cref="InvalidWorkoutAssignmentException">When an admin makes themselves the owner, the new trainer cannot train, or the new owner or trainer is deactivated.</exception>
    /// <exception cref="UserNotFoundException">When the new owner does not exist.</exception>
    /// <exception cref="TrainerCannotBeWorkoutOwnerException">When the trainer is the owner.</exception>
    /// <exception cref="IncompleteMesocyclePlacementException">When only one of the mesocycle and the week is sent.</exception>
    /// <exception cref="MissingWorkoutDateException">When the workout is left standalone and no date is sent.</exception>
    /// <exception cref="MesocycleNotFoundException">When the mesocycle does not exist.</exception>
    /// <exception cref="MesocycleAthleteMismatchException">When the mesocycle is for another athlete than the resulting owner.</exception>
    /// <exception cref="MesocycleTrainerMismatchException">When the resulting trainer does not prepare the mesocycle.</exception>
    /// <exception cref="MesocycleWeekOutOfRangeException">When the week is beyond the mesocycle's duration.</exception>
    /// <exception cref="WorkoutPlacementLockedException">When the placement changes on a started or finished workout, whoever the caller is.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        WorkoutName name,
        WorkoutDescription? description,
        DateTime? workoutDate,
        Id userId,
        Id? trainerId,
        Id? mesocycleId,
        WeekNumber? mesocycleWeekNumber,
        CancellationToken cancellationToken)
    {
        MesocyclePlacementValidator.EnsureComplete(mesocycleId, mesocycleWeekNumber);
        MesocyclePlacementValidator.EnsureDateOrPlacement(mesocycleId, workoutDate);

        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        var ownerChanged = userId != workout.UserId;
        var trainerChanged = trainerId != workout.TrainerId;

        // Checked before the edit rule so a non-admin who tries to reassign always gets 403, never 409
        if (callerRole != Role.ADMIN && (ownerChanged || trainerChanged))
            throw new UnauthorizedOperationException(Operation);

        _authorizer.EnsureCanEdit(workout, callerId, callerRole, Operation);

        var placementChanged = mesocycleId != workout.MesocycleId || mesocycleWeekNumber != workout.MesocycleWeekNumber;

        if (ownerChanged || trainerChanged)
        {
            if (ownerChanged)
            {
                if (userId == callerId)
                    throw new InvalidWorkoutAssignmentException("Admins cannot make themselves the owner of a workout");

                var owner = await _assignment.EnsureOwnerExists(userId, cancellationToken);

                _assignment.EnsureCanBeAssigned(owner);
            }

            if (trainerChanged && trainerId is not null)
                await _assignment.EnsureCanTrain(trainerId, cancellationToken);
        }

        // A reassignment can break a placement that is kept, so the placement is checked whenever either changes
        if (mesocycleId is not null && (placementChanged || ownerChanged || trainerChanged))
            await _placement.EnsureCanPlace(
                userId, trainerId, mesocycleId, mesocycleWeekNumber!, callerId, callerRole, Operation, cancellationToken);

        // Resolved even for an unchanged placement, so a rescheduled mesocycle reaches its workouts on their next update
        var date = await _placement.ResolveWorkoutDate(mesocycleId, mesocycleWeekNumber, workoutDate, cancellationToken);

        // Only a real change reaches the aggregate, so sending back the placement of a locked workout is accepted
        if (placementChanged)
        {
            if (mesocycleId is null)
                workout.UnlinkFromMesocycle();
            else
                workout.LinkToMesocycle(mesocycleId, mesocycleWeekNumber!);
        }

        if (ownerChanged || trainerChanged)
            workout.Reassign(userId, trainerId);

        workout.UpdateDetails(name, description, date);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }
}
