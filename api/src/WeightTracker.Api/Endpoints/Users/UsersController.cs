using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Users.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Users;

/// <summary>
/// Users. The <c>me</c> routes act on the caller's own profile, the user the token belongs to;
/// <c>GET /users</c> lets an administrator list every user, and <c>POST /users</c> lets an administrator
/// create someone else's account. The <c>activate</c> and <c>deactivate</c> routes let an administrator
/// switch another account on and off.
/// </summary>
[ApiController]
[Route("users")]
public class UsersController : AuthorizedController
{
    private readonly UpdateMyProfileUseCase _updateMyProfile;
    private readonly CreateUserUseCase _createUser;
    private readonly GetAllUsersUseCase _getAllUsers;
    private readonly ActivateUserUseCase _activateUser;
    private readonly DeactivateUserUseCase _deactivateUser;

    public UsersController(
        UpdateMyProfileUseCase updateMyProfile,
        CreateUserUseCase createUser,
        GetAllUsersUseCase getAllUsers,
        ActivateUserUseCase activateUser,
        DeactivateUserUseCase deactivateUser,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _updateMyProfile = updateMyProfile;
        _createUser = createUser;
        _getAllUsers = getAllUsers;
        _activateUser = activateUser;
        _deactivateUser = deactivateUser;
    }

    /// <summary>
    /// Reads the caller's stored profile. It is the user already resolved for this request.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetMe()
    {
        try
        {
            return Ok(UserProfileResponse.From(CurrentUser!));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces the caller's name and surname. Email and role cannot be changed here.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _updateMyProfile.Execute(CurrentUser!.Id, request.ToDto(), cancellationToken);

            return Ok(UserProfileResponse.From(user));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Lists every user with their current trainer. Only administrators may call it; trainers list their athletes through
    /// <c>GET /trainer-athletes</c>.
    /// </summary>
    [HttpGet]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType<IReadOnlyList<UserListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _getAllUsers.Execute(CurrentUser!.Role, cancellationToken);

            return Ok(users.Select(UserListItemResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Creates a user of any role on someone else's behalf. Only administrators may call it. No session
    /// is started for the created user, who logs in on their own.
    /// </summary>
    /// <remarks>
    /// There is no route that reads another user, so the response carries no <c>Location</c>.
    /// </remarks>
    [HttpPost]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType<CreatedUserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _createUser.Execute(request.ToDto(), cancellationToken);

            return StatusCode(StatusCodes.Status201Created, CreatedUserResponse.From(user));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Switches a deactivated account back on. Only administrators may call it.
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType<UserSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _activateUser.Execute(CurrentUser!.Role, Id.From(id), cancellationToken);

            return Ok(UserSummaryResponse.From(user));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Switches another user's account off, so they can no longer log in nor be given workouts. Only
    /// administrators may call it, and never on their own account.
    /// </summary>
    [HttpPut("{id:guid}/deactivate")]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType<UserSummaryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _deactivateUser.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(id), cancellationToken);

            return Ok(UserSummaryResponse.From(user));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
