# Tasks

## 1. Application: load exercises and mesocycle for the by-id read

- [x] 1.1 Extend `WorkoutView` with `IReadOnlyDictionary<Id, Exercise> Exercises` and `Mesocycle? Mesocycle`, updating its summary; verify `dotnet build WeightTracker.slnx` reports the call sites to fix and nothing else.
- [x] 1.2 Inject `IExerciseRepository` and `IMesocycleRepository` into `GetWorkoutByIdUseCase`; after the visibility check, read the distinct referenced exercises with `GetAllByIds` and, when `MesocycleId` is set, the mesocycle with `GetById`; throw `InvalidOperationException` for a missing exercise or mesocycle, as `Find` does for users; update the remarks and `<exception>` docs; verify `dotnet build WeightTracker.slnx` passes with warnings as errors and `Program.cs` needs no registration change.
- [x] 1.3 Update `test/WeightTracker.Unit/Application/Workouts/GetWorkoutByIdUseCaseTests.cs`: the view carries the referenced exercises (only the ids its entries reference; the domain forbids repeating an exercise in a workout) and the mesocycle of a placed workout; a standalone workout has `null` mesocycle and `IMesocycleRepository` is not called; on a refused read and on a missing workout neither repository is called; a missing exercise or mesocycle throws `InvalidOperationException`. Keep labelled `// Arrange` / `// Act` / `// Assert` phases; verify `dotnet test test/WeightTracker.Unit` passes.
- [x] 1.4 Review: compare the use case against `WorkoutListLoader` (exercise join) and the existing user lookup, and the tests against the existing `GetWorkoutByIdUseCaseTests` structure; verify no visibility, ordering or error-message change slipped in.

## 2. Api: by-id response DTOs

- [x] 2.1 Add `WorkoutByIdExerciseResponse` in `Endpoints/Workouts/Dtos` with the `WorkoutExerciseDetailResponse` fields plus `Translations`, `VideoUrl`, `Muscle`, `BodyPart` and `Type`, built by `From(WorkoutExercise entry, Exercise exercise)` mirroring `WorkoutExerciseResponse.From`; verify the build passes and `WorkoutExerciseDetailResponse` is untouched.
- [x] 2.2 Add `WorkoutMesocycleResponse(Id, Name, Objective, StartDate, DurationInWeeks, Weeks)` with `From(Mesocycle)` reusing `MesocycleWeekResponse.From`; verify the build passes and `MesocycleResponse` is untouched.
- [x] 2.3 Change `WorkoutByIdResponse` to use `WorkoutByIdExerciseResponse` for `Exercises` and add a nullable `Mesocycle`, updating its XML docs (the summary no longer says entries reference the exercise by id only); verify the build passes.
- [x] 2.4 Update `test/WeightTracker.Integration/Workouts/WorkoutGetByIdEndpointTests.cs`: replace the no-catalog-data assertions in `Get_ShouldReturnTheWorkoutWithItsEntriesAndSetsButNoCatalogData` (and rename it) with the catalog fields and no `name`; add the Press Banca by-id case and its equality with the `GET /workouts` entry; add a custom exercise case and an archived custom exercise case set up through the API; add the placed-workout case (mesocycle fields, weeks in ascending order, `mesocycleWeekNumber` still 1, no people properties), the standalone `null` case, the admin/trainer/athlete same-mesocycle case, the `403` case with no `exercises`, `mesocycle`, `translations` or `weeks`, and extend the `PUT` shape test with no `mesocycle` and no catalog fields. Authenticate through `AuthenticatedClientFor`; keep the existing `401` no-token, `401` invalid-token, `403` and `404` cases; verify with Docker running that `dotnet test test/WeightTracker.Integration` passes.
- [x] 2.5 Update `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs` so `WorkoutByIdResponse` declares a nullable `mesocycle` whose `weeks` items reference `MesocycleWeekResponse`, its `exercises` items declare `translations`, `muscle`, `bodyPart` and `type`, and `PUT /workouts/{id}` still references `WorkoutDetailResponse`; verify the OpenAPI tests pass.
- [x] 2.6 Review: compare the new DTOs against `WorkoutExerciseResponse` and `MesocycleResponse`/`MesocycleWeekResponse`, and the new tests against the closest existing cases in `WorkoutGetByIdEndpointTests` and `WorkoutResponseShapeTests`; verify naming, JSON property names and test structure match.

## 3. Integration checks

- [x] 3.1 Run the `.stpr/template` drift check from `AGENTS.md`; verify it prints no `DRIFT:` line.
- [x] 3.2 With Docker running, run `dotnet build WeightTracker.slnx` and `dotnet test WeightTracker.slnx`; verify both pass with no skipped integration tests.
- [x] 3.3 Walk the `AGENTS.md` quality checklist and `openspec validate enrich-get-workout-by-id-response --strict`; verify every applicable item holds and validation passes.

## Workflow follow-up

- When archiving, update the `## Purpose` of `openspec/specs/workout-retrieval/spec.md`, which still says the read is "without catalog exercise data".
- Archive the change after review.
