using WeightTracker.Domain.Mesocycles;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.Mesocycles;

/// <summary>
/// A mesocycle together with the trainer and the athlete it refers to, so callers can show their names without
/// looking them up.
/// </summary>
public sealed record MesocycleView(Mesocycle Mesocycle, User Trainer, User Athlete);
