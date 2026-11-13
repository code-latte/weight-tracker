using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Queries;

public class ExercisePreviousExecutionQuery : IExercisePreviousExecutionQuery
{
    private readonly WeightTrackerDbContext _context;

    public ExercisePreviousExecutionQuery(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ExercisePreviousExecutionView>> Get(
        Id ownerId,
        IReadOnlyCollection<Id> exerciseIds,
        DateOnly beforeDate,
        Id beforeWorkoutId,
        Id? prescribedBy,
        CancellationToken cancellationToken)
    {
        var finished = new[] { (int)WorkoutStatus.COMPLETED, (int)WorkoutStatus.REVIEWED };
        var owner = ownerId.Value;
        var anchorId = beforeWorkoutId.Value;
        Guid? trainer = prescribedBy?.Value;

        var matches = new List<(Id ExerciseId, Id WorkoutId, DateOnly WorkoutDate, Guid WorkoutExerciseId)>();

        // One walk per exercise, newest first, so each plan follows IX_Workouts_UserId_WorkoutDate_Id and stops at the first match
        foreach (var exerciseId in exerciseIds.Distinct())
        {
            var exercise = exerciseId.Value;

            var previous = await _context.Workouts.AsNoTracking()
                .Where(workout => workout.UserId == owner && finished.Contains(workout.Status))
                // The date bound is redundant with the next condition, but it lets the index range stop at the anchor date
                // instead of filtering every newer workout of the user
                .Where(workout => workout.WorkoutDate <= beforeDate)
                .Where(workout => workout.WorkoutDate < beforeDate || workout.Id.CompareTo(anchorId) < 0)
                .Where(workout => trainer == null || workout.TrainerId == trainer)
                .OrderByDescending(workout => workout.WorkoutDate)
                .ThenByDescending(workout => workout.Id)
                .SelectMany(
                    workout => workout.WorkoutExercises.Where(entry => entry.ExerciseId == exercise && entry.SetRecords.Any()),
                    (workout, entry) => new { WorkoutId = workout.Id, workout.WorkoutDate, WorkoutExerciseId = entry.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (previous is not null)
                matches.Add((exerciseId, Id.From(previous.WorkoutId), previous.WorkoutDate, previous.WorkoutExerciseId));
        }

        if (matches.Count == 0)
            return [];

        var workoutExerciseIds = matches.Select(match => match.WorkoutExerciseId).ToList();

        var records = await _context.SetRecords.AsNoTracking()
            .Where(setRecord => workoutExerciseIds.Contains(setRecord.WorkoutExerciseId))
            .OrderBy(setRecord => setRecord.WorkoutExerciseId)
            .ThenBy(setRecord => setRecord.Position)
            .Select(setRecord => new { setRecord.WorkoutExerciseId, Set = new LastPerformanceSetView(setRecord.Reps, setRecord.DurationSeconds, setRecord.Weight) })
            .ToListAsync(cancellationToken);

        var setsByEntry = records.ToLookup(record => record.WorkoutExerciseId, record => record.Set);

        return matches
            .Select(match => new ExercisePreviousExecutionView(match.ExerciseId, match.WorkoutId, match.WorkoutDate, setsByEntry[match.WorkoutExerciseId].ToList()))
            .ToList();
    }
}
