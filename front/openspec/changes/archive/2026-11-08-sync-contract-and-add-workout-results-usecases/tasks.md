# Tasks

## 1. Contract snapshot

- [x] 1.1 Run the API from `main` (Debug API, port 5199), then `pnpm --filter @weight-tracker/core openapi:refresh` and `pnpm --filter @weight-tracker/core openapi:generate`. Verify the diff of `core/openapi/v1.json` and `core/src/contract/generated` shows only the following, and stop and report anything else:
  - the 5 set record and review operations;
  - the 4 new schemas;
  - `WorkoutStatus` changing to `COMPLETED`/`REVIEWED`;
  - `reviewedAt`/`reviewedBy`/`resultsUpdatedAt` on the 4 workout responses;
  - `setRecords` on `WorkoutExerciseDetailResponse`.
- [x] 1.2 Confirm nothing under `core/src/contract/generated` was edited by hand, and that `grep -rn FINISHED` across `front/` (outside `generated` and `node_modules`) returns nothing. Verify by running `pnpm typecheck` and recording the failing fixture files for group 2.

## 2. Fixtures for the new required fields

- [x] 2.1 Add `reviewedAt: null`, `reviewedBy: null` and `resultsUpdatedAt: null` to each typed workout fixture, and `setRecords: []` to each typed workout exercise fixture, in the files listed in 1.2 (core, backoffice, app tests). Put the fields in the shared builder where a file has one. Verify that `pnpm typecheck` reports no fixture error and that `pnpm test` still passes.

## 3. Core domain and contracts

- [x] 3.1 In `core/src/domain/workouts/workout.ts`, add these aliases over the generated schemas and re-export them from the domain barrel. Verify with `pnpm --filter @weight-tracker/core typecheck`.
  - Request and response aliases: `SetRecord`, `AddOrUpdateSetRecordRequest`, `ReorderSetRecordsRequest`, `ReviewWorkoutRequest`, `ChangeWorkoutStatusRequest`, `WorkoutStatus`.
  - Inputs: `AddSetRecordInput`, `UpdateSetRecordInput`, `RemoveSetRecordInput`, `ReorderSetRecordsInput`, `ReviewWorkoutInput`, `ChangeWorkoutStatusInput`. Each comment says which ids travel in the path and that `exerciseId` is the catalog id.
- [x] 3.2 Add `addSetRecord`, `updateSetRecord`, `removeSetRecord`, `reorderSetRecords`, `review` and `changeStatus` to `core/src/application/ports/workout-repository-interface.ts`, all returning `Result<WorkoutDetail, DomainError>`. Verify with the core typecheck (expected to fail on `WorkoutRepository` until 4.1).

## 4. Core repository

- [x] 4.1 Implement the six methods in `core/src/infrastructure/repositories/workout-repository.ts`, each modelled on `reorderExercises`, and verify with the core typecheck:
  - `POST`, `PUT` and `DELETE` on `/workouts/{workoutId}/exercises/{exerciseId}/set-records[/{setRecordId}]`;
  - `PUT …/set-records/order`;
  - `PUT /workouts/{workoutId}/review`;
  - `PUT /workouts/{workoutId}/status`.

  Bodies are sent unchanged, `DELETE` sends no body, and failures go through `mapHttpError`.

- [x] 4.2 Extend `core/test/infrastructure/repositories/workout-repository.test.ts` following its existing `reorderExercises` cases. For each of the six methods, cover:
  - the method, path and body sent (no body on `DELETE`, `resultsUpdatedAt: null` kept on review);
  - success returning the workout detail;
  - `400` → `VALIDATION`, `401` → `UNAUTHORIZED`, `403` → `FORBIDDEN`, `404` → `NOT_FOUND`, `409` → `CONFLICT`, `500` → `UNEXPECTED`, and a rejected request → `NETWORK`.

  Use AAA with exactly one Act per test. Verify with `pnpm --filter @weight-tracker/core test`.

## 5. Core use cases

- [x] 5.1 Create the six use case files in `core/src/application/use-cases/workouts/`, each modelled on `reorder-workout-exercises-use-case.ts` with a one-line JSDoc naming the rule the API owns. Re-export them from the workouts barrel. Verify with the core typecheck.
  - `add-set-record-use-case.ts`
  - `update-set-record-use-case.ts`
  - `remove-set-record-use-case.ts`
  - `reorder-set-records-use-case.ts`
  - `review-workout-use-case.ts`
  - `change-workout-status-use-case.ts`
- [x] 5.2 Add the six keys to `core/src/application/dependencies.ts`, typed `UseCaseInterface<…Input, WorkoutDetail>`. Verify with the core typecheck.
  - `addSetRecordUseCase`, `updateSetRecordUseCase`, `removeSetRecordUseCase`
  - `reorderSetRecordsUseCase`, `reviewWorkoutUseCase`, `changeWorkoutStatusUseCase`
- [x] 5.3 Add one core test per use case in `core/test/application/use-cases/workouts/`, following `reorder-workout-exercises-use-case.test.ts`, with a fake repository. Each test covers:
  - path ids split from the body, and the body forwarded unchanged (including `resultsUpdatedAt: null`);
  - success returned;
  - failure returned unchanged;
  - the repository called once.

  Use AAA with one Act each. Verify with the core tests.

- [x] 5.4 Review core against `workout.ts`, `reorderExercises`, `reorder-workout-exercises-use-case.ts` and their tests. Verify the result has the same shape, uses aliases only, never throws, adds no validation or role check, and leaves the error mapper unchanged.

## 6. Wiring in both fronts

- [x] 6.1 Register the six keys in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`, next to the other workout use cases, each resolving `workoutRepository`. Verify with `pnpm typecheck`.
- [x] 6.2 Add a placeholder per key to `backoffice/test/support/render-with-providers.tsx` and `app/test/support/render-with-providers.tsx`, and add the six keys to both `test/infrastructure/container.test.ts`. Verify both container tests pass.
- [x] 6.3 Review both fronts' wiring against the existing workout registrations: same order and style, no presentation, copy or error mapper change. Verify that the quality checklist in `front/AGENTS.md` holds.

## 7. Verification

- [x] 7.1 Check that `.stpr/template/` has not drifted, using the loop in `front/AGENTS.md`. Verify it prints no `DRIFT:` line.
- [x] 7.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes. No E2E flow is planned.

## Workflow follow-up

- Commit the snapshot refresh (group 1) as its own step, the fixture updates (group 2) as the next, then the core and wiring work.
- Archive the change after review.
