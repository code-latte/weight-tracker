# Design

## Context

See `proposal.md` for the motivation. Today `core` has a full workouts area (`domain/workouts/workout.ts`,
`WorkoutRepository`, nine use cases) and no mesocycles area. `core/openapi/v1.json` has no `/mesocycles` paths, and its
`CreateWorkoutRequest`, `UpdateWorkoutRequest` and `WorkoutResponse` lack `mesocycleId` and `mesocycleWeekNumber`.
`http-error-mapper.ts` already maps 400, 401, 403, 404 and 409 to `DomainErrorCode`s, which covers every status the new
endpoints declare.

The API side lives on `feat/api/wire-mesocycles-related-use-cases-and-endpoints` and is not merged yet; the snapshot
refresh needs an API built from it.

## Goals / Non-Goals

**Goals:**

- Every mesocycle operation reachable from either front's container as a use case returning `Result`.
- The workout placement pair flowing through the existing workout use cases with no signature change.

**Non-Goals:**

- Any role check, validation of the placement pair, or default placement in `core`.
- Any presentation code, provider, translation or error code.

## Decisions

### 1. Contract refresh first, alone

Run `pnpm --filter @weight-tracker/core openapi:refresh` (API on port 5199) and `openapi:generate`, review the diff and
commit it before hand-written code. Expected additions: seven mesocycle operations, `CreateMesocycleRequest`,
`UpdateMesocycleRequest`, `DefineMesocycleWeekRequest`, `RedefineMesocycleWeekRequest`, `MesocycleResponse`,
`MesocycleWeekResponse`, `TrainingWeekType` (or a string enum inline), and the placement pair on the three workout
schemas. Anything else is drift and stops the change.

### 2. Domain aliases in `core/src/domain/mesocycles/mesocycle.ts`

```
Mesocycle                    = MesocycleResponse
MesocycleWeek                = MesocycleWeekResponse
CreateMesocycleRequest       = CreateMesocycleRequest
UpdateMesocycleRequest       = UpdateMesocycleRequest
DefineMesocycleWeekRequest   = DefineMesocycleWeekRequest
RedefineMesocycleWeekRequest = RedefineMesocycleWeekRequest
TrainingWeekType             = the generated enum schema, or MesocycleWeek["trainingWeekType"] if none
GetMesocycleByIdInput        = { id: string }
GetAthleteMesocyclesInput    = { userId: string }
UpdateMesocycleInput         = UpdateMesocycleRequest & { id: string }
DefineMesocycleWeekInput     = DefineMesocycleWeekRequest & { mesocycleId: string }
RedefineMesocycleWeekInput   = RedefineMesocycleWeekRequest & { mesocycleId: string; weekNumber: number }
```

Input types follow `UpdateWorkoutInput` and `UpdateWorkoutExerciseInput`: the generated body plus the path keys.
Re-exported from `core/src/domain/index.ts`. Names follow the generated schema names if they differ.

Alternative considered: a plain `string` input, as `GetTrainerAthletesUseCase` takes. Rejected: the workouts area,
the nearest reference, uses named input objects (`GetWorkoutByIdInput`), and a named key keeps `id` and `userId` apart.

### 3. `MesocycleRepositoryInterface` and `MesocycleRepository`

| Method                                       | Request                                   | Returns       |
| -------------------------------------------- | ----------------------------------------- | ------------- |
| `getAll()`                                   | `GET /mesocycles`                         | `Mesocycle[]` |
| `getById(id)`                                | `GET /mesocycles/{id}`                    | `Mesocycle`   |
| `getByAthlete(userId)`                       | `GET /users/{userId}/mesocycles`          | `Mesocycle[]` |
| `create(request)`                            | `POST /mesocycles`                        | `Mesocycle`   |
| `update(id, request)`                        | `PUT /mesocycles/{id}`                    | `Mesocycle`   |
| `defineWeek(mesocycleId, request)`           | `POST /mesocycles/{id}/weeks`             | `Mesocycle`   |
| `redefineWeek(mesocycleId, weekNumber, req)` | `PUT /mesocycles/{id}/weeks/{weekNumber}` | `Mesocycle`   |

Each method follows `WorkoutRepository`: `httpClient.request`, `ok(response)`, `fail(mapHttpError(error))`. Bodies are
sent unchanged.

`getByAthlete` sits here, not in `UserRepository`, because it returns mesocycles: the repository is per domain area of
the result, not per URL prefix.

### 4. Use cases in `core/src/application/use-cases/mesocycles/`

Each is a one-line delegation; the update-style ones destructure the path keys out of the input, as
`UpdateWorkoutUseCase` does:

- `GetMesocyclesUseCase` (`void` -> `Mesocycle[]`)
- `GetMesocycleByIdUseCase` (`GetMesocycleByIdInput` -> `Mesocycle`)
- `GetAthleteMesocyclesUseCase` (`GetAthleteMesocyclesInput` -> `Mesocycle[]`)
- `CreateMesocycleUseCase` (`CreateMesocycleRequest` -> `Mesocycle`)
- `UpdateMesocycleUseCase` (`UpdateMesocycleInput` -> `Mesocycle`)
- `DefineMesocycleWeekUseCase` (`DefineMesocycleWeekInput` -> `Mesocycle`)
- `RedefineMesocycleWeekUseCase` (`RedefineMesocycleWeekInput` -> `Mesocycle`)

Each has a JSDoc line saying who may call it is decided by the API. Barrel `index.ts` per folder, re-exported from
`use-cases/index.ts`.

### 5. Workout placement without code change

`CreateWorkoutRequest`, `UpdateWorkoutRequest` and `Workout` are aliases, so the regenerated schemas carry the pair
into `CreateWorkoutUseCase`, `UpdateWorkoutUseCase` and `WorkoutRepository` as they are. Only their tests change. The
update use case does not fill in an omitted pair: the API's "omitted means unlink" is a business rule, and the future
edit screen sends back what it read.

Alternative considered: a dedicated `PlaceWorkoutInMesocycleUseCase`. Rejected: the API has no such endpoint, and the
placement is part of the workout body.

### 6. Wiring

New `Dependencies` keys in `core/src/application/dependencies.ts`:

`mesocycleRepository`, `getMesocyclesUseCase`, `getMesocycleByIdUseCase`, `getAthleteMesocyclesUseCase`,
`createMesocycleUseCase`, `updateMesocycleUseCase`, `defineMesocycleWeekUseCase`, `redefineMesocycleWeekUseCase`.

Registered in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts` as
`mesocycleRepository: (c) => new MesocycleRepository(c.resolve("httpClient"))` and each use case resolving
`mesocycleRepository`, placed after the workout entries. Both fronts register all keys: `Dependencies` is one shared
contract, and the app will need the reads.

Both `render-with-providers.tsx` get a placeholder per key, in the style of the workout ones. Both `container.test.ts`
gain the new keys wherever they list or resolve registrations. No test helper or test pattern changes otherwise.

### 7. Errors

No new `DomainErrorCode`, no mapper branch and no translation. All failures map to existing codes (see the specs), whose
`errors` copy already exists in `es` and `en` in both fronts.

### 8. Fronts

Nothing in presentation changes: no state, view, component, route or translation, so there is no atomic level to
assign. `backoffice` and `app` stay independent; they only share `core`.

## Risks / Trade-offs

- [The API branch is unmerged and may still change] -> refresh from the API branch at implementation time; if a route
  or schema differs from the brief, update the plan before writing code.
- [`trainingWeekType` may be generated as `string`] -> keep the alias as generated; never hand-write the enum. A future
  screen gets the values from the contract or the API.
- [A future edit screen forgets to send the placement back and unlinks a workout] -> recorded in the
  `workout-management` spec ("Placement omitted") and in the brief's notes for the screen change.
- [Registering read and write use cases in the app though the athlete only reads] -> accepted: unused registrations
  cost nothing, and keeping `Dependencies` complete in both fronts is the existing rule.

## Migration Plan

None. Additive; no stored state or route changes. Rollback is reverting the commits.
