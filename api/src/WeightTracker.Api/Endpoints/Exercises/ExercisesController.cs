using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Exercises.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;

namespace WeightTracker.Api.Endpoints.Exercises;

/// <summary>
/// Exercises are the system catalog plus the custom exercises that trainers and athletes create. Reading them is
/// public; creating, editing and archiving custom exercises needs a token. System exercises are maintained in the
/// database, never through the API.
/// </summary>
/// <remarks>
/// <see cref="AuthorizedController"/> requires authentication by default, so the public reads opt
/// out with <c>[AllowAnonymous]</c> per action rather than on the class: an <c>[AllowAnonymous]</c>
/// on the class would also skip the caller lookup for the protected actions. The reads add
/// <see cref="OptionalCallerAttribute"/> so an authenticated caller also sees their own exercises.
/// </remarks>
[ApiController]
[Route("exercises")]
public class ExercisesController : AuthorizedController
{
    private readonly GetAllExercisesUseCase _getAll;
    private readonly GetExerciseByIdUseCase _getById;
    private readonly CreateExerciseUseCase _create;
    private readonly UpdateExerciseUseCase _update;
    private readonly ArchiveExerciseUseCase _archive;

    public ExercisesController(
        GetAllExercisesUseCase getAll,
        GetExerciseByIdUseCase getById,
        CreateExerciseUseCase create,
        UpdateExerciseUseCase update,
        ArchiveExerciseUseCase archive,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _getAll = getAll;
        _getById = getById;
        _create = create;
        _update = update;
        _archive = archive;
    }

    /// <summary>
    /// Lists the exercises the caller may see: the system exercises for everyone, plus the caller's own and the
    /// ones used in workouts they can see when a valid token is sent. A token that is sent must be valid.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [OptionalCaller]
    [ProducesResponseType<IReadOnlyList<ExerciseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var exercises = await _getAll.Execute(CurrentUser, cancellationToken);

            return Ok(exercises.Select(ExerciseResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Reads one exercise. A custom exercise the caller may not see is reported as not found.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [OptionalCaller]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var exercise = await _getById.Execute(Id.From(id), CurrentUser, cancellationToken);

            return Ok(ExerciseResponse.From(exercise));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Creates a custom exercise owned by the caller, named in a single language. Trainers and athletes only.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] ExerciseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var exercise = await _create.Execute(
                CurrentUser!,
                LanguageCode.From(request.LanguageCode),
                ExerciseName.From(request.Name),
                request.Muscle!.Value,
                request.Type!.Value,
                request.VideoUrl is null ? null : Domain.Exercises.Url.From(request.VideoUrl),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = exercise.Id.Value }, ExerciseResponse.From(exercise));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces the muscle, type and video of a custom exercise and sets its name in one language, keeping the
    /// other languages. The owner or an admin only; system exercises cannot be changed.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ExerciseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var exercise = await _update.Execute(
                CurrentUser!,
                Id.From(id),
                LanguageCode.From(request.LanguageCode),
                ExerciseName.From(request.Name),
                request.Muscle!.Value,
                request.Type!.Value,
                request.VideoUrl is null ? null : Domain.Exercises.Url.From(request.VideoUrl),
                cancellationToken);

            return Ok(ExerciseResponse.From(exercise));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Archives a custom exercise: it can no longer be added to workouts, and the workouts that use it keep it.
    /// The owner or an admin only; system exercises cannot be archived.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _archive.Execute(CurrentUser!, Id.From(id), cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
