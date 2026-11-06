using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Mesocycles;

public class CreateMesocycleUseCase
{
    private const string Operation = "POST /mesocycles";

    private IMesocycleRepository _mesocycles;

    private ITrainerAthleteRepository _relationships;

    private MesocycleAssignmentValidator _assignment;

    private MesocycleViewLoader _views;

    private IUnitOfWork _unitOfWork;

    public CreateMesocycleUseCase(
        IMesocycleRepository mesocycles,
        ITrainerAthleteRepository relationships,
        MesocycleAssignmentValidator assignment,
        MesocycleViewLoader views,
        IUnitOfWork unitOfWork
    )
    {
        _mesocycles = mesocycles;
        _relationships = relationships;
        _assignment = assignment;
        _views = views;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates a mesocycle with no week defined. A trainer creates it for an athlete they actively train, with
    /// themselves as trainer; an admin creates it for any trainer and athlete.
    /// </summary>
    /// <param name="trainerId">Requested trainer. Required for an admin, refused for a trainer.</param>
    /// <returns>The created mesocycle with its trainer and athlete.</returns>
    /// <exception cref="UnauthorizedOperationException">When the caller is a regular user, or a trainer without an active relationship with the athlete.</exception>
    /// <exception cref="InvalidMesocycleAssignmentException">When the trainer id is sent by a trainer or missing for an admin, or the trainer or athlete cannot take part.</exception>
    /// <exception cref="UserNotFoundException">When the athlete does not exist.</exception>
    /// <exception cref="MesocycleTrainerIsAthleteException">When the trainer is the athlete.</exception>
    public async Task<MesocycleView> Execute(
        Id callerId,
        Role callerRole,
        Id? trainerId,
        Id athleteId,
        MesocycleName name,
        MesocycleObjective objective,
        DateTime startDate,
        DurationInWeeks durationInWeeks,
        CancellationToken cancellationToken)
    {
        var resolvedTrainerId = ResolveTrainer(callerId, callerRole, trainerId);

        await _assignment.EnsureAthlete(athleteId, cancellationToken);

        if (callerRole == Role.ADMIN)
            await _assignment.EnsureTrainer(resolvedTrainerId, cancellationToken);
        else if (await _relationships.GetActiveByTrainerIdAndUserId(callerId, athleteId, cancellationToken) is null)
            throw new UnauthorizedOperationException(Operation);

        var mesocycle = Mesocycle.Create(resolvedTrainerId, athleteId, name, objective, startDate, durationInWeeks);

        await _mesocycles.Save(mesocycle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _views.Load(mesocycle, cancellationToken);
    }

    /// <summary>
    /// Decides the trainer from the caller's role, before any lookup.
    /// </summary>
    private static Id ResolveTrainer(Id callerId, Role callerRole, Id? trainerId)
    {
        switch (callerRole)
        {
            case Role.TRAINER:
                if (trainerId is not null)
                    throw new InvalidMesocycleAssignmentException("Only an admin can set the trainer of a mesocycle");

                return callerId;

            case Role.ADMIN:
                return trainerId
                    ?? throw new InvalidMesocycleAssignmentException("The trainer of the mesocycle must be specified");

            default:
                throw new UnauthorizedOperationException(Operation);
        }
    }
}
