using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Application.UseCases.Exercises;

public class ArchiveExerciseUseCase
{
    private IExerciseRepository _exercises;

    private ExerciseVisibility _visibility;

    private ExerciseEditAuthorizer _authorizer;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public ArchiveExerciseUseCase(
        IExerciseRepository exercises,
        ExerciseVisibility visibility,
        ExerciseEditAuthorizer authorizer,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _exercises = exercises;
        _visibility = visibility;
        _authorizer = authorizer;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Archives a custom exercise: it can no longer be added to workouts, and the workouts that use it keep it.
    /// </summary>
    /// <exception cref="ExerciseNotFoundException">When no exercise has the given id, or the caller may not see it.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller can see the exercise but is neither its owner nor an admin.</exception>
    /// <exception cref="SystemExerciseNotEditableException">When the exercise is a system exercise.</exception>
    /// <exception cref="ExerciseArchivedException">When the exercise is already archived.</exception>
    public async Task Execute(AuthenticatedUser caller, Id exerciseId, CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetById(exerciseId, cancellationToken)
            ?? throw new ExerciseNotFoundException(exerciseId);

        // Checked before the permission so a caller who may not see the exercise never learns that it exists
        await _visibility.EnsureVisible(exercise, caller, cancellationToken);

        _authorizer.EnsureCanEdit(exercise, caller.Id, caller.Role, "DELETE /exercises/{id}");

        exercise.Archive(_clock.UtcNow);

        await _exercises.Save(exercise, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
