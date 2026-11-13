using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Mesocycles.Dtos;
using WeightTracker.Api.Endpoints.Users.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Application.UseCases.Users;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Users;

/// <summary>
/// Users. The <c>me</c> routes act on the caller's own profile, the user the token belongs to;
/// <c>GET /users</c> lets an administrator list every user, and <c>POST /users</c> lets an administrator
/// create someone else's account. The <c>activate</c> and <c>deactivate</c> routes let an administrator
/// switch another account on and off. <c>GET /users/{userId}/mesocycles</c> lets a trainer or an administrator
/// list the mesocycles planned for one athlete, and <c>GET /users/{userId}/trainer</c> lets a user read their own
/// current trainer, or an administrator anyone's. <c>GET /users/me/exercises/{exerciseId}/last-performance</c> lets
/// any caller read what they did the last time they finished a workout with an exercise, and
/// <c>GET /users/{userId}/exercises/{exerciseId}/history</c> lets a caller read how a user's sessions of an exercise evolve.
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
    private readonly GetUserMesocyclesUseCase _getUserMesocycles;
    private readonly GetUserTrainerUseCase _getUserTrainer;
    private readonly GetMyExerciseLastPerformanceUseCase _getMyExerciseLastPerformance;
    private readonly GetUserExerciseHistoryUseCase _getUserExerciseHistory;

    public UsersController(
        UpdateMyProfileUseCase updateMyProfile,
        CreateUserUseCase createUser,
        GetAllUsersUseCase getAllUsers,
        ActivateUserUseCase activateUser,
        DeactivateUserUseCase deactivateUser,
        GetUserMesocyclesUseCase getUserMesocycles,
        GetUserTrainerUseCase getUserTrainer,
        GetMyExerciseLastPerformanceUseCase getMyExerciseLastPerformance,
        GetUserExerciseHistoryUseCase getUserExerciseHistory,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _updateMyProfile = updateMyProfile;
        _createUser = createUser;
        _getAllUsers = getAllUsers;
        _activateUser = activateUser;
        _deactivateUser = deactivateUser;
        _getUserMesocycles = getUserMesocycles;
        _getUserTrainer = getUserTrainer;
        _getMyExerciseLastPerformance = getMyExerciseLastPerformance;
        _getUserExerciseHistory = getUserExerciseHistory;
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

    /// <summary>
    /// Lists the mesocycles planned for one athlete, latest start date first. A trainer gets the ones they prepare
    /// for that athlete; an administrator gets all of them. Regular users may not call it, not even for themselves.
    /// </summary>
    [HttpGet("{userId:guid}/mesocycles")]
    [RequireRole(Role.TRAINER, Role.ADMIN)]
    [ProducesResponseType<IReadOnlyList<MesocycleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMesocycles(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var views = await _getUserMesocycles.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(userId), cancellationToken);

            return Ok(views.Select(MesocycleResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Reads the current trainer of one user, the trainer of their active relationship. A user may ask only about
    /// themselves and an administrator about anyone. Answers <c>204</c> when the user has no active trainer.
    /// </summary>
    [HttpGet("{userId:guid}/trainer")]
    [RequireRole(Role.USER, Role.ADMIN)]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrainer(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var trainer = await _getUserTrainer.Execute(CurrentUser!.Id, CurrentUser.Role, Id.From(userId), cancellationToken);

            return trainer is null ? NoContent() : Ok(UserProfileResponse.From(trainer));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Reads the sets the caller recorded the last time they finished a workout with the exercise, with that workout's
    /// date and the effort it prescribed. Only <c>COMPLETED</c> and <c>REVIEWED</c> workouts count. Answers <c>204</c>
    /// when the caller never did it, and <c>404</c> when the exercise does not exist or the caller may not see it.
    /// </summary>
    [HttpGet("me/exercises/{exerciseId:guid}/last-performance")]
    [ProducesResponseType<ExerciseLastPerformanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyExerciseLastPerformance(Guid exerciseId, CancellationToken cancellationToken)
    {
        try
        {
            var performance = await _getMyExerciseLastPerformance.Execute(Id.From(exerciseId), CurrentUser!, cancellationToken);

            return performance is null ? NoContent() : Ok(ExerciseLastPerformanceResponse.From(performance));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Reads the latest finished sessions in which a user did an exercise, newest first, each with its max weight, volume
    /// (reps × kg) and estimated one-rep max (Epley). Only <c>COMPLETED</c> and <c>REVIEWED</c> workouts count. A user
    /// may only read their own history; a trainer reads their own, or for anyone else only the sessions they prescribed;
    /// an administrator reads everyone's. Answers an empty list when the user never did the exercise, <c>400</c> when
    /// <c>limit</c> is outside 1 to 50, and <c>404</c> when the user or the exercise does not exist or the caller may not
    /// see the exercise.
    /// </summary>
    /// <param name="userId">User whose sessions are read.</param>
    /// <param name="exerciseId">Exercise whose sessions are read.</param>
    /// <param name="limit">Number of sessions to return, from 1 to 50. Defaults to 10.</param>
    [HttpGet("{userId:guid}/exercises/{exerciseId:guid}/history")]
    [ProducesResponseType<IReadOnlyList<ExerciseHistorySessionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExerciseHistory(Guid userId, Guid exerciseId, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        try
        {
            var sessions = await _getUserExerciseHistory.Execute(
                CurrentUser!.Id, CurrentUser.Role, Id.From(userId), Id.From(exerciseId), limit, cancellationToken);

            return Ok(sessions.Select(ExerciseHistorySessionResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
