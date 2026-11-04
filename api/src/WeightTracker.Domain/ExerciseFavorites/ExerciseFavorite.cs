using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.ExerciseFavorites;

/// <summary>
/// An exercise a user has marked as a favorite, and when.
/// </summary>
/// <remarks>
/// A favorite is created and removed, never edited, and it has no rule of its own: whether the exercise exists
/// and may be seen is checked against the database in Application. The pair (<see cref="UserId"/>,
/// <see cref="ExerciseId"/>) is unique and both are references by <see cref="Id"/>, so neither changes.
/// </remarks>
public sealed class ExerciseFavorite : AggregateRoot
{
    public Id UserId { get; private set; }

    public Id ExerciseId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private ExerciseFavorite(Id id, Id userId, Id exerciseId, DateTime createdAtUtc)
    {
        this.Id = id;
        UserId = userId;
        ExerciseId = exerciseId;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Creates a favorite that does not exist in the system yet.
    /// </summary>
    public static ExerciseFavorite Create(Id userId, Id exerciseId, DateTime nowUtc)
        => new(Id.New(), userId, exerciseId, nowUtc) { IsNew = true };

    /// <summary>
    /// Restores a favorite already stored in the system.
    /// </summary>
    public static ExerciseFavorite Rehydrate(Id id, Id userId, Id exerciseId, DateTime createdAtUtc)
        => new(id, userId, exerciseId, createdAtUtc) { IsNew = false };
}
