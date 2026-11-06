using WeightTracker.Api.Endpoints.Workouts.Dtos;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Api.Endpoints.WorkoutTemplates.Dtos;

/// <summary>
/// A workout template with its exercises in order. Entries reference the catalog exercise by <c>ExerciseId</c>
/// only, as in <c>GET /workouts/{id}</c>.
/// </summary>
/// <param name="TrainerId">Trainer who owns the template.</param>
public sealed record WorkoutTemplateResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid TrainerId,
    IReadOnlyList<WorkoutExerciseDetailResponse> Exercises)
{
    public static WorkoutTemplateResponse From(WorkoutTemplate template)
        => new(
            template.Id.Value,
            template.Name.Value,
            template.Description?.Value,
            template.TrainerId.Value,
            template.Exercises.Select(WorkoutExerciseDetailResponse.From).ToList());
}
