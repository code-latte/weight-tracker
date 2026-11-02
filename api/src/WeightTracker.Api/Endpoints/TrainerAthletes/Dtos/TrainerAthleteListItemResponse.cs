using WeightTracker.Application.UseCases.TrainerAthletes;

namespace WeightTracker.Api.Endpoints.TrainerAthletes.Dtos;

/// <summary>
/// One of a trainer's current athletes: the relationship id and start, the athlete's public profile and the trainer's
/// name.
/// </summary>
public sealed record TrainerAthleteListItemResponse(
    Guid Id,
    Guid AthleteId,
    string Email,
    string Name,
    string Surname,
    string TrainerName,
    string TrainerSurname,
    DateTime StartedAtUtc)
{
    public static TrainerAthleteListItemResponse From(TrainerAthleteView view)
        => new(
            view.Relationship.Id.Value, view.Athlete.Id.Value, view.Athlete.Email.Value, view.Athlete.Name.Value,
            view.Athlete.Surname.Value, view.Trainer.Name.Value, view.Trainer.Surname.Value,
            view.Relationship.StartedAtUtc);
}
