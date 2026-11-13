using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Workouts;
using WeightTracker.Infra.Persistence;

namespace WeightTracker.Infra.Queries;

public class ExerciseLastPerformanceQuery : IExerciseLastPerformanceQuery
{
    private readonly WeightTrackerDbContext _context;

    public ExerciseLastPerformanceQuery(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<ExerciseLastPerformanceView?> Get(Id userId, Id exerciseId, CancellationToken cancellationToken)
    {
        var finished = new[] { (int)WorkoutStatus.COMPLETED, (int)WorkoutStatus.REVIEWED };

        // Newest workouts first, so the plan walks IX_Workouts_UserId_WorkoutDate_Id and stops at the first match
        var latest = await _context.Workouts.AsNoTracking()
            .Where(workout => workout.UserId == userId.Value && finished.Contains(workout.Status))
            .OrderByDescending(workout => workout.WorkoutDate)
            .ThenByDescending(workout => workout.Id)
            .SelectMany(
                workout => workout.WorkoutExercises.Where(entry => entry.ExerciseId == exerciseId.Value && entry.SetRecords.Any()),
                (workout, entry) => new { workout.WorkoutDate, entry.Effort, WorkoutExerciseId = entry.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
            return null;

        var sets = await _context.SetRecords.AsNoTracking()
            .Where(setRecord => setRecord.WorkoutExerciseId == latest.WorkoutExerciseId)
            .OrderBy(setRecord => setRecord.Position)
            .Select(setRecord => new LastPerformanceSetView(setRecord.Reps, setRecord.DurationSeconds, setRecord.Weight))
            .ToListAsync(cancellationToken);

        return new ExerciseLastPerformanceView(latest.WorkoutDate, (Effort)latest.Effort, sets);
    }
}
