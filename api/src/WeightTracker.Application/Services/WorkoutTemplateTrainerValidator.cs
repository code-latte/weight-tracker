using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Services;

/// <summary>
/// Checks the user a workout template belongs to: an active trainer. Every use case that decides the owner of a
/// template goes through it, so the rule cannot drift between them.
/// </summary>
public class WorkoutTemplateTrainerValidator
{
    private IUserRepository _users;

    public WorkoutTemplateTrainerValidator(IUserRepository users)
    {
        _users = users;
    }

    /// <exception cref="InvalidWorkoutTemplateTrainerException">
    /// When the user does not exist, is not a trainer, or is deactivated. Admins never own a workout template.
    /// </exception>
    public async Task EnsureCanOwnTemplates(Id trainerId, CancellationToken cancellationToken)
    {
        var trainer = await _users.GetById(trainerId, cancellationToken);

        // An unknown user is refused like any other invalid trainer, as for mesocycles
        if (trainer is null || trainer.CanBeTrainer() == false)
            throw new InvalidWorkoutTemplateTrainerException("The trainer of a workout template must be an active trainer");
    }
}
