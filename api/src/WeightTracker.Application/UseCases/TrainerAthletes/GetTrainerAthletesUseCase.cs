using WeightTracker.Application.Exceptions;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Common;

namespace WeightTracker.Application.UseCases.TrainerAthletes;

/// <summary>
/// Lists a trainer's current athletes.
/// </summary>
/// <remarks>
/// Who may ask for which trainer is decided by the route: a trainer always asks for themselves, an admin for any
/// trainer.
/// </remarks>
public class GetTrainerAthletesUseCase
{
    private IUserRepository _users;

    private ITrainerAthleteRepository _trainerAthletes;

    private TrainerAthleteViewLoader _views;

    public GetTrainerAthletesUseCase(
        IUserRepository users,
        ITrainerAthleteRepository trainerAthletes,
        TrainerAthleteViewLoader views)
    {
        _users = users;
        _trainerAthletes = trainerAthletes;
        _views = views;
    }

    /// <returns>The trainer's active relationships with their trainer and athletes, ordered by athlete email ascending.</returns>
    /// <exception cref="UserNotFoundException">When no user has the given id.</exception>
    public async Task<IReadOnlyList<TrainerAthleteView>> Execute(Id trainerId, CancellationToken cancellationToken)
    {
        if (await _users.GetById(trainerId, cancellationToken) is null)
            throw new UserNotFoundException(trainerId);

        var relationships = await _trainerAthletes.GetAllActiveByTrainerId(trainerId, cancellationToken);

        var views = await _views.Load(relationships, cancellationToken);

        return views
            .OrderBy(view => view.Athlete.Email.Value, StringComparer.Ordinal)
            .ToList();
    }
}
