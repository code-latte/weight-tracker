using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Workouts;
using WeightTracker.Domain.Workouts.Observations;

namespace WeightTracker.Unit.Domain.Workouts.Observations;

/// <summary>
/// Builders shared by the observation rule tests: a completed prescribed workout, and earlier sessions as executions.
/// </summary>
internal static class ObservationTestData
{
    public static readonly DateTime CreatedAt = new(2026, 11, 13, 10, 0, 0, DateTimeKind.Utc);

    public static readonly DateOnly Today = new(2026, 11, 13);

    public static WorkoutObservationGenerator Generator() => new(new ObservationThresholds(3, 4, 3, 70, 15, 5, 10));

    public static SetPrescription Sets(int count, int reps) => new(count, new SetTarget.Reps(reps));

    public static SetPrescription Range(int count, int min, int max) => new(count, new SetTarget.RepsRange(min, max));

    public static SetPrescription Max(int count) => new(count, new SetTarget.MaxReps());

    public static SetPrescription Timed(int count, int seconds) => new(count, new SetTarget.Duration(seconds));

    /// <summary>A recorded set of reps, with an optional weight.</summary>
    public static SetRecord Rep(int reps, decimal? weight = null)
        => SetRecord.Create(new SetResult.Reps(RepCount.From(reps)), weight is null ? null : Weight.From(weight.Value), CreatedAt);

    public static SetRecord Held(int seconds, decimal? weight = null)
        => SetRecord.Create(new SetResult.Duration(DurationInSeconds.From(seconds)), weight is null ? null : Weight.From(weight.Value), CreatedAt);

    /// <summary>One exercise of the current workout: what was prescribed and what was recorded.</summary>
    public sealed record Entry(Id ExerciseId, SetPrescription[] Prescriptions, SetRecord[] Records, bool PerSide = false);

    public static Entry Exercise(Id exerciseId, SetPrescription[] prescriptions, params SetRecord[] records)
        => new(exerciseId, prescriptions, records);

    public static Workout CompletedWorkout(params Entry[] entries)
        => Workout.Rehydrate(
            Id.New(), Id.New(), Id.New(), WorkoutName.From("Leg day"), null, new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc),
            WorkoutStatus.COMPLETED,
            entries.Select(entry => WorkoutExercise.Rehydrate(
                Id.New(), entry.ExerciseId, Effort.MID, entry.Prescriptions, null, entry.PerSide, null, entry.Records)));

    /// <summary>
    /// An earlier session of the exercise. <paramref name="targets"/> is how many sets of how many reps were prescribed.
    /// </summary>
    public static ExerciseExecution Earlier(
        int daysAgo,
        (int Sets, int Reps) targets,
        (int Reps, decimal? Weight)[] sets,
        TrainingWeekType? weekType = null,
        bool perSide = false)
        => new(
            Id.New(),
            Today.AddDays(-daysAgo),
            weekType,
            perSide,
            Enumerable.Repeat<SetTarget>(new SetTarget.Reps(targets.Reps), targets.Sets),
            sets.Select(set => new ExecutionSet(
                new SetResult.Reps(RepCount.From(set.Reps)), set.Weight is null ? null : Weight.From(set.Weight.Value))));

    /// <summary>An earlier session with 3 prescribed sets of 10, whose best set weighed <paramref name="top"/>.</summary>
    public static ExerciseExecution TopOf(int daysAgo, decimal top, int reps = 10, TrainingWeekType? weekType = null)
        => Earlier(daysAgo, (3, 10), [(reps, top), (reps, top), (reps, top)], weekType);

    public static IReadOnlyDictionary<Id, ExerciseHistory> HistoryOf(Id exerciseId, params ExerciseExecution[] newestFirst)
        => new Dictionary<Id, ExerciseHistory> { [exerciseId] = ExerciseHistory.Of(newestFirst) };

    public static IReadOnlyDictionary<Id, ExerciseHistory> NoHistory => new Dictionary<Id, ExerciseHistory>();

    public static WorkoutObservation Observation(GeneratedObservations result, ObservationCode code, Id? exerciseId = null)
        => result.Observations.SingleOrDefault(observation => observation.Code == code && observation.ExerciseId == exerciseId)
            ?? throw new InvalidOperationException($"no {code} observation");

    public static bool Has(GeneratedObservations result, ObservationCode code, Id? exerciseId = null)
        => result.Observations.Any(observation => observation.Code == code && observation.ExerciseId == exerciseId);
}
