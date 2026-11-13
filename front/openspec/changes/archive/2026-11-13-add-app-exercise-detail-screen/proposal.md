# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-app-exercise-detail-screen.md`.

## Why

The athlete can open a workout but cannot record anything: the caret on each exercise row is a placeholder. The API
now accepts timed sets (plank, 45 s), sets without a weight (jumping jacks) and corrections through the existing
`POST` / `PUT .../set-records` routes (API change `add-duration-and-optional-weight-set-records`), but the front
snapshot still has the old shape, where `reps` and `weight` are required and never null. The API also only accepts
records while a workout is `STARTED` or `COMPLETED`, and the app has no way to start one.

## What Changes

- **Snapshot refresh**: `openapi:refresh` + `openapi:generate` against the API working tree of
  `add-duration-and-optional-weight-set-records`, in a commit of its own. `AddOrUpdateSetRecordRequest` becomes
  `{ reps?, durationSeconds?, weight? }` and `SetRecordResponse` becomes
  `{ id, type: "Reps" | "Duration", reps?, durationSeconds?, weight?, registeredAtUtc }`.
- **core**: no new use case, port or `Dependencies` key. The aliases are regenerated; the set-record use case and
  repository tests gain the new shape (timed set, no weight). The backoffice keeps compiling against the nullable
  fields with no behaviour change.
- **app, workout screen** (`/workouts/[id]`):
  - each exercise row navigates to its exercise screen;
  - the jumbo panel gains a start button next to the progress bar: "Iniciar entrenamiento" on a `PUBLISHED` workout, which moves
    it to `STARTED` through `changeWorkoutStatusUseCase` and re-reads it; on `STARTED` a "Finalizar entrenamiento" button (stop icon) that, after a confirmation dialog saying it goes to the trainer for review and records can be changed until then, moves it to `COMPLETED`; a disabled "Finalizado" on `COMPLETED` and `REVIEWED`; nothing on `DRAFT`. A failure shows the error and keeps "Iniciar".
- **app, new exercise screen** (`/workouts/[id]/exercises/[exerciseId]`):
  - a jumbo header with back, the workout name, the exercise name in the app language, the English name greyed
    below it only when the app language is not `en`, and the video link when there is one;
  - a horizontally scrollable navigator with one item per exercise of the workout, in API order, showing the same
    progress icon as the workout list and the name; the current one highlighted; tapping another switches to it;
  - one row per prescribed set, expanding each prescription `count × target` in order: set number, the target as text
    (`n reps`, `min–max reps`, `máx. reps`, `n s`, `n min`), the inputs that target asks for (weight for `Reps`; reps
    and weight for `RepsRange` / `MaxReps`; none for `Duration` / `Minutes`) and a save button;
  - saving row `i` does `POST` when `setRecords[i]` does not exist and `PUT` on that record when it does; both send
    the explicit value (`reps`, or `durationSeconds` from the target) and `weight` only when typed; after a save the
    workout is re-read so rows, navigator and header reflect it;
  - while the workout is not `STARTED` or `COMPLETED` the rows are read-only: inputs disabled, save hidden;
  - a fixed card at the bottom with the exercise's rest time ("Descanso: n min" / "Sin descanso"), display only;
  - loading, screen error with retry, and per-row save errors.

### Fronts and roles

- app, `USER`: everything above. The front mirrors the API's published rules (which statuses accept records, which
  transition the athlete may make) to decide what to show; the API still enforces them.
- backoffice, `ADMIN` / `TRAINER`: contract only. Nothing it shows changes.

### Components (atomic level, reuse)

- Atoms reused, none added: `text`, `text-input`, `button`, `icon-button`, `icon`, `card`, `chip`, `helper-text`,
  `safe-area`, `text-link`.
- Molecules: new `ExerciseHeroPanel`, `ExerciseNavigatorItem`, `RestCard`; `WorkoutHeroPanel` gains the start / finish button; new `ConfirmDialog` over a new `Dialog` atom;
  `WorkoutExerciseRow` gains `onPress`; reused `ExerciseProgressIcon`, `LoadingState`, `ErrorState`.
- Organisms: new `ExerciseNavigator`, `SetRecordRow`.

### Canonical references

UI references come from the app only. Atom, molecule and organism references are `pending` in
`.stpr/template/README.md`, so this change follows the app shapes already in place.

- Aliases -> `core/src/domain/workouts/workout.ts`, regenerated only.
- Core tests -> `add-set-record-use-case.test.ts`, `update-set-record-use-case.test.ts`, the `addSetRecord` /
  `updateSetRecord` blocks of `workout-repository.test.ts`.
- Nested route and stack -> `workouts/_layout.tsx`, `workouts/[id].tsx`.
- `WorkoutExerciseView` and `useWorkoutExercise` -> `workout-view.tsx`, `use-workout.ts` (its `exerciseProgressOf`
  is reused, not copied).
- `useSaveSetRecord`, `useChangeWorkoutStatus` -> `use-workout.ts` for container and `Result` handling; the app has no
  submit hook yet, so this change establishes that pattern.
- `ExerciseHeroPanel` -> `workout-hero-panel.tsx`. `ExerciseNavigator` -> `week-navigator.tsx`. `SetRecordRow` ->
  `workout-exercise-row.tsx`. `RestCard` -> `week-progress-card.tsx`.
- Tests -> `app/test/presentation/workouts/views/workout-view.test.tsx`.

### Conflicts and stop cases

None. Every rule stays in the API: which kinds a set accepts, which transitions exist, when results are locked.
Expanding prescriptions into rows, choosing inputs by `target.type`, pairing rows with records by position and
showing the start button by status are presentation derivations of data the API returns. Structural validation only:
reps required and ≥ 1 where the athlete types them, weight ≥ 0 with two decimals when typed. `409` is already
`CONFLICT`; no new error code.

Decided with the product owner before this proposal: rows pair with records by position; `POST` sends the explicit
value rather than the API's empty-body shortcut; locked workouts show read-only rows; the start button lives on the
workout screen's panel.

## Capabilities

### New Capabilities

- `app-workout-exercise`: the exercise screen with its header, navigator, set rows, recording and rest card.

### Modified Capabilities

- `app-workouts-list`: the list reads again when it regains focus, so changes made in a workout show without a pull.
- `app-workout-detail`: rows open the exercise screen; the panel offers the start / finish button by status, with a confirmation before finishing.
- `workout-management`: recording and correcting a set send `reps` or `durationSeconds` and an optional `weight`,
  and the workout detail carries set records in the new shape.

## Impact

- `core/openapi/v1.json`, `core/src/contract/generated` (regenerated); `core/src/domain/workouts/workout.ts`
  (comments only); `core/test` set-record tests.
- `backoffice`: whatever stops compiling on nullable `reps` / `weight`, fixed in place (`use-exercises-editor.ts` builds
  `setRecords: []` and should be unaffected).
- `app/src/app/(app)/(tabs)/workouts/` (new nested route, `[id]` moved to `[id]/index.tsx`),
  `app/src/presentation/workouts/` (new view, hooks, helpers, molecules, organisms; `workout-view`,
  `workout-hero-panel`, `workout-exercise-row` extended), `app/src/i18n/{es,en}/workouts.json`, `app/test`.
- No new dependency, no API change, no mapper change, no native module.
