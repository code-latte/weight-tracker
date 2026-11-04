using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Application.UseCases.Exercises;

public class UpdateExerciseUseCase
{
    private IExerciseRepository _exercises;

    private ExerciseVisibility _visibility;

    private ExerciseEditAuthorizer _authorizer;

    private IUnitOfWork _unitOfWork;

    public UpdateExerciseUseCase(
        IExerciseRepository exercises,
        ExerciseVisibility visibility,
        ExerciseEditAuthorizer authorizer,
        IUnitOfWork unitOfWork
    )
    {
        _exercises = exercises;
        _visibility = visibility;
        _authorizer = authorizer;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Replaces the muscle, the type and the video of a custom exercise and sets its name in one language.
    /// </summary>
    /// <returns>The updated exercise.</returns>
    /// <exception cref="ExerciseNotFoundException">When no exercise has the given id, or the caller may not see it.</exception>
    /// <exception cref="UnauthorizedOperationException">When the caller can see the exercise but is neither its owner nor an admin.</exception>
    /// <exception cref="SystemExerciseNotEditableException">When the exercise is a system exercise.</exception>
    /// <exception cref="ExerciseArchivedException">When the exercise is archived.</exception>
    /// <exception cref="InvalidMuscleException">When the muscle is not a defined value.</exception>
    /// <exception cref="InvalidExerciseTypeException">When the type is not a defined value.</exception>
    public async Task<Exercise> Execute(
        AuthenticatedUser caller,
        Id exerciseId,
        LanguageCode languageCode,
        ExerciseName name,
        Muscles muscle,
        ExerciseTypes type,
        Url? videoUrl,
        CancellationToken cancellationToken)
    {
        var exercise = await _exercises.GetById(exerciseId, cancellationToken)
            ?? throw new ExerciseNotFoundException(exerciseId);

        // Checked before the permission so a caller who may not see the exercise never learns that it exists
        await _visibility.EnsureVisible(exercise, caller, cancellationToken);

        _authorizer.EnsureCanEdit(exercise, caller.Id, caller.Role, "PUT /exercises/{id}");

        exercise.UpdateDetails(ExerciseTranslation.Create(languageCode, name), muscle, type, videoUrl);

        await _exercises.Save(exercise, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return exercise;
    }
}
