using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.TrainerAthletes;

namespace WeightTracker.Application.UseCases.TrainerAthletes;

/// <summary>
/// Starts a relationship between a trainer and an athlete.
/// </summary>
/// <remarks>
/// It takes no caller: over HTTP only an administrator reaches it, and it is meant to be reused by system
/// behaviour (an accepted invitation) that has no caller at all.
/// </remarks>
public class LinkAthleteToTrainerUseCase
{
    private IUserRepository _users;

    private ITrainerAthleteRepository _trainerAthletes;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public LinkAthleteToTrainerUseCase(
        IUserRepository users,
        ITrainerAthleteRepository trainerAthletes,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _users = users;
        _trainerAthletes = trainerAthletes;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <returns>The new, active relationship with its trainer and athlete.</returns>
    /// <exception cref="UserNotFoundException">When the trainer or the athlete does not exist.</exception>
    /// <exception cref="InvalidTrainerAthleteRoleException">
    /// When the trainer is not an active trainer or the athlete is not an active athlete.
    /// </exception>
    /// <exception cref="AthleteAlreadyHasTrainerException">When the athlete already has an active trainer.</exception>
    public async Task<TrainerAthleteView> Execute(Id trainerId, Id athleteId, CancellationToken cancellationToken)
    {
        var trainer = await _users.GetById(trainerId, cancellationToken)
            ?? throw new UserNotFoundException(trainerId);

        var athlete = await _users.GetById(athleteId, cancellationToken)
            ?? throw new UserNotFoundException(athleteId);

        if (trainer.CanBeTrainer() == false)
            throw new InvalidTrainerAthleteRoleException("The trainer must be an active user with the TRAINER role");

        if (athlete.CanBeAthlete() == false)
            throw new InvalidTrainerAthleteRoleException("The athlete must be an active user with the USER role");

        if (await _trainerAthletes.ExistsActiveByUserId(athleteId, cancellationToken))
            throw new AthleteAlreadyHasTrainerException(athleteId);

        var relationship = TrainerAthlete.Create(trainerId, athleteId, _clock.UtcNow);

        await _trainerAthletes.Save(relationship, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TrainerAthleteView(relationship, trainer, athlete);
    }
}
