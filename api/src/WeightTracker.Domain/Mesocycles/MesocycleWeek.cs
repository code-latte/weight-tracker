using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Mesocycles;

/// <summary>
/// One week (microcycle) of a mesocycle: what kind of training it is and what it is meant to achieve.
/// </summary>
/// <remarks>
/// Entity of the Mesocycle aggregate. It is only created, loaded and saved through <see cref="Mesocycle"/>.
/// </remarks>
public sealed class MesocycleWeek : Entity
{
    /// <summary>
    /// Position of the week inside its mesocycle. Immutable once the week exists.
    /// </summary>
    public WeekNumber WeekNumber { get; }

    public TrainingWeekType TrainingWeekType { get; private set; }

    public MesocycleObjective Objective { get; private set; }

    private MesocycleWeek(Id id, WeekNumber weekNumber, TrainingWeekType trainingWeekType, MesocycleObjective objective, bool isNew)
    {
        EnsureDefined(trainingWeekType);

        this.Id = id;
        this.IsNew = isNew;
        WeekNumber = weekNumber;
        TrainingWeekType = trainingWeekType;
        Objective = objective;
    }

    /// <summary>
    /// Creates a week that does not exist in the system yet. Only <see cref="Mesocycle"/> calls it,
    /// because the week number must fit the mesocycle duration.
    /// </summary>
    /// <exception cref="InvalidTrainingWeekTypeException">When the type is not a defined value.</exception>
    internal static MesocycleWeek Create(WeekNumber weekNumber, TrainingWeekType trainingWeekType, MesocycleObjective objective)
        => new(Id.New(), weekNumber, trainingWeekType, objective, isNew: true);

    /// <summary>
    /// Restores a week already stored in the system.
    /// </summary>
    /// <exception cref="InvalidTrainingWeekTypeException">When the type is not a defined value.</exception>
    public static MesocycleWeek Rehydrate(Id id, WeekNumber weekNumber, TrainingWeekType trainingWeekType, MesocycleObjective objective)
        => new(id, weekNumber, trainingWeekType, objective, isNew: false);

    /// <summary>
    /// Replaces the type and the objective of the week. The week number is kept.
    /// Nothing changes when the type is invalid.
    /// </summary>
    /// <exception cref="InvalidTrainingWeekTypeException">When the type is not a defined value.</exception>
    internal void Redefine(TrainingWeekType trainingWeekType, MesocycleObjective objective)
    {
        EnsureDefined(trainingWeekType);

        TrainingWeekType = trainingWeekType;
        Objective = objective;
    }

    private static void EnsureDefined(TrainingWeekType trainingWeekType)
    {
        if (Enum.IsDefined(trainingWeekType) == false)
            throw new InvalidTrainingWeekTypeException((int)trainingWeekType);
    }
}
