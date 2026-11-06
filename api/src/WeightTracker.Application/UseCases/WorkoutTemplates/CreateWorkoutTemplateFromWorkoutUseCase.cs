using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class CreateWorkoutTemplateFromWorkoutUseCase
{
    private const string Operation = "POST /workouts/{workoutId}/templates";

    private IWorkoutRepository _workouts;

    private IWorkoutTemplateRepository _templates;

    private WorkoutTemplateTrainerValidator _trainers;

    private IUnitOfWork _unitOfWork;

    public CreateWorkoutTemplateFromWorkoutUseCase(
        IWorkoutRepository workouts,
        IWorkoutTemplateRepository templates,
        WorkoutTemplateTrainerValidator trainers,
        IUnitOfWork unitOfWork
    )
    {
        _workouts = workouts;
        _templates = templates;
        _trainers = trainers;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Saves an existing workout as a new template owned by the workout's trainer. A trainer may only save the
    /// workouts they prescribe; an admin may save any workout that has a trainer, and never owns the template.
    /// The workout is only read.
    /// </summary>
    /// <returns>The created template.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user, or a trainer who is not the workout's trainer.</exception>
    /// <exception cref="InvalidWorkoutTemplateTrainerException">When the workout's trainer is not an active trainer.</exception>
    /// <exception cref="WorkoutWithoutTrainerException">When the workout has no trainer.</exception>
    public async Task<WorkoutTemplate> Execute(Id callerId, Role callerRole, Id workoutId, CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        EnsureCanSave(workout, callerId, callerRole);

        // An admin can be a workout's trainer, and a trainer can be deactivated, but only an active trainer owns templates
        if (workout.TrainerId is not null)
            await _trainers.EnsureCanOwnTemplates(workout.TrainerId, cancellationToken);

        var template = WorkoutTemplate.CreateFromWorkout(workout);

        await _templates.Save(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template;
    }

    /// <summary>
    /// A trainer gets the same refusal for another trainer's workout and for a workout without trainer, so nothing is
    /// revealed about workouts they do not prescribe.
    /// </summary>
    private static void EnsureCanSave(Workout workout, Id callerId, Role callerRole)
    {
        switch (callerRole)
        {
            case Role.ADMIN:
                return;

            case Role.TRAINER when workout.TrainerId == callerId:
                return;

            default:
                throw new UnauthorizedOperationException(Operation);
        }
    }
}
