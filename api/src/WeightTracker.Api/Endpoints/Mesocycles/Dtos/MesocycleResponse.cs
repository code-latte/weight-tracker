using WeightTracker.Api.Endpoints.Users.Dtos;
using WeightTracker.Application.UseCases.Mesocycles;
using WeightTracker.Domain.Mesocycles;

namespace WeightTracker.Api.Endpoints.Mesocycles.Dtos;

/// <summary>
/// A mesocycle with its defined weeks, ordered by week number, and the public profiles of its trainer and athlete.
/// </summary>
/// <param name="Trainer">Trainer who prepares the mesocycle, the user whose id is <paramref name="TrainerId"/>.</param>
/// <param name="Athlete">Athlete the mesocycle is for, the user whose id is <paramref name="AthleteId"/>.</param>
public sealed record MesocycleResponse(
    Guid Id,
    Guid TrainerId,
    Guid AthleteId,
    string Name,
    string Objective,
    DateOnly StartDate,
    int DurationInWeeks,
    IReadOnlyList<MesocycleWeekResponse> Weeks,
    UserProfileResponse Trainer,
    UserProfileResponse Athlete)
{
    public static MesocycleResponse From(MesocycleView view)
    {
        var mesocycle = view.Mesocycle;

        return new(
            mesocycle.Id.Value,
            mesocycle.TrainerId.Value,
            mesocycle.AthleteId.Value,
            mesocycle.Name.Value,
            mesocycle.Objective.Value,
            DateOnly.FromDateTime(mesocycle.StartDate),
            mesocycle.DurationInWeeks.Value,
            mesocycle.Weeks.Select(MesocycleWeekResponse.From).ToList(),
            UserProfileResponse.From(view.Trainer),
            UserProfileResponse.From(view.Athlete));
    }
}

/// <summary>
/// One defined week. It is identified by its number inside the mesocycle.
/// </summary>
/// <param name="TrainingWeekType">Kind of training, as an upper-case name such as <c>ACCUMULATION</c>.</param>
public sealed record MesocycleWeekResponse(int WeekNumber, string TrainingWeekType, string Objective)
{
    public static MesocycleWeekResponse From(MesocycleWeek week)
        => new(week.WeekNumber.Value, week.TrainingWeekType.ToString(), week.Objective.Value);
}
