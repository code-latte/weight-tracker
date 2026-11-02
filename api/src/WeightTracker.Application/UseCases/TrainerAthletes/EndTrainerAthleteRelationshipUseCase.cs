using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.TrainerAthletes;

public class EndTrainerAthleteRelationshipUseCase
{
    private ITrainerAthleteRepository _trainerAthletes;

    private TrainerAthleteViewLoader _views;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public EndTrainerAthleteRelationshipUseCase(
        ITrainerAthleteRepository trainerAthletes,
        TrainerAthleteViewLoader views,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _trainerAthletes = trainerAthletes;
        _views = views;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Ends the active relationship between a trainer and an athlete. An admin may end any; otherwise only the
    /// trainer or the athlete of that relationship. The permission check runs before the lookup, so a stranger
    /// learns nothing about the relationship.
    /// </summary>
    /// <returns>The ended relationship with its trainer and athlete.</returns>
    /// <exception cref="UnauthorizedOperationException">When the caller is neither an admin nor one of the two sides.</exception>
    /// <exception cref="TrainerAthleteNotFoundException">When there is no active relationship between them.</exception>
    /// <exception cref="InvalidTrainerAthleteStatusTransitionException">When the relationship already ended.</exception>
    public async Task<TrainerAthleteView> Execute(
        Id callerId,
        Role callerRole,
        Id trainerId,
        Id athleteId,
        CancellationToken cancellationToken)
    {
        if (callerRole != Role.ADMIN && callerId != trainerId && callerId != athleteId)
            throw new UnauthorizedOperationException("PUT /trainer-athletes/{trainerId}/end");

        var relationship = await _trainerAthletes.GetActiveByTrainerIdAndUserId(trainerId, athleteId, cancellationToken)
            ?? throw new TrainerAthleteNotFoundException(trainerId, athleteId);

        var view = await _views.Load(relationship, cancellationToken);

        relationship.End(_clock.UtcNow);

        await _trainerAthletes.Save(relationship, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return view;
    }
}
