using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

public static class TrainerAthleteMapper
{
    public static TrainerAthleteEntity MapToEntity(TrainerAthlete relationship)
    {
        return new TrainerAthleteEntity
        {
            Id = relationship.Id.Value,
            TrainerId = relationship.TrainerId.Value,
            UserId = relationship.UserId.Value,
            Status = (int)relationship.Status,
            StartedAtUtc = DateTime.SpecifyKind(relationship.StartedAtUtc, DateTimeKind.Utc),
            FinishedAtUtc = relationship.FinishedAtUtc is null
                ? null
                : DateTime.SpecifyKind(relationship.FinishedAtUtc.Value, DateTimeKind.Utc),
        };
    }

    public static TrainerAthlete MapToDomain(TrainerAthleteEntity entity)
    {
        try
        {
            return TrainerAthlete.Rehydrate(
                Id.From(entity.Id),
                Id.From(entity.TrainerId),
                Id.From(entity.UserId),
                MapStatus(entity.Status),
                entity.StartedAtUtc,
                entity.FinishedAtUtc);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored trainer–athlete relationship '{entity.Id}' could not be restored", exception);
        }
    }

    private static TrainerAthleteStatus MapStatus(int status)
    {
        if (Enum.IsDefined(typeof(TrainerAthleteStatus), status) == false)
            throw new PersistenceMappingException($"'{status}' is not a known trainer–athlete status");

        return (TrainerAthleteStatus)status;
    }
}
