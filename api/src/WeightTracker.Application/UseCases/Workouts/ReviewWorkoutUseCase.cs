using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.UseCases.Workouts;

public class ReviewWorkoutUseCase
{
    private IWorkoutRepository _workouts;

    private WorkoutStatusAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    private IClock _clock;

    public ReviewWorkoutUseCase(
        IWorkoutRepository workouts,
        WorkoutStatusAuthorizer authorizer,
        IUnitOfWork unitOfWork,
        IClock clock
    )
    {
        _workouts = workouts;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Marks a completed workout as reviewed by the caller. The review time is the server time.
    /// </summary>
    /// <param name="expectedResultsUpdatedAt">Last change of the results the caller read, <c>null</c> when there was none</param>
    /// <returns>The reviewed workout.</returns>
    /// <exception cref="WorkoutNotFoundException">When no workout has the given id.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is neither an admin nor the prescribing trainer.</exception>
    /// <exception cref="WorkoutReviewNotAllowedException">When the workout is not completed or has no prescribing trainer.</exception>
    /// <exception cref="WorkoutResultsChangedException">When the results changed after the caller read them.</exception>
    /// <exception cref="WorkoutConcurrencyConflictException">When another request changed the workout while it was being reviewed.</exception>
    public async Task<Workout> Execute(
        Id callerId,
        Role callerRole,
        Id workoutId,
        DateTime? expectedResultsUpdatedAt,
        CancellationToken cancellationToken)
    {
        var workout = await _workouts.GetById(workoutId, cancellationToken)
            ?? throw new WorkoutNotFoundException(workoutId);

        _authorizer.EnsureCanReview(workout, callerId, callerRole, "PUT /workouts/{workoutId}/review");

        workout.MarkReviewed(callerId, _clock.UtcNow, expectedResultsUpdatedAt);

        await _workouts.Save(workout, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workout;
    }
}
