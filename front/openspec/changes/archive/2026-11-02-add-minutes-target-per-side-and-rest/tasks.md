# Tasks

## 1. Contract snapshot

- [x] 1.1 With the API change `add-minutes-target-per-side-and-rest` running (Debug API, port 5199), run
      `pnpm --filter @weight-tracker/core openapi:refresh` and `openapi:generate`.
  - Verify the diff of `core/openapi/v1.json` and `core/src/contract/generated` holds only:
    - `perSide` and `restMinutes` on `AddOrUpdateExerciseRequest`, `WorkoutExerciseResponse` and
      `WorkoutExerciseDetailResponse`;
    - `targetMinutes` on `SetPrescriptionRequest`;
    - `minutes` on `SetTargetResponse`.
  - Stop the change if anything else appears.
- [ ] 1.2 Commit the snapshot and the generated types as a step of their own. Verify `pnpm typecheck` passes, or
      record any type errors the new fields cause for the next tasks.

## 2. Core: minutes format and rules

- [x] 2.1 Add `core/src/application/validation/time-in-minutes.ts` (`isMinutesText`, `minutesFromText`,
      `minutesToText`, the maximum), modelled on `calendar-date.ts`, and export it from `validation/index.ts`.
      Verify `pnpm --filter @weight-tracker/core typecheck` passes.
- [x] 2.2 Add `core/test/application/validation/time-in-minutes.test.ts`, modelled on `workout-rules.test.ts` with
      labelled Arrange / Act / Assert phases. Cover:
  - accepted texts: `2`, `1:30`, `0:45`, `1:05`, `999:59`;
  - rejected texts: `0`, `0:00`, `1:60`, `1:5`, `1.5`, `1,5`, `-1`, `abc`, `1000`, empty;
  - conversions: `1:30` → 1.5, `0:45` → 0.75, `1:20` → 1.33, `2` → 2;
  - formatting: 1 → `1`, 1.5 → `1:30`, 1.33 → `1:20`, `"1.5"` → `1:30`;
  - the round trip from `0:01` to `2:00`.

  Verify the core tests pass.

- [x] 2.3 Extend `workout-rules.ts`:
  - `minutesFormat` violation;
  - the `Minutes` branch in `validateSetForm`;
  - optional `rest` in `validateExerciseConfiguration`.

  Extend `workout-rules.test.ts` for both. Verify the core tests pass.

- [x] 2.4 Extend the fixtures of `add-exercise-to-workout-use-case.test.ts`, `update-workout-exercise-use-case.test.ts`
      and `workout-repository.test.ts` with `perSide`, `restMinutes` and a `Minutes` set. Check that the body and the
      answer pass through unchanged. Verify the core tests pass with no production change to the use cases or the
      repository.
- [x] 2.5 Review core against its references (`calendar-date.ts`, `workout-rules.ts`, the existing use-case and
      repository tests) and `STPR_INVARIANTS.md`. Verify:
  - no hand-written domain type;
  - `core/src/contract/generated` was not edited by hand;
  - the use cases, repositories and containers are unchanged.

## 3. Backoffice: copy and labels

- [x] 3.1 Add the new keys to `backoffice/src/i18n/es/workouts.json` and `en/workouts.json`:
  - `targetTypes.Minutes`;
  - `workout.configuration.rest`, `restHint`, `minutesHint`, `perSide`, `perSideSuffix`, `validation.minutesFormat` and
    `summary.Minutes`;
  - `workout.exercises.compactSummary.Minutes`, `perSideSuffix` and `restLine`.

  Verify the translation parity test passes.

- [x] 3.2 Extend `useExerciseLabels`:
  - `"Minutes"` in `SUMMARY_TYPES`;
  - optional `perSide` in `SetSummaryValues`, appending the suffix in `setSummary`.

  Verify `pnpm --filter backoffice typecheck` passes.

## 4. Backoffice: organisms

- [x] 4.1 Extend `ExerciseConfigurationDialog`:
  - `"Minutes"` in `TARGET_TYPES`;
  - `toFormValues` / `toPrescription` use `minutesToText` / `minutesFromText` for minutes;
  - for `Minutes`, the Valor field is text with the "m:ss" hint and the `minutesFormat` message;
  - `rest` and `perSide` state come from the stored entry;
  - the rest `TextField` (existing atom) goes below the effort, and the "Por lado" `Switch` (existing atom) is the last
    cell of the set form's grid;
  - set summaries pass `perSide`;
  - `handleSave` validates the rest and sends `perSide`, plus `restMinutes` only when the rest is filled.

  Verify `pnpm --filter backoffice typecheck` passes.

- [x] 4.2 Extend `WorkoutExercisesList.linesOf`:
  - minutes sets formatted with `minutesToText`;
  - the per side suffix on the sets line;
  - the rest line when `restMinutes != null`.

  Verify `pnpm --filter backoffice typecheck` passes.

## 5. Backoffice: tests

- [x] 5.1 Extend `backoffice/test/presentation/workouts/views/workout-view.test.tsx`, with use cases faked through the
      container and labelled phases, for the spec scenarios:
  - "Minutos" is offered, and a `1:30` set is added and listed as "3 × 1:30 min";
  - invalid minutes and an invalid rest show "Usa minutos (2) o minutos:segundos (1:30)." and send nothing;
  - stored `restMinutes` 1.5 / 2 and `perSide` true are loaded as `1:30` / `2` and on;
  - saving sends `targetMinutes` 1.5, `restMinutes` 2 and `perSide` true;
  - an empty rest sends no `restMinutes`;
  - "por lado" appears in the dialog and on the list line;
  - the "Descanso: 1:30 min" line appears and is absent without rest;
  - a 400 keeps rest and per side as edited;
  - the English labels.

  Verify `pnpm --filter backoffice test` passes.

- [x] 5.2 Extend `backoffice/e2e/workout.spec.ts`:
  - fixtures gain `perSide: false` and `restMinutes: null`;
  - one flow configures a `1:30` minutes set, `2` minutes of rest and "Por lado", then checks the intercepted request
    body (`targetMinutes` 1.5, `restMinutes` 2, `perSide` true) and the updated card.

  Verify `pnpm test:e2e` passes.

- [x] 5.3 Review the backoffice against its references:
  - the effort field and `inactive-filter.tsx` for the switch;
  - `use-exercise-labels.ts` for the summaries;
  - the existing dialog and list tests;
  - `STPR_INVARIANTS.md`.

  Verify:
  - only atoms import MUI widgets, imports flow downward, and no new atom or molecule was added;
  - every string is in `es` and `en`;
  - `container.ts` is unchanged.

## 6. App and closing

- [x] 6.1 Review the app. Verify `pnpm --filter app typecheck` and `pnpm --filter app test` pass with the regenerated
      types and no app code change.
- [x] 6.2 Run the template drift check from `AGENTS.md`. Verify it prints no `DRIFT:` line.
- [ ] 6.3 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test`, then `pnpm test:e2e`. Verify all pass.
