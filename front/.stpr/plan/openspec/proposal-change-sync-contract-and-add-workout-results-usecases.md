# Change Proposal: Sync The API Contract And Add Workout Results Use Cases

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/sync-contract-and-add-workout-results-usecases/`.

## Change

Refresh the front's contract snapshot from the running API, regenerate the types, and bring every API operation that
`core` does not cover yet into `core` as use cases: the four set record operations, workout review and workout status
change. Both fronts register them. No screen is added.

## Objective

The API has merged set records (the reps and weight an athlete actually performed) and a new workout lifecycle ending in
`COMPLETED` → `REVIEWED`. The snapshot in `core/openapi/v1.json` predates both. Comparing it with the live
`GET /openapi/v1.json` (Debug API, port 5199, `main` at `bcaec73`) gives:

- **5 new operations**, all under the `Workouts` tag (see API Contract).
- **4 new schemas**: `AddOrUpdateSetRecordRequest`, `ReorderSetRecordsRequest`, `ReviewWorkoutRequest`,
  `SetRecordResponse`.
- **6 changed schemas**:
  - `WorkoutStatus`: `FINISHED` is replaced by `COMPLETED`, and `REVIEWED` is added
    (`DRAFT | PUBLISHED | STARTED | COMPLETED | REVIEWED`).
  - `WorkoutResponse`, `WorkoutDetailResponse`, `WorkoutByIdResponse`, `WorkoutListItemResponse`: new required,
    nullable `reviewedAt`, `reviewedBy` and `resultsUpdatedAt`.
  - `WorkoutExerciseDetailResponse`: new required `setRecords: SetRecordResponse[]`.
- **No removed operation or schema.**

Separately, `PUT /workouts/{workoutId}/status` is already in the snapshot but has no repository method or use case, so
the lifecycle that set records and review depend on (`STARTED` to record, `COMPLETED` to review) cannot be driven from
the front.

After this change the snapshot matches the API, and `core` exposes these operations through use cases that return
`Result` and never throw, so the athlete's "log my sets" screen (app) and the trainer's "review results" screen
(backoffice) can be built later without touching `core`.

## Capabilities

- Record a performed set (`reps`, `weight`) on an exercise of a started workout.
- Correct the `reps` and `weight` of a recorded set.
- Remove a recorded set.
- Reorder the recorded sets of an exercise.
- Mark a completed workout as reviewed, sending the `resultsUpdatedAt` last read so the API can reject stale reviews.
- Move a workout to another status within the lifecycle.
- Workout reads expose `setRecords` per exercise and `reviewedAt`, `reviewedBy`, `resultsUpdatedAt` per workout, through
  the existing aliases (they follow the regenerated schemas, no alias is added for them).
- Every API failure comes back as a `DomainError` code, with the API's detail kept in the message.

## Fronts And Roles

- backoffice -> lands here: wiring only -> roles: `ADMIN`, `TRAINER` -> the container registers the new use cases. No
  screen, hook or copy changes.
- app -> lands here: wiring only -> roles: `USER` -> same registrations. No screen, hook or copy changes.

Who may call what is decided by the API; the use cases add no role check and forward the input unchanged:

| Operation          | Allowed by the API                                                | Precondition (API-owned)                                           |
| ------------------ | ----------------------------------------------------------------- | ------------------------------------------------------------------ |
| Set record ops (4) | the workout's athlete (`USER`) or an `ADMIN`                      | workout is `STARTED`                                               |
| Review             | the workout's prescribing `TRAINER` or an `ADMIN`                 | workout is `COMPLETED`, results unchanged since `resultsUpdatedAt` |
| Status change      | `ADMIN` any status; others follow the lifecycle within their role | `REVIEWED` is never reached or left this way                       |

A forbidden caller gets `FORBIDDEN`; a broken precondition gets `CONFLICT`. Future screens decide what each role sees.

## Classification

Contract:

- Snapshot refresh + regeneration -> `core/openapi/v1.json` and `core/src/contract/generated/api.ts` -> its own step,
  reviewed and committed on its own (see API Contract).

Core:

- `SetRecord` (alias of `SetRecordResponse`), `AddOrUpdateSetRecordRequest`, `ReorderSetRecordsRequest`,
  `ReviewWorkoutRequest`, `ChangeWorkoutStatusRequest`, `WorkoutStatus`, and the use case inputs
  (`AddSetRecordInput`, `UpdateSetRecordInput`, `RemoveSetRecordInput`, `ReorderSetRecordsInput`, `ReviewWorkoutInput`,
  `ChangeWorkoutStatusInput`) -> entity types -> `core/src/domain/workouts/`
- `WorkoutRepositoryInterface` gains `addSetRecord`, `updateSetRecord`, `removeSetRecord`, `reorderSetRecords`,
  `review`, `changeStatus` -> repository contract -> `core/src/application/ports/`
- `WorkoutRepository` implements them -> repository implementation -> `core/src/infrastructure/repositories/workout-repository.ts`
- `AddSetRecordUseCase`, `UpdateSetRecordUseCase`, `RemoveSetRecordUseCase`, `ReorderSetRecordsUseCase`,
  `ReviewWorkoutUseCase`, `ChangeWorkoutStatusUseCase` -> use cases -> `core/src/application/use-cases/workouts/`
- `Dependencies` keys for the six use cases -> composition contract -> `core/src/application/dependencies.ts`
- Barrels re-export the new types and use cases.

Backoffice and app:

- Registrations of the six keys -> composition root -> `backoffice/src/infrastructure/container.ts` and
  `app/src/infrastructure/container.ts`
- Placeholders for the six keys -> test helper -> `backoffice/test/support/render-with-providers.tsx` and
  `app/test/support/render-with-providers.tsx`

Exact alias names follow the generated schema names after the refresh.

## Canonical Template Mapping

- Domain aliases -> existing `core/src/domain/workouts/` aliases -> same area, aliases over `components["schemas"][...]`
  only. No hand-written type.
- New repository methods -> `reorderExercises`, `removeExercise`, `updateExercise` in `workout-repository.ts` -> same
  repository and the same shape: path ids plus body, response returned as the domain type, failures through
  `mapHttpError`.
- Use cases -> `reorder-workout-exercises-use-case.ts`, `remove-exercise-from-workout-use-case.ts`,
  `update-workout-exercise-use-case.ts` -> input object holding path ids and body, forwarded unchanged; repository
  `Result` returned as is.
- Core tests -> the tests of those same files and `workout-repository.test.ts`.
- Both `container.ts` and both `render-with-providers.tsx` -> extended in place, structure kept.

`.stpr/template/` and its README are not updated here.

## Screens And Navigation

None. No route, view or component changes.

## API Contract

- `POST /workouts/{workoutId}/exercises/{exerciseId}/set-records` -> in snapshot: **no** -> request
  `AddOrUpdateSetRecordRequest` (`reps`, `weight` required), response `WorkoutDetailResponse`; `400`, `401`, `403`,
  `404`, `409`.
- `PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}` -> **no** -> request
  `AddOrUpdateSetRecordRequest`, response `WorkoutDetailResponse`; same failures.
- `DELETE /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}` -> **no** -> no body, response
  `WorkoutDetailResponse`; same failures.
- `PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/order` -> **no** -> request `ReorderSetRecordsRequest`
  (`setRecordIds`, every set record of the exercise exactly once), response `WorkoutDetailResponse`; same failures.
- `PUT /workouts/{workoutId}/review` -> **no** -> request `ReviewWorkoutRequest` (`resultsUpdatedAt`, nullable), response
  `WorkoutDetailResponse`; same failures.
- `PUT /workouts/{workoutId}/status` -> **yes** -> request `ChangeWorkoutStatusRequest` (`status`), response
  `WorkoutDetailResponse`; `400`, `401`, `403`, `404`, `409`.
- Existing workout reads (`GET /workouts`, `GET /workouts/{id}`) and every operation returning a workout -> **yes,
  stale** -> pick up the new fields after regeneration.

In the set record routes `exerciseId` is the **catalog exercise id**, not the workout entry id.

The changes come from the merged API changes `add-reviewed-workout-status`, `add-set-record-endpoints` and
`add-workout-exercise-set-records` (`api/openspec/changes/archive/2026-11-07-*`). This change refreshes the snapshot:
run the API (Debug API, port 5199) from `main`, then `pnpm --filter @weight-tracker/core openapi:refresh` and
`openapi:generate`, as its own commit before any hand-written code.

Expected diff: exactly the operations and schemas listed in Objective. Anything else means the API drifted, and the
change stops there.

## Held State

None. No use case stores anything, and no provider or hook is added.

## Forms And Structural Validation

None here. A future set record form mirrors the contract: `reps` required integer, `weight` required number. `core` adds
no validation.

## Errors And Copy

- Missing or invalid token -> `UNAUTHORIZED` (401) -> already mapped.
- Not the workout's athlete / trainer, or role not allowed -> `FORBIDDEN` (403) -> already mapped.
- Invalid reps/weight or status value -> `VALIDATION` (400) -> already mapped.
- Unknown workout, exercise in workout or set record -> `NOT_FOUND` (404) -> already mapped.
- Workout not `STARTED` / results locked, set record not allowed for the exercise target, reorder list mismatch, review
  not allowed, results changed since `resultsUpdatedAt`, invalid status transition, concurrency conflict -> `CONFLICT`
  (409) -> already mapped.
- No response -> `NETWORK`; any other status -> `UNEXPECTED` -> already mapped.

The mapper maps by HTTP status and every new API exception lands on 400/404/409, so **no new error code and no new copy
key** is needed.

## Platform Capabilities And Dependencies

None. No new port and no new package.

## Out Of Scope

- Any screen, hook, provider or copy: logging sets in the app, reviewing results in the backoffice, showing
  `COMPLETED` / `REVIEWED` or the review fields.
- `GET`, `PUT` and `DELETE /exercises/{id}`: also in the snapshot without a use case, but they are catalog
  administration, unrelated to workout results. Deferred to their own change.
- Updating `.stpr/template/` or its README.
- Must not change: the API, the existing use cases' signatures, the HTTP error mapper.

## Notes

- **`FINISHED` → `COMPLETED`.** No front code, test or spec references `FINISHED` today, so the rename breaks nothing.
  `pnpm typecheck` after regeneration confirms it; any hit is fixed in the same change.
- **New required fields in fixtures.** `setRecords`, `reviewedAt`, `reviewedBy` and `resultsUpdatedAt` become required in
  the generated types. Existing test fixtures that build workouts (core, backoffice, app) must add them
  (`setRecords: []`, the others `null`). This is a mechanical fixture update, not a behaviour change.
- **Every set record and review operation returns the whole `WorkoutDetailResponse`**, so the use cases return the
  workout detail, not the single set record.
- **Specs.** The OpenSpec delta goes to `openspec/specs/workout-management` (the capability that already holds the
  workout use cases).
- **No stop case**, provided the refresh shows exactly the expected diff.
- **Verification tiers:**
  - core unit, one test per new use case: input forwarded unchanged (including a `null` `resultsUpdatedAt`), success
    returned, repository failure returned unchanged.
  - core unit, `workout-repository.test.ts`: method, path and body sent for the six operations; success returns the
    response; `400`, `401`, `403`, `404`, `409`, `500` → `UNEXPECTED`, network failure → `NETWORK`.
  - container tests in both fronts gain the six keys where they list or resolve registrations.
  - no component or E2E test, since nothing visible changes. Existing suites must still pass.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. No E2E flow is planned.
- No deviation from the mapped references is allowed.
