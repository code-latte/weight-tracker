using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

/// <summary>
/// Maps the WorkoutTemplate aggregate across the three tables it spans:
/// <c>WorkoutTemplates</c>, <c>WorkoutTemplateExercises</c> and <c>WorkoutTemplateExerciseSets</c>.
/// Set targets and effort are stored exactly as for workouts, so their conversions are shared with <see cref="WorkoutMapper"/>.
/// </summary>
public static class WorkoutTemplateMapper
{
    public static WorkoutTemplateEntity MapToEntity(WorkoutTemplate template)
    {
        var entity = new WorkoutTemplateEntity
        {
            Id = template.Id.Value,
            TrainerId = template.TrainerId.Value,
            Name = template.Name.Value,
            Description = template.Description?.Value,
        };

        var exercisePosition = 0;

        foreach (var entry in template.Exercises)
        {
            var templateExercise = new WorkoutTemplateExerciseEntity
            {
                Id = entry.Id.Value,
                WorkoutTemplateId = entity.Id,
                ExerciseId = entry.ExerciseId.Value,
                Position = exercisePosition++,
                Effort = (int)entry.Effort,
                PerSide = entry.PerSide,
                RestMinutes = entry.Rest?.Value,
                SuperSeriesId = entry.SuperSeriesId?.Value,
            };

            var setPosition = 0;

            foreach (var prescription in entry.Sets)
            {
                var (targetType, targetValue, targetMaxValue, targetMinutes) = WorkoutMapper.MapTargetToColumns(prescription.target);

                templateExercise.Sets.Add(new WorkoutTemplateExerciseSetEntity
                {
                    Id = Id.New().Value,
                    WorkoutTemplateExerciseId = templateExercise.Id,
                    Position = setPosition++,
                    Count = prescription.count,
                    TargetType = targetType,
                    TargetValue = targetValue,
                    TargetMaxValue = targetMaxValue,
                    TargetMinutes = targetMinutes,
                });
            }

            entity.Exercises.Add(templateExercise);
        }

        return entity;
    }

    /// <summary>
    /// Restores the aggregate.
    /// </summary>
    /// <param name="entity">Template rows, with its exercises and sets loaded.</param>
    public static WorkoutTemplate MapToDomain(WorkoutTemplateEntity entity)
    {
        try
        {
            var exercises = entity.Exercises
                .OrderBy(item => item.Position)
                .Select(item => WorkoutExercise.Rehydrate(
                    Id.From(item.Id),
                    Id.From(item.ExerciseId),
                    WorkoutMapper.MapEffort(item.Effort),
                    item.Sets
                        .OrderBy(set => set.Position)
                        .Select(set => new SetPrescription(
                            set.Count,
                            WorkoutMapper.MapTargetToDomain(set.TargetType, set.TargetValue, set.TargetMaxValue, set.TargetMinutes))),
                    item.SuperSeriesId is null ? null : SuperSeriesId.From(item.SuperSeriesId.Value),
                    item.PerSide,
                    item.RestMinutes is null ? null : TimeInMinutes.From(item.RestMinutes.Value)))
                .ToList();

            return WorkoutTemplate.Rehydrate(
                Id.From(entity.Id),
                Id.From(entity.TrainerId),
                WorkoutName.From(entity.Name),
                WorkoutDescription.From(entity.Description),
                exercises);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored workout template '{entity.Id}' could not be restored", exception);
        }
    }
}
