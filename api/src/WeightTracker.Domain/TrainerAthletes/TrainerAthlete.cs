using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Domain.TrainerAthletes;

/// <summary>
/// A period during which a trainer prepares an athlete. It starts active and ends once; ended relationships are
/// kept as history, so the same pair can be linked again with a new relationship.
/// </summary>
/// <remarks>
/// The aggregate only holds the users' ids. Whether they have the right roles, and whether the athlete already has
/// an active trainer, span other aggregates and are checked in Application.
/// </remarks>
public sealed class TrainerAthlete : AggregateRoot
{
    public Id TrainerId { get; private set; }

    public Id UserId { get; private set; }

    public TrainerAthleteStatus Status { get; private set; }

    public DateTime StartedAtUtc { get; private set; }

    public DateTime? FinishedAtUtc { get; private set; }

    private TrainerAthlete(
        Id id,
        Id trainerId,
        Id userId,
        TrainerAthleteStatus status,
        DateTime startedAtUtc,
        DateTime? finishedAtUtc
    )
    {
        this.Id = id;
        TrainerId = trainerId;
        UserId = userId;
        Status = status;
        StartedAtUtc = startedAtUtc;
        FinishedAtUtc = finishedAtUtc;
    }

    /// <summary>
    /// Starts a new active relationship.
    /// </summary>
    public static TrainerAthlete Create(Id trainerId, Id userId, DateTime nowUtc)
    {
        return new TrainerAthlete(Id.New(), trainerId, userId, TrainerAthleteStatus.ACTIVE, nowUtc, null)
        {
            IsNew = true,
        };
    }

    /// <exception cref="InvalidTrainerAthleteStateException">
    /// When <paramref name="finishedAtUtc"/> is set on an active relationship, missing on an ended one, or earlier
    /// than <paramref name="startedAtUtc"/>.
    /// </exception>
    public static TrainerAthlete Rehydrate(
        Id id,
        Id trainerId,
        Id userId,
        TrainerAthleteStatus status,
        DateTime startedAtUtc,
        DateTime? finishedAtUtc
    )
    {
        if ((status == TrainerAthleteStatus.ENDED) != (finishedAtUtc is not null) || finishedAtUtc < startedAtUtc)
            throw new InvalidTrainerAthleteStateException(id);

        return new TrainerAthlete(id, trainerId, userId, status, startedAtUtc, finishedAtUtc)
        {
            IsNew = false,
        };
    }

    /// <summary>
    /// Ends the relationship and records when.
    /// </summary>
    /// <exception cref="InvalidTrainerAthleteStatusTransitionException">When the relationship already ended.</exception>
    public void End(DateTime nowUtc)
    {
        if (Status != TrainerAthleteStatus.ACTIVE)
            throw new InvalidTrainerAthleteStatusTransitionException(Status, TrainerAthleteStatus.ENDED);

        Status = TrainerAthleteStatus.ENDED;
        FinishedAtUtc = nowUtc;
    }
}
