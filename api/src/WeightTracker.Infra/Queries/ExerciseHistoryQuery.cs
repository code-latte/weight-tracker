using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Queries;

public class ExerciseHistoryQuery : IExerciseHistoryQuery
{
    private readonly WeightTrackerDbContext _context;

    public ExerciseHistoryQuery(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ExerciseSessionView>> Get(Id userId, Id exerciseId, Id? prescribedBy, int limit, CancellationToken cancellationToken)
    {
        var finished = new[] { (int)WorkoutStatus.COMPLETED, (int)WorkoutStatus.REVIEWED };
        var user = userId.Value;
        var exercise = exerciseId.Value;
        Guid? trainer = prescribedBy?.Value;

        // Newest workouts first, so the plan walks IX_Workouts_UserId_WorkoutDate_Id and stops after the limit.
        // A workout holds an exercise at most once, so each workout yields at most one row.
        var sessions = await _context.Workouts.AsNoTracking()
            .Where(workout => workout.UserId == user && finished.Contains(workout.Status))
            .Where(workout => trainer == null || workout.TrainerId == trainer)
            .OrderByDescending(workout => workout.WorkoutDate)
            .ThenByDescending(workout => workout.Id)
            .SelectMany(
                workout => workout.WorkoutExercises.Where(entry => entry.ExerciseId == exercise && entry.SetRecords.Any()),
                (workout, entry) => new { WorkoutId = workout.Id, workout.WorkoutDate, WorkoutExerciseId = entry.Id })
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
            return [];

        var workoutExerciseIds = sessions.Select(session => session.WorkoutExerciseId).ToList();

        var records = await _context.SetRecords.AsNoTracking()
            .Where(setRecord => workoutExerciseIds.Contains(setRecord.WorkoutExerciseId))
            .OrderBy(setRecord => setRecord.WorkoutExerciseId)
            .ThenBy(setRecord => setRecord.Position)
            .Select(setRecord => new { setRecord.WorkoutExerciseId, Set = new LastPerformanceSetView(setRecord.Reps, setRecord.DurationSeconds, setRecord.Weight) })
            .ToListAsync(cancellationToken);

        var setsByEntry = records.ToLookup(record => record.WorkoutExerciseId, record => record.Set);

        return sessions
            .Select(session => new ExerciseSessionView(Id.From(session.WorkoutId), session.WorkoutDate, setsByEntry[session.WorkoutExerciseId].ToList()))
            .ToList();
    }
}
