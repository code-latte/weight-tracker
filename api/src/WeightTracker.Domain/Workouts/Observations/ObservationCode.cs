namespace WeightTracker.Domain.Workouts.Observations;

/// <summary>
/// What an observation states. The names are public business codes stored and returned as they are: once released,
/// a code is never renamed or reused with another meaning, and the declaration order is the display order within an exercise.
/// </summary>
public enum ObservationCode
{
    WORKOUT_INCOMPLETE = 1,
    EXERCISE_SKIPPED = 2,
    REPS_BELOW_TARGET = 3,
    REPS_ABOVE_TARGET = 4,
    ALL_TARGETS_MET = 5,
    WEIGHT_INCREASE = 6,
    WEIGHT_DECREASE = 7,
    WEIGHT_PERSONAL_RECORD = 8,
    REPS_PERSONAL_RECORD = 9,
    VOLUME_INCREASE = 10,
    VOLUME_DECREASE = 11,
    REPS_INCREASE = 12,
    REPS_DECREASE = 13,
    REPEATED_TARGET_MISS = 14,
    WEIGHT_PLATEAU = 15,
    WEIGHT_UPTREND = 16,
    WEIGHT_DOWNTREND = 17,
}
