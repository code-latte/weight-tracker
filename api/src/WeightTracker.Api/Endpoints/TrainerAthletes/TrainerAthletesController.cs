using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.TrainerAthletes.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.TrainerAthletes;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.TrainerAthletes;

/// <summary>
/// Trainer–athlete relationships. An administrator links an athlete to a trainer directly; either side or an
/// administrator ends it; a trainer lists their own athletes and an administrator any trainer's.
/// </summary>
[ApiController]
[Route("trainer-athletes")]
public class TrainerAthletesController : AuthorizedController
{
    private readonly LinkAthleteToTrainerUseCase _link;
    private readonly EndTrainerAthleteRelationshipUseCase _end;
    private readonly GetTrainerAthletesUseCase _getAthletes;

    public TrainerAthletesController(
        LinkAthleteToTrainerUseCase link,
        EndTrainerAthleteRelationshipUseCase end,
        GetTrainerAthletesUseCase getAthletes,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _link = link;
        _end = end;
        _getAthletes = getAthletes;
    }

    /// <summary>
    /// Lists the caller's current athletes, ordered by email. Only trainers may call it.
    /// </summary>
    [HttpGet]
    [RequireRole(Role.TRAINER)]
    [ProducesResponseType<IReadOnlyList<TrainerAthleteListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        try
        {
            var athletes = await _getAthletes.Execute(CurrentUser!.Id, cancellationToken);

            return Ok(athletes.Select(TrainerAthleteListItemResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Lists a trainer's current athletes, ordered by email. Only administrators may call it.
    /// </summary>
    [HttpGet("{trainerId:guid}")]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType<IReadOnlyList<TrainerAthleteListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTrainer(Guid trainerId, CancellationToken cancellationToken)
    {
        try
        {
            var athletes = await _getAthletes.Execute(Id.From(trainerId), cancellationToken);

            return Ok(athletes.Select(TrainerAthleteListItemResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Links an athlete to a trainer, starting an active relationship. Only administrators may call it.
    /// </summary>
    /// <remarks>
    /// There is no route that reads a single relationship, so the response carries no <c>Location</c>.
    /// </remarks>
    [HttpPost("{trainerId:guid}")]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType<TrainerAthleteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Link(
        Guid trainerId, [FromBody] TrainerAthleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var relationship = await _link.Execute(Id.From(trainerId), Id.From(request.AthleteId!.Value), cancellationToken);

            return StatusCode(StatusCodes.Status201Created, TrainerAthleteResponse.From(relationship));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Ends the active relationship between the trainer and the athlete. Administrators may end any; otherwise
    /// only the trainer or the athlete of that relationship. The relationship is kept as history.
    /// </summary>
    [HttpPut("{trainerId:guid}/end")]
    [ProducesResponseType<TrainerAthleteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> End(
        Guid trainerId, [FromBody] TrainerAthleteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var relationship = await _end.Execute(
                CurrentUser!.Id, CurrentUser.Role, Id.From(trainerId), Id.From(request.AthleteId!.Value), cancellationToken);

            return Ok(TrainerAthleteResponse.From(relationship));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
