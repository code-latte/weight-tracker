using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.CustomerLeads.Dtos;
using WeightTracker.Application.UseCases.CustomerLeads;

namespace WeightTracker.Api.Endpoints.CustomerLeads;

[ApiController]
[Route("customer-leads")]
[AllowAnonymous]
public class CustomerLeadsController : ControllerBase
{
    private readonly RegisterCustomerLeadUseCase _register;
    private readonly ExceptionMapper _exceptionMapper;

    public CustomerLeadsController(
        RegisterCustomerLeadUseCase register,
        ExceptionMapper exceptionMapper
    )
    {
        _register = register;
        _exceptionMapper = exceptionMapper;
    }

    /// <summary>
    /// Registers the email of someone interested in the product. A known email answers the same as a new one.
    /// </summary>
    [HttpPost]
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
            return _exceptionMapper.Map(exception);
        }
    }
}
