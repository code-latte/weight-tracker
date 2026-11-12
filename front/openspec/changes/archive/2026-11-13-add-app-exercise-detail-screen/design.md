# Design

## Context

See proposal.md (Why). Facts the approach builds on:

- The API change `add-duration-and-optional-weight-set-records` is in the `api/` working tree, not committed. The
  snapshot must be refreshed against an API built from it. Set records may only be added or corrected while the
  workout is `STARTED` or `COMPLETED`; the athlete's own transition is `PUBLISHED` -> `STARTED`, and the core already
  wires it as `changeWorkoutStatusUseCase` (`PUT /workouts/{id}/status`, returns the workout detail).
- `WorkoutByIdExercise` already carries everything the screen needs: `translations`, `videoUrl`, `restMinutes`,
  `sets[]` (`count`, `target.{type,value,maxValue,minutes}`) and `setRecords[]`. One `GET /workouts/{id}` is enough.
- `use-workout.ts` resolves names by `i18n.language` with a first-translation fallback and exports
  `exerciseProgressOf`. The workouts tab already has a stack (`workouts/_layout.tsx`) with `index` and `[id]`.
- The app has read hooks but no submit hook yet. `addSetRecordUseCase`, `updateSetRecordUseCase` and
  `changeWorkoutStatusUseCase` are registered in the app container; the test helper has placeholders for them.
- `AddSetRecordInput` / `UpdateSetRecordInput` are `AddOrUpdateSetRecordRequest & { ids }`, so the regenerated request
  type flows into them with no code change. The backoffice only builds `setRecords: []`; nothing there reads
  `reps` / `weight`.
- Set target types in the contract: `Reps`, `RepsRange`, `MaxReps`, `Duration`, `Minutes`. Set record types:
  `Reps`, `Duration`.

## Goals / Non-Goals

**Goals:**

- Sync the contract once, keep `core` and the backoffice green with no behaviour change.
- Build the exercise screen from existing atoms and the workout screen's shapes; establish the app's submit-hook
  pattern (`Result`-driven, local state, never throws).
- Keep every decision about what a set may hold, and when, in the API.

**Non-Goals:**

- The rest countdown, deleting or reordering records, super-series, per-side, refresh on
  focus, offline.
- Caching the workout across screens: the exercise screen reads it itself, so a deep link works.

## Decisions

### Core

- **Aliases**: `SetRecord` and `AddOrUpdateSetRecordRequest` stay aliases over the regenerated schemas. The comments
  on `AddSetRecordInput` / `UpdateSetRecordInput` say the body carries `reps` or `durationSeconds` and an optional
  `weight`. Nothing else in `core/src` changes.
- **Tests**: the set-record use case tests and the `addSetRecord` / `updateSetRecord` blocks of the repository test
  keep their structure; fixtures gain `type`, `durationSeconds` and a null `weight`, and each block gains one timed,
  weightless case so the new shape is exercised end to end.

### App: routes

- `workouts/[id].tsx` moves to `workouts/[id]/index.tsx`; the new route is
  `workouts/[id]/exercises/[exerciseId].tsx`. Both only default-export their view. `workouts/_layout.tsx` registers
  `[id]/index` and `[id]/exercises/[exerciseId]` with headers off (the jumbo panels are the headers).
- The route param `exerciseId` is the **workout exercise id** (`WorkoutByIdExercise.id`), because that is what the
  row and the navigator know. The hook maps it to `exerciseId` (the catalog id) for the set-record calls, as the
  inputs document.
- Switching exercises in the navigator uses `router.setParams({ exerciseId })`: the same screen instance re-derives
  the exercise from its params, there is no transition, and the back stack stays `workouts -> [id] -> exercise`.

### App: hooks and helpers

| Piece                                                                                                          | Level        | Path                                                           |
| -------------------------------------------------------------------------------------------------------------- | ------------ | -------------------------------------------------------------- |
| `useWorkoutExercise`                                                                                           | state hook   | `src/presentation/workouts/hooks/use-workout-exercise.ts`      |
| `useSaveSetRecord`                                                                                             | state hook   | `src/presentation/workouts/hooks/use-save-set-record.ts`       |
| `useChangeWorkoutStatus`                                                                                       | state hook   | `src/presentation/workouts/hooks/use-change-workout-status.ts` |
| `expandSets`, `setTargetLabel`, `setRecordBodyOf`, `nameOf`, `englishSubtitleOf`, `isRecordable`, `restTimeOf` | pure helpers | `src/presentation/workouts/set-rows.ts`                        |

- **`useWorkoutExercise(workoutId, workoutExerciseId)`**: same read / retry / generation shape as `useWorkout`,
  plus `reload()` (re-read without clearing the ready state, so rows do not flash). It returns `state`, the current
  `exercise` (or `null` when the id is not in the workout: shown as `NOT_FOUND`), `workoutName`, `status`, the
  navigator `items` (`{ id, name, progress }` via `exerciseProgressOf`) and `rows` from `expandSets`.
- **`expandSets(exercise)`** returns `SetRow[]`: `{ number, target, record: SetRecord | null, kind }` where `kind`
  is `"reps" | "range" | "timed"` derived from `target.type` (`Reps` -> reps; `RepsRange` / `MaxReps` -> range;
  `Duration` / `Minutes` -> timed) and `record = exercise.setRecords[index] ?? null`. Positional pairing is the
  decided rule.
- **`isRecordable(status)`** is `status === "STARTED" || status === "COMPLETED"`. It is the only place the app
  reads a status for this screen, and it mirrors the API's documented rule rather than deciding one.
- **`useSaveSetRecord(workoutId, exerciseId)`** exposes `save(row, values)`, `savingNumber` and
  `errors: Record<number, DomainErrorCode>`. `save` builds the body:
  - `kind === "reps"` -> `{ reps: Number(target.value) }`;
  - `kind === "range"` -> `{ reps: values.reps }`;
  - `kind === "timed"` -> `{ durationSeconds: target.type === "Minutes" ? Math.round(Number(target.minutes) * 60) : Number(target.value) }`;
  - `weight` added only when `values.weight` is not empty.
    It calls `addSetRecordUseCase` when `row.record` is null, else `updateSetRecordUseCase` with `row.record.id`; on
    `ok` it resolves `true` so the view calls `reload()`; on failure it stores the code for that row and resolves
    `false`. It never throws.
- **`useChangeWorkoutStatus(workoutId)`** exposes `change(status)`, `changing`, `errorCode`; it calls
  `changeWorkoutStatusUseCase` with `{ workoutId, status }` and resolves `true` on `ok`. The view uses it to start
  (`STARTED`) and to finish (`COMPLETED`, after a `ConfirmDialog`: a new molecule over a new `Dialog` atom). `WorkoutView` then calls `useWorkout`'s new
  `reload()`.
  - Why not update local state from the returned detail: both use cases return the workout, but re-reading keeps one
    source of truth (`GET /workouts/{id}`) and matches how the workout screen already behaves on retry.

### App: components

| Piece                   | Level    | Path                                                                         |
| ----------------------- | -------- | ---------------------------------------------------------------------------- |
| `WorkoutExerciseView`   | view     | `src/presentation/workouts/views/workout-exercise-view.tsx`                  |
| `ExerciseHeroPanel`     | molecule | `src/presentation/workouts/components/molecules/exercise-hero-panel.tsx`     |
| `ExerciseNavigatorItem` | molecule | `src/presentation/workouts/components/molecules/exercise-navigator-item.tsx` |
| `RestCard`              | molecule | `src/presentation/workouts/components/molecules/rest-card.tsx`               |
| `ExerciseNavigator`     | organism | `src/presentation/workouts/components/organisms/exercise-navigator.tsx`      |
| `SetRecordRow`          | organism | `src/presentation/workouts/components/organisms/set-record-row.tsx`          |

- **`ExerciseHeroPanel`**: copy of `WorkoutHeroPanel`'s shell (primary background, top inset, back `IconButton`),
  with `workoutName` (bodyMedium), `name` (titleLarge, heading level 1), optional `subtitle` (bodyMedium, `onPrimary`
  at reduced opacity) and optional `video: { label, onPress }` as a `TextLink`.
- **`ExerciseNavigator`**: horizontal `FlatList` (`accessibilityLabel` = "Ejercicios", `showsHorizontalScrollIndicator`
  off) of `ExerciseNavigatorItem`s; `initialScrollIndex` on the selected item so it is visible when the screen opens.
- **`ExerciseNavigatorItem`**: a `Chip` atom with `ExerciseProgressIcon` as its icon, the name as label, `selected`
  and `onPress`. The chip gives the selected state and the touch target for free.
- **`SetRecordRow`**: the set number alone (headline, secondary colour) beside an outlined `Card`. Inside, a row: the
  target text; then by `kind`: a flat weight `TextInput` (`keyboardType="decimal-pad"`), or flat reps (`number-pad`)
  - weight inputs, or nothing; then one action: a `check-circle` `Icon` labelled "Guardada" while the inputs match
    the saved record, else a contained `IconButton` (`content-save`) labelled "Guardar", `loading` while saving.
    Below, a `HelperText` for the row error or validation message. `readOnly` disables the inputs and shows only the
    tick when there is a record. It holds its input strings in local state, seeded from `record`.
  * Structural validation is local to the row (required integer ≥ 1 for reps when `kind === "range"`; weight, if
    typed, matches `/^\d+([.,]\d{1,2})?$/`, comma accepted and normalised to a dot). Nothing is sent on a violation.
  * "Matches the record" compares the reps string and the normalised weight with the record's values, so "60,00"
    still counts as 60.
- **`RestCard`**: a full-width contained `Card` with square corners under the list (the list is `flex: 1`, so the
  band sits on the tab bar; the tab bar owns the bottom inset), with a timer icon and the rest text centred.
- **`WorkoutHeroPanel`** gains `start?: { label, onPress, disabled, loading }`, rendered as a `Button`
  (`mode="contained-tonal"`) in a row with the progress label. `WorkoutView` derives it from `workout.status`:
  `PUBLISHED` -> "Iniciar entrenamiento" (play); `STARTED` -> "Finalizar entrenamiento" (stop, opens the confirm dialog); `COMPLETED` / `REVIEWED` ->
  "Finalizado" disabled; `DRAFT` -> none.
  A start error renders as a `HelperText` under the panel.
- **`WorkoutHeroPanel`**'s progress bar is 14 px high, filled with the theme's `secondary` orange over a translucent white
  track.
- **`WorkoutExerciseRow`** gains `onPress`; the `Card` becomes pressable. `WorkoutView` pushes
  `/workouts/${id}/exercises/${row.id}`.
- Imports flow view -> organisms -> molecules -> atoms; only atoms import Paper widgets. `FlatList`, `View`,
  `Linking` and `StyleSheet` from React Native are used as the workout view already does.

### Translations

New keys in `app/src/i18n/{es,en}/workouts.json`:

- `workout.start` "Iniciar entrenamiento" / "Start workout"; `workout.finish` "Finalizar entrenamiento" / "Finish workout"; `workout.finished` "Finalizado" / "Finished";
  `workout.finishDialog.{title,message,messageNoTrainer,confirm,cancel}`.
- `exercise.navigator` "Ejercicios" / "Exercises"; `exercise.target.reps` "{{count}} reps"; `exercise.target.range` "{{min}}–{{max}} reps"; `exercise.target.max`
  "máx. reps" / "max reps"; `exercise.target.seconds` "{{count}} s"; `exercise.target.minutes` "{{count}} min";
  `exercise.reps` "Reps"; `exercise.weight` "Peso (kg)" / "Weight (kg)"; `exercise.save` "Guardar" / "Save";
  `exercise.saved` "Guardada" / "Saved"; `exercise.repsRequired` "Indica las repeticiones." / "Enter the reps.";
  `exercise.weightInvalid` "Peso no válido." / "Invalid weight."; `exercise.rest` "Descanso: {{time}}" / "Rest: {{time}}" (`time` is `M:SS` from
  `restTimeOf`, built on core's `minutesToText`); `exercise.noRest` "Sin descanso" / "No rest"; `exercise.errors.CONFLICT` "No se puede
  registrar esta serie." / "This set cannot be recorded.".
- `common:back`, `workout.video`, `workout.unknownExercise` and `workout.exerciseStatus.*` are reused.

### Errors

| Failure                               | Code                     | Shown                                         |
| ------------------------------------- | ------------------------ | --------------------------------------------- |
| workout read                          | as mapped                | `ErrorState` under the header, retry          |
| exercise id not in the workout        | `NOT_FOUND` (local)      | `ErrorState`, retry re-reads                  |
| save, locked / wrong kind             | `CONFLICT`               | row: `exercise.errors.CONFLICT`               |
| save, bad values                      | `VALIDATION`             | row: `errors:VALIDATION`                      |
| save, other                           | `errors:<code>`          | row                                           |
| start, not allowed / wrong transition | `FORBIDDEN` / `CONFLICT` | `HelperText` under the panel, `errors:<code>` |
| session                               | `UNAUTHORIZED`           | existing handling                             |

No new mapper code.

### State

| Holder                   | Holds                                     | Changed by                    | Resets                      |
| ------------------------ | ----------------------------------------- | ----------------------------- | --------------------------- |
| `useWorkoutExercise`     | workout, status, error code               | mount, route change, `reload` | screen exit                 |
| `useSaveSetRecord`       | saving row number, error code per row     | `save`                        | next save of that row, exit |
| `useChangeWorkoutStatus` | `starting`, error code                    | `start`                       | next `start`, exit          |
| `SetRecordRow`           | reps / weight strings, validation message | the athlete                   | `record` change, unmount    |

Nothing persists across screens or after logout.

### Tests

The templates are the existing tests of the referenced files. The test helper does not change (the three use cases
already have placeholders).

- **core**: set-record use case tests and the repository `addSetRecord` / `updateSetRecord` blocks: fixtures in the
  new shape, plus a timed weightless case each.
- **app**:
  - `set-rows.test.ts`: expansion across prescriptions, positional pairing, `kind` by target type, label keys,
    `isRecordable`, English subtitle rule.
  - `workout-exercise-view.test.tsx`: every `app-workout-exercise` scenario, with `getWorkoutByIdUseCase`,
    `addSetRecordUseCase` and `updateSetRecordUseCase` faked through the container; `expo-router` mocked with
    `router.back` / `router.setParams` and `useLocalSearchParams` returning both ids; `Linking` mocked.
  - `workout-view.test.tsx`: row tap pushes the exercise route; start button per status; start success re-reads and
    offers "Finalizar"; finishing asks first, sends `COMPLETED` once confirmed and re-reads; cancel and failure; "Finalizado" on
    `COMPLETED` / `REVIEWED`; start and finish failures show the error.
  - Translation parity picks up the new keys.
- No backoffice tests change unless the typecheck forces a fixture; no E2E (app E2E is a known gap).

## Risks / Trade-offs

- [The snapshot depends on an uncommitted API change] → Refresh from the current `api/` working tree; task 1 verifies
  the two schemas before anything else is built. If the API change moves before merge, refresh again.
- [Positional pairing drifts if records are deleted or reordered elsewhere] → Deleting and reordering are out of
  scope in the app; the API's own "next prescribed set" rule is positional too. Records beyond the prescribed total
  are simply not shown.
- [Status mirrored in the app] → Only through `isRecordable` and the start button mapping, both documented API
  rules. The API still answers `409` / `403`, which the rows and the panel show.
- [`Minutes` targets rounded to seconds client-side] → Same rounding the API applies for its empty-body shortcut;
  a `PUT` later can correct it.
- [Decimal separator] → The weight input accepts `,` and `.` and normalises before sending; the row shows "Peso no
  válido." otherwise.

## Migration Plan

Snapshot refresh first, in its own commit, with the API running from the `api/` working tree. Then core tests, the
backoffice check, and the app. No data migration. Rollback is reverting the commits; the API endpoints are usable
without the front.
