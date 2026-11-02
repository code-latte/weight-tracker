using System.ComponentModel.DataAnnotations;

namespace WeightTracker.Api.Endpoints.TrainerAthletes.Dtos;

/// <summary>
/// Body of <c>POST /trainer-athletes/{trainerId}</c> and <c>PUT /trainer-athletes/{trainerId}/end</c>.
/// <c>AthleteId</c> is nullable so a missing value is rejected instead of defaulting to an empty id.
/// </summary>
public sealed record TrainerAthleteRequest
{
    [Required]
    public Guid? AthleteId { get; init; }
}
