# Tasks

No `core` or backoffice work: the change adds no alias, port, use case, repository or container key.

## 1. App: copy and helpers

- [x] 1.1 Add `exercise.restFinished`, `exercise.restPlay`, `exercise.restPause` and `exercise.restReset` to `app/src/i18n/es/workouts.json` and `app/src/i18n/en/workouts.json` with the texts in design.md. Verify the app's translation parity test passes (`pnpm --filter app test`).
- [x] 1.2 Add `restSecondsOf` and `countdownTextOf` to `app/src/presentation/workouts/set-rows.ts` and re-express `restTimeOf` on top of them without changing its output. Extend `app/test/presentation/workouts/set-rows.test.ts`: `countdownTextOf` for `0`, `9`, `60`, `90`, `600`; `restSecondsOf` for whole, fractional and string minutes; existing `restTimeOf` cases untouched. AAA, one Act each. Verify `pnpm --filter app test` passes.

## 2. App: hook

- [x] 2.1 Create `app/src/presentation/workouts/hooks/use-rest-countdown.ts` (template `common/hooks/use-keyboard-visible.ts`): `useRestCountdown(exerciseKey, totalSeconds)` returning `status`, `remainingSeconds`, `play`, `pause`, `reset`, with the end-timestamp mechanism, the 250 ms interval that stores only changed seconds, finish at zero, the `AppState` `active` recomputation, cleanup on unmount, and the reset rules in design.md (key change or new non-null total; `null` ignored). Verify the app typecheck passes (`pnpm --filter app typecheck`); its behaviour is covered through the view in 4.2.

## 3. App: molecule

- [x] 3.1 Extend `app/src/presentation/workouts/components/molecules/rest-card.tsx` (template `workout-hero-panel.tsx` for the action prop) with `finished` and the optional `controls` (play / pause and reset `IconButton` atoms with accessibility labels, disabled states). Without `controls` it renders exactly as today. Only atoms imported. Verify the app typecheck and lint pass.

## 4. App: view and tests

- [x] 4.1 In `app/src/presentation/workouts/views/workout-exercise-view.tsx`, call `useRestCountdown(exerciseId, exercise?.restMinutes == null ? null : restSecondsOf(exercise.restMinutes))` unconditionally, build the label (`noRest`, `restFinished` or `rest` with `countdownTextOf(remainingSeconds)`) and pass `finished` and `controls` (only when there is rest) to `RestCard`. Verify the app typecheck passes and, in Expo Go, play / pause / reset work, the countdown survives the keyboard and a save, and backgrounding mid-rest shows the right time on return.
- [x] 4.2 Extend `app/test/presentation/workouts/views/workout-exercise-view.test.tsx` with every new and modified `app-workout-exercise` scenario: controls with and without rest and on `PUBLISHED`; start; resume; pause; reset while running and while paused; finish and reset after finishing; late tick (`jest.setSystemTime`); back from the background and rest ended in the background (mocked `AppState` handler); keyboard (mocked `use-keyboard-visible`); saving a set keeps the countdown; switching exercise resets it; remaining time in the band; English labels. Fake timers via `jest.useFakeTimers({ now })`, restored after each test; fakes through the container; `es` by default; AAA with one Act. Verify `pnpm --filter app test` passes.

## 5. Review and final verification

- [x] 5.1 Review the app changes against their mapped references (`use-keyboard-visible.ts`, `restTimeOf`, `workout-hero-panel.tsx`, `workout-exercise-view.test.tsx`): naming, folder per atomic level, downward imports, only atoms import React Native Paper, every string in `es` and `en`, no business rule, no `core/infrastructure` import. Verify by reading the diff against the checklist in `AGENTS.md`.
- [x] 5.2 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test` and verify all pass. Then run the template drift loop from `AGENTS.md` and verify it prints nothing.

## Workflow follow-up

- Archive the change after review.
