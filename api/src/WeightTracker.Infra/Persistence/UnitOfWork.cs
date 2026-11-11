using Microsoft.EntityFrameworkCore;
using Npgsql;
using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;

namespace WeightTracker.Infra.Persistence;

public class UnitOfWork : IUnitOfWork
{
    /// <summary>Defined by the CreateCustomerLeadsTable migration.</summary>
    private const string CustomerLeadEmailConstraint = "UQ_CustomerLeads_Email";

    private readonly WeightTrackerDbContext _context;

    public UnitOfWork(WeightTrackerDbContext context)
    {
        _context = context;
    }

    /// <exception cref="WorkoutConcurrencyConflictException">When a workout changed after it was read. Only workouts carry a row version.</exception>
    /// <exception cref="CustomerLeadAlreadyExistsException">When a concurrent request already stored the same lead email.</exception>
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
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: CustomerLeadEmailConstraint,
        })
        {
            throw new CustomerLeadAlreadyExistsException(exception);
        }
    }
}
