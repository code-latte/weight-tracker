using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.Workouts;

public abstract record SetTarget
{
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
        public override string ToString()
        {
            return $"{value} min.";
        }
    }
}