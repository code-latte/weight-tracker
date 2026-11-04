using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Exercises;

public class CreateExerciseUseCase
{
    private IExerciseRepository _exercises;

    private IUnitOfWork _unitOfWork;

    public CreateExerciseUseCase(
        IExerciseRepository exercises,
        IUnitOfWork unitOfWork
    )
    {
        _exercises = exercises;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates a custom exercise owned by the caller, named in a single language. Trainers and athletes may
    /// create exercises; admins may not.
    /// </summary>
    /// <returns>The created exercise.</returns>
    /// <exception cref="UnauthorizedOperationException">When the caller is an admin.</exception>
    /// <exception cref="InvalidMuscleException">When the muscle is not a defined value.</exception>
    /// <exception cref="InvalidExerciseTypeException">When the type is not a defined value.</exception>
    public async Task<Exercise> Execute(
        AuthenticatedUser caller,
        LanguageCode languageCode,
        ExerciseName name,
        Muscles muscle,
        ExerciseTypes type,
        Url? videoUrl,
        CancellationToken cancellationToken)
    {
        if (caller.Role == Role.ADMIN)
            throw new UnauthorizedOperationException("POST /exercises");

        var exercise = Exercise.Create(caller.Id, ExerciseTranslation.Create(languageCode, name), muscle, type, videoUrl);

        await _exercises.Save(exercise, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return exercise;
    }
}
