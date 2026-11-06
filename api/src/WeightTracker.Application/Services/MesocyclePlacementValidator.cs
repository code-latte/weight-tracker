using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Checks that a workout may be placed in a mesocycle week. Shared by creating and updating a workout, so both apply
/// the same rules spanning the two aggregates.
/// </summary>
/// <remarks>
/// Placing a workout changes the plan, so it needs the same permission as editing the mesocycle. The workout must
/// belong to the mesocycle's athlete and, when it has a trainer, to the mesocycle's trainer. The week only needs to be
/// within the duration: weeks may be placed before they are defined. A placed workout is dated by its week, so the
/// date a request sends only counts for a standalone workout.
/// </remarks>
public class MesocyclePlacementValidator
{
    private IMesocycleRepository _mesocycles;

    private MesocycleEditAuthorizer _authorizer;

    public MesocyclePlacementValidator(IMesocycleRepository mesocycles, MesocycleEditAuthorizer authorizer)
    {
        _mesocycles = mesocycles;
        _authorizer = authorizer;
    }

    /// <summary>
    /// Refuses a request that names only one of the two placement values.
    /// </summary>
    /// <exception cref="IncompleteMesocyclePlacementException">When exactly one of the two is <c>null</c>.</exception>
    public static void EnsureComplete(Id? mesocycleId, WeekNumber? weekNumber)
    {
        if ((mesocycleId is null) != (weekNumber is null))
            throw new IncompleteMesocyclePlacementException();
    }

    /// <summary>
    /// Refuses a standalone workout without a date. A placed workout needs none, since its week dates it.
    /// </summary>
    /// <exception cref="MissingWorkoutDateException">When there is no placement and no date.</exception>
    public static void EnsureDateOrPlacement(Id? mesocycleId, DateTime? workoutDate)
    {
        if (mesocycleId is null && workoutDate is null)
            throw new MissingWorkoutDateException();
    }

    /// <summary>
    /// Date the workout takes: the date of its mesocycle week when placed, ignoring the sent one, or the sent one
    /// when standalone. Does not check permissions; call it once the placement has been allowed.
    /// </summary>
    /// <param name="workoutDate">Date sent by the request. Required when there is no placement.</param>
    /// <exception cref="MissingWorkoutDateException">When there is no placement and no date.</exception>
    /// <exception cref="MesocycleNotFoundException">When no mesocycle has the given id.</exception>
    /// <exception cref="MesocycleWeekOutOfRangeException">When the week is beyond the mesocycle's duration.</exception>
    public async Task<DateTime> ResolveWorkoutDate(
        Id? mesocycleId,
        WeekNumber? weekNumber,
        DateTime? workoutDate,
        CancellationToken cancellationToken)
    {
        if (mesocycleId is null || weekNumber is null)
            return workoutDate ?? throw new MissingWorkoutDateException();

        var mesocycle = await _mesocycles.GetById(mesocycleId, cancellationToken)
            ?? throw new MesocycleNotFoundException(mesocycleId);

        return mesocycle.DateOfWeek(weekNumber);
    }

    /// <param name="ownerId">Owner the workout has once the request is applied.</param>
    /// <param name="trainerId">Trainer the workout has once the request is applied, <c>null</c> for none.</param>
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="MesocycleNotFoundException">When no mesocycle has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller may not change the mesocycle.</exception>
    /// <exception cref="MesocycleAthleteMismatchException">When the owner is not the mesocycle's athlete.</exception>
    /// <exception cref="MesocycleTrainerMismatchException">When the workout has a trainer who is not the mesocycle's trainer.</exception>
    /// <exception cref="MesocycleWeekOutOfRangeException">When the week is beyond the mesocycle's duration.</exception>
    public async Task EnsureCanPlace(
        Id ownerId,
        Id? trainerId,
        Id mesocycleId,
        WeekNumber weekNumber,
        Id callerId,
        Role callerRole,
        string operation,
        CancellationToken cancellationToken)
    {
        var mesocycle = await _mesocycles.GetById(mesocycleId, cancellationToken)
            ?? throw new MesocycleNotFoundException(mesocycleId);

        _authorizer.EnsureCanEdit(mesocycle, callerId, callerRole, operation);

        if (ownerId != mesocycle.AthleteId)
            throw new MesocycleAthleteMismatchException(ownerId, mesocycleId);

        if (trainerId is not null && trainerId != mesocycle.TrainerId)
            throw new MesocycleTrainerMismatchException(trainerId, mesocycleId);

        if (weekNumber.Value > mesocycle.DurationInWeeks.Value)
            throw new MesocycleWeekOutOfRangeException(weekNumber, mesocycle.DurationInWeeks.Value, mesocycleId);
    }
}
