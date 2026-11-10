using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

/// <summary>
/// Maps the Workout aggregate across the four tables it spans:
/// <c>workouts</c>, <c>workout_exercises</c>, <c>workout_exercise_sets</c> and <c>SetRecords</c>.
/// </summary>
public static class WorkoutMapper
{
    public static WorkoutEntity MapToEntity(Workout workout)
    {
        var entity = new WorkoutEntity
        {
            Id = workout.Id.Value,
            UserId = workout.UserId.Value,
            TrainerId = workout.TrainerId?.Value,
            WorkoutDate = DateOnly.FromDateTime(workout.WorkoutDate),
            Name = workout.Name.Value,
            Description = workout.Description?.Value,
            Status = (int)workout.Status,
            MesocycleId = workout.MesocycleId?.Value,
            MesocycleWeekNumber = workout.MesocycleWeekNumber?.Value,
            ReviewedAtUtc = workout.ReviewedAt,
            ReviewedBy = workout.ReviewedBy?.Value,
            ResultsUpdatedAtUtc = workout.ResultsUpdatedAt,
            CompletedAtUtc = workout.CompletedAt,
        };

        var exercisePosition = 0;

        foreach (var entry in workout.Exercises)
        {
            var workoutExercise = new WorkoutExerciseEntity
            {
                Id = entry.Id.Value,
                WorkoutId = entity.Id,
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
                var (targetType, targetValue, targetMaxValue, targetMinutes) = MapTargetToColumns(prescription.target);

                workoutExercise.Sets.Add(new WorkoutExerciseSetEntity
                {
                    Id = Id.New().Value,
                    WorkoutExerciseId = workoutExercise.Id,
                    Position = setPosition++,
                    Count = prescription.count,
                    TargetType = targetType,
                    TargetValue = targetValue,
                    TargetMaxValue = targetMaxValue,
                    TargetMinutes = targetMinutes,
                });
            }

            var setRecordPosition = 0;

            // Set records have identity, so they keep their own id instead of getting a fresh one like the sets
            foreach (var setRecord in entry.SetRecords)
            {
                workoutExercise.SetRecords.Add(new SetRecordEntity
                {
                    Id = setRecord.Id.Value,
                    WorkoutExerciseId = workoutExercise.Id,
                    Position = setRecordPosition++,
                    Reps = setRecord.Reps.Value,
                    Weight = setRecord.Weight.Value,
                    RegisterDatetimeUtc = setRecord.RegisteredAtUtc,
                });
            }

            entity.WorkoutExercises.Add(workoutExercise);
        }

        return entity;
    }

    /// <summary>
    /// Restores the aggregate.
    /// </summary>
    /// <param name="entity">Workout rows, with its exercises, sets and set records loaded.</param>
    public static Workout MapToDomain(WorkoutEntity entity)
    {
        try
        {
            var exercises = entity.WorkoutExercises
                .OrderBy(item => item.Position)
                .Select(item => WorkoutExercise.Rehydrate(
                    Id.From(item.Id),
                    Id.From(item.ExerciseId),
                    MapEffort(item.Effort),
                    item.Sets
                        .OrderBy(set => set.Position)
                        .Select(set => new SetPrescription(set.Count, MapTargetToDomain(set.TargetType, set.TargetValue, set.TargetMaxValue, set.TargetMinutes))),
                    item.SuperSeriesId is null ? null : SuperSeriesId.From(item.SuperSeriesId.Value),
                    item.PerSide,
                    item.RestMinutes is null ? null : TimeInMinutes.From(item.RestMinutes.Value),
                    item.SetRecords
                        .OrderBy(setRecord => setRecord.Position)
                        .Select(setRecord => SetRecord.Rehydrate(
                            Id.From(setRecord.Id),
                            RepCount.From(setRecord.Reps),
                            Weight.From(setRecord.Weight),
                            setRecord.RegisterDatetimeUtc))))
                .ToList();

            return Workout.Rehydrate(
                Id.From(entity.Id),
                Id.From(entity.UserId),
                entity.TrainerId is null ? null : Id.From(entity.TrainerId.Value),
                WorkoutName.From(entity.Name),
                WorkoutDescription.From(entity.Description),
                entity.WorkoutDate.ToDateTime(TimeOnly.MinValue),
                MapStatus(entity.Status),
                exercises,
                entity.MesocycleId is null ? null : Id.From(entity.MesocycleId.Value),
                entity.MesocycleWeekNumber is null ? null : WeekNumber.From(entity.MesocycleWeekNumber.Value),
                entity.ReviewedAtUtc,
                entity.ReviewedBy is null ? null : Id.From(entity.ReviewedBy.Value),
                entity.ResultsUpdatedAtUtc,
                entity.CompletedAtUtc);
        }
        catch (DomainException exception)
        {
            throw new PersistenceMappingException(
                $"Stored workout '{entity.Id}' could not be restored", exception);
        }
    }

    internal static (string TargetType, int? TargetValue, int? TargetMaxValue, decimal? TargetMinutes) MapTargetToColumns(SetTarget target) => target switch
    {
        SetTarget.Reps reps => (SetTargetTypes.Reps, reps.value, null, null),
        SetTarget.Duration duration => (SetTargetTypes.Duration, duration.value, null, null),
        SetTarget.MaxReps => (SetTargetTypes.MaxReps, null, null, null),
        SetTarget.RepsRange range => (SetTargetTypes.RepsRange, range.min, range.max, null),
        SetTarget.Minutes minutes => (SetTargetTypes.Minutes, null, null, minutes.value.Value),
        _ => throw new PersistenceMappingException($"'{target.GetType().Name}' is not a known set target"),
    };

    internal static SetTarget MapTargetToDomain(string targetType, int? targetValue, int? targetMaxValue, decimal? targetMinutes) => targetType switch
    {
        SetTargetTypes.Reps => new SetTarget.Reps(RequireValue(targetType, targetValue)),
        SetTargetTypes.Duration => new SetTarget.Duration(RequireValue(targetType, targetValue)),
        SetTargetTypes.MaxReps => new SetTarget.MaxReps(),
        SetTargetTypes.RepsRange => new SetTarget.RepsRange(
            RequireValue(targetType, targetValue), RequireValue(targetType, targetMaxValue)),
        SetTargetTypes.Minutes => new SetTarget.Minutes(TimeInMinutes.From(RequireMinutes(targetType, targetMinutes))),
        _ => throw new PersistenceMappingException($"'{targetType}' is not a known set target"),
    };

    internal static Effort MapEffort(int effort)
    {
        if (Enum.IsDefined(typeof(Effort), effort) == false)
            throw new PersistenceMappingException($"'{effort}' is not a known effort");

        return (Effort)effort;
    }

    private static WorkoutStatus MapStatus(int status)
    {
        if (Enum.IsDefined(typeof(WorkoutStatus), status) == false)
            throw new PersistenceMappingException($"'{status}' is not a known workout status");

        return (WorkoutStatus)status;
    }

    private static int RequireValue(string targetType, int? targetValue)
        => targetValue ?? throw new PersistenceMappingException($"Set target '{targetType}' requires a value");

    private static decimal RequireMinutes(string targetType, decimal? targetMinutes)
        => targetMinutes ?? throw new PersistenceMappingException($"Set target '{targetType}' requires minutes");
}
