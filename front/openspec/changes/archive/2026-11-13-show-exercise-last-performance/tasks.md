# Tasks

## 1. Contract sync

- [x] 1.1 With the API of `add-exercise-last-performance-endpoint` running, run `pnpm --filter @weight-tracker/core openapi:refresh` then `openapi:generate`; verify `core/openapi/v1.json` has `GET /api/users/me/exercises/{exerciseId}/last-performance` (`200`, `204`, `401`, `404`) and that the generated types include `ExerciseLastPerformanceResponse` and `LastPerformanceSetResponse`. Note whether `effort` is a typed enum and whether it is nullable.
- [x] 1.2 Review the `v1.json` diff for unrelated changes. Verify `pnpm typecheck` passes for `core`, `backoffice` and `app`, and stop and report if an unrelated change breaks it. Commit the snapshot and the generated types on their own.

## 2. Core

- [x] 2.1 Add the `ExerciseLastPerformance` and `LastPerformanceSet` aliases in `core/src/domain/users` and export them from the domain index; verify they resolve from `@weight-tracker/core/domain`.
- [x] 2.2 Add `getMyExerciseLastPerformance(exerciseId)` to `UserRepositoryInterface` and implement it in `UserRepository` like `getTrainer` (`204` -> `ok(null)`, errors through `mapHttpError`). Extend `core/test/infrastructure/repositories/user-repository.test.ts` with `200`, `204` -> `null`, `401` -> `UNAUTHORIZED`, `404` -> `NOT_FOUND`, network -> `NETWORK` and the request path, and verify they pass.
- [x] 2.3 Add `GetMyExerciseLastPerformanceUseCase` in `core/src/application/use-cases/users`, export it from the index, and add the `getMyExerciseLastPerformanceUseCase` key to `Dependencies`. Add `core/test/application/use-cases/users/get-my-exercise-last-performance-use-case.test.ts`, mirroring `get-user-trainer-use-case.test.ts` (Arrange/Act/Assert), and verify it passes.
- [x] 2.4 Review core against `get-user-trainer-use-case.ts`, `UserRepository.getTrainer` and their tests (naming, comments, file layout); verify `pnpm --filter @weight-tracker/core test` passes.

## 3. Wiring

- [x] 3.1 Register `getMyExerciseLastPerformanceUseCase` in `app/src/infrastructure/container.ts` and in `backoffice/src/infrastructure/container.ts` (wiring only, per design D2); verify `pnpm typecheck` passes in both.

## 4. App: copy and effort helper

- [x] 4.1 Add `efforts` and `effortsByType` to `app/src/i18n/{es,en}/workouts.json`, copied verbatim from `backoffice/src/i18n/{es,en}/workouts.json`, and the `exercise.lastPerformance.*` keys from design D6 in both languages. Verify the app's translation parity test passes.
- [x] 4.2 Add `app/src/presentation/workouts/effort.ts` with `effortKeysOf` (the candidate keys: the type's scale, then the generic one), plus `app/test/presentation/workouts/effort.test.ts` covering `STRENGTH`, `CARDIO`, `STRETCHING`, no type and an unknown type. Verify the tests pass.

## 5. App: state hook and components

- [x] 5.1 Add `useExerciseLastPerformance` in `app/src/presentation/workouts/hooks/use-exercise-last-performance.ts`, modelled on `use-workout.ts` (`load`, `reset`, `performance`, `loading`, `errorCode`, stale-answer guard, never throws); verify it typechecks.
- [x] 5.2 Show the previous sets link (history `icon` + `text-link`) in `WorkoutExerciseView`, between the exercise navigator and the list of sets, once the exercise is read; leave `ExerciseHeroPanel` as it was; verify it typechecks and that the existing view tests still pass.
- [x] 5.3 Add the `LastPerformanceSetLine` molecule in `components/molecules/last-performance-set-line.tsx` (text by props only); verify it typechecks.
- [x] 5.4 Add the `LastPerformanceDialog` organism in `components/organisms/last-performance-dialog.tsx`, on the `dialog` atom like `confirm-dialog.tsx`: title, date formatted for the active language from a local `YYYY-MM-DD`, effort line (hidden when empty), set lines, `LoadingState` / `ErrorState` with retry / empty text, and the close button. Verify it typechecks and imports only downward.

## 6. App: view

- [ ] 6.1 Wire the link and dialog in `workout-exercise-view.tsx`: open sets visibility and calls `load(exercise.exerciseId)`; close and dismiss reset it; a change of the route's `exerciseId` closes it; pass `exercise.type`. Verify manually in Expo Go.
- [x] 6.2 Extend `app/test/presentation/workouts/views/workout-exercise-view.test.tsx`, following its structure and faking the use case through the container. Cover: the link with and without video; tapping it calls the use case with the catalog `exerciseId`; date; effort for `STRENGTH` ("RIR 0"), `CARDIO` ("RPE 5-6"), `STRETCHING` ("Muy suave") and an unknown type ("Medio-bajo"); lines for reps + kg, reps + "sin peso", seconds, and `0 kg`; empty message on `null`; `NOT_FOUND` with retry that then succeeds; close; reopening reads again; switching exercise closes it; and one English case. Verify they pass.
- [x] 6.3 Review the app against its mapped references (`exercise-hero-panel.tsx` video link, `confirm-dialog.tsx`, `set-record-row.tsx`, `use-workout.ts`, the existing view test). Check atomic levels, downward imports, that only atoms import `react-native-paper`, no hard-coded strings, and that the backoffice is untouched beyond `container.ts`.

## 7. Dialog layout

- [x] 7.1 Redesign `LastPerformanceDialog`: calendar icon and short date, divider, two stat cards ("Series realizadas", "Esfuerzo"), the title "Series registradas" and a table card with a separator between rows. Add the `Divider` atom and the `LastPerformanceStatCard` molecule, rework `LastPerformanceSetLine` into a table row, make `formatDay` short, and replace the `date`, `set` and `effort` copy with `setsDone`, `effort` and `setsRecorded` in `es` and `en`; verify the view tests, the `formatDay` test and the translation parity test pass.

- [x] 7.2 Mark the set with the highest weight in the table with a primary-colour star before the weight and the small primary-colour text "Mayor carga" under it, only the last one when it is repeated and none when no set has a weight above 0: add `highestWeightIndexOf`, the star and text in `LastPerformanceSetLine` and the `highest` copy in `es` and `en`; verify `last-performance.test.ts` and the view tests pass.

- [x] 7.3 Read the last performance when an exercise is shown and show the link only when at least one set has a weight: the hook loads in an effect keyed on the catalog exercise id, add `hasWeightIn`, drop the dialog's loading, empty and error states and the unused `empty` copy; verify the view tests for the link, no-weight, never-done, failed-read and no-reread cases pass.
- [x] 7.4 Show a skeleton in place of the link and the list of sets while the last performance is read: add the `Skeleton` atom and the `ExerciseSetsSkeleton` molecule, rendered in the list's place so the rest band stays at the bottom; verify the view tests for reading, settled, failed and switching exercise pass.

## 8. Integration

- [x] 8.1 Run the `.stpr/template/` drift check from `AGENTS.md` and verify it prints no `DRIFT`.
- [x] 8.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes. No E2E is planned (app E2E is a known gap).
