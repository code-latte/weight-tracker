using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Workouts;
using WeightTracker.Api.Endpoints.Workouts.Dtos;
using WeightTracker.Api.Endpoints.WorkoutTemplates.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.WorkoutTemplates;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.WorkoutTemplates;

/// <summary>
/// Reusable workout content owned by a trainer. Only trainers and admins use templates: a trainer acts on their own
/// templates only, and another trainer's template answers <c>404</c> as if it did not exist.
/// </summary>
[ApiController]
[Route("workout-templates")]
public class WorkoutTemplatesController : AuthorizedController
{
    private readonly GetAllWorkoutTemplatesUseCase _getAll;
    private readonly GetWorkoutTemplateByIdUseCase _getById;
    private readonly CreateWorkoutTemplateUseCase _create;
    private readonly UpdateWorkoutTemplateUseCase _update;
    private readonly DeleteWorkoutTemplateUseCase _delete;
    private readonly AddWorkoutTemplateExerciseUseCase _addExercise;
    private readonly UpdateWorkoutTemplateExerciseUseCase _updateExercise;
    private readonly RemoveWorkoutTemplateExerciseUseCase _removeExercise;
    private readonly ReorderWorkoutTemplateExercisesUseCase _reorderExercises;
    private readonly CreateWorkoutTemplateSuperSeriesUseCase _createSuperSeries;
    private readonly BreakWorkoutTemplateSuperSeriesUseCase _breakSuperSeries;
    private readonly CreateWorkoutFromTemplateUseCase _createWorkout;

    public WorkoutTemplatesController(
        GetAllWorkoutTemplatesUseCase getAll,
        GetWorkoutTemplateByIdUseCase getById,
        CreateWorkoutTemplateUseCase create,
        UpdateWorkoutTemplateUseCase update,
        DeleteWorkoutTemplateUseCase delete,
        AddWorkoutTemplateExerciseUseCase addExercise,
        UpdateWorkoutTemplateExerciseUseCase updateExercise,
        RemoveWorkoutTemplateExerciseUseCase removeExercise,
        ReorderWorkoutTemplateExercisesUseCase reorderExercises,
        CreateWorkoutTemplateSuperSeriesUseCase createSuperSeries,
        BreakWorkoutTemplateSuperSeriesUseCase breakSuperSeries,
        CreateWorkoutFromTemplateUseCase createWorkout,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _getAll = getAll;
        _getById = getById;
        _create = create;
        _update = update;
        _delete = delete;
        _addExercise = addExercise;
        _updateExercise = updateExercise;
        _removeExercise = removeExercise;
        _reorderExercises = reorderExercises;
        _createSuperSeries = createSuperSeries;
        _breakSuperSeries = breakSuperSeries;
        _createWorkout = createWorkout;
    }

    /// <summary>
    /// Lists templates ordered by name: a trainer gets their own, an admin gets every template, optionally only
    /// those of <c>trainerId</c>. A trainer's <c>trainerId</c> is ignored.
    /// </summary>
    [HttpGet]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<IReadOnlyList<WorkoutTemplateResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? trainerId, CancellationToken cancellationToken)
    {
        try
        {
            var templates = await _getAll.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                trainerId is null ? null : Id.From(trainerId.Value),
                cancellationToken);

            return Ok(templates.Select(WorkoutTemplateResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Reads one template with its exercises.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<WorkoutTemplateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _getById.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(id), cancellationToken);

            return Ok(WorkoutTemplateResponse.From(template));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Creates an empty template. A trainer creates it for themselves; an admin must send <c>trainerId</c>, an
    /// active trainer who owns it.
    /// </summary>
    /// <remarks>
    /// The <c>Location</c> header points at <c>GET /workout-templates/{id}</c>.
    /// </remarks>
    [HttpPost]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<WorkoutTemplateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateWorkoutTemplateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _create.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                request.TrainerId is null ? null : Id.From(request.TrainerId.Value),
                WorkoutName.From(request.Name),
                WorkoutDescription.From(request.Description),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = template.Id.Value }, WorkoutTemplateResponse.From(template));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces the name and the description of the template. Omitting <c>description</c> clears it.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<WorkoutTemplateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateWorkoutTemplateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _update.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(id),
                WorkoutName.From(request.Name),
                WorkoutDescription.From(request.Description),
                cancellationToken);

            return Ok(WorkoutTemplateResponse.From(template));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Deletes the template and its content. Workouts created from it are copies and stay as they are.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _delete.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(id), cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Appends a catalog exercise to the template with an effort and its set prescriptions. A catalog exercise can
    /// appear once per template, and a custom exercise only in its owner's templates.
    /// </summary>
    /// <remarks>
    /// The <c>Location</c> header points at <c>GET /workout-templates/{id}</c>, since entries have no route of their own.
    /// </remarks>
    [HttpPost("{templateId:guid}/exercises")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<WorkoutExerciseDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddExercise(
        Guid templateId, [FromBody] AddOrUpdateExerciseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _addExercise.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(templateId),
                Id.From(request.ExerciseId!.Value),
                request.Effort!.Value,
                request.Sets!.Select(set => set.ToDomain()).ToList(),
                request.PerSide ?? false,
                request.ToRest(),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = templateId }, WorkoutExerciseDetailResponse.From(entry));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces the effort, the per-side flag, the rest and every set prescription of an exercise of the template.
    /// <c>exerciseId</c> is the catalog exercise, and the body must name the same one.
    /// </summary>
    [HttpPut("{templateId:guid}/exercises/{exerciseId:guid}")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<WorkoutExerciseDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExercise(
        Guid templateId, Guid exerciseId, [FromBody] AddOrUpdateExerciseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _updateExercise.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(templateId),
                Id.From(exerciseId),
                Id.From(request.ExerciseId!.Value),
                request.Effort!.Value,
                request.Sets!.Select(set => set.ToDomain()).ToList(),
                request.PerSide ?? false,
                request.ToRest(),
                cancellationToken);

            return Ok(WorkoutExerciseDetailResponse.From(entry));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Removes an exercise, and its set prescriptions, from the template. <c>exerciseId</c> is the catalog exercise.
    /// </summary>
    [HttpDelete("{templateId:guid}/exercises/{exerciseId:guid}")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveExercise(Guid templateId, Guid exerciseId, CancellationToken cancellationToken)
    {
        try
        {
            await _removeExercise.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(templateId), Id.From(exerciseId), cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Puts the exercises of the template in a new order. <c>exerciseIds</c> are catalog exercises and must list
    /// every exercise of the template exactly once.
    /// </summary>
    [HttpPut("{templateId:guid}/exercises/order")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<WorkoutTemplateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderExercises(
        Guid templateId, [FromBody] ReorderWorkoutExercisesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _reorderExercises.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(templateId),
                request.ExerciseIds!.Select(Id.From).ToList(),
                cancellationToken);

            return Ok(WorkoutTemplateResponse.From(template));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Groups entries of the template into a new superseries. <c>exerciseIds</c> are the <c>id</c> of each entry,
    /// not the catalog <c>exerciseId</c>.
    /// </summary>
    /// <remarks>
    /// There is no route that reads a single superseries, so the response carries no <c>Location</c>.
    /// </remarks>
    [HttpPost("{templateId:guid}/superseries")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<SuperSeriesResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSuperSeries(
        Guid templateId, [FromBody] CreateSuperSeriesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var superSeriesId = await _createSuperSeries.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(templateId),
                request.ExerciseIds!.Select(Id.From).ToList(),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, SuperSeriesResponse.From(superSeriesId));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Ungroups a superseries of the template. Its exercises stay in the template.
    /// </summary>
    [HttpDelete("{templateId:guid}/superseries/{superSeriesId:int}")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BreakSuperSeries(Guid templateId, int superSeriesId, CancellationToken cancellationToken)
    {
        try
        {
            await _breakSuperSeries.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(templateId), superSeriesId, cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Creates a draft workout for a regular user by copying the template: name, description, exercises, sets
    /// and superseries. The template's trainer prescribes it, whoever calls. Send <c>workoutDate</c> for a
    /// standalone workout, or <c>mesocycleId</c> and <c>mesocycleWeekNumber</c> to place it in a mesocycle week.
    /// Later changes to the template never reach the workout.
    /// </summary>
    /// <remarks>
    /// The <c>Location</c> header points at <c>GET /workouts/{id}</c>.
    /// </remarks>
    [HttpPost("{templateId:guid}/workouts")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<WorkoutResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateWorkout(
        Guid templateId, [FromBody] CreateWorkoutFromTemplateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var view = await _createWorkout.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(templateId),
                request.UserId is null ? null : Id.From(request.UserId.Value),
                request.WorkoutDate?.ToDateTime(TimeOnly.MinValue),
                request.MesocycleId is null ? null : Id.From(request.MesocycleId.Value),
                request.MesocycleWeekNumber is null ? null : WeekNumber.From(request.MesocycleWeekNumber.Value),
                cancellationToken);

            return CreatedAtAction(
                nameof(WorkoutsController.GetById),
                "Workouts",
                new { id = view.Workout.Id.Value },
                WorkoutResponse.From(view.Workout, view.Exercises));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
