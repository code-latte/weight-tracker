using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Users;

/// <summary>
/// A user together with their current trainer, so callers can show who trains them without looking it up. The trainer
/// is <c>null</c> for trainers, administrators and athletes without an active relationship.
/// </summary>
public sealed record UserWithTrainerView(User User, User? Trainer);
