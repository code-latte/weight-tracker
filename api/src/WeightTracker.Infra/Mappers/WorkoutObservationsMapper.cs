using System.Text.Json;
using System.Text.Json.Nodes;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using WeightTracker.Domain.Workouts.Observations;
using WeightTracker.Infra.Entities;
using WeightTracker.Infra.Exceptions;

namespace WeightTracker.Infra.Mappers;

/// <summary>
/// Maps the WorkoutObservations aggregate to its rows in <c>WorkoutObservations</c>, one per observation. The aggregate has
/// no row of its own: its identifier is the workout's, which every row carries. The evidence is stored as a flat JSON
/// object of numbers and lists of numbers.
/// </summary>
public static class WorkoutObservationsMapper
{
    public static IReadOnlyList<WorkoutObservationEntity> MapToEntities(WorkoutObservations observations)
        => observations.Observations
            .Select(observation => new WorkoutObservationEntity
            {
                Id = observation.Id.Value,
                WorkoutId = observations.WorkoutId.Value,
                ExerciseId = observation.ExerciseId?.Value,
                Code = observation.Code.ToString(),
                Severity = (int)observation.Severity,
                Priority = observation.Priority is null ? null : (int)observation.Priority,
                HighlightRank = observation.HighlightRank,
                Data = Serialize(observation.Evidence),
                CreatedAt = observation.CreatedAt,
            })
            .ToList();

    /// <exception cref="PersistenceMappingException">When a stored code, severity, priority or evidence is not valid.</exception>
    public static WorkoutObservations MapToDomain(Id workoutId, IEnumerable<WorkoutObservationEntity> entities)
    {
        try
        {
            return WorkoutObservations.Rehydrate(workoutId, entities.Select(MapObservation));
        }
        catch (InvalidWorkoutObservationException exception)
        {
            throw new PersistenceMappingException($"workout '{workoutId}' has stored observations that are not valid", exception);
        }
    }

    private static WorkoutObservation MapObservation(WorkoutObservationEntity entity)
    {
        if (Enum.TryParse<ObservationCode>(entity.Code, out var code) == false || Enum.IsDefined(code) == false)
            throw new PersistenceMappingException($"'{entity.Code}' is not a known observation code");

        return WorkoutObservation.Rehydrate(
            Id.From(entity.Id),
            entity.ExerciseId is null ? null : Id.From(entity.ExerciseId.Value),
            code,
            (ObservationSeverity)entity.Severity,
            entity.Priority is null ? null : (HighlightPriority)entity.Priority,
            entity.HighlightRank,
            Deserialize(entity.Data),
            DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc));
    }

    private static string Serialize(ObservationEvidence evidence)
    {
        var json = new JsonObject();

        foreach (var (name, value) in evidence.Values)
        {
            json[name] = value switch
            {
                EvidenceValue.Single single => JsonValue.Create(single.Value),
                EvidenceValue.Many many => new JsonArray(many.Values.Select(number => (JsonNode?)JsonValue.Create(number)).ToArray()),
                _ => throw new PersistenceMappingException($"evidence '{name}' has an unknown kind"),
            };
        }

        return json.ToJsonString();
    }

    private static ObservationEvidence Deserialize(string data)
    {
        try
        {
            using var document = JsonDocument.Parse(data);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new PersistenceMappingException("stored evidence is not a JSON object");

            return ObservationEvidence.From(
                document.RootElement.EnumerateObject().Select(property => KeyValuePair.Create(property.Name, ReadValue(property))));
        }
        catch (JsonException exception)
        {
            throw new PersistenceMappingException("stored evidence is not valid JSON", exception);
        }
        catch (InvalidWorkoutObservationException exception)
        {
            throw new PersistenceMappingException("stored evidence is not valid", exception);
        }
    }

    private static EvidenceValue ReadValue(JsonProperty property)
        => property.Value.ValueKind switch
        {
            JsonValueKind.Number => new EvidenceValue.Single(property.Value.GetDecimal()),
            JsonValueKind.Array when property.Value.EnumerateArray().All(item => item.ValueKind == JsonValueKind.Number)
                => new EvidenceValue.Many(property.Value.EnumerateArray().Select(item => item.GetDecimal())),
            _ => throw new PersistenceMappingException($"stored evidence '{property.Name}' is neither a number nor a list of numbers"),
        };
}
