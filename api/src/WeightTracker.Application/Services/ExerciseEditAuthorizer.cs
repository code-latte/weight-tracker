using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Services;

/// <summary>
/// Decides who may change an existing exercise. Every use case that edits or archives an exercise goes through
/// it, so the rule cannot drift between endpoints.
/// </summary>
/// <remarks>
/// A system exercise can never be changed through the application, not even by an admin. A custom exercise may
/// be changed by its owner and by an admin.
/// </remarks>
public class ExerciseEditAuthorizer
{
    /// <param name="operation">Operation being attempted, used in the error message.</param>
    /// <exception cref="SystemExerciseNotEditableException">When the exercise is a system exercise.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller is neither the owner nor an admin.</exception>
    public void EnsureCanEdit(Exercise exercise, Id callerId, Role callerRole, string operation)
    {
        if (exercise.IsSystem)
            throw new SystemExerciseNotEditableException(exercise.Id);

        if (callerRole != Role.ADMIN && exercise.OwnerId != callerId)
            throw new UnauthorizedOperationException(operation);
    }
}
