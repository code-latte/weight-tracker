using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Checks the users a new mesocycle goes from and to: an active trainer and an active regular user.
/// </summary>
public class MesocycleAssignmentValidator
{
    private IUserRepository _users;

    public MesocycleAssignmentValidator(IUserRepository users)
    {
        _users = users;
    }

    /// <exception cref="UserNotFoundException">When no user has the given id.</exception>
    /// <exception cref="InvalidMesocycleAssignmentException">When the user is not a regular user or is deactivated.</exception>
    public async Task EnsureAthlete(Id athleteId, CancellationToken cancellationToken)
    {
        var athlete = await _users.GetById(athleteId, cancellationToken)
            ?? throw new UserNotFoundException(athleteId);

        if (athlete.CanBeAthlete() == false)
            throw new InvalidMesocycleAssignmentException("The athlete of a mesocycle must be an active regular user");
    }

    /// <exception cref="InvalidMesocycleAssignmentException">
    /// When the user does not exist, is not a trainer, or is deactivated. Admins cannot be the trainer of a mesocycle.
    /// </exception>
    public async Task EnsureTrainer(Id trainerId, CancellationToken cancellationToken)
    {
        var trainer = await _users.GetById(trainerId, cancellationToken);

        if (trainer is null || trainer.CanBeTrainer() == false)
            throw new InvalidMesocycleAssignmentException("The trainer of a mesocycle must be an active trainer");
    }
}
