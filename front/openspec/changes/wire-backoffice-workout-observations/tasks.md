# Tasks

## 1. Contract sync

- [x] 1.1 With the API of `add-workout-observations` running, run `pnpm --filter @weight-tracker/core openapi:refresh`
      then `openapi:generate`.
  - Verify `core/openapi/v1.json` has `GET /workouts/{workoutId}/observations`.
  - Verify the generated types include `WorkoutObservationsResponse`, `WorkoutObservationResponse` and
    `WorkoutHighlightResponse`.
  - Note how `code`, `severity`, `priority`, `exerciseId` and `data` are typed, for D4's narrowing.
- [x] 1.2 Review the `v1.json` diff for unrelated changes. Verify `pnpm typecheck` passes for `core`, `backoffice` and
      `app`, and stop and report if an unrelated change breaks it. Commit the snapshot and the generated types on their
      own.

## 2. Core: workout observations

- [x] 2.1 Add the `WorkoutObservations`, `WorkoutObservation` and `WorkoutHighlight` aliases and
      `GetWorkoutObservationsInput` in `core/src/domain/workouts`, and export them from the domain index. Verify they
      resolve from `@weight-tracker/core/domain`.
- [x] 2.2 Add `getObservations(workoutId)` to `WorkoutRepositoryInterface` and implement it in `WorkoutRepository`,
      like `getSummary`. Extend `core/test/infrastructure/repositories/workout-repository.test.ts` with the request
      path, `200` with `null`s and empty lists kept, `401`, `403`, `404` and network. Verify they pass.
- [x] 2.3 Add `GetWorkoutObservationsUseCase` in `core/src/application/use-cases/workouts`, export it, and add the
      `getWorkoutObservationsUseCase` key to `Dependencies`. Add its test mirroring `get-workout-summary-use-case.test.ts`
      (Arrange/Act/Assert), and verify it passes.
- [x] 2.4 Review core against `get-workout-summary-use-case.ts`, `WorkoutRepository.getSummary` and their tests:
      verify naming, `Result` return, error mapping and test structure match, with no deviation.

## 3. Wiring

- [x] 3.1 Register `getWorkoutObservationsUseCase` beside `getWorkoutSummaryUseCase` in
      `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`. Verify `pnpm typecheck`
      passes for both fronts.

## 4. Backoffice: state hook and helpers

- [x] 4.1 Add `presentation/workouts/hooks/use-workout-observations.ts`, copying `use-workout-summary.ts` (`load` once,
      `reload`, generation guard, `runWithSession`). Verify typecheck passes.
- [x] 4.2 Add `presentation/workouts/workout-observations.ts` with the D4 helpers: `highlightedObservationsOf`,
      `highlightedIdsOf`, `observationGroupsOf`, `exerciseOf` and `observationCopyOf`. Add
      `backoffice/test/presentation/workouts/workout-observations.test.ts` (Arrange/Act/Assert) and verify it passes.
      It covers:
  - rank order with a dangling id dropped;
  - the workout group first, in first-appearance order;
  - an exercise lookup with no match;
  - copy values for one code of each family (counts, weight, volume, reps, streak), including list details, a present
    and an absent baseline, a missing required value, and an unknown code.

## 5. Backoffice: copy

- [x] 5.1 In `backoffice/src/i18n/{es,en}/workouts.json` under `review.metrics.observations`:
  - add `viewAll`, `highlighted`, `none`, `noHighlights`, `workoutGroup`, `unknownExercise`, `unknown.title`,
    `setDetail`, `codes.<CODE>.title|text` for the 17 codes, and `baseline.weight|volume|reps`;
  - remove `preview`, `LOAD_PROGRESSION`, `GOOD_COMPLIANCE` and `SET_BELOW_TARGET`.

  Verify the translation parity test passes.

## 6. Backoffice: components and view

- [x] 6.1 Change the `ObservationItem` molecule (D5): API severity mapped to `success | info | warning` with an `info`
      fallback, an optional `text` and an optional `mark`. Verify typecheck passes.
- [x] 6.2 Add the `WorkoutObservationsDialog` organism, modelled on `exercise-video-dialog.tsx`. It has a title, a close
      button reusing `workout.catalog.close`, groups headed by "Entrenamiento" or the exercise name, and a "Destacada"
      chip on highlighted items. Verify typecheck passes.
- [x] 6.3 Rewire the `SessionObservationsCard` organism:
  - it takes `workout` and the observations state;
  - it shows a skeleton, `LoadError` with retry, the highlights, or the two empty states (D3);
  - it shows the "Ver todas las observaciones (N)" button when observations exist, and the dialog's open state is
    local;
  - the "Vista previa" chip is removed.

  Delete `session-observations.mock.ts` and verify nothing imports it.

- [x] 6.4 Pass the observations from `WorkoutReviewView` through `WorkoutReviewMetrics` to the card. The view holds
      `useWorkoutObservations(workout.id)` and calls its `load()` beside `metrics.load()`.
- [x] 6.5 Update `backoffice/test/presentation/workouts/views/workout-view.test.tsx` (Arrange/Act/Assert, `es`), with
      `getWorkoutObservationsUseCase` faked through the container like `getWorkoutSummaryUseCase`. Verify it passes.
  - Default the new fake to empty lists in the existing metrics tests.
  - Replace "shows the three sample observations marked as a preview" with the spec scenarios:
    - one request on first opening and none on coming back;
    - highlights in rank order with a dangling one skipped, and no "Vista previa";
    - the severity, title and text of a weight record and an incomplete workout;
    - the button count and opening the dialog with groups and "Destacada";
    - closing the dialog;
    - nothing highlighted;
    - nothing generated, with no button;
    - a load error and retry, with the other blocks still shown.
- [x] 6.6 Review the backoffice against its references (`session-observations-card.tsx`, `observation-item.tsx`,
      `exercise-video-dialog.tsx`, `use-workout-summary.ts`, `workout-review.ts`). Verify:
  - atomic levels and folders;
  - imports flow downward and only atoms import MUI widgets;
  - no presentation import of `@weight-tracker/core/infrastructure`;
  - no ranking or filtering logic beyond the D4 resolution.

## 7. Integration

- [x] 7.1 Run the `.stpr/template/` drift check from `front/` and verify it prints no `DRIFT`.
- [ ] 7.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes. No E2E flow is planned.

## Workflow follow-up

- Archive the change after review.
