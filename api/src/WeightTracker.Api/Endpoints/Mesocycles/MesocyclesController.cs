using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Mesocycles.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Mesocycles;

/// <summary>
/// Mesocycles: training plans from a trainer to an athlete. Their athlete, their trainer and admins read them;
/// only their trainer and admins change them. The mesocycles of one athlete are listed through
/// <c>GET /users/{userId}/mesocycles</c>.
/// </summary>
[ApiController]
[Route("mesocycles")]
public class MesocyclesController : AuthorizedController
{
    private readonly GetAllMesocyclesUseCase _getAllMesocycles;
    private readonly GetMesocycleByIdUseCase _getMesocycleById;
    private readonly CreateMesocycleUseCase _createMesocycle;
    private readonly UpdateMesocycleUseCase _updateMesocycle;
    private readonly DefineMesocycleWeekUseCase _defineWeek;
    private readonly RedefineMesocycleWeekUseCase _redefineWeek;
    private readonly RemoveMesocycleWeekUseCase _removeWeek;

    public MesocyclesController(
        GetAllMesocyclesUseCase getAllMesocycles,
        GetMesocycleByIdUseCase getMesocycleById,
        CreateMesocycleUseCase createMesocycle,
        UpdateMesocycleUseCase updateMesocycle,
        DefineMesocycleWeekUseCase defineWeek,
        RedefineMesocycleWeekUseCase redefineWeek,
        RemoveMesocycleWeekUseCase removeWeek,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _getAllMesocycles = getAllMesocycles;
        _getMesocycleById = getMesocycleById;
        _createMesocycle = createMesocycle;
        _updateMesocycle = updateMesocycle;
        _defineWeek = defineWeek;
        _redefineWeek = redefineWeek;
        _removeWeek = removeWeek;
    }

    /// <summary>
    /// Lists the mesocycles the caller is entitled to see, latest start date first: an athlete gets the ones planned
    /// for them, a trainer the ones they prepare, an admin all of them.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MesocycleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var views = await _getAllMesocycles.Execute(CurrentUser!.Id, CurrentUser.Role, cancellationToken);

            return Ok(views.Select(MesocycleResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Reads one mesocycle with its weeks. Its athlete, its trainer and admins may read it.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MesocycleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var view = await _getMesocycleById.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(id), cancellationToken);

            return Ok(MesocycleResponse.From(view));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Creates a mesocycle with no week defined. A trainer creates it for an athlete they actively train and is set
    /// as its trainer; an admin names both the trainer and the athlete.
    /// </summary>
    /// <remarks>
    /// The <c>Location</c> header points at <c>GET /mesocycles/{id}</c>.
    /// </remarks>
    [HttpPost]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<MesocycleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateMesocycleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var view = await _createMesocycle.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                request.TrainerId is null ? null : Id.From(request.TrainerId.Value),
                Id.From(request.AthleteId!.Value),
                MesocycleName.From(request.Name),
                MesocycleObjective.From(request.Objective),
                request.StartDate!.Value.ToDateTime(TimeOnly.MinValue),
                DurationInWeeks.From(request.DurationInWeeks!.Value),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = view.Mesocycle.Id.Value }, MesocycleResponse.From(view));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces the name, objective, start date and duration of the mesocycle. Only its trainer and admins may.
    /// Trainer, athlete and weeks are not changed here.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<MesocycleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMesocycleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var view = await _updateMesocycle.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(id),
                MesocycleName.From(request.Name),
                MesocycleObjective.From(request.Objective),
                request.StartDate!.Value.ToDateTime(TimeOnly.MinValue),
                DurationInWeeks.From(request.DurationInWeeks!.Value),
                cancellationToken);

            return Ok(MesocycleResponse.From(view));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Defines a week of the mesocycle that is not defined yet. Only its trainer and admins may.
    /// </summary>
    /// <remarks>
    /// Weeks are read through the mesocycle, so the <c>Location</c> header points at <c>GET /mesocycles/{id}</c>.
    /// </remarks>
    [HttpPost("{id:guid}/weeks")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<MesocycleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DefineWeek(
        Guid id, [FromBody] DefineMesocycleWeekRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var view = await _defineWeek.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(id),
                WeekNumber.From(request.WeekNumber!.Value),
                request.TrainingWeekType!.Value,
                MesocycleObjective.From(request.Objective),
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = view.Mesocycle.Id.Value }, MesocycleResponse.From(view));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces the training type and objective of a defined week. The week keeps its number. Only the mesocycle's
    /// trainer and admins may.
    /// </summary>
    [HttpPut("{id:guid}/weeks/{weekNumber:int}")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<MesocycleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedefineWeek(
        Guid id, int weekNumber, [FromBody] RedefineMesocycleWeekRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var view = await _redefineWeek.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                Id.From(id),
                WeekNumber.From(weekNumber),
                request.TrainingWeekType!.Value,
                MesocycleObjective.From(request.Objective),
                cancellationToken);

            return Ok(MesocycleResponse.From(view));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Removes a defined week. Refused while any workout is placed in it. The other weeks keep their numbers and the
    /// duration is unchanged. Only the mesocycle's trainer and admins may.
    /// </summary>
    [HttpDelete("{id:guid}/weeks/{weekNumber:int}")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveWeek(Guid id, int weekNumber, CancellationToken cancellationToken)
    {
        try
        {
            await _removeWeek.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(id), WeekNumber.From(weekNumber), cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
