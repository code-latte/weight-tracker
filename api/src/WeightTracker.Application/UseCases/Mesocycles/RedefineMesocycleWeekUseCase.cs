using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Mesocycles;

public class RedefineMesocycleWeekUseCase
{
    private const string Operation = "PUT /mesocycles/{id}/weeks/{weekNumber}";

    private IMesocycleRepository _mesocycles;

    private MesocycleEditAuthorizer _authorizer;

    private MesocycleViewLoader _views;

    private IUnitOfWork _unitOfWork;

    public RedefineMesocycleWeekUseCase(
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
    /// Replaces the training type and objective of a defined week. Its number is kept.
    /// </summary>
    /// <returns>The mesocycle with its weeks, trainer and athlete.</returns>
    /// <exception cref="MesocycleNotFoundException">When no mesocycle has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not change the mesocycle.</exception>
    /// <exception cref="MesocycleWeekNotFoundException">When the week is not defined.</exception>
    /// <exception cref="InvalidTrainingWeekTypeException">When the type is not a defined value.</exception>
    public async Task<MesocycleView> Execute(
        Id callerId,
        Role callerRole,
        Id mesocycleId,
        WeekNumber weekNumber,
        TrainingWeekType trainingWeekType,
        MesocycleObjective objective,
        CancellationToken cancellationToken)
    {
        var mesocycle = await _mesocycles.GetById(mesocycleId, cancellationToken)
            ?? throw new MesocycleNotFoundException(mesocycleId);

        _authorizer.EnsureCanEdit(mesocycle, callerId, callerRole, Operation);

        mesocycle.RedefineWeek(weekNumber, trainingWeekType, objective);

        await _mesocycles.Save(mesocycle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _views.Load(mesocycle, cancellationToken);
    }
}
