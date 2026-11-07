using Microsoft.EntityFrameworkCore;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;

namespace WeightTracker.Infra.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly WeightTrackerDbContext _context;

    public UnitOfWork(WeightTrackerDbContext context)
    {
        _context = context;
    }

    /// <exception cref="WorkoutConcurrencyConflictException">When a workout changed after it was read. Only workouts carry a row version.</exception>
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new WorkoutConcurrencyConflictException(exception);
        }
    }
}
