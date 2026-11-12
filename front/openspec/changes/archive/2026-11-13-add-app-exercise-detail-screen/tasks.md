# Tasks

## 1. API contract snapshot

- [x] 1.1 Run the API from the current `api/` working tree (change `add-duration-and-optional-weight-set-records`) on `http://localhost:5199`, then `pnpm --filter @weight-tracker/core openapi:refresh` and `openapi:generate`. Verify in `core/openapi/v1.json` that `AddOrUpdateSetRecordRequest` has optional `reps`, `durationSeconds` and `weight`, and `SetRecordResponse` has `type`, nullable `reps`, `durationSeconds` and `weight`. Review the diff and commit the snapshot and generated types alone. Never hand-edit `core/src/contract/generated`.

## 2. Core: set-record shape

- [x] 2.1 Update the comments on `AddSetRecordInput` / `UpdateSetRecordInput` in `core/src/domain/workouts/workout.ts` (body: `reps` or `durationSeconds`, optional `weight`). Verify `pnpm --filter @weight-tracker/core typecheck` passes.
- [x] 2.2 Update `add-set-record-use-case.test.ts`, `update-set-record-use-case.test.ts` and the `addSetRecord` / `updateSetRecord` blocks of `workout-repository.test.ts`: fixtures in the new shape (`type`, `durationSeconds`, nullable `weight`) and one timed, weightless case per block (`{ durationSeconds: 45 }` sent unchanged, record returned with `type` "Duration"). Verify `pnpm --filter @weight-tracker/core test` passes.
- [x] 2.3 Run `pnpm typecheck` from `front/`. If the backoffice fails on nullable `reps` / `weight`, fix the fixture or read in place without behaviour change. Verify `pnpm --filter backoffice typecheck && pnpm --filter backoffice test` pass.
- [x] 2.4 Review core against `get-users-use-case.ts` / `workout-repository.ts` shapes and the invariants (aliases only, no hand-written type). Verify the drift loop from `AGENTS.md` prints nothing.

## 3. App: copy and helpers

- [x] 3.1 Add `workout.start`, `workout.started` and the `exercise.*` keys (design → Translations) to `app/src/i18n/{es,en}/workouts.json`. Verify the translation parity test passes.
- [x] 3.2 Create `src/presentation/workouts/set-rows.ts` (`expandSets`, `setTargetLabelKey`, `nameOf`, `englishSubtitleOf`, `isRecordable`) and `test/presentation/workouts/set-rows.test.ts` covering: expansion of `3×10 + 1×8` into four numbered rows; positional pairing with fewer, equal and more records; `kind` per target type; label key and params per target type; `isRecordable` for every status; English subtitle shown only when the language is not `en`, the `en` name exists and differs. AAA, one Act each. Verify `pnpm --filter app test` passes.

## 4. App: hooks

- [x] 4.1 Add `reload()` to `use-workout.ts` (re-read without clearing the ready state). Create `hooks/use-workout-exercise.ts` (template `use-workout.ts`): read by id, `retry`, `reload`, current exercise by workout-exercise id (`null` → `NOT_FOUND`), `workoutName`, `status`, navigator `items` via `exerciseProgressOf`, `rows` via `expandSets`. Verify the app typecheck passes.
- [x] 4.2 Create `hooks/use-save-set-record.ts` (`save(row, values)` building the body per `kind`, `POST` when `row.record` is null else `PUT`, `savingNumber`, per-row `errors`, never throws) and `hooks/use-start-workout.ts` (`start()`, `starting`, `errorCode` over `changeWorkoutStatusUseCase`). Verify the app typecheck passes.

## 5. App: molecules

- [x] 5.1 Create `ExerciseHeroPanel` (template `workout-hero-panel.tsx`): back, workout name, exercise name as heading, optional greyed subtitle, optional video `TextLink`. Verify typecheck and lint (no Paper widget outside atoms).
- [x] 5.2 Create `ExerciseNavigatorItem` (`Chip` atom with `ExerciseProgressIcon`, label, `selected`, `onPress`) and `RestCard` (template `week-progress-card.tsx`: card with icon and rest text). Verify typecheck and lint.
- [x] 5.3 Extend `WorkoutHeroPanel` with the optional `start` button beside the progress label, and `WorkoutExerciseRow` with `onPress`. Verify `workout-view.test.tsx` still passes unchanged.

## 6. App: organisms

- [x] 6.1 Create `ExerciseNavigator` (template `week-navigator.tsx`): horizontal `FlatList` of items, accessibility label, selected item scrolled into view. Verify typecheck and lint.
- [x] 6.2 Create `SetRecordRow`: set number and target text; inputs by `kind`; "Guardar" with loading; local structural validation (reps required ≥ 1 for `range`; weight `^\d+([.,]\d{1,2})?$` when typed, comma normalised); `HelperText` for messages; "Guardada" mark when `record` exists; `readOnly` disables inputs and hides the button; inputs reseeded when `record` changes. Verify typecheck and lint.

## 7. App: view and routes

- [x] 7.1 Create `views/workout-exercise-view.tsx` (template `workout-view.tsx`): `ExerciseHeroPanel`, `ExerciseNavigator` (tap → `router.setParams`), `LoadingState` / `ErrorState`, `FlatList` of `SetRecordRow`s wired to `useSaveSetRecord` (success → `reload()`), `readOnly = !isRecordable(status)`, `RestCard` under the list. Move `app/src/app/(app)/(tabs)/workouts/[id].tsx` to `[id]/index.tsx`, add `[id]/exercises/[exerciseId].tsx` (default-exports the view) and register both in `workouts/_layout.tsx`. Verify the app typecheck passes and `pnpm exec expo start` opens both routes.
- [x] 7.2 Wire `WorkoutView`: row tap pushes `/workouts/${id}/exercises/${row.id}`; `useChangeWorkoutStatus` behind the panel's start button mapped from `workout.status` (`PUBLISHED` → "Iniciar"; `STARTED` / `COMPLETED` / `REVIEWED` → disabled "Iniciado"; `DRAFT` → none); success → `reload()`; failure → `HelperText` under the panel. Verify the app typecheck passes.
- [x] 7.3 Add `test/presentation/workouts/views/workout-exercise-view.test.tsx` (template `workout-view.test.tsx`) covering every `app-workout-exercise` scenario: header with and without English subtitle and video; back; navigator order, labels and selection; switch via `router.setParams`; four rows from `3×10 + 1×8`; target texts per kind; inputs per kind; prefilled recorded row with "Guardada"; `POST` bodies for reps / range / timed; `PUT` on an existing record; reps required and invalid weight send nothing; `CONFLICT` row error keeps values; reload after success; read-only rows on `PUBLISHED` and `REVIEWED`; rest card with and without `restMinutes`; loading; read failure and retry; unknown exercise id; English copy. Fakes through the container, `es` by default, AAA. Verify `pnpm --filter app test` passes.
- [x] 7.4 Extend `workout-view.test.tsx`: tapping a row pushes the exercise route; "Iniciar" on `PUBLISHED` calls `changeWorkoutStatusUseCase` with `{ workoutId, status: "STARTED" }`, re-reads and shows disabled "Iniciado"; "Iniciado" disabled on `STARTED` and `REVIEWED`; no button on `DRAFT`; start failure shows the error and keeps "Iniciar"; English "Start". Verify `pnpm --filter app test` passes.
- [x] 7.5 Review the app against its references (`workout-view.tsx`, `use-workout.ts`, `workout-hero-panel.tsx`, `week-navigator.tsx`, `workout-exercise-row.tsx`, `week-progress-card.tsx`) and the `AGENTS.md` checklist: atomic levels, downward imports, routes only in `src/app`, no `core/infrastructure` import, every string in `es` and `en`, no business rule beyond the documented status mirror. Verify by reading the diff against each reference, and check in Expo Go: panel fixed, navigator scrolls, rest card stays at the bottom with the keyboard closed, back returns to the workout after switching exercises.

## 8. Final verification

- [x] 8.1 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test` and verify that all pass. Then run the template drift loop from `AGENTS.md` and verify it prints nothing.
