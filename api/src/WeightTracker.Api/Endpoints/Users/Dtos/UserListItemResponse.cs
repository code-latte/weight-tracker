using WeightTracker.Application.UseCases.Users;

namespace WeightTracker.Api.Endpoints.Users.Dtos;

/// <summary>
/// A user as an administrator lists them: the summary fields plus the public profile of their current trainer, which
/// is <c>null</c> for trainers, administrators and athletes without one.
/// </summary>
public sealed record UserListItemResponse(
    Guid Id, string Email, string Name, string Surname, string Role, bool IsActive, DateTime? DeactivatedAtUtc,
    UserProfileResponse? Trainer)
{
    public static UserListItemResponse From(UserWithTrainerView view)
        => new(
            view.User.Id.Value, view.User.Email.Value, view.User.Name.Value, view.User.Surname.Value,
            view.User.Role.ToString(), view.User.IsActive, view.User.DeactivatedAtUtc,
            view.Trainer is null ? null : UserProfileResponse.From(view.Trainer));
}
