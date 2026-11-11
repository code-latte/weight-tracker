namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised on commit when another request stored the same lead email between the existence check and the
/// insert. The registering use case always catches it, because a duplicate lead must look exactly like a new
/// one to the caller, so it never reaches a controller and has no <c>ExceptionMapper</c> entry. If it ever
/// escapes, the mapper's default 500 surfaces the bug.
/// </summary>
public sealed class CustomerLeadAlreadyExistsException : ApplicationLayerException
{
    public CustomerLeadAlreadyExistsException(Exception innerException)
        : base("A customer lead with this email is already stored", innerException)
    { }
}
