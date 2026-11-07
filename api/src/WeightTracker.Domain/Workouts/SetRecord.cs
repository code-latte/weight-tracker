using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// One set actually performed by the athlete: how many reps and with how much weight.
/// </summary>
/// <remarks>
/// Entity of the Workout aggregate, held by a <see cref="WorkoutExercise"/>. It is only added through
/// <see cref="Workout.AddSetRecord"/> and only changed through its root. Two sets with the same reps, weight and time are still two sets, so it has identity.
/// </remarks>
public sealed class SetRecord : Entity
{
    public RepCount Reps { get; private set; }

    /// <summary>
    /// Weight lifted, in kilograms.
    /// </summary>
    public Weight Weight { get; private set; }

    /// <summary>
    /// When the set was registered, in UTC.
    /// </summary>
    public DateTime RegisteredAtUtc { get; }

    private SetRecord(Id id, RepCount reps, Weight weight, DateTime registeredAtUtc, bool isNew)
    {
        this.Id = id;
        this.IsNew = isNew;
        Reps = reps;
        Weight = weight;
        RegisteredAtUtc = registeredAtUtc;
    }

    /// <summary>
    /// Creates a set record that does not exist in the system yet.
    /// </summary>
    public static SetRecord Create(RepCount reps, Weight weight, DateTime registeredAtUtc)
        => new(Id.New(), reps, weight, registeredAtUtc, isNew: true);

    /// <summary>
    /// Restores a set record already stored in the system.
    /// </summary>
    public static SetRecord Rehydrate(Id id, RepCount reps, Weight weight, DateTime registeredAtUtc)
        => new(id, reps, weight, registeredAtUtc, isNew: false);

    /// <summary>
    /// Corrects the reps and weight performed. The identity and the registration time never change.
    /// </summary>
    internal void Change(RepCount reps, Weight weight)
    {
        Reps = reps;
        Weight = weight;
    }
}
