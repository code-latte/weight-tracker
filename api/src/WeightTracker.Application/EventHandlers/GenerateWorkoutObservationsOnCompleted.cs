using WeightTracker.Application.Common.Dispatcher;
using WeightTracker.Application.Services;
using WeightTracker.Domain.Workouts;

namespace WeightTracker.Application.EventHandlers;

/// <summary>
/// Reacts to a completed workout by generating its observations. It only delegates: the work lives in
/// <see cref="WorkoutObservationService"/>, which does not depend on the dispatcher.
/// </summary>
public sealed class GenerateWorkoutObservationsOnCompleted : IEventHandler<WorkoutCompletedEvent>
{
    private readonly WorkoutObservationService _service;

    public GenerateWorkoutObservationsOnCompleted(WorkoutObservationService service)
    {
        _service = service;
    }

    public Task HandleAsync(WorkoutCompletedEvent domainEvent, CancellationToken cancellationToken)
        => _service.Generate(domainEvent.WorkoutId, cancellationToken);
}
