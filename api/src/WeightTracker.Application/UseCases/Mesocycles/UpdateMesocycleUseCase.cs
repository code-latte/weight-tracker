using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Mesocycles;

public class UpdateMesocycleUseCase
{
    private const string Operation = "PUT /mesocycles/{id}";

    private IMesocycleRepository _mesocycles;

    private MesocycleEditAuthorizer _authorizer;

    private MesocycleViewLoader _views;

    private IUnitOfWork _unitOfWork;

    public UpdateMesocycleUseCase(
        IMesocycleRepository mesocycles,
        MesocycleEditAuthorizer authorizer,
        MesocycleViewLoader views,
        IUnitOfWork unitOfWork
    )
    {
        _mesocycles = mesocycles;
        _authorizer = authorizer;
        _views = views;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Replaces the name, objective, start date and duration of a mesocycle. Trainer, athlete and weeks are untouched.
    /// </summary>
    /// <returns>The updated mesocycle with its trainer and athlete.</returns>
    /// <exception cref="MesocycleNotFoundException">When no mesocycle has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not change the mesocycle.</exception>
    /// <exception cref="MesocycleDurationTooShortException">When a defined week is beyond the new duration.</exception>
    public async Task<MesocycleView> Execute(
        Id callerId,
        Role callerRole,
        Id mesocycleId,
        MesocycleName name,
        MesocycleObjective objective,
        DateTime startDate,
        DurationInWeeks durationInWeeks,
        CancellationToken cancellationToken)
    {
        var mesocycle = await _mesocycles.GetById(mesocycleId, cancellationToken)
            ?? throw new MesocycleNotFoundException(mesocycleId);

        _authorizer.EnsureCanEdit(mesocycle, callerId, callerRole, Operation);

        // The duration goes first: it is the only change that can be refused, and nothing is saved when it is
        mesocycle.ChangeDuration(durationInWeeks);
        mesocycle.Rename(name);
        mesocycle.ChangeObjective(objective);
        mesocycle.Reschedule(startDate);

        await _mesocycles.Save(mesocycle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _views.Load(mesocycle, cancellationToken);
    }
}
