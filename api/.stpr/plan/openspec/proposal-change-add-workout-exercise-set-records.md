## Change

Let an athlete record the reps and weight they actually lift in each set of a workout exercise. This change covers the domain model, the database table and the mapping only.

## Objective

Right now a workout only stores the prescription (sets × target). Athletes need to log what they actually did in each set, such as "8 reps at 62.5 kg", against the exercise in the workout. This change adds the domain model and persistence for that log so a later change can add the use cases and endpoints.

## Capabilities

- Add a set record (reps + weight) to an exercise in a workout through the `Workout` aggregate.
- Read the set records of each `WorkoutExercise` when a workout is loaded.
- Save new set records together with the workout.

## Module Classification

- Domain: Workouts -> mutable aggregate (`Workout` root; `WorkoutExercise` entity gains a child collection of `SetRecord`) -> `src/WeightTracker.Domain/Workouts`
- Infra: Workouts persistence -> mutable aggregate persistence -> `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs`, `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`, `src/WeightTracker.Infra/Persistence/WeightTrackerDbContext.cs`
- Migrations: new table -> `CreateWorkoutsTable` reference -> `src/WeightTracker.Migrations/Migrations`

Workouts is a real mutable aggregate, not a catalog.

## Canonical Template Mapping

- `SetRecord` entity -> `WorkoutExercise` / `SetPrescription` in `src/WeightTracker.Domain/Workouts` -> it is the nearest child of the same aggregate.
- `Workout.AddSetRecord` -> `Workout.AddExercise` / `UpdateExercise` -> same pattern: look up the entry by `ExerciseId` and throw a domain exception when it is not there.
- Aggregate tests -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts/WorkoutTests`
- Mapping -> `WorkoutMapper` round-trip, following how `WorkoutExerciseSetEntity` is mapped today.
- Migration -> `CreateWorkoutTemplateExerciseSetsTable_202611061202`, which has the same child-of-child table shape with a cascading FK.

## State And Mutability

- `WorkoutExercise` gets a private list of `SetRecord`, exposed as a read-only snapshot (`IReadOnlyList<SetRecord> SetRecords`), the same way `Sets` is exposed.
- Set records are only added through `Workout.AddSetRecord(exerciseId, reps, weight, registeredAtUtc)`, which calls an `internal` method on `WorkoutExercise`.
- `WorkoutExercise.Rehydrate` takes the stored set records.
- `WorkoutExercise.Create` and `Workout.CreateFromTemplate` always start with no set records.
- `WorkoutTemplate` reuses `WorkoutExercise`, but its entries never have set records. `WorkoutTemplateMapper` does not change.
- Set records can only be added in this change. Editing and deleting them are out of scope.

## Business Rules

- Set records can only be added while the workout is `STARTED`. Any other status throws a new domain exception, e.g. `WorkoutNotStartedException` (409).
- The `exerciseId` must belong to the workout, or the method throws `ExerciseNotInWorkoutException` (already exists). Since an exercise can appear only once per workout, `exerciseId` identifies the entry.
- Set records are allowed only when every prescription of the exercise is rep-based: `Reps`, `RepsRange` or `MaxReps`, in any mix. If any prescription is `Duration` or `Minutes`, the method throws a new domain exception, e.g. `SetRecordNotAllowedForTargetException` (409). A mix of rep-based targets is fine because every set record carries the exact number of reps done.
- No cap: an athlete may add more set records than `TotalSets`, and may do more or fewer reps than prescribed.
- Reps must be ≥ 1. Weight is in kilograms, must be ≥ 0 (0 means bodyweight) and has at most 2 decimals. Both are value objects, not bare primitives (for example `RepCount` and `Weight`). Invalid values throw before any state changes.
- `registeredAtUtc` is passed in by the caller (UTC) rather than read inside the domain, so tests are deterministic.
- Adding a set record is not a content edit. The workout edit rule and its `STARTED`/`FINISHED` lock do not apply to it. `STPR_INVARIANTS.md` needs a short clarification saying so.
- A workout with set records cannot have its content modified. Set records only exist on `STARTED` or `FINISHED` workouts, and the existing edit rule already locks those for everyone except an `ADMIN`. An `ADMIN` keeps full edit and status-override rights.
- While a `WorkoutExercise` has set records, `Workout.UpdateExercise` refuses any prescription that is `Duration` or `Minutes`, for every caller including an `ADMIN`. It throws `SetRecordNotAllowedForTargetException` (409) and nothing changes. The guard lives in the aggregate. Changes between rep-based targets (`Reps`, `RepsRange`, `MaxReps`) are still allowed.

## Roles And Visibility

- Set records are meant for the workout's athlete (`Workout.UserId`). Who may call this is enforced later in the application layer, which is out of scope here. The aggregate itself does not check the caller.
- A set record has no `UserId`. It belongs to the workout's athlete through `WorkoutExercise` -> `Workout.UserId`.

## Business Identifiers And Mutability

- `SetRecord.Id` is a technical primary key generated with `Id.New()`. It is immutable.
- `WorkoutExerciseId` is immutable once the set record exists.
- No new unique business identifiers.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Persistence

Table `SetRecords`, mapped by `SetRecordEntity`:

| Column | Type | Notes |
|---|---|---|
| `Id` | uuid | PK, `.ValueGeneratedNever()` |
| `WorkoutExerciseId` | uuid | not null, FK → `WorkoutExercises.Id`, `ON DELETE CASCADE` |
| `Position` | int | not null, order within the exercise: by registration time, ties in the order added |
| `Reps` | int | not null |
| `Weight` | decimal(6,2) | not null, kilograms |
| `RegisterDatetimeUtc` | timestamp | not null, UTC, same convention as existing timestamp columns |

- Add an index on `WorkoutExerciseId`.
- `Down()` drops the table.
- No EF Core migration. The EF configuration matches this table exactly.
- When an exercise is removed from a workout, its set records are deleted by the cascade.

## Out Of Scope

- Use cases, DTOs, endpoints, `ExceptionMapper` entries beyond the new domain exceptions' statuses, and integration tests for routes.
- Editing or deleting set records, and reading them through the API.
- Set records for duration or minutes targets.
- Front-end.

## Testing

- Unit tests:
  - `WorkoutTests` for `AddSetRecord`: happy path, workout not `STARTED` (each other status), unknown exercise, an exercise with a time-based prescription, mixed rep-based prescriptions accepted, more set records than `TotalSets` accepted, invalid reps and weight, and set records surviving `Rehydrate`.
  - `WorkoutTests` for `UpdateExercise` on an exercise with set records: a time-based prescription is refused and nothing changes; a change between rep-based targets is accepted. Also cover that an exercise without set records can still switch to a time-based target.
  - Value object tests for `RepCount` and `Weight`.
  - `WorkoutMapper` round-trip with set records.
- The full `dotnet test WeightTracker.slnx` runs with Docker, because this change touches persistence and mappers.

## Notes

- **Naming:** `SetRecord` / `SetRecords` everywhere: domain entity, collection property, table, and `SetRecordEntity`.
- **Weight unit:** kilograms, with no unit column.
- **Admins:** an `ADMIN` can edit everything and override any status, even when set records exist. The only aggregate guard is the one above: an exercise with set records cannot switch to a time-based target.
- No deviations from the mapped template references are allowed beyond the invariant clarification above.
