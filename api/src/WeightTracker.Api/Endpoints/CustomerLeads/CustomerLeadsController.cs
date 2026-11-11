using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.CustomerLeads.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.CustomerLeads;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.CustomerLeads;

/// <summary>
/// Customer leads. <c>POST /customer-leads</c> lets anyone leave their email, and <c>GET /customer-leads</c> lets an
/// administrator list every registered lead.
/// </summary>
[ApiController]
[Route("customer-leads")]
public class CustomerLeadsController : AuthorizedController
{
    private readonly RegisterCustomerLeadUseCase _register;
    private readonly GetAllCustomerLeadsUseCase _getAll;

    public CustomerLeadsController(
        RegisterCustomerLeadUseCase register,
        GetAllCustomerLeadsUseCase getAll,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _register = register;
        _getAll = getAll;
    }

    /// <summary>
    /// Lists every registered lead, newest first. Only administrators may call it.
    /// </summary>
    [HttpGet]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType<IReadOnlyList<CustomerLeadResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var leads = await _getAll.Execute(CurrentUser!.Role, cancellationToken);

            return Ok(leads.Select(CustomerLeadResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Registers the email of someone interested in the product. A known email answers the same as a new one.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(CustomerLeadsRateLimitOptions.PolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCustomerLeadRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _register.Execute(request.Email, cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
