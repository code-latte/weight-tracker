using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.WorkoutTemplates;

public class CreateWorkoutTemplateUseCase
{
    private const string Operation = "POST /workout-templates";

    private IWorkoutTemplateRepository _templates;

    private WorkoutTemplateTrainerValidator _trainers;

    private IUnitOfWork _unitOfWork;

    public CreateWorkoutTemplateUseCase(
        IWorkoutTemplateRepository templates,
        WorkoutTemplateTrainerValidator trainers,
        IUnitOfWork unitOfWork
    )
    {
        _templates = templates;
        _trainers = trainers;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates an empty template. A trainer creates it for themselves; an admin creates it for an active trainer
    /// and never owns it.
    /// </summary>
    /// <param name="trainerId">Requested owner. Required for an admin, refused from a trainer.</param>
    /// <param name="description">Optional description, <c>null</c> when the template has none.</param>
    /// <returns>The created template.</returns>
    /// <exception cref="InvalidWorkoutTemplateTrainerException">When the requested trainer is not allowed for the caller's role, or cannot own a template.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user.</exception>
    public async Task<WorkoutTemplate> Execute(
        Id callerId,
        Role callerRole,
        Id? trainerId,
        WorkoutName name,
        WorkoutDescription? description,
        CancellationToken cancellationToken)
    {
        var ownerId = await ResolveTrainer(callerId, callerRole, trainerId, cancellationToken);

        var template = WorkoutTemplate.Create(ownerId, name, description);

        await _templates.Save(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return template;
    }

    private async Task<Id> ResolveTrainer(Id callerId, Role callerRole, Id? trainerId, CancellationToken cancellationToken)
    {
        switch (callerRole)
        {
            case Role.TRAINER:
                if (trainerId is not null)
                    throw new InvalidWorkoutTemplateTrainerException("Only an admin can set the trainer of a workout template");

                return callerId;

            case Role.ADMIN:
                if (trainerId is null)
                    throw new InvalidWorkoutTemplateTrainerException("The trainer the workout template is for must be specified");

                await _trainers.EnsureCanOwnTemplates(trainerId, cancellationToken);

                return trainerId;

            default:
                throw new UnauthorizedOperationException(Operation);
        }
    }
}
