using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

public abstract record SetTarget
{
    /// <summary>
    /// Whether a performed set meets this target. A result of another kind than the target never meets it.
    /// </summary>
    /// <param name="result">What was measured in the set.</param>
    public abstract bool IsMetBy(SetResult result);

    /// <summary>
    /// When we want to do a fixed number of reps (i.e. BenchPress 2x15)
    /// </summary>
    /// <param name="value">Number of reps per set</param>
    public sealed record Reps : SetTarget
    {
        public int value { get; }

        /// <exception cref="InvalidSetPrescriptionException">When the value is lower than 1.</exception>
        public Reps(int value)
        {
            if (value < 1)
                throw new InvalidSetPrescriptionException($"reps must be at least 1, got {value}");

            this.value = value;
        }

        /// <summary>
        /// Met by the target reps or more.
        /// </summary>
        public override bool IsMetBy(SetResult result)
            => result is SetResult.Reps reps && reps.Count.Value >= value;

        public override string ToString()
        {
            return $"{value.ToString()} reps.";
        }
    }

    /// <summary>
    /// When we want to do a fixed amount of time (i.e. Plank 3x40)
    /// </summary>
    /// <param name="value">Number of seconds per set</param>
    public sealed record Duration : SetTarget
    {
        public int value { get; }

        /// <exception cref="InvalidSetPrescriptionException">When the value is lower than 1.</exception>
        public Duration(int value)
        {
            if (value < 1)
                throw new InvalidSetPrescriptionException($"duration must be at least 1 second, got {value}");

            this.value = value;
        }

        /// <summary>
        /// Met by the target seconds or more.
        /// </summary>
        public override bool IsMetBy(SetResult result)
            => result is SetResult.Duration duration && duration.Value.Value >= value;

        public override string ToString()
        {
            return $"{value.ToString()} s.";
        }
    }

    /// <summary>
    /// When we want to do the exercise until we dead (i.e. LegCurl 2xMax)
    /// </summary>
    public sealed record MaxReps() : SetTarget
    {
        /// <summary>
        /// Always met by a reps result, whatever the reps.
        /// </summary>
        public override bool IsMetBy(SetResult result)
            => result is SetResult.Reps;

        public override string ToString()
        {
            return $"max.";
        }
    }

    /// <summary>
    /// When any number of reps between two bounds completes the set (i.e. Squat 3x8-12)
    /// </summary>
    /// <param name="min">Minimum number of reps per set</param>
    /// <param name="max">Maximum number of reps per set</param>
    public sealed record RepsRange : SetTarget
    {
        public int min { get; }

        public int max { get; }

        /// <exception cref="InvalidSetPrescriptionException">When the min is lower than 1 or the max is not greater than the min.</exception>
        public RepsRange(int min, int max)
        {
            if (min < 1)
                throw new InvalidSetPrescriptionException($"min reps must be at least 1, got {min}");

            if (max <= min)
                throw new InvalidSetPrescriptionException($"max reps must be greater than min reps, got {min}-{max}");

            this.min = min;
            this.max = max;
        }

        /// <summary>
        /// Met by the minimum reps or more; going above the maximum still counts.
        /// </summary>
        public override bool IsMetBy(SetResult result)
            => result is SetResult.Reps reps && reps.Count.Value >= min;

        public override string ToString()
        {
            return $"{min.ToString()}-{max.ToString()} reps.";
        }
    }

    /// <summary>
    /// When we want to do a fixed amount of time expressed in minutes (i.e. Bike 30 minutes)
    /// </summary>
    /// <param name="value">Minutes per set, with up to two decimals</param>
    public sealed record Minutes(TimeInMinutes value) : SetTarget
    {
        /// <summary>
        /// Met by the target minutes or more, measured in seconds.
        /// </summary>
        public override bool IsMetBy(SetResult result)
            => result is SetResult.Duration duration && duration.Value.Value >= value.Value * 60;

        public override string ToString()
        {
            return $"{value} min.";
        }
    }
}