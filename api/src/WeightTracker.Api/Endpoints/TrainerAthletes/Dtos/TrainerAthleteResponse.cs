using WeightTracker.Application.UseCases.TrainerAthletes;

namespace WeightTracker.Api.Endpoints.TrainerAthletes.Dtos;

/// <summary>
/// A relationship with the names of its trainer and athlete.
/// </summary>
public sealed record TrainerAthleteResponse(
    Guid Id,
    Guid TrainerId,
    string TrainerName,
    string TrainerSurname,
    Guid AthleteId,
    string AthleteName,
    string AthleteSurname,
    string Status,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc)
{
    public static TrainerAthleteResponse From(TrainerAthleteView view)
        => new(
            view.Relationship.Id.Value, view.Trainer.Id.Value, view.Trainer.Name.Value, view.Trainer.Surname.Value,
            view.Athlete.Id.Value, view.Athlete.Name.Value, view.Athlete.Surname.Value,
            view.Relationship.Status.ToString(), view.Relationship.StartedAtUtc, view.Relationship.FinishedAtUtc);
}
