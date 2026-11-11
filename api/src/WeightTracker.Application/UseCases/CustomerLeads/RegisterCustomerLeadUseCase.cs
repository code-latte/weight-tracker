using WeightTracker.Application.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Application.UseCases.CustomerLeads;

/// <summary>
/// Stores the email of someone interested in the product. An email is stored once; registering it again
/// succeeds without writing, so the caller cannot tell a new email from a known one.
/// </summary>
public class RegisterCustomerLeadUseCase
{
    private ICustomerLeadRepository _leads;

    private IClock _clock;

    private IUnitOfWork _unitOfWork;

    public RegisterCustomerLeadUseCase(
        ICustomerLeadRepository leads,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _leads = leads;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    /// <exception cref="InvalidEmailException">When the email is missing, malformed or too long.</exception>
    public async Task Execute(string email, CancellationToken cancellationToken)
    {
        var parsedEmail = Email.From(email);

        var alreadyRegistered = await _leads.ExistsByEmail(parsedEmail, cancellationToken);

        if (alreadyRegistered)
            return;

        await _leads.Add(Id.New(), parsedEmail, _clock.UtcNow, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (CustomerLeadAlreadyExistsException)
        {
            // A concurrent request stored the same email after the check; the outcome is the same
        }
    }
}
