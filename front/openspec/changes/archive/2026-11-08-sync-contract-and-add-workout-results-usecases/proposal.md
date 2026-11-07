# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-sync-contract-and-add-workout-results-usecases.md`.

## Why

The API has merged set records (the reps and weight an athlete actually performed) and a workout lifecycle that now ends
in `COMPLETED` → `REVIEWED`, but the front's snapshot `core/openapi/v1.json` predates both. Also, `PUT /workouts/{workoutId}/status`
is already in the snapshot with no use case, so neither front can drive the lifecycle that set records (`STARTED`) and
review (`COMPLETED`) depend on. This change closes that gap in `core` so that later screens can be built without
touching `core`.

## What Changes

- Refresh `core/openapi/v1.json` from the running API and regenerate `core/src/contract/generated/api.ts`, as a step of
  its own. The live spec differs from the snapshot in exactly these ways:
  - 5 new operations: `POST`, `PUT`, `DELETE` and `PUT …/order` under
    `/workouts/{workoutId}/exercises/{exerciseId}/set-records`, and `PUT /workouts/{workoutId}/review`.
  - 4 new schemas: `AddOrUpdateSetRecordRequest`, `ReorderSetRecordsRequest`, `ReviewWorkoutRequest`, `SetRecordResponse`.
  - **BREAKING (generated types)**: in `WorkoutStatus`, `FINISHED` is renamed to `COMPLETED`, and `REVIEWED` is new.
    No front code, test or spec references `FINISHED`.
  - **BREAKING (generated types)**: `WorkoutResponse`, `WorkoutDetailResponse`, `WorkoutByIdResponse` and
    `WorkoutListItemResponse` gain required, nullable `reviewedAt`, `reviewedBy` and `resultsUpdatedAt`.
    `WorkoutExerciseDetailResponse` gains a required `setRecords`. Existing fixtures that build workouts need the new
    fields.
  - Nothing is removed.
- Add six use cases in `core`, each returning `Result` and never throwing: `AddSetRecordUseCase`,
  `UpdateSetRecordUseCase`, `RemoveSetRecordUseCase`, `ReorderSetRecordsUseCase`, `ReviewWorkoutUseCase` and
  `ChangeWorkoutStatusUseCase`. Each one returns the `WorkoutDetail` that the API answers with.
- Extend `WorkoutRepositoryInterface` and `WorkoutRepository` with the matching six methods, and add the domain aliases
  over the new schemas.
- Register the six use cases in both fronts' `container.ts`, and add their placeholders to both test helpers.
- No screen, hook, provider, copy or error code changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-management`: adds requirements for recording, correcting, removing and reordering set records, reviewing a
  workout and changing its status. It also widens "Reading a workout by id" so that the answer carries the set records
  and the review fields, and renames "finished" to "completed" in the placement-lock scenario of "Updating a workout".

## Fronts And Roles

- **core**: the only place with new behaviour.
- **backoffice** (`ADMIN`, `TRAINER`): wiring only, with no visible change.
- **app** (`USER`): wiring only, with no visible change.

The use cases do not check roles. The API decides who may call each operation:

- **Set records**: the workout's athlete or an `ADMIN`, only while the workout is `STARTED`.
- **Review**: the prescribing `TRAINER` or an `ADMIN`, only once the workout is `COMPLETED`.
- **Status change**: an `ADMIN` may set any status, and other roles follow the lifecycle. Nobody reaches or leaves
  `REVIEWED` this way.

A caller the API forbids gets `FORBIDDEN`, and a broken precondition gets `CONFLICT`. No role sees anything new, so
there is nothing to hide per role.

## Canonical Template Mapping

- **Domain aliases**: the existing `core/src/domain/workouts/workout.ts` (aliases over `components["schemas"]` only).
- **Repository methods**: `reorderExercises`, `updateExercise` and `removeExercise` in
  `core/src/infrastructure/repositories/workout-repository.ts`.
- **Use cases**: `reorder-workout-exercises-use-case.ts`, `update-workout-exercise-use-case.ts` and
  `remove-exercise-from-workout-use-case.ts`. Each takes path ids plus the body as one input and forwards them unchanged.
- **Tests**: the tests of those files and `core/test/infrastructure/repositories/workout-repository.test.ts`.
- **Wiring**: both `container.ts`, both `render-with-providers.tsx` and both `container.test.ts`, extended in place.

No reference is pending. `.stpr/template/` is not changed. The change has no conflict with the invariants and no stop
case.

## API Contract

All endpoints come from the merged API changes `add-reviewed-workout-status`, `add-set-record-endpoints` and
`add-workout-exercise-set-records` (`api/openspec/changes/archive/2026-11-07-*`). After the refresh, every endpoint
below is in `core/openapi/v1.json`, and each one answers `WorkoutDetailResponse` on 200 and 400/401/403/404/409 on
failure:

- `POST /workouts/{workoutId}/exercises/{exerciseId}/set-records`, with body `AddOrUpdateSetRecordRequest`
- `PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}`, with body `AddOrUpdateSetRecordRequest`
- `DELETE /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}`, with no body
- `PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/order`, with body `ReorderSetRecordsRequest`
- `PUT /workouts/{workoutId}/review`, with body `ReviewWorkoutRequest`
- `PUT /workouts/{workoutId}/status`, with body `ChangeWorkoutStatusRequest` (already in the snapshot)

## Out Of Scope

- **Screens**: any screen, hook, provider or copy for logging sets, reviewing results, or showing `COMPLETED`,
  `REVIEWED` or the review fields.
- **Exercise endpoints**: `GET`, `PUT` and `DELETE /exercises/{id}`, which have no use case either. They are catalog
  administration and are deferred to their own change.
- **Must not change**: the API, the existing use case signatures, the HTTP error mapper and `.stpr/template/`.

## Acceptance Criteria

- The refreshed `core/openapi/v1.json` differs from the previous snapshot only as listed in What Changes. Any other
  difference means the API drifted, and the change stops.
- `core/src/contract/generated` is regenerated, never hand-edited.
- The six use cases are covered by core unit tests, and the six repository methods are tested for success and every
  failure status.
- Both containers resolve the six new keys.
- `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. No E2E flow is planned.

## Impact

- **core**: `openapi/v1.json`, `src/contract/generated/api.ts`, `src/domain/workouts/workout.ts`, the workout repository
  port and implementation, six new use case files plus the barrel, `src/application/dependencies.ts`, and new and
  updated tests.
- **backoffice** and **app**: `src/infrastructure/container.ts`, `test/support/render-with-providers.tsx`,
  `test/infrastructure/container.test.ts`, and any typed workout fixture that needs the new required fields.
- **Dependencies**: no new package and no new port.
