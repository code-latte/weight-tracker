# Design

## Context

See `proposal.md` for motivation and `specs/` for the required behavior.

Current state:

- `GetWorkoutByIdUseCase` reads the workout, checks `WorkoutVisibility.CanSee`, then reads only the owner and trainer
  and returns `WorkoutView(Workout, Athlete, Trainer)`.
- `WorkoutByIdResponse.From(WorkoutView)` projects entries with `WorkoutExerciseDetailResponse.From(entry)`, which is
  also used by `WorkoutDetailResponse`, the body of `PUT /workouts/{id}` and the other whole-workout write responses.
- `WorkoutListLoader` already joins workouts with only the exercises they reference through
  `IExerciseRepository.GetAllByIds`, and `WorkoutExerciseResponse.From(entry, exercise)` projects the catalog fields
  (`translations`, `muscle`, `bodyPart`, `type`) for `GET /workouts`.
- `IMesocycleRepository.GetById` returns the `Mesocycle` aggregate with its weeks; `MesocycleWeekResponse.From`
  projects one week.
- `MesocyclePlacementValidator` only lets a workout be placed in a mesocycle whose athlete is the workout owner and
  whose trainer is the workout trainer when it has one.

## Goals / Non-Goals

**Goals:**

- One `GET /workouts/{id}` request gives the front everything the workout screen shows.
- Only the by-id read path changes; every other response stays byte-for-byte the same.

**Non-Goals:**

- No new use case, service, repository method, migration, EF configuration or mapper.
- No change to `WorkoutListLoader`, `WorkoutDetailResponse` or `WorkoutExerciseDetailResponse`.
- No caching of exercises or mesocycles.

## Decisions

### Layer mapping

- **Domain**: unchanged. The read needs no new rule; no aggregate method is added.
- **Application**: `WorkoutView` gains `IReadOnlyDictionary<Id, Exercise> Exercises` and `Mesocycle? Mesocycle`.
  `GetWorkoutByIdUseCase` takes `IExerciseRepository` and `IMesocycleRepository` and, after the visibility check and
  next to the existing user lookup, reads the distinct referenced exercises with `GetAllByIds` and, when
  `MesocycleId` is set, the mesocycle with `GetById`.
- **Infrastructure**: unchanged. Both repositories already exist and are registered.
- **Api**: `WorkoutByIdResponse` gains `Mesocycle` and its `Exercises` become a by-id specific entry DTO. The
  controller action is unchanged apart from the `ProducesResponseType` it already declares.
- **Wiring**: `Program.cs` already registers `GetWorkoutByIdUseCase` by type and both repositories, so DI resolves the
  new constructor without a registration change.

Cross-module dependencies: Workouts (Application) reads Exercises and Mesocycles through their repository interfaces
only, as `WorkoutListLoader` and `MesocyclePlacementValidator` already do.

### Extend `GetWorkoutByIdUseCase` and `WorkoutView` in place

Alternative: call `WorkoutListLoader.Load([workout])`. Rejected: it is documented as the shared join for list use
cases, it would replace the existing user lookup and its error messages, and it still would not load the mesocycle.
The exercise read is two lines following the same pattern (distinct ids, `GetAllByIds`, key by `Id`).

### Reads happen only after the visibility check

The workout is read and `CanSee` checked first, as today. Exercises, users and the mesocycle are read only once it
passes, so a `403` or `404` never touches or discloses them. Unit tests assert the repositories are not called on a
refused read.

### No `MesocycleVisibility` check on the embedded mesocycle

`MesocyclePlacementValidator` guarantees that the workout owner is the mesocycle athlete and that a set workout
trainer is the mesocycle trainer, so admins, the prescribing trainer and the owner, the only callers `WorkoutVisibility`
admits, can all already see the mesocycle. A second check would add a read and a failure mode with no case to cover.

### Missing referenced data is a data-integrity failure

A referenced exercise or mesocycle that does not exist throws `InvalidOperationException`, mirroring the existing
`Find` for users, and is not mapped to an HTTP status. Archived and custom exercises are returned like any other,
since `GetAllByIds` does not filter them.

### Flat catalog fields on a dedicated entry DTO

A new `WorkoutByIdExerciseResponse` carries the `WorkoutExerciseDetailResponse` fields (including `setRecords`) plus
`translations`, `muscle`, `bodyPart` and `type`, with `From(WorkoutExercise entry, Exercise exercise)`, reusing
`SetResponse`, `SetRecordResponse` and `ExerciseTranslationResponse`.

Alternatives:

- Add the fields to `WorkoutExerciseDetailResponse`: rejected, it would change `PUT /workouts/{id}` and the other
  write responses.
- Nest an `exercise` object (`ExerciseResponse`): rejected, it diverges from the flat shape `GET /workouts` already
  uses, so the front would handle two shapes for the same data, and it would expose `ownerId`, `videoUrl` and
  `archivedAtUtc` that nobody asked for.

### Slim embedded mesocycle DTO

A new `WorkoutMesocycleResponse(Id, Name, Objective, StartDate, DurationInWeeks, Weeks)` with
`From(Mesocycle)`, whose weeks reuse `MesocycleWeekResponse.From`, ordered by week number as the aggregate exposes
them.

Alternative: reuse `MesocycleResponse`. Rejected: it requires a `MesocycleView` with trainer and athlete users, which
the workout already returns as `athlete` and `trainer`, and it repeats `trainerId`/`athleteId`.

## Risks / Trade-offs

- [Two extra queries per by-id read] → Each is a keyed read (`id IN (...)` and a primary-key lookup); the front saves
  two HTTP round trips, so the net cost drops.
- [Clients asserting the absence of catalog fields break] → Only the integration test does; it is updated. The change
  is otherwise additive.
- [The exercise fields are projected in two DTOs] → Both build from the same `Exercise` value objects and
  `ExerciseTranslationResponse`; an integration scenario compares by-id and list output for Press Banca.
- [A future placement rule lets callers see a workout without seeing its mesocycle] → The decision above depends on
  `MesocyclePlacementValidator`; revisit it if that rule changes.

## Migration Plan

No data migration. Deploy as a normal release; rollback is reverting the code, since no stored data changes.
