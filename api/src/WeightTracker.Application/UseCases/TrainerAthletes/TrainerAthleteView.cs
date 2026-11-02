using WeightTracker.Domain.TrainerAthletes;
using WeightTracker.Domain.Users;

namespace WeightTracker.Application.UseCases.TrainerAthletes;

/// <summary>
/// A relationship together with the trainer and the athlete it refers to, so callers can show their names without
/// looking them up.
/// </summary>
public sealed record TrainerAthleteView(TrainerAthlete Relationship, User Trainer, User Athlete);
