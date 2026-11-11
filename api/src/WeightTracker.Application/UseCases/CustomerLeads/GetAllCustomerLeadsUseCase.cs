using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.CustomerLeads;

/// <summary>
/// Lists every registered customer lead for an administrator.
/// </summary>
/// <remarks>
/// The caller is passed explicitly instead of through an ambient accessor, so the
/// Application layer stays free of request-scoped infrastructure.
/// </remarks>
public class GetAllCustomerLeadsUseCase
{
    private ICustomerLeadRepository _leads;

    public GetAllCustomerLeadsUseCase(ICustomerLeadRepository leads)
    {
        _leads = leads;
    }

    /// <returns>Every stored lead, newest registration first.</returns>
    /// <exception cref="UnauthorizedOperationException">When the caller is not an administrator.</exception>
    public Task<IReadOnlyList<CustomerLeadView>> Execute(Role callerRole, CancellationToken cancellationToken)
    {
        if (callerRole != Role.ADMIN)
            throw new UnauthorizedOperationException("GET /customer-leads");

        return _leads.GetAll(cancellationToken);
    }
}
