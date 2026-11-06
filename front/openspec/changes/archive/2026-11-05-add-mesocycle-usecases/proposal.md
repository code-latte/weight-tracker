# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-mesocycle-usecases.md`.

## Why

The API change `add-mesocycles-usecases-endpoints` exposes mesocycles over HTTP and lets a workout be placed in a
mesocycle week, but the front snapshot predates it: `core` has no mesocycles area and the workout schemas lack the
placement pair. Wiring it in `core` now lets the coming mesocycle screens be built without touching `core`.

## What Changes

- Refresh `core/openapi/v1.json` and regenerate `core/src/contract/generated/`, as a step of its own, reviewed before any
  hand-written code.
- Add the `mesocycles` domain area in `core`: aliases of the generated mesocycle schemas plus input types that add the
  path keys. No hand-written type.
- Add `MesocycleRepositoryInterface` and `MesocycleRepository` (`getAll`, `getById`, `getByAthlete`, `create`, `update`,
  `defineWeek`, `redefineWeek`).
- Add seven use cases returning `Result`: `GetMesocyclesUseCase`, `GetMesocycleByIdUseCase`,
  `GetAthleteMesocyclesUseCase`, `CreateMesocycleUseCase`, `UpdateMesocycleUseCase`, `DefineMesocycleWeekUseCase`,
  `RedefineMesocycleWeekUseCase`.
- Workout create and update carry the optional `mesocycleId` + `mesocycleWeekNumber` pair through the regenerated
  types, and the workout response exposes it. No signature changes.
- Register the repository and the seven use cases in `Dependencies` and in both fronts' `container.ts`, with
  placeholders in both `render-with-providers.tsx`.
- No screen, hook, provider, copy, error code or mapper change.

## Capabilities

### New Capabilities

- `mesocycle-management`: reading, listing, creating and updating mesocycles and defining and redefining their weeks
  from `core`, each operation returning the API's answer or a `DomainError` as a result.

### Modified Capabilities

- `workout-management`: creating and updating a workout also forward the optional mesocycle placement pair unchanged,
  and an update that omits it sends no pair (the API then unlinks the workout).

## Fronts And Roles

- **core** -> every capability lands here. Use cases add no role check and forward the input unchanged; who may do what
  is the API's (see the brief's role table).
- **backoffice** (`ADMIN`, `TRAINER`) -> wiring only: container registrations. Nothing visible changes; no role sees
  anything new.
- **app** (`USER`) -> wiring only: same registrations, for a future athlete read. Nothing visible changes.

Each future screen decides which actions each role sees; `USER` must not be offered create, update, week or placement
actions, which the API answers with `403`.

## Canonical Template Mapping

- Domain aliases -> `core/src/domain/auth/auth.ts` (skeleton-backed), shaped like `core/src/domain/workouts/workout.ts`.
- Port and repository -> `auth-repository-interface.ts` / `auth-repository.ts` (skeleton-backed), shaped like
  `workout-repository.ts`.
- `GetMesocyclesUseCase` -> `get-workouts-use-case.ts`; `GetMesocycleByIdUseCase` -> `get-workout-by-id-use-case.ts`;
  `GetAthleteMesocyclesUseCase` -> `trainer-athletes/get-trainer-athletes-use-case.ts`; `CreateMesocycleUseCase` ->
  `create-workout-use-case.ts`; `UpdateMesocycleUseCase`, `DefineMesocycleWeekUseCase`, `RedefineMesocycleWeekUseCase`
  -> `update-workout-use-case.ts` and `update-workout-exercise-use-case.ts`.
- Tests -> `signup-use-case.test.ts` and `auth-repository.test.ts` (skeleton-backed), plus the workout tests of the same
  shape.
- Containers and test helpers -> extended in place.

No UI component is added, so there is nothing to classify by atomic level. No deviation from these references is
allowed, and `.stpr/template/` is not updated.

## API Contract

Every endpoint comes from the API change `add-mesocycles-usecases-endpoints`, not yet in `core/openapi/v1.json`:

- `GET /mesocycles`, `GET /mesocycles/{id}`, `GET /users/{userId}/mesocycles`
- `POST /mesocycles`, `PUT /mesocycles/{id}`
- `POST /mesocycles/{id}/weeks`, `PUT /mesocycles/{id}/weeks/{weekNumber}`
- `POST /workouts`, `PUT /workouts/{id}` (in the snapshot, stale: gain the placement pair, also on `WorkoutResponse`)

**Stop case:** if the refreshed snapshot lacks any of these operations or the placement pair, the change stops; that is
an API problem.

## Impact

- `core`: contract snapshot and generated types, new `domain/mesocycles`, port, repository, use cases, barrels and
  `Dependencies`; tests for all of them and new placement cases in the workout tests.
- `backoffice`, `app`: `container.ts`, container tests and `render-with-providers.tsx`.
- No change to the API, the HTTP error mapper, translations, other domain areas or existing use case signatures.
- Acceptance: `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. No E2E flow.
