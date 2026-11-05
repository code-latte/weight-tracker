using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

/// <summary>
/// Maps the Mesocycle aggregate across the two tables it spans: <c>Mesocycles</c> and <c>MesocycleWeeks</c>.
/// </summary>
public static class MesocycleMapper
{
    public static MesocycleEntity MapToEntity(Mesocycle mesocycle)
    {
        var entity = new MesocycleEntity
        {
            Id = mesocycle.Id.Value,
            TrainerId = mesocycle.TrainerId.Value,
            AthleteId = mesocycle.AthleteId.Value,
            Name = mesocycle.Name.Value,
            Objective = mesocycle.Objective.Value,
            StartDate = DateOnly.FromDateTime(mesocycle.StartDate),
            DurationInWeeks = mesocycle.DurationInWeeks.Value,
        };

        foreach (var week in mesocycle.Weeks)
        {
            entity.Weeks.Add(new MesocycleWeekEntity
            {
                Id = week.Id.Value,
                MesocycleId = entity.Id,
                WeekNumber = week.WeekNumber.Value,
                TrainingWeekType = (int)week.TrainingWeekType,
                Objective = week.Objective.Value,
            });
        }

        return entity;
    }

    /// <summary>
    /// Restores the aggregate.
    /// </summary>
    /// <param name="entity">Mesocycle row, with its weeks loaded.</param>
    public static Mesocycle MapToDomain(MesocycleEntity entity)
    {
        try
        {
            var weeks = entity.Weeks
                .Select(week => MesocycleWeek.Rehydrate(
                    Id.From(week.Id),
                    WeekNumber.From(week.WeekNumber),
                    MapTrainingWeekType(week.TrainingWeekType),
                    MesocycleObjective.From(week.Objective)))
                .ToList();

            return Mesocycle.Rehydrate(
                Id.From(entity.Id),
                Id.From(entity.TrainerId),
                Id.From(entity.AthleteId),
                MesocycleName.From(entity.Name),
                MesocycleObjective.From(entity.Objective),
                entity.StartDate.ToDateTime(TimeOnly.MinValue),
                DurationInWeeks.From(entity.DurationInWeeks),
                weeks);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored mesocycle '{entity.Id}' could not be restored", exception);
        }
    }

    private static TrainingWeekType MapTrainingWeekType(int trainingWeekType)
    {
        if (Enum.IsDefined(typeof(TrainingWeekType), trainingWeekType) == false)
            throw new PersistenceMappingException($"'{trainingWeekType}' is not a known training week type");

        return (TrainingWeekType)trainingWeekType;
    }
}
