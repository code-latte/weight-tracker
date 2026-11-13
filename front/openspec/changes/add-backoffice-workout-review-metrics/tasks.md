# Tasks

## 1. Contract sync

- [x] 1.1 With the API of `add-workout-summary-and-exercise-history` running, run
      `pnpm --filter @weight-tracker/core openapi:refresh` then `openapi:generate`.
  - Verify `core/openapi/v1.json` has `GET /workouts/{workoutId}/summary` and
    `GET /users/{userId}/exercises/{exerciseId}/history`.
  - Verify the generated types include `WorkoutSummaryResponse`, `ExerciseSummaryResponse`, `SetsInTargetResponse`,
    `PreviousExecutionResponse` and `ExerciseHistorySessionResponse`.
  - Note how `volume`, `maxWeight` and `weight` are typed (number or `number | string`, nullable or not).
- [x] 1.2 Review the `v1.json` diff for unrelated changes. Verify `pnpm typecheck` passes for `core`, `backoffice` and
      `app`, and stop and report if an unrelated change breaks it. Commit the snapshot and the generated types on their
      own.

## 2. Core: workout summary

- [x] 2.1 Add the `WorkoutSummary`, `ExerciseSummary`, `SetsInTarget` and `PreviousExecution` aliases and the
      `GetWorkoutSummaryInput` interface in `core/src/domain/workouts`, and export them from the domain index. Verify they
      resolve from `@weight-tracker/core/domain`.
- [x] 2.2 Add `getSummary(workoutId)` to `WorkoutRepositoryInterface` and implement it in `WorkoutRepository`, like
      `getById`. Extend `core/test/infrastructure/repositories/workout-repository.test.ts` with the request path, `200`
      with `null`s kept, `401`, `403`, `404` and network, and verify they pass.
- [x] 2.3 Add `GetWorkoutSummaryUseCase` in `core/src/application/use-cases/workouts`, export it, and add the
      `getWorkoutSummaryUseCase` key to `Dependencies`. Add its test mirroring `get-workout-by-id-use-case.test.ts`
      (Arrange/Act/Assert), and verify it passes.

## 3. Core: exercise history

- [x] 3.1 Add the `ExerciseHistorySession` alias and the `GetUserExerciseHistoryInput` interface in
      `core/src/domain/users`, and export them. Verify they resolve from `@weight-tracker/core/domain`.
- [x] 3.2 Add `getExerciseHistory(userId, exerciseId, limit)` to `UserRepositoryInterface` and implement it in
      `UserRepository`, like `getMyExerciseLastPerformance`. Use the HTTP client's query option if it has one, per design
      D1.
  - Extend `user-repository.test.ts` with the request path and `limit`, `200` in order, an empty list, `400`, `401`,
    `403`, `404` and network.
  - Verify the tests pass.
- [x] 3.3 Add `GetUserExerciseHistoryUseCase` in `core/src/application/use-cases/users`, export it, and add the
      `getUserExerciseHistoryUseCase` key to `Dependencies`. Add its test mirroring
      `get-my-exercise-last-performance-use-case.test.ts`, and verify it passes.
- [x] 3.4 Review core against `GetWorkoutByIdUseCase`, `GetMyExerciseLastPerformanceUseCase`, their repository
      methods and their tests (naming, comments, file layout). Verify `pnpm --filter @weight-tracker/core test` passes.

## 4. Wiring

- [x] 4.1 Register `getWorkoutSummaryUseCase` and `getUserExerciseHistoryUseCase` in
      `backoffice/src/infrastructure/container.ts` and in `app/src/infrastructure/container.ts` (wiring only in the app,
      design D1). Verify `pnpm typecheck` passes in both.

## 5. Backoffice: dependency and atoms

- [ ] 5.1 Run `pnpm --filter backoffice add @mui/x-charts` (the workspace uses pnpm). Verify it lands in
      `backoffice/package.json` and `pnpm-lock.yaml`, and that `pnpm --filter backoffice typecheck` passes.
- [ ] 5.2 Add the `LineChart` and `BarChart` atoms in `backoffice/src/presentation/common/components/atoms/`, per
      design D4. They take labels, values, a value formatter, a height and an aria label by props, hold no copy, and
      highlight the last value. Verify they typecheck and are the only files importing `@mui/x-charts`.

## 6. Backoffice: helpers and copy

- [ ] 6.1 Add the design D5 helpers to `presentation/workouts/workout-review.ts`: `isWeighted`, `weightedExercisesOf`,
      `weightChangeOf`, `percentOf`, `muscleDistributionOf` and `historyPointsOf`.
  - Extend `test/presentation/workouts/workout-review.test.ts` to cover:
    - weight change up, down, equal, no previous and a previous of 0;
    - percent with 0 total;
    - the muscle distribution: untrained muscle dropped, extras discarded by the counting rule, sorted;
    - history reversed with `null` points dropped;
    - the weighted pairing order.
  - Verify the tests pass.
- [ ] 6.2 Add the `review.metrics.*` keys from the brief and design D8 to `backoffice/src/i18n/{es,en}/workouts.json`.
      Verify the backoffice translation parity test passes.
- [ ] 6.3 Add `presentation/workouts/session-observations.mock.ts` with the three preview observations (`code`,
      `severity`). Verify it typechecks.

## 7. Backoffice: state hooks

- [ ] 7.1 Add `useWorkoutSummary(workoutId, enabled)` in `presentation/workouts/hooks/use-workout-summary.ts`,
      modelled on `use-workout.ts`. It reads once when `enabled` first becomes true, has a `reload` and the generation
      guard, and never throws. Verify it typechecks.
- [ ] 7.2 Add `useExerciseHistory(userId, exerciseId)` in `hooks/use-exercise-history.ts` with the same shape. It reads
      with `limit: 5` on each non-null `exerciseId` and ignores stale answers. Verify it typechecks.

## 8. Backoffice: components

- [ ] 8.1 Extend the `ReviewStatCard` molecule with an optional `caption` and `captionColor`. Add the
      `WeightChangeChip`, `ComplianceChip`, `MuscleSetsRow` and `ObservationItem` molecules in `components/molecules/`
      (text by props, only atoms imported). Verify they typecheck and that the existing review tests still pass.
- [ ] 8.2 Add the organisms in `components/organisms/`, per design D4 and D6: `SessionResultsCard`,
      `ExerciseEvolutionCard` (load and volume variants, the selector on `text-field` with `select`, and loading, error,
      empty and chart states), `ExerciseComparisonCard`, `MuscleDistributionCard` and `SessionObservationsCard`. Verify
      they typecheck and import only downward.
- [ ] 8.3 Add the `WorkoutReviewMetrics` organism. It lays out the five blocks per `brand/review.png` (results; the two
      evolution cards side by side, stacking when narrow; comparison; muscles and observations side by side) and shows
      `LoadError` with retry while the summary has failed. Verify it typechecks.

## 9. Backoffice: view and tests

- [ ] 9.1 Give `WorkoutReviewTabs` a `metrics` prop rendered on the "metrics" tab, per design D2.
  - In `WorkoutReviewView`:
    - hold the selected chart exercise;
    - call `useWorkoutSummary(workout.id, tab === "metrics")` and
      `useExerciseHistory(workout.athlete.id, selected)`;
    - default the selection to the first weighted exercise once the summary loads;
    - pass `elapsed` and the hooks' state to `WorkoutReviewMetrics`.
  - Verify the existing `workout-view.test.tsx` still passes.
- [ ] 9.2 Extend `backoffice/test/presentation/workouts/views/workout-view.test.tsx`, following its structure and
      faking `getWorkoutSummaryUseCase` and `getUserExerciseHistoryUseCase` through the container. Mock the chart atoms
      (design, Tests). Cover the scenarios of `specs/backoffice-workout-review-metrics` and the modified tab scenarios:
  - the summary is read once on first opening and not again on return, and is not read when the tab is never opened;
  - a summary error, then a retry that succeeds;
  - the five cards with their percentages and "—" cases;
  - a non-weighted exercise hidden;
  - comparison rows with up, down, equal and no previous, and compliance at 100% and below;
  - the muscle rows, sorted, with an untrained muscle absent;
  - the observations with "Vista previa";
  - the default exercise's history request (athlete id, catalog id, `limit` 5) and the chart data oldest first;
  - a selector change that reads again;
  - an empty history, and a history error with retry while the comparison stays;
  - no weighted exercise means no history request;
  - an `ADMIN` / `TRAINER` pair seeing the same content;
  - one English case;
  - "Feedback" still empty.

  Verify they pass.

- [ ] 9.3 Review the backoffice against its mapped references (`review-stat-card.tsx`, `review-exercise-card.tsx`,
      `review-sets-table.tsx`, `use-workout.ts`, `load-error.tsx` and the existing view test). Check:
  - atomic levels and downward imports;
  - that only atoms import `@mui/material` widgets and `@mui/x-charts`;
  - that there are no hard-coded strings and no business rule;
  - that the app is untouched beyond `container.ts`.
- [ ] 9.4 Check the tab against `brand/review.png` in `pnpm dev:backoffice`, in light and dark mode and at a narrow
      width, with the API running. Note any visual difference that is not covered by a spec decision.

## 10. Integration

- [ ] 10.1 Run the `.stpr/template/` drift check from `AGENTS.md` and verify it prints no `DRIFT`.
- [ ] 10.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes. No E2E is planned.
