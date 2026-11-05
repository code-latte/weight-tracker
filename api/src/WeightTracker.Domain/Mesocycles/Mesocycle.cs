using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Mesocycles;

/// <summary>
/// Training plan from a trainer to an athlete: a block of weeks with an objective, where each week can be
/// defined with its own type and objective.
/// </summary>
/// <remarks>
/// Users are referenced by identifier only. Workouts point to a mesocycle week by identifier and week number;
/// the mesocycle never holds its workouts.
/// </remarks>
public sealed class Mesocycle : AggregateRoot
{
    private DateTime _startDate;

    private List<MesocycleWeek> _weeks;

    /// <summary>
    /// Trainer who prepares the plan. Immutable.
    /// </summary>
    public Id TrainerId { get; }

    /// <summary>
    /// Athlete the plan is for. Immutable.
    /// </summary>
    public Id AthleteId { get; }

    public MesocycleName Name { get; private set; }

    public MesocycleObjective Objective { get; private set; }

    /// <summary>
    /// Day the mesocycle starts. Any day of the week is valid. Time is not kept.
    /// </summary>
    public DateTime StartDate => _startDate;

    public DurationInWeeks DurationInWeeks { get; private set; }

    /// <summary>
    /// Snapshot of the defined weeks, ordered by week number. Weeks may be missing while the plan is being built.
    /// The list is copied so callers cannot mutate the aggregate through it.
    /// </summary>
    public IReadOnlyList<MesocycleWeek> Weeks => _weeks.OrderBy(week => week.WeekNumber.Value).ToList();

    private Mesocycle(
        Id trainerId,
        Id athleteId,
        MesocycleName name,
        MesocycleObjective objective,
        DateTime startDate,
        DurationInWeeks durationInWeeks)
    {
        EnsureTrainerIsNotAthlete(trainerId, athleteId);

        this.Id = Id.New();
        TrainerId = trainerId;
        AthleteId = athleteId;
        Name = name;
        Objective = objective;
        _startDate = startDate.Date;
        DurationInWeeks = durationInWeeks;
        _weeks = new List<MesocycleWeek>();
    }

    /// <summary>
    /// Creates a mesocycle from a trainer to an athlete, with no week defined yet.
    /// </summary>
    /// <exception cref="MesocycleTrainerIsAthleteException">When the trainer is the athlete.</exception>
    public static Mesocycle Create(
        Id trainerId,
        Id athleteId,
        MesocycleName name,
        MesocycleObjective objective,
        DateTime startDate,
        DurationInWeeks durationInWeeks)
        => new(trainerId, athleteId, name, objective, startDate, durationInWeeks)
        {
            IsNew = true,
        };

    /// <summary>
    /// Restores a mesocycle already stored in the system.
    /// </summary>
    /// <exception cref="MesocycleTrainerIsAthleteException">When the trainer is the athlete.</exception>
    /// <exception cref="DuplicateMesocycleWeekException">When two weeks share the same number.</exception>
    /// <exception cref="MesocycleWeekOutOfRangeException">When a week is beyond the duration.</exception>
    public static Mesocycle Rehydrate(
        Id id,
        Id trainerId,
        Id athleteId,
        MesocycleName name,
        MesocycleObjective objective,
        DateTime startDate,
        DurationInWeeks durationInWeeks,
        IEnumerable<MesocycleWeek> weeks)
    {
        var entries = weeks.ToList();

        var duplicate = entries
            .GroupBy(week => week.WeekNumber)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
            throw new DuplicateMesocycleWeekException(duplicate.Key, id);

        var beyond = entries.FirstOrDefault(week => week.WeekNumber.Value > durationInWeeks.Value);

        if (beyond is not null)
            throw new MesocycleWeekOutOfRangeException(beyond.WeekNumber, durationInWeeks.Value, id);

        return new Mesocycle(trainerId, athleteId, name, objective, startDate, durationInWeeks)
        {
            Id = id,
            IsNew = false,
            _weeks = entries,
        };
    }

    public void Rename(MesocycleName name)
    {
        Name = name;
    }

    public void ChangeObjective(MesocycleObjective objective)
    {
        Objective = objective;
    }

    /// <summary>
    /// Moves the start of the mesocycle. Weeks and duration are untouched.
    /// </summary>
    /// <param name="startDate">New first day. Time is not kept.</param>
    public void Reschedule(DateTime startDate)
    {
        _startDate = startDate.Date;
    }

    /// <summary>
    /// Changes how many weeks the mesocycle lasts. Defined weeks are never dropped to make it fit.
    /// </summary>
    /// <exception cref="MesocycleDurationTooShortException">When a defined week is beyond the new duration.</exception>
    public void ChangeDuration(DurationInWeeks durationInWeeks)
    {
        var highest = _weeks.MaxBy(week => week.WeekNumber.Value);

        if (highest is not null && highest.WeekNumber.Value > durationInWeeks.Value)
            throw new MesocycleDurationTooShortException(durationInWeeks.Value, highest.WeekNumber, this.Id);

        DurationInWeeks = durationInWeeks;
    }

    /// <summary>
    /// Defines a week, or redefines it when its number already exists. Weeks can be defined in any order and
    /// leave gaps.
    /// </summary>
    /// <exception cref="MesocycleWeekOutOfRangeException">When the week is beyond the duration.</exception>
    /// <exception cref="InvalidTrainingWeekTypeException">When the type is not a defined value.</exception>
    public void DefineWeek(WeekNumber weekNumber, TrainingWeekType trainingWeekType, MesocycleObjective objective)
    {
        if (weekNumber.Value > DurationInWeeks.Value)
            throw new MesocycleWeekOutOfRangeException(weekNumber, DurationInWeeks.Value, this.Id);

        var existing = _weeks.FirstOrDefault(week => week.WeekNumber == weekNumber);

        if (existing is not null)
        {
            existing.Redefine(trainingWeekType, objective);
            return;
        }

        _weeks.Add(MesocycleWeek.Create(weekNumber, trainingWeekType, objective));
    }

    /// <summary>
    /// Removes a defined week. The other weeks keep their numbers.
    /// </summary>
    /// <exception cref="MesocycleWeekNotFoundException">When the week is not defined.</exception>
    public void RemoveWeek(WeekNumber weekNumber)
    {
        var existing = _weeks.FirstOrDefault(week => week.WeekNumber == weekNumber)
            ?? throw new MesocycleWeekNotFoundException(weekNumber, this.Id);

        _weeks.Remove(existing);
    }

    private static void EnsureTrainerIsNotAthlete(Id trainerId, Id athleteId)
    {
        if (trainerId == athleteId)
            throw new MesocycleTrainerIsAthleteException(trainerId);
    }
}
