namespace WeightTracker.Api.Endpoints;

/// <summary>
/// Marks an <c>[AllowAnonymous]</c> action of an <see cref="AuthorizedController"/> that answers differently to an
/// authenticated caller. Without an <c>Authorization</c> header the action runs anonymously. With one, the token
/// must be valid and its user must exist, otherwise the request ends with <c>401</c>: a client with an expired
/// token must refresh it instead of silently receiving the anonymous result.
/// </summary>
/// <remarks>
/// Every other <c>[AllowAnonymous]</c> action ignores the header exactly as before.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class OptionalCallerAttribute : Attribute
{
}
