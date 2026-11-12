using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Workouts;

/// <summary>
/// One set actually performed by the athlete: what was measured (reps or seconds) and, when tracked, with how much weight.
/// </summary>
/// <remarks>
/// Entity of the Workout aggregate, held by a <see cref="WorkoutExercise"/>. It is only added through
/// <see cref="Workout.AddSetRecord"/> and only changed through its root. Two sets with the same result, weight and time are still two sets, so it has identity.
/// </remarks>
public sealed class SetRecord : Entity
{
    /// <summary>
    /// Reps done or seconds held. Its kind matches the prescriptions of the exercise.
    /// </summary>
    public SetResult Result { get; private set; }

    /// <summary>
    /// Weight lifted, in kilograms. Null when the weight is not tracked, which is not the same as 0 (bodyweight).
    /// </summary>
    public Weight? Weight { get; private set; }

    /// <summary>
    /// When the set was registered, in UTC.
    /// </summary>
    public DateTime RegisteredAtUtc { get; }

    private SetRecord(Id id, SetResult result, Weight? weight, DateTime registeredAtUtc, bool isNew)
    {
        this.Id = id;
        this.IsNew = isNew;
        Result = result;
        Weight = weight;
        RegisteredAtUtc = registeredAtUtc;
    }

    /// <summary>
    /// Creates a set record that does not exist in the system yet.
    /// </summary>
    public static SetRecord Create(SetResult result, Weight? weight, DateTime registeredAtUtc)
        => new(Id.New(), result, weight, registeredAtUtc, isNew: true);

    /// <summary>
    /// Restores a set record already stored in the system.
    /// </summary>
    public static SetRecord Rehydrate(Id id, SetResult result, Weight? weight, DateTime registeredAtUtc)
        => new(id, result, weight, registeredAtUtc, isNew: false);

    /// <summary>
    /// Corrects the result and the weight performed. The identity and the registration time never change.
    /// </summary>
    internal void Change(SetResult result, Weight? weight)
    {
        Result = result;
        Weight = weight;
    }
}
