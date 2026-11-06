# Change Proposal: Add Mesocycle Use Cases

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-mesocycle-usecases/`.

## Change

Bring the API's mesocycle operations into `core`: refresh the contract snapshot, add the `mesocycles` domain aliases,
a `MesocycleRepository` and its use cases, and carry the new optional workout placement (`mesocycleId` +
`mesocycleWeekNumber`) through the existing workout create and update use cases. Both fronts register the new keys. No
screen is added.

## Objective

The API change `add-mesocycles-usecases-endpoints` exposes mesocycles over HTTP (`/mesocycles`,
`/users/{userId}/mesocycles`) and lets `POST /workouts` and `PUT /workouts/{id}` place a workout in a mesocycle week.
The front snapshot predates all of it: `core` has no mesocycles area, and the workout schemas lack the placement pair.

After this change `core` offers every mesocycle operation through use cases that return `Result` and never throw, and the
workout create/update use cases accept and return the placement, so later changes can build the mesocycle screens
without touching `core`.

## Capabilities

- List the caller's mesocycles (`GET /mesocycles`), scoped by role in the API.
- Read one mesocycle with its weeks and the trainer and athlete profiles (`GET /mesocycles/{id}`).
- List the mesocycles of one athlete (`GET /users/{userId}/mesocycles`), for trainers and admins.
- Create a mesocycle: `athleteId`, `name`, `objective`, `startDate`, `durationInWeeks`; `trainerId` only when the caller
  is an `ADMIN`.
- Update a mesocycle's `name`, `objective`, `startDate` and `durationInWeeks`.
- Define a new week (`weekNumber`, `trainingWeekType`, `objective`) and redefine an existing one (`trainingWeekType`,
  `objective`), identified by its week number.
- Create or update a workout with an optional `mesocycleId` + `mesocycleWeekNumber` pair; the workout response exposes
  the pair (`null` for a standalone workout).
- Every API failure comes back as a `DomainError` code, with the API's detail kept in the message.

## Fronts And Roles

- backoffice -> lands here: wiring only -> roles: `ADMIN`, `TRAINER` -> the container registers the repository and the
  mesocycle use cases. No screen, hook or copy changes.
- app -> lands here: wiring only -> roles: `USER` -> same registrations, so athletes can later read their mesocycles. No
  screen, hook or copy changes.

Who may call what is decided by the API, which the front mirrors in the data it passes, not in checks:

| Operation                                 | USER          | TRAINER                                  | ADMIN                |
| ----------------------------------------- | ------------- | ---------------------------------------- | -------------------- |
| `GET /mesocycles`, `GET /mesocycles/{id}` | own (athlete) | own (trainer)                            | all                  |
| `GET /users/{userId}/mesocycles`          | `403`         | own for that athlete                     | all of that athlete  |
| `POST /mesocycles`                        | `403`         | `trainerId` omitted, active relationship | `trainerId` required |
| `PUT /mesocycles/{id}`, weeks             | `403`         | own                                      | all                  |
| Workout placement pair                    | `403`         | mesocycle's trainer                      | any                  |

The use cases add no role check and forward the input unchanged. A future screen decides which fields and actions each
role sees, and must hide the ones the role cannot use.

## Classification

Contract:

- Snapshot refresh + regeneration -> `core/openapi/v1.json` and `core/src/contract/generated/` -> its own step, reviewed
  on its own (see API Contract).

Core:

- `Mesocycle`, `MesocycleWeek`, `CreateMesocycleRequest`, `UpdateMesocycleRequest`, `DefineMesocycleWeekRequest`,
  `RedefineMesocycleWeekRequest`, `TrainingWeekType` (aliases of the generated schemas), plus input types that add the
  path keys (`UpdateMesocycleInput` with `id`, `DefineMesocycleWeekInput` with `mesocycleId`,
  `RedefineMesocycleWeekInput` with `mesocycleId` and `weekNumber`, `GetMesocycleByIdInput`) -> entity types ->
  `core/src/domain/mesocycles/mesocycle.ts`
- `MesocycleRepositoryInterface` (`getAll`, `getById`, `getByAthlete`, `create`, `update`, `defineWeek`,
  `redefineWeek`) -> repository contract -> `core/src/application/ports/mesocycle-repository-interface.ts`
- `MesocycleRepository` -> repository implementation -> `core/src/infrastructure/repositories/mesocycle-repository.ts`
- `GetMesocyclesUseCase`, `GetMesocycleByIdUseCase`, `GetAthleteMesocyclesUseCase`, `CreateMesocycleUseCase`,
  `UpdateMesocycleUseCase`, `DefineMesocycleWeekUseCase`, `RedefineMesocycleWeekUseCase` -> use cases ->
  `core/src/application/use-cases/mesocycles/`
- `Workout`, `CreateWorkoutRequest`, `UpdateWorkoutRequest` -> existing entity types, unchanged aliases that pick up the
  placement pair from the regenerated schemas -> `core/src/domain/workouts/workout.ts`
- `CreateWorkoutUseCase`, `UpdateWorkoutUseCase`, `WorkoutRepository` -> existing, no code change expected; their tests
  gain the placement cases
- `Dependencies` keys `mesocycleRepository` and one per new use case -> composition contract ->
  `core/src/application/dependencies.ts`
- Barrels re-export the new types, port, repository and use cases

Backoffice and app:

- Registrations of the new keys -> composition root -> `backoffice/src/infrastructure/container.ts` and
  `app/src/infrastructure/container.ts`
- Placeholders for the new keys -> test helper -> `backoffice/test/support/render-with-providers.tsx` and
  `app/test/support/render-with-providers.tsx`

`GET /users/{userId}/mesocycles` returns mesocycles, so it belongs to `MesocycleRepository`, not `UserRepository`. Exact
alias names follow the generated schema names after the refresh.

## Canonical Template Mapping

- Domain aliases -> `core/src/domain/auth/auth.ts` (skeleton-backed) and `core/src/domain/workouts/workout.ts` ->
  aliases over `components["schemas"][...]` only, input types as alias + path key, one folder per domain area,
  re-exported from the domain barrel. No hand-written type.
- `MesocycleRepositoryInterface` / `MesocycleRepository` -> `auth-repository-interface.ts` / `auth-repository.ts`
  (skeleton-backed), shaped like `workout-repository.ts` -> call `HttpClientInterface`, return the response as the
  domain type, failures through `mapHttpError`.
- `GetMesocyclesUseCase` -> `get-workouts-use-case.ts` -> no-input list.
- `GetMesocycleByIdUseCase` -> `get-workout-by-id-use-case.ts` -> read one by id.
- `GetAthleteMesocyclesUseCase` -> `trainer-athletes/get-trainer-athletes-use-case.ts` -> list keyed by a user id in the
  path.
- `CreateMesocycleUseCase` -> `create-workout-use-case.ts` -> body forwarded unchanged.
- `UpdateMesocycleUseCase`, `DefineMesocycleWeekUseCase`, `RedefineMesocycleWeekUseCase` ->
  `update-workout-use-case.ts` and `update-workout-exercise-use-case.ts` -> path keys plus body as one input object,
  split by the use case before calling the repository.
- Core tests -> `signup-use-case.test.ts` and `auth-repository.test.ts` (skeleton-backed), plus the workout tests of the
  same shape.
- Both `container.ts` and both `render-with-providers.tsx` -> extended in place, structure kept.

The workouts slice is the nearest reference because it is the latest area with list, read, create, update and
inner-entity operations. `.stpr/template/` and its README are not updated here.

## Screens And Navigation

None. No route, view or component changes.

## API Contract

- `GET /mesocycles` -> in `core/openapi/v1.json`: **no** -> response `MesocycleResponse[]`; failures `401`, `403`.
- `GET /mesocycles/{id}` -> **no** -> response `MesocycleResponse`; failures `401`, `403`, `404`.
- `GET /users/{userId}/mesocycles` -> **no** -> response `MesocycleResponse[]`; failures `401`, `403`, `404`.
- `POST /mesocycles` -> **no** -> request `CreateMesocycleRequest`, response `MesocycleResponse` (`201`); failures
  `400`, `401`, `403`, `404`, `409`.
- `PUT /mesocycles/{id}` -> **no** -> request `UpdateMesocycleRequest`, response `MesocycleResponse`; failures `400`,
  `401`, `403`, `404`, `409`.
- `POST /mesocycles/{id}/weeks` -> **no** -> request `DefineMesocycleWeekRequest`, response `MesocycleResponse` (`201`);
  failures `400`, `401`, `403`, `404`, `409`.
- `PUT /mesocycles/{id}/weeks/{weekNumber}` -> **no** -> request `RedefineMesocycleWeekRequest`, response
  `MesocycleResponse`; failures `400`, `401`, `403`, `404`.
- `POST /workouts`, `PUT /workouts/{id}` -> **yes, stale** -> `CreateWorkoutRequest` / `UpdateWorkoutRequest` gain
  optional `mesocycleId` and `mesocycleWeekNumber`; `WorkoutResponse` gains both, nullable.

`MesocycleResponse` carries `id`, `trainerId`, `athleteId`, `name`, `objective`, `startDate` (`yyyy-MM-dd`),
`durationInWeeks`, `weeks[]` (`weekNumber`, `trainingWeekType` as an UPPER_CASE string, `objective`) and the `trainer`
and `athlete` public profiles.

The endpoints come from the API change `add-mesocycles-usecases-endpoints` (`api/.stpr/plan/openspec/`), still
uncommitted on `feat/api/wire-mesocycles-related-use-cases-and-endpoints`. This change refreshes the snapshot: run the
API (Debug API, port 5199) from a branch that contains that change, then
`pnpm --filter @weight-tracker/core openapi:refresh` and `openapi:generate`. This is its own commit-sized step, before
any hand-written code.

Review the diff of `core/openapi/v1.json` and the generated types. Expected: the seven mesocycle operations, the
mesocycle request/response schemas, a `TrainingWeekType` schema or string enum, and the placement pair on the three
workout schemas. Anything else unexpected means the API drifted, and the change stops there. If generated names differ
from those above, the aliases follow the generated names. If `trainingWeekType` is generated as a plain `string` with no
enum, the alias stays a string; the front does not hand-write the enum.

## Held State

None. No use case stores anything, and no provider or hook is added. Mesocycles are not cached.

## Forms And Structural Validation

None here. `core` adds no validation, including the both-or-neither rule of the placement pair: that is the API's
(`400` on a half-set pair). A future form mirrors the API's declared limits, taken from the refreshed snapshot.

## Errors And Copy

- Missing or invalid token -> `UNAUTHORIZED` (401) -> already mapped.
- Role not allowed, trainer without an active relationship, user placing a workout -> `FORBIDDEN` (403) -> already mapped.
- Missing required field, half-set placement pair, invalid assignment -> `VALIDATION` (400) -> already mapped.
- Unknown mesocycle, week, user or trainer -> `NOT_FOUND` (404) -> already mapped.
- Duplicate week, week beyond duration, duration shorter than a defined week, athlete or trainer mismatch on placement,
  placement change on a started/finished workout -> `CONFLICT` (409) -> already mapped.
- No response -> `NETWORK`; any other status -> `UNEXPECTED` -> already mapped.

No new `DomainErrorCode`, no mapper change and no new copy keys: every status the endpoints return is already mapped in
`core/src/infrastructure/http/http-error-mapper.ts`.

## Platform Capabilities And Dependencies

None. No new port and no new package.

## Out Of Scope

- Any screen, hook, provider or copy, and any role-based form logic.
- Deleting mesocycles or weeks, and `GET /mesocycles/{id}/weeks`: the API does not offer them.
- Listing the workouts of a mesocycle or a week, and placement in `WorkoutListItemResponse`.
- Reassigning a mesocycle's trainer or athlete.
- Updating `.stpr/template/` or its README.
- Must not change: the API, the HTTP error mapper, the existing workout use case signatures and the other domain areas.

## Notes

- **Placement on update replaces.** On `PUT /workouts/{id}` omitting both values unlinks the workout. A later edit
  screen must send back the pair it read from `GET /workouts/{id}` to keep it. `UpdateWorkoutUseCase` forwards the input
  unchanged and does not fill it in.
- **Week path key** is `weekNumber`, not a week id; `MesocycleWeek` exposes no id.
- **Create returns `201`** with the body on `POST /mesocycles` and `POST /mesocycles/{id}/weeks`; the repository returns
  the body as `Mesocycle`, as the workout create does.
- **Role rules live in the API.** `core` must not branch on the role. The role table above is documentation for the
  future screens.
- **Stop case:** if the refreshed snapshot lacks any of the seven operations or the placement pair, stop; that is an API
  problem.
- **Verification tiers:**
  - core unit, one test per new use case under `core/test/application/use-cases/mesocycles/`: input forwarded unchanged
    (including an omitted `trainerId` on create), path keys split from the body, success returned, repository failure
    returned unchanged.
  - core unit, `mesocycle-repository.test.ts`: method, path and body per operation; success returns the response; every
    status the contract lists per operation, `500` -> `UNEXPECTED`, network failure -> `NETWORK`.
  - core unit, existing `create-workout-use-case.test.ts`, `update-workout-use-case.test.ts` and
    `workout-repository.test.ts`: the placement pair is forwarded and sent, an omitted pair is not added, and the
    response pair is returned.
  - container tests in both fronts gain the new keys wherever they list or resolve registrations.
  - front tiers: no component or E2E test, since nothing visible changes. Existing suites must still pass.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. No E2E flow is planned.
- No deviation from the mapped references is allowed.
