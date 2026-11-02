using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.AthleteInvitations.Dtos;
using WeightTracker.Application.UseCases.AthleteInvitations;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.AthleteInvitations;

/// <summary>
/// Invitations from a trainer to an email to become their athlete. The trainer invites and revokes; the invited
/// user accepts or rejects with the token the trainer passed on. Accepting does not link them yet. Administrators
/// list every invitation and trainers their own.
/// </summary>
[ApiController]
[Route("athlete-invitations")]
public class AthleteInvitationsController : AuthorizedController
{
    private readonly InviteAthleteUseCase _invite;
    private readonly RevokeInvitationUseCase _revoke;
    private readonly AcceptInvitationUseCase _accept;
    private readonly RejectInvitationUseCase _reject;
    private readonly GetTrainerInvitationsUseCase _getInvitations;

    public AthleteInvitationsController(
        InviteAthleteUseCase invite,
        RevokeInvitationUseCase revoke,
        AcceptInvitationUseCase accept,
        RejectInvitationUseCase reject,
        GetTrainerInvitationsUseCase getInvitations,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper
    )
        : base(getAuthenticatedUser, exceptionMapper)
    {
        _invite = invite;
        _revoke = revoke;
        _accept = accept;
        _reject = reject;
        _getInvitations = getInvitations;
    }

    /// <summary>
    /// Lists the invitations the caller may see, newest first: every invitation for an administrator, only their
    /// own for a trainer. No token or hash is returned. The scoping rule lives in the use case.
    /// </summary>
    [HttpGet]
    [RequireRole(Role.ADMIN, Role.TRAINER)]
    [ProducesResponseType<IReadOnlyList<InvitationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var invitations = await _getInvitations.Execute(
                CurrentUser!.Id,
                CurrentUser.Role,
                cancellationToken
            );

            return Ok(invitations.Select(InvitationResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Invites an email to become the caller's athlete. Only trainers may call it. The response is the only one
    /// that carries the invitation token.
    /// </summary>
    /// <remarks>
    /// There is no route that reads a single invitation, so the response carries no <c>Location</c>.
    /// </remarks>
    [HttpPost]
    [RequireRole(Role.TRAINER)]
    [ProducesResponseType<CreatedInvitationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Invite(
        [FromBody] InviteAthleteRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var created = await _invite.Execute(CurrentUser!.Id, request.Email, cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                CreatedInvitationResponse.From(created)
            );
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Revokes a pending invitation. Only the trainer who sent it may.
    /// </summary>
    [HttpPut("{invitationId:guid}/revoke")]
    [RequireRole(Role.TRAINER)]
    [ProducesResponseType<InvitationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Revoke(Guid invitationId, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await _revoke.Execute(
                CurrentUser!.Id,
                Id.From(invitationId),
                cancellationToken
            );

            return Ok(InvitationResponse.From(invitation));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Accepts a pending invitation. Only the invited user may, with the invitation token.
    /// </summary>
    [HttpPut("{invitationId:guid}/accept")]
    [RequireRole(Role.USER)]
    [ProducesResponseType<InvitationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(
        Guid invitationId,
        [FromBody] InvitationTokenRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var invitation = await _accept.Execute(
                CurrentUser!.Id,
                Id.From(invitationId),
                request.Token,
                cancellationToken
            );

            return Ok(InvitationResponse.From(invitation));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Rejects a pending invitation. Only the invited user may, with the invitation token.
    /// </summary>
    [HttpPut("{invitationId:guid}/reject")]
    [RequireRole(Role.USER)]
    [ProducesResponseType<InvitationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        Guid invitationId,
        [FromBody] InvitationTokenRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var invitation = await _reject.Execute(
                CurrentUser!.Id,
                Id.From(invitationId),
                request.Token,
                cancellationToken
            );

            return Ok(InvitationResponse.From(invitation));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
