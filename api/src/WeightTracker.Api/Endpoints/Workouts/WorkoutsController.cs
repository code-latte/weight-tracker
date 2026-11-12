using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Workouts.Dtos;
using WeightTracker.Api.Endpoints.WorkoutTemplates;
using WeightTracker.Api.Endpoints.WorkoutTemplates.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Application.UseCases.WorkoutTemplates;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exercises;
using WeightTracker.Domain.Users;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.Workouts;

[ApiController]
[Route("workouts")]
public class WorkoutsController : AuthorizedController
{
    private readonly GetAllWorkoutsUseCase _getAllWorkouts;
    private readonly GetWorkoutByIdUseCase _getWorkoutById;
    private readonly CreateWorkoutUseCase _createWorkout;
    private readonly UpdateWorkoutUseCase _updateWorkout;
    private readonly DeleteWorkoutUseCase _deleteWorkout;
    private readonly CreateSuperSeriesUseCase _createSuperSeries;
    private readonly BreakSuperSeriesUseCase _breakSuperSeries;
    private readonly AddWorkoutExerciseUseCase _addExercise;
    private readonly UpdateWorkoutExerciseUseCase _updateExercise;
    private readonly RemoveWorkoutExerciseUseCase _removeExercise;
    private readonly ReorderWorkoutExercisesUseCase _reorderExercises;
    private readonly AddSetRecordUseCase _addSetRecord;
    private readonly UpdateSetRecordUseCase _updateSetRecord;
    private readonly RemoveSetRecordUseCase _removeSetRecord;
    private readonly ReorderSetRecordsUseCase _reorderSetRecords;
    private readonly ChangeWorkoutStatusUseCase _changeStatus;
    private readonly ReviewWorkoutUseCase _review;
    private readonly CreateWorkoutTemplateFromWorkoutUseCase _createTemplate;

    public WorkoutsController(
        GetAllWorkoutsUseCase getAllWorkouts,
        GetWorkoutByIdUseCase getWorkoutById,
        CreateWorkoutUseCase createWorkout,
        UpdateWorkoutUseCase updateWorkout,
        DeleteWorkoutUseCase deleteWorkout,
        CreateSuperSeriesUseCase createSuperSeries,
        BreakSuperSeriesUseCase breakSuperSeries,
        AddWorkoutExerciseUseCase addExercise,
        UpdateWorkoutExerciseUseCase updateExercise,
        RemoveWorkoutExerciseUseCase removeExercise,
        ReorderWorkoutExercisesUseCase reorderExercises,
        AddSetRecordUseCase addSetRecord,
        UpdateSetRecordUseCase updateSetRecord,
        RemoveSetRecordUseCase removeSetRecord,
        ReorderSetRecordsUseCase reorderSetRecords,
        ChangeWorkoutStatusUseCase changeStatus,
        ReviewWorkoutUseCase review,
        CreateWorkoutTemplateFromWorkoutUseCase createTemplate,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _getAllWorkouts = getAllWorkouts;
        _getWorkoutById = getWorkoutById;
        _createWorkout = createWorkout;
        _updateWorkout = updateWorkout;
        _deleteWorkout = deleteWorkout;
        _createSuperSeries = createSuperSeries;
        _breakSuperSeries = breakSuperSeries;
        _addExercise = addExercise;
        _updateExercise = updateExercise;
        _removeExercise = removeExercise;
        _reorderExercises = reorderExercises;
        _addSetRecord = addSetRecord;
        _updateSetRecord = updateSetRecord;
        _removeSetRecord = removeSetRecord;
        _reorderSetRecords = reorderSetRecords;
        _changeStatus = changeStatus;
        _review = review;
        _createTemplate = createTemplate;
    }

    /// <summary>
    /// Lists the workouts the caller is entitled to see, each with the public profiles of its athlete and trainer.
    /// The scoping rule itself lives in the use case; the controller only resolves who the caller is.
    /// With <c>standalone=true</c> only the workouts not placed in a mesocycle are listed.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WorkoutListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] bool standalone, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getAllWorkouts.Execute(CurrentUser!.Id, CurrentUser.Role, standalone, cancellationToken);

            return Ok(result.Workouts.Select(workout => WorkoutListItemResponse.From(workout, result.Exercises, result.Users)).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Reads one workout the caller is entitled to see, with the public profiles of its athlete and trainer.
    /// Catalog exercise data is not embedded: entries carry the catalog <c>exerciseId</c> only.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<WorkoutByIdResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var view = await _getWorkoutById.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(id), cancellationToken);

            return Ok(WorkoutByIdResponse.From(view));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Creates a workout. A user creates it for themselves, a trainer prescribes it to a user
    /// (<c>userId</c> required) and an admin creates it for another user (<c>userId</c> required)
    /// and may set <c>trainerId</c>. The mesocycle's trainer or an admin may place it in a mesocycle week by sending
    /// <c>mesocycleId</c> and <c>mesocycleWeekNumber</c> together.
    /// </summary>
    /// <remarks>
    /// The <c>Location</c> header points at <c>GET /workouts/{id}</c>.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType<WorkoutResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateWorkoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workout = await _createWorkout.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                request.UserId is null ? null : Id.From(request.UserId.Value),
                request.TrainerId is null ? null : Id.From(request.TrainerId.Value),
                WorkoutName.From(request.Name),
                WorkoutDescription.From(request.Description),
                request.WorkoutDate?.ToDateTime(TimeOnly.MinValue),
                request.MesocycleId is null ? null : Id.From(request.MesocycleId.Value),
                request.MesocycleWeekNumber is null ? null : WeekNumber.From(request.MesocycleWeekNumber.Value),
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById), new { id = workout.Id.Value }, WorkoutResponse.From(workout, new Dictionary<Id, Exercise>()));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces the name, description and day of the workout, and its owner and trainer. Send back what
    /// <c>GET /workouts/{id}</c> returned with your changes: only an admin may change <c>userId</c> or
    /// <c>trainerId</c>. The mesocycle placement is replaced too: omitting <c>mesocycleId</c> and
    /// <c>mesocycleWeekNumber</c> takes the workout out of its mesocycle. Status and exercises are not changed here.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<WorkoutDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workout = await _updateWorkout.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(id),
                WorkoutName.From(request.Name),
                WorkoutDescription.From(request.Description),
                request.WorkoutDate?.ToDateTime(TimeOnly.MinValue),
                Id.From(request.UserId!.Value),
                request.TrainerId is null ? null : Id.From(request.TrainerId.Value),
                request.MesocycleId is null ? null : Id.From(request.MesocycleId.Value),
                request.MesocycleWeekNumber is null ? null : WeekNumber.From(request.MesocycleWeekNumber.Value),
                cancellationToken);

            return Ok(WorkoutDetailResponse.From(workout));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Permanently deletes a draft workout, with its exercises, sets and superseries. Only a draft can be
    /// deleted, by an admin too; move a published workout back to draft first.
    /// </summary>
    [HttpDelete("{workoutId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid workoutId, CancellationToken cancellationToken)
    {
        try
        {
            await _deleteWorkout.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(workoutId), cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Groups entries of the workout into a new superseries. <c>exerciseIds</c> are the <c>id</c> of
    /// each workout exercise entry, not the catalog <c>exerciseId</c>.
    /// </summary>
    /// <remarks>
    /// There is no route that reads a single superseries, so the response carries no <c>Location</c>.
    /// </remarks>
    [HttpPost("{workoutId:guid}/superseries")]
    [ProducesResponseType<SuperSeriesResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSuperSeries(
        Guid workoutId, [FromBody] CreateSuperSeriesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var superSeriesId = await _createSuperSeries.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
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
    /// Ungroups a superseries of the workout. Its exercises stay in the workout.
    /// </summary>
    [HttpDelete("{workoutId:guid}/superseries/{superSeriesId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BreakSuperSeries(
        Guid workoutId, int superSeriesId, CancellationToken cancellationToken)
    {
        try
        {
            await _breakSuperSeries.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(workoutId), superSeriesId, cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Appends a catalog exercise to the workout with an effort and its set prescriptions.
    /// A catalog exercise can appear once per workout.
    /// </summary>
    /// <remarks>
    /// The <c>Location</c> header points at <c>GET /workouts/{id}</c>, since entries have no route of their own.
    /// </remarks>
    [HttpPost("{workoutId:guid}/exercises")]
    [ProducesResponseType<WorkoutExerciseDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddExercise(
        Guid workoutId, [FromBody] AddOrUpdateExerciseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _addExercise.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
                Id.From(request.ExerciseId!.Value),
                request.Effort!.Value,
                request.Sets!.Select(set => set.ToDomain()).ToList(),
                request.PerSide ?? false,
                request.ToRest(),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = workoutId }, WorkoutExerciseDetailResponse.From(entry));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces the effort, the per-side flag, the rest and every set prescription of an exercise of the workout.
    /// A missing <c>perSide</c> becomes <c>false</c> and a missing <c>restMinutes</c> clears the rest.
    /// <c>exerciseId</c> is the catalog exercise, not the workout entry id, and the body must name the same one.
    /// </summary>
    [HttpPut("{workoutId:guid}/exercises/{exerciseId:guid}")]
    [ProducesResponseType<WorkoutExerciseDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateExercise(
        Guid workoutId, Guid exerciseId, [FromBody] AddOrUpdateExerciseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var entry = await _updateExercise.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
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
    /// Removes an exercise, and its set prescriptions, from the workout. <c>exerciseId</c> is the catalog
    /// exercise, not the workout entry id.
    /// </summary>
    [HttpDelete("{workoutId:guid}/exercises/{exerciseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveExercise(
        Guid workoutId, Guid exerciseId, CancellationToken cancellationToken)
    {
        try
        {
            await _removeExercise.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(workoutId), Id.From(exerciseId), cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Puts the exercises of the workout in a new order. <c>exerciseIds</c> are catalog exercises and must
    /// list every exercise of the workout exactly once. Entry ids, effort, sets and superseries are kept.
    /// </summary>
    [HttpPut("{workoutId:guid}/exercises/order")]
    [ProducesResponseType<WorkoutDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderExercises(
        Guid workoutId, [FromBody] ReorderWorkoutExercisesRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workout = await _reorderExercises.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
                request.ExerciseIds!.Select(Id.From).ToList(),
                cancellationToken);

            return Ok(WorkoutDetailResponse.From(workout));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Records a set actually performed for an exercise of a started workout. It goes after the existing set records
    /// of that exercise and is registered at the time of the request. The body carries <c>reps</c> or <c>durationSeconds</c>
    /// according to how the exercise is prescribed, and an optional <c>weight</c>. An empty body means the set was done
    /// as prescribed. Only the workout's athlete or an admin may record. <c>exerciseId</c> is the catalog exercise, not
    /// the workout entry id.
    /// </summary>
    [HttpPost("{workoutId:guid}/exercises/{exerciseId:guid}/set-records")]
    [ProducesResponseType<WorkoutDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddSetRecord(
        Guid workoutId, Guid exerciseId, [FromBody] AddOrUpdateSetRecordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workout = await _addSetRecord.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
                Id.From(exerciseId),
                request.Reps,
                request.DurationSeconds,
                request.Weight,
                cancellationToken);

            return Ok(WorkoutDetailResponse.From(workout));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Corrects the result and weight of a set already recorded in a started workout. The body must carry <c>reps</c> or
    /// <c>durationSeconds</c>, matching how the exercise is prescribed; omitting <c>weight</c> clears it. Its id,
    /// registration time and position are kept. Only the workout's athlete or an admin may change it.
    /// </summary>
    [HttpPut("{workoutId:guid}/exercises/{exerciseId:guid}/set-records/{setRecordId:guid}")]
    [ProducesResponseType<WorkoutDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSetRecord(
        Guid workoutId, Guid exerciseId, Guid setRecordId, [FromBody] AddOrUpdateSetRecordRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workout = await _updateSetRecord.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
                Id.From(exerciseId),
                Id.From(setRecordId),
                request.Reps,
                request.DurationSeconds,
                request.Weight,
                cancellationToken);

            return Ok(WorkoutDetailResponse.From(workout));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Removes a set already recorded in a started workout; the other set records keep their order.
    /// Only the workout's athlete or an admin may remove it.
    /// </summary>
    [HttpDelete("{workoutId:guid}/exercises/{exerciseId:guid}/set-records/{setRecordId:guid}")]
    [ProducesResponseType<WorkoutDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveSetRecord(
        Guid workoutId, Guid exerciseId, Guid setRecordId, CancellationToken cancellationToken)
    {
        try
        {
            var workout = await _removeSetRecord.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
                Id.From(exerciseId),
                Id.From(setRecordId),
                cancellationToken);

            return Ok(WorkoutDetailResponse.From(workout));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Puts the set records of an exercise of a started workout in a new order. <c>setRecordIds</c> must list every
    /// set record of that exercise exactly once. Only the workout's athlete or an admin may reorder them.
    /// </summary>
    [HttpPut("{workoutId:guid}/exercises/{exerciseId:guid}/set-records/order")]
    [ProducesResponseType<WorkoutDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReorderSetRecords(
        Guid workoutId, Guid exerciseId, [FromBody] ReorderSetRecordsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workout = await _reorderSetRecords.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
                Id.From(exerciseId),
                request.SetRecordIds!.Select(Id.From).ToList(),
                cancellationToken);

            return Ok(WorkoutDetailResponse.From(workout));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Moves the workout to another status. Admins may set any status; everybody else follows the
    /// lifecycle (draft, published, started, completed) within what their role allows. Nobody reaches or
    /// leaves reviewed through this route.
    /// </summary>
    [HttpPut("{workoutId:guid}/status")]
    [ProducesResponseType<WorkoutDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(
        Guid workoutId, [FromBody] ChangeWorkoutStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workout = await _changeStatus.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
                request.Status!.Value,
                cancellationToken);

            return Ok(WorkoutDetailResponse.From(workout));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Marks a completed workout as reviewed. Only its prescribing trainer or an admin may review it, and only
    /// when the results did not change after the <c>resultsUpdatedAt</c> sent, which is the value last read.
    /// </summary>
    [HttpPut("{workoutId:guid}/review")]
    [ProducesResponseType<WorkoutDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Review(
        Guid workoutId, [FromBody] ReviewWorkoutRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var workout = await _review.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(workoutId),
                request.ResultsUpdatedAt,
                cancellationToken);

            return Ok(WorkoutDetailResponse.From(workout));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Saves the workout as a new template owned by its trainer, copying its name, description, exercises, sets and
    /// superseries. A trainer may only save the workouts they prescribe; an admin may save any workout that has a
    /// trainer, who owns the template. The workout itself is not changed, whatever its status.
    /// </summary>
    /// <remarks>
    /// The <c>Location</c> header points at <c>GET /workout-templates/{id}</c>.
    /// </remarks>
    [HttpPost("{workoutId:guid}/templates")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<WorkoutTemplateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTemplate(Guid workoutId, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _createTemplate.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(workoutId), cancellationToken);

            return CreatedAtAction(
                nameof(WorkoutTemplatesController.GetById),
                "WorkoutTemplates",
                new { id = template.Id.Value },
                WorkoutTemplateResponse.From(template));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
