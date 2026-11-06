using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Mesocycles;

public class RemoveMesocycleWeekUseCase
{
    private const string Operation = "DELETE /mesocycles/{id}/weeks/{weekNumber}";

    private IMesocycleRepository _mesocycles;

    private IWorkoutRepository _workouts;

    private MesocycleEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public RemoveMesocycleWeekUseCase(
        IMesocycleRepository mesocycles,
        IWorkoutRepository workouts,
        MesocycleEditAuthorizer authorizer,
        IUnitOfWork unitOfWork
    )
    {
        _mesocycles = mesocycles;
        _workouts = workouts;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Removes a defined week that has no workouts placed in it. The other weeks keep their numbers and the
    /// duration is unchanged.
    /// </summary>
    /// <exception cref="MesocycleNotFoundException">When no mesocycle has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not change the mesocycle.</exception>
    /// <exception cref="MesocycleWeekNotFoundException">When the week is not defined.</exception>
    /// <exception cref="MesocycleWeekHasWorkoutsException">When any workout, whatever its status, is placed in the week.</exception>
    public async Task Execute(
        Id callerId,
        Role callerRole,
        Id mesocycleId,
        WeekNumber weekNumber,
        CancellationToken cancellationToken)
    {
        var mesocycle = await _mesocycles.GetById(mesocycleId, cancellationToken)
            ?? throw new MesocycleNotFoundException(mesocycleId);

        _authorizer.EnsureCanEdit(mesocycle, callerId, callerRole, Operation);

        // Checked before the workouts so an undefined week is always not found, even if workouts point to its number
        if (mesocycle.Weeks.All(week => week.WeekNumber != weekNumber))
            throw new MesocycleWeekNotFoundException(weekNumber, mesocycleId);

        if (await _workouts.ExistsByMesocycleWeek(mesocycleId, weekNumber, cancellationToken))
            throw new MesocycleWeekHasWorkoutsException(weekNumber, mesocycleId);

        mesocycle.RemoveWeek(weekNumber);

        await _mesocycles.Save(mesocycle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
