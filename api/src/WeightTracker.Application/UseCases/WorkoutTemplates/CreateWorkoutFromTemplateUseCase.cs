using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class CreateWorkoutFromTemplateUseCase
{
    private const string Operation = "POST /workout-templates/{templateId}/workouts";

    private IWorkoutTemplateRepository _templates;

    private IWorkoutRepository _workouts;

    private IExerciseRepository _exercises;

    private WorkoutTemplateEditAuthorizer _authorizer;

    private WorkoutAssignmentValidator _assignment;

    private MesocyclePlacementValidator _placement;

    private IUnitOfWork _unitOfWork;

    public CreateWorkoutFromTemplateUseCase(
        IWorkoutTemplateRepository templates,
        IWorkoutRepository workouts,
        IExerciseRepository exercises,
        WorkoutTemplateEditAuthorizer authorizer,
        WorkoutAssignmentValidator assignment,
        MesocyclePlacementValidator placement,
        IUnitOfWork unitOfWork
    )
    {
        _templates = templates;
        _workouts = workouts;
        _exercises = exercises;
        _authorizer = authorizer;
        _assignment = assignment;
        _placement = placement;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates a draft workout for a regular user by copying a template. The template's trainer prescribes it,
    /// whoever the caller is, and the owner and placement rules are those of a workout prescribed by that trainer.
    /// The template itself is only read.
    /// </summary>
    /// <param name="userId">Owner of the new workout. Required.</param>
    /// <param name="workoutDate">Day of a standalone workout. Ignored when the workout is placed, since its mesocycle week dates it.</param>
    /// <param name="mesocycleId">Mesocycle to place the workout in, <c>null</c> for a standalone workout. Sent with <paramref name="mesocycleWeekNumber"/>.</param>
    /// <param name="mesocycleWeekNumber">Week of the mesocycle to place the workout in, <c>null</c> for a standalone workout.</param>
    /// <returns>The created workout with the exercises it references.</returns>
    /// <exception cref="IncompleteMesocyclePlacementException">When only one of the mesocycle and the week is sent.</exception>
    /// <exception cref="MissingWorkoutDateException">When the workout is not placed and no date is sent.</exception>
    /// <exception cref="WorkoutTemplateNotFoundException">When no template has the given id, or it belongs to another trainer.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user, or a trainer names an owner who is not a regular user.</exception>
    /// <exception cref="InvalidWorkoutAssignmentException">When the owner is missing or deactivated, an admin names an owner who is not a regular user, or the template's trainer is deactivated.</exception>
    /// <exception cref="UserNotFoundException">When the owner does not exist.</exception>
    /// <exception cref="MesocycleNotFoundException">When the mesocycle does not exist.</exception>
    /// <exception cref="MesocycleAthleteMismatchException">When the mesocycle is for another athlete.</exception>
    /// <exception cref="MesocycleTrainerMismatchException">When the template's trainer does not prepare the mesocycle.</exception>
    /// <exception cref="MesocycleWeekOutOfRangeException">When the week is beyond the mesocycle's duration.</exception>
    public async Task<WorkoutFromTemplateView> Execute(
        Id callerId,
        Role callerRole,
        Id templateId,
        Id? userId,
        DateTime? workoutDate,
        Id? mesocycleId,
        WeekNumber? mesocycleWeekNumber,
        CancellationToken cancellationToken)
    {
        MesocyclePlacementValidator.EnsureComplete(mesocycleId, mesocycleWeekNumber);
        MesocyclePlacementValidator.EnsureDateOrPlacement(mesocycleId, workoutDate);

        var template = await _templates.GetById(templateId, cancellationToken)
            ?? throw new WorkoutTemplateNotFoundException(templateId);

        _authorizer.EnsureCanEdit(template, callerId, callerRole, Operation);

        if (userId is null)
            throw new InvalidWorkoutAssignmentException("The user the workout is for must be specified");

        var owner = await _assignment.EnsureOwnerExists(userId, cancellationToken);

        if (owner.Role != Role.USER)
        {
            // A trainer gets the same answer as when prescribing through POST /workouts
            if (callerRole == Role.TRAINER)
                throw new UnauthorizedOperationException(Operation);

            throw new InvalidWorkoutAssignmentException("A workout from a template can only be for a regular user");
        }

        _assignment.EnsureCanBeAssigned(owner);

        // The trainer may have been deactivated since the template was created
        await _assignment.EnsureCanTrain(template.TrainerId, cancellationToken);

        if (mesocycleId is not null)
            await _placement.EnsureCanPlace(
                userId, template.TrainerId, mesocycleId, mesocycleWeekNumber!, callerId, callerRole, Operation, cancellationToken);

        var date = await _placement.ResolveWorkoutDate(mesocycleId, mesocycleWeekNumber, workoutDate, cancellationToken);

        var workout = Workout.CreateFromTemplate(template, userId, date);

        // A new workout is a draft, so placing it is never refused by the lock
        if (mesocycleId is not null)
            workout.LinkToMesocycle(mesocycleId, mesocycleWeekNumber!);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var exercises = await _exercises.GetAllByIds(workout.Exercises.Select(entry => entry.ExerciseId), cancellationToken);

        return new WorkoutFromTemplateView(workout, exercises.ToDictionary(exercise => exercise.Id));
    }
}
