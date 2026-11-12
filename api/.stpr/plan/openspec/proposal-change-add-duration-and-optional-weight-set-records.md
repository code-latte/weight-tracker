## Change

Let an athlete record sets that have no weight (jumping jacks, 10 reps) or that are timed (plank, 45 s), and check a set as done in one call, through the existing set-record endpoints.

## Objective

Today a set record always carries reps and a weight, both required, and it is only allowed when every prescription of the exercise is rep-based. A plank cannot be recorded at all, and a bodyweight exercise forces the athlete to type a weight. Athletes need to track every set they do, including when there is nothing to measure beyond "done". This change extends `SetRecord` and the `POST`/`PUT .../set-records` routes. It adds no new routes.

## Capabilities

- Record a set of a time-based exercise (`Duration` or `Minutes` target) with the seconds actually held.
- Record a set without a weight. A missing weight means "not tracked", which is different from `0` (bodyweight).
- Check a set as done with an empty body. The server fills in the value from the prescription of that set.
- Correct a recorded duration or weight through the existing `PUT`.

## Module Classification

- Domain: Workouts -> mutable aggregate (`Workout` root, `WorkoutExercise` entity, `SetRecord` child entity) -> `src/WeightTracker.Domain/Workouts`
- Application: Workouts set-record use cases -> mutable aggregate orchestration -> `src/WeightTracker.Application/UseCases/Workouts/AddSetRecordUseCase.cs`, `UpdateSetRecordUseCase.cs`
- API: Workouts set-record routes -> mutable aggregate endpoints -> `src/WeightTracker.Api/Endpoints/Workouts/WorkoutsController.cs`, `Dtos/AddOrUpdateSetRecordRequest.cs`, `Dtos/WorkoutResponse.cs`
- Infra: Workouts persistence -> mutable aggregate persistence -> `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs`, `src/WeightTracker.Infra/Persistence/WeightTrackerDbContext.cs`
- Migrations: altered table -> `202611071200_CreateSetRecordsTable` -> `src/WeightTracker.Migrations/Migrations`

Workouts is a real mutable aggregate, not a catalog.

## Canonical Template Mapping

- `SetResult` value object -> `SetTarget` in `src/WeightTracker.Domain/Workouts/SetTarget.cs` -> same closed hierarchy of sealed records, one per measurement kind.
- `DurationInSeconds` value object -> `RepCount` / `TimeInMinutes` -> same `From` factory with validation that throws a domain exception.
- `Workout.AddSetRecord` / `UpdateSetRecord` -> their current implementation -> only the parameters and the target guard change.
- Use cases -> current `AddSetRecordUseCase` / `UpdateSetRecordUseCase` -> same orchestration, nullable inputs.
- Aggregate tests -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests`
- Integration tests -> existing set-record route tests in `test/WeightTracker.Integration`.
- Migration -> `202611101200_AddCompletedAtToWorkouts` -> nearest "alter an existing table" migration.

## State And Mutability

- `SetRecord` replaces its `Reps` and `Weight` with:
  - `SetResult Result`, which is either `SetResult.Reps(RepCount)` or `SetResult.Duration(DurationInSeconds)`.
  - `Weight? Weight`, which is optional for both kinds. `null` means not tracked.
- `SetRecord.Create`, `Rehydrate` and `Change` take `(SetResult, Weight?)`.
- `Workout.AddSetRecord(exerciseId, SetResult?, Weight?, registeredAtUtc)`: a `null` result means "done as prescribed" and the aggregate resolves it (see the rules).
- `Workout.UpdateSetRecord(exerciseId, setRecordId, SetResult, Weight?, updatedAtUtc)`: the result is always explicit.
- Existing rows become `SetResult.Reps` with their current weight. No existing data changes meaning.

## Business Rules

- **A record means done.** There is no `completed` flag. A set that was not done has no record, and unchecking a set uses the existing `DELETE`.
- **Recordable entries.** An exercise can have set records when all its prescriptions are rep-based (`Reps`, `RepsRange`, `MaxReps`) or all are time-based (`Duration`, `Minutes`). An entry that mixes rep-based and time-based prescriptions still throws `SetRecordNotAllowedForTargetException` (409).
- **Kind must match.** A `SetResult.Reps` is only accepted on a rep-based entry, and a `SetResult.Duration` only on a time-based entry. A mismatch throws `SetRecordNotAllowedForTargetException` (409).
- **Done as prescribed (empty result).** The set being recorded is the next prescribed set: with `n` existing records, it is set `n + 1` of the expanded prescriptions in order (for example `2x10 reps, 1x8 reps` expands to 10, 10, 8).
  - `Reps(v)` -> `SetResult.Reps(v)`.
  - `Duration(v)` -> `SetResult.Duration(v)`.
  - `Minutes(m)` -> `SetResult.Duration(m × 60)`, rounded to the nearest second.
  - `RepsRange`, `MaxReps`, or no prescribed set left (beyond `TotalSets`) -> a new `SetResultRequiredException` (400). The athlete must send the value.
- **Values.** Reps ≥ 1 (`RepCount`). Duration is a whole number of seconds ≥ 1 (new `DurationInSeconds`, `InvalidDurationInSecondsException` -> 400). Weight, when present, follows the existing `Weight` rules (≥ 0, at most 2 decimals). Invalid values throw before any state changes.
- **No cap.** Recording more sets than `TotalSets`, or more or less than prescribed, is still allowed when the value is explicit.
- **Prescription guard.** While a `WorkoutExercise` has set records, `Workout.UpdateExercise` refuses prescriptions of the other kind: a rep-based entry cannot switch to time-based, and the reverse. It throws `SetRecordNotAllowedForTargetException` (409) for every caller including an `ADMIN`. This generalizes the current rep-only guard.
- **Unchanged.** Status rules (`WorkoutResultsLockedException`), `ExerciseNotInWorkoutException`, ordering, reordering and removal are unchanged. Reordering does not revalidate kinds, because every record of an entry has the same kind.
- Recording is still not a content edit. The workout edit rule does not apply.

## Request And Response

`POST` and `PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records[/{setRecordId}]`, same body for both:

```json
{ "reps": 10, "durationSeconds": null, "weight": null }
```

- All fields are optional and `[Required]` is removed.
- Structural validation in the DTO: `reps` and `durationSeconds` cannot both be sent (400).
- `POST` with neither `reps` nor `durationSeconds` means "done as prescribed".
- `PUT` must send exactly one of them (400 otherwise).

`SetRecordResponse` becomes `(Id, Type, Reps?, DurationSeconds?, Weight?, RegisteredAtUtc)`:

- `Type` is `"Reps"` or `"Duration"`, using the same naming style as `SetTargetTypes`.
- `Weight` is now nullable. This is a breaking change for clients.

`ExceptionMapper`:

- `SetResultRequiredException` -> 400.
- `InvalidDurationInSecondsException` -> 400.

## Roles And Visibility

- Unchanged: only the workout's athlete or an `ADMIN` may record or correct sets (`WorkoutRecordingAuthorizer`).

## Business Identifiers And Mutability

- `SetRecord.Id` is unchanged: a technical primary key from `Id.New()`, immutable.
- The kind of a record (reps or duration) is fixed by its entry. It can only change by removing the record and adding a new one.
- No new unique business identifiers.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Persistence

Migration `AddDurationToSetRecords` on table `SetRecords`:

| Column | Change |
|---|---|
| `Reps` | becomes nullable |
| `Weight` | becomes nullable (`null` = not tracked) |
| `DurationSeconds` | new, `int`, nullable |

- Check constraint: exactly one of `Reps` and `DurationSeconds` is not null. The kind is read from which column is set, so there is no type column.
- `Down()` is destructive:
  - It deletes the rows with `DurationSeconds` not null and sets a null `Weight` to `0`.
  - It then drops the constraint and the column and makes `Reps` and `Weight` not null again.
- No EF Core migration. The EF configuration and `SetRecordEntity` match the table, and `WorkoutMapper` maps the two columns to `SetResult`.

## Out Of Scope

- A "skipped" or "failed" status for a set. A set that wasn't done has no record.
- Recording on entries that mix rep-based and time-based prescriptions.
- Other measurements (distance, calories) for cardio.
- Stats or progress features that read set records.
- Front-end changes, apart from the response change noted above.

## Testing

- Unit tests:
  - Value object tests for `DurationInSeconds` and `SetResult`.
  - `WorkoutTests` for `AddSetRecord`:
    - Empty result resolved from `Reps`, `Duration` and `Minutes` (including rounding), and following the position across expanded prescriptions.
    - Empty result refused for `RepsRange`, `MaxReps` and a set beyond `TotalSets`.
    - Explicit reps and explicit duration accepted, both with and without weight.
    - Kind mismatch refused in both directions, and a mixed-kind entry refused.
    - The existing status and unknown-exercise cases kept.
  - `WorkoutTests` for `UpdateSetRecord`: explicit value accepted, kind mismatch refused, weight cleared to `null`.
  - `WorkoutTests` for `UpdateExercise`: with set records, a switch to the other kind is refused and nothing changes, while a switch within the same kind is accepted.
  - Use case tests for the nullable inputs.
  - `WorkoutMapper` round-trip for reps-with-weight, reps-without-weight and duration records.
- Integration tests:
  - `POST`: empty body on a `Reps` and on a `Duration` exercise, explicit duration, reps without weight, both `reps` and `durationSeconds` (400), empty body on `RepsRange` (400), kind mismatch (409).
  - `PUT`: empty body (400) and a duration correction.
  - The existing auth matrix on both routes kept.
- The full `dotnet test WeightTracker.slnx` runs with Docker, because this change touches the API, domain, persistence and mappers.

## Notes

- **Assumptions to confirm:**
  - The empty body is allowed for `Reps(n)` and not only for timed targets, so a set can be checked off in one tap. A wrong count is corrected with `PUT`.
  - `Minutes` targets are recorded in seconds.
- **Naming:**
  - `SetResult` (`Reps`, `Duration`) in the domain, `DurationSeconds` in the table and the DTOs.
  - `SetRecord` keeps its name.
- `STPR_INVARIANTS.md` does not need changes.
- No deviations from the mapped template references are allowed.
