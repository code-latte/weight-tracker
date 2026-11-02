# Design

## Context

See proposal.md - Why. Today `GetAllWorkoutsUseCase.Execute(callerId, callerRole, ct)` scopes workouts by role, then reads only the referenced exercises with `IExerciseRepository.GetAllByIds` and returns `WorkoutListDTO(Workouts, Exercises)`. `WorkoutsController.GetAll` maps each workout with `WorkoutResponse.From(workout, exercises)`. `WorkoutResponse` is also the `201` body of `POST /workouts`, built with an empty exercise dictionary. `IUserRepository.GetAllByIds` already exists ("unknown ids are skipped; order is not guaranteed").

`Workouts.UserId` and `Workouts.TrainerId` are foreign keys to `Users` without cascade, and `IUserRepository` has no delete method, so every referenced user exists.

## Goals / Non-Goals

**Goals:**
- Resolve owners and trainers in Application with one extra query, joined the same way as exercises.
- Leave `POST /workouts` and `WorkoutResponse` untouched.

**Non-Goals:**
- Domain, persistence, migration or `IUserRepository` changes.
- People on `GET /workouts/{id}` or any other workout response.

## Decisions

### Application: users joined like exercises

`WorkoutListDTO` becomes `WorkoutListDTO(IReadOnlyList<Workout> Workouts, IReadOnlyDictionary<Id, Exercise> Exercises, IReadOnlyDictionary<Id, User> Users)`. `GetAllWorkoutsUseCase` takes `IUserRepository` as a third constructor dependency. After scoping (unchanged, including the `UnauthorizedOperationException` for an unknown role before any read) and the exercise read:

1. Collect the distinct `UserId` values and non-null `TrainerId` values of the scoped workouts.
2. Call `IUserRepository.GetAllByIds` once with them, and index the result by `Id`.
3. If any collected id is missing from the result, throw `InvalidOperationException` (not mapped, so `500`).

The call is made even for an empty id set, exactly as the exercise read is today, so both joins look the same.

- Alternative: return `null` for an unresolved person. Rejected: the brief requires a non-null `athlete`, and foreign keys make a missing user a data-integrity failure, not a business case.
- Alternative: a dedicated read model per item (like `UserWithTrainerView`). Rejected: the nearest reference is this use case's own dictionary join, and the brief maps to it.

### Api: dedicated list item DTO

Add `WorkoutListItemResponse` in `Endpoints/Workouts/Dtos/` with the `WorkoutResponse` fields plus `UserProfileResponse Athlete` and `UserProfileResponse? Trainer`. `From(Workout, IReadOnlyDictionary<Id, Exercise>, IReadOnlyDictionary<Id, User>)` builds the entries with the existing `WorkoutExerciseResponse.From` and the people with `UserProfileResponse.From(User)`. `GetAll` returns `IReadOnlyList<WorkoutListItemResponse>`, and its `[ProducesResponseType]` changes accordingly; `401` and `403` stay. The controller only maps; it never queries.

- Alternative: add the people to `WorkoutResponse`. Rejected: `POST /workouts` would have to load users or return misleading values. This mirrors `UserListItemResponse` vs. `UserSummaryResponse`.

### Dependencies

Workouts (Application) now depends on the Users port `IUserRepository`, as `GetAllUsersUseCase` and the trainer-athlete use cases already do. No new registration in `Program.cs`. Authorization is unchanged: the join resolves only users referenced by workouts the caller may already see.

## Risks / Trade-offs

- [The response exposes a trainer's email to their athletes] → Already visible through the trainer–athlete endpoints; the profile is limited to `id`, `email`, `name`, `surname`, `role`.
- [A future hard-delete path would make the list fail with `500`] → Foreign keys without cascade prevent it today; the unit test pins the throw so the behaviour is explicit.
- [Front still bound to `WorkoutResponse` for the list] → The front plan points its list type at `WorkoutListItemResponse` after refreshing the snapshot.
