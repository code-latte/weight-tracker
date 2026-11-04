using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WeightTracker.Api.Common;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;

namespace WeightTracker.Api.Endpoints;

/// <summary>
/// Base of every controller with protected routes. After <c>[Authorize]</c> has accepted the
/// token and before the action runs, it resolves the caller through
/// <see cref="GetAuthenticatedUserUseCase"/> and exposes it as <see cref="CurrentUser"/>.
/// </summary>
/// <remarks>
/// A token without a usable subject, or whose user no longer exists, ends the request with
/// <c>401</c> and the action never runs. Only then is <see cref="RequireRoleAttribute"/> checked
/// against the stored role, so a caller that is not who the token says is never answered with <c>403</c>. Actions marked <c>[AllowAnonymous]</c> are not
/// resolved and see a <c>null</c> <see cref="CurrentUser"/>, unless they also carry <see cref="OptionalCallerAttribute"/>
/// and the request sends an <c>Authorization</c> header.
/// </remarks>
[Authorize]
public abstract class AuthorizedController : ControllerBase, IAsyncActionFilter
{
    private readonly GetAuthenticatedUserUseCase _getAuthenticatedUser;

    protected AuthorizedController(GetAuthenticatedUserUseCase getAuthenticatedUser, ExceptionMapper exceptionMapper)
    {
        _getAuthenticatedUser = getAuthenticatedUser;
        ExceptionMapper = exceptionMapper;
    }

    protected ExceptionMapper ExceptionMapper { get; }

    /// <summary>
    /// The caller of the current request. <c>null</c> only on <c>[AllowAnonymous]</c> actions.
    /// </summary>
    protected AuthenticatedUser? CurrentUser { get; private set; }

    // Public, so MVC would otherwise discover it as a route
    [NonAction]
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var metadata = context.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            var optionalCaller = metadata.OfType<OptionalCallerAttribute>().Any()
                && Request.Headers.ContainsKey("Authorization");

            // A token that was sent but not accepted must not fall back to the anonymous result
            if (optionalCaller && await ResolveCaller(context) == false)
                return;

            await next();
            return;
        }

        if (await ResolveCaller(context) == false)
            return;

        var requiredRoles = metadata.OfType<RequireRoleAttribute>().FirstOrDefault()?.Roles;

        if (requiredRoles is not null && requiredRoles.Contains(CurrentUser!.Role) == false)
        {
            context.Result = StatusCode(StatusCodes.Status403Forbidden);
            return;
        }

        await next();
    }

    /// <summary>
    /// Resolves <see cref="CurrentUser"/> from the token subject. Sets the result of the request and returns
    /// <c>false</c> when the token has no usable subject or its user no longer exists.
    /// </summary>
    private async Task<bool> ResolveCaller(ActionExecutingContext context)
    {
        if (User.Identity?.IsAuthenticated != true || Id.TryFrom(User.FindFirst("sub")?.Value, out var callerId) == false)
        {
            context.Result = Unauthorized();
            return false;
        }

        try
        {
            CurrentUser = await _getAuthenticatedUser.Execute(callerId!, HttpContext.RequestAborted);
        }
        catch (Exception exception)
        {
            context.Result = ExceptionMapper.Map(exception);
            return false;
        }

        return true;
    }
}
