# Change Proposal: Sync Set-Record Contract And Add App Exercise Detail Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-app-exercise-detail-screen/`.

## Change

Refresh the OpenAPI snapshot to pick up the new set-record contract (API change
`add-duration-and-optional-weight-set-records`: `reps`, `durationSeconds` and `weight` all optional, `SetRecordResponse`
with `type`), adapt `core` and the backoffice to it, and add the app's exercise detail screen at
`/workouts/[id]/exercises/[exerciseId]`: a jumbo header, a horizontal exercise navigator, the list of prescribed sets
with a form to record each one, and a fixed rest-time card at the bottom.

## Objective

The athlete can see a workout's exercises but cannot record anything: the caret on each exercise row is a
placeholder. The API now accepts timed sets (plank, 45 s), sets without weight (jumping jacks) and "done as
prescribed" with an empty body, but the front snapshot still has the old shape, where `reps` and `weight` are required
and non-null. This change syncs the contract and gives the athlete the screen where sets are recorded.

## Capabilities

- Contract sync: `AddOrUpdateSetRecordRequest` (`reps?`, `durationSeconds?`, `weight?`), `SetRecordResponse`
  (`type: "Reps" | "Duration"`, `reps?`, `durationSeconds?`, `weight?`). `core` use cases and repository keep their
  shape; only the aliases and tests change. The backoffice keeps compiling against the nullable fields.
- Exercise detail header: back button, workout name, exercise name in the app language, the English name as a greyed
  subtitle only when the app language is not `en`, and a "Ver vídeo" link when the exercise has a video.
- Exercise navigator: a horizontally scrollable row with one chip per exercise of the workout, in API order, each with
  the same progress icon as the workout's exercise list (done / in progress / not started) and the name in the app
  language. The current exercise is highlighted. Tapping another one switches the screen to it (route replace, so
  back still returns to the workout).
- Set list: one row per prescribed set, expanding each prescription `count × target` in order. Each row shows, left to
  right: the set number; the target as text; the inputs the target asks for; a save button.
  - Target text by `target.type`: `Reps` -> "n reps"; `RepsRange` -> "min–max reps"; `MaxReps` -> "máx. reps";
    `Duration` -> "n s"; `Minutes` -> "n min".
  - Inputs: `Reps` -> weight; `RepsRange` and `MaxReps` -> reps done and weight; `Duration` and `Minutes` -> none.
    Weight is always optional (empty = not tracked). Reps done is required for `RepsRange` / `MaxReps`.
  - Save: the set at position `i` is persisted when `setRecords[i]` exists. Without a record the button does `POST`
    (`addSetRecordUseCase`); with one it does `PUT` (`updateSetRecordUseCase`) on that record's id. A row with a record
    shows the recorded values prefilled and a "saved" mark.
  - `POST` and `PUT` send the same explicit body (decided): `reps` for rep-based targets (the prescribed `value` for
    `Reps`, the typed count for `RepsRange` / `MaxReps`), `durationSeconds` for timed ones (`value` for `Duration`,
    `minutes × 60` rounded for `Minutes`), plus `weight` only when typed. The API's empty-body shortcut is not used.
  - After a successful save the workout is re-read so the list, the navigator icons and the header reflect it.
- Locked results (decided): the API only accepts set records while the workout is `STARTED` or `COMPLETED`. In any
  other status (`DRAFT`, `PUBLISHED`, `REVIEWED`) the rows are read-only: inputs disabled with their values, save
  button hidden, saved marks kept. No request is sent.
- Start button (decided): the workout screen's jumbo panel gains a button next to the progress bar. On a `PUBLISHED`
  workout it reads "Iniciar" and calls `changeWorkoutStatusUseCase` with `{ workoutId, status: "STARTED" }`; on
  success the workout is re-read and the button becomes a disabled "Iniciado". On `STARTED` / `COMPLETED` /
  `REVIEWED` it shows disabled "Iniciado" from the start; on `DRAFT` it is not shown. A failure shows the error
  under the panel and keeps "Iniciar" enabled.
- Rest card: fixed at the bottom, showing the exercise's `restMinutes` as "Descanso: n min" (or "Sin descanso" when
  null). Display only.
- Loading, error (with retry) and per-row save error states.

## Fronts And Roles

- app -> lands here: yes -> roles: `USER` -> the athlete opens an exercise from the workout screen, switches between
  exercises, records sets and sees the rest time. The workout exercise row's caret becomes the way in. The athlete
  starts a `PUBLISHED` workout from the workout screen; until then, and after review, the set rows are read-only.
- backoffice -> lands here: contract only -> roles: `ADMIN`, `TRAINER` -> `use-exercises-editor.ts` and anything that
  reads `SetRecord.weight` / `reps` must tolerate `null`. No screen, copy or behaviour change.

## Classification

Core:

- `SetRecord`, `AddOrUpdateSetRecordRequest` -> entity type -> existing aliases in `core/src/domain/workouts/workout.ts`,
  regenerated; `AddSetRecordInput` / `UpdateSetRecordInput` unchanged (comment updated: body may carry `durationSeconds`).
- No new use case, port or `Dependencies` key.

App (`workouts` domain area):

- Exercise route -> route -> `app/src/app/(app)/(tabs)/workouts/[id]/exercises/[exerciseId].tsx` (default-exports the
  view); `[id].tsx` moves to `[id]/index.tsx`; the stack in `workouts/_layout.tsx` registers the new screen.
- `WorkoutExerciseView` -> view -> `app/src/presentation/workouts/views/workout-exercise-view.tsx`
- `useWorkoutExercise` (reads the workout through `getWorkoutByIdUseCase`, exposes the current exercise, the navigator
  items, the expanded set rows, `reload`) -> state hook -> `app/src/presentation/workouts/hooks/use-workout-exercise.ts`
- `useSaveSetRecord` (`save(row, values)`, per-row `saving` / `errorCode`) -> state hook ->
  `app/src/presentation/workouts/hooks/use-save-set-record.ts`
- `expandSets(exercise)` (prescriptions -> rows with set number, target, existing record) and `setTargetLabel` ->
  pure helpers -> `app/src/presentation/workouts/set-rows.ts`
- `ExerciseHeroPanel` (back, workout name, exercise name, English subtitle, video link) -> molecule ->
  `app/src/presentation/workouts/components/molecules/exercise-hero-panel.tsx`
- `ExerciseNavigator` (horizontal `FlatList` of `ExerciseNavigatorItem`) -> organism ->
  `app/src/presentation/workouts/components/organisms/exercise-navigator.tsx`
- `ExerciseNavigatorItem` (progress icon + name, selected state) -> molecule ->
  `app/src/presentation/workouts/components/molecules/exercise-navigator-item.tsx`
- `SetRecordRow` (number, target text, inputs by target kind, save button, saved mark, row error) -> organism ->
  `app/src/presentation/workouts/components/organisms/set-record-row.tsx`
- `RestCard` -> molecule -> `app/src/presentation/workouts/components/molecules/rest-card.tsx`
- `useStartWorkout` (`start()`, `starting`, `errorCode`) -> state hook ->
  `app/src/presentation/workouts/hooks/use-start-workout.ts`; `WorkoutHeroPanel` gains a `start` prop
  (`{ label, onPress, disabled }`), `WorkoutView` wires it from `workout.status`.
- Atoms reused: `text`, `text-input`, `button`, `icon-button`, `icon`, `card`, `chip`, `helper-text`, `safe-area`,
  `text-link`. Molecules reused: `ExerciseProgressIcon`, `LoadingState`, `ErrorState`.
- `WorkoutExerciseRow` gains `onPress`; `WorkoutView` navigates to the exercise route.
- Copy -> new keys under `exercise.*` in `app/src/i18n/{es,en}/workouts.json`.

## Canonical Template Mapping

- Aliases -> `core/src/domain/workouts/workout.ts` -> regenerated aliases only.
- Core tests -> `add-set-record-use-case.test.ts`, `update-set-record-use-case.test.ts`,
  `workout-repository.test.ts` (`addSetRecord` / `updateSetRecord`) -> fixtures gain `type`, `durationSeconds`, nullable
  `weight`; add a timed-set and an empty-body case.
- Nested route and stack -> `app/src/app/(app)/(tabs)/workouts/_layout.tsx` and `[id].tsx` -> same tab stack, headers off.
- `WorkoutExerciseView` -> `app/src/presentation/workouts/views/workout-view.tsx` -> hero panel over a list, loading /
  error states, `useLocalSearchParams`.
- `useWorkoutExercise` -> `app/src/presentation/workouts/hooks/use-workout.ts` -> same read, `i18n.language` name
  resolution and `exerciseProgressOf` (reused, not duplicated).
- `useSaveSetRecord`, `useStartWorkout` -> `app/src/presentation/workouts/hooks/use-workout.ts` (container +
  `Result` handling) -> the app has no submit hook yet; this change establishes the pattern (`Result`-driven, local
  `saving` / `errorCode`, never throws).
- `ExerciseHeroPanel` -> `workout-hero-panel.tsx` -> same colours, back button and title row.
- `ExerciseNavigator` / `ExerciseNavigatorItem` -> `week-navigator.tsx` -> the area's horizontal switcher.
- `SetRecordRow` -> `workout-exercise-row.tsx` -> card row with icon, texts and action.
- `RestCard` -> `week-progress-card.tsx` -> fixed card with a label and a value.
- `set-rows.ts` -> `exerciseProgressOf` in `use-workout.ts` -> pure presentation derivation.
- View test -> `app/test/presentation/workouts/views/workout-view.test.tsx` -> fakes through the container, `es` by
  default, `Linking` and `expo-router` mocked.
- Atom/molecule/organism references are `pending` in `.stpr/template/README.md`; the app shapes above apply.

## Screens And Navigation

- app -> `/workouts/[id]/exercises/[exerciseId]` -> `WorkoutExerciseView` -> tapping an exercise row in
  `/workouts/[id]` -> back returns to the workout; navigator tap replaces the route with another `exerciseId`;
  save success stays on the screen and re-reads; save failure stays with the row error; a workout or exercise that
  cannot be read shows `ErrorState` with retry under the header.

## API Contract

- `GET /workouts/{id}` -> yes -> `WorkoutByIdResponse`; uses `name`, `status`, `exercises[]` (`translations`,
  `videoUrl`, `restMinutes`, `sets[].count`, `sets[].target.{type,value,maxValue,minutes}`, `setRecords[]`).
- `PUT /workouts/{workoutId}/status` -> yes -> `ChangeWorkoutStatusRequest` `{ status: "STARTED" }`, `200
WorkoutByIdResponse`; already wired as `changeWorkoutStatusUseCase`. Failures `400`, `403`, `404`, `409`.
- `POST /workouts/{workoutId}/exercises/{exerciseId}/set-records` -> in snapshot: yes, **shape stale, refresh
  needed** -> `AddOrUpdateSetRecordRequest`, `201 SetRecordResponse`. Failures: `400` (`VALIDATION`: both values,
  empty body on `RepsRange` / `MaxReps`), `401`, `403`, `404`, `409` (`CONFLICT`: results locked, kind mismatch).
- `PUT .../set-records/{setRecordId}` -> same, `200 SetRecordResponse`; `400` on an empty body.
- The refresh runs against the API working tree of `add-duration-and-optional-weight-set-records`, in a commit of
  its own. `exerciseId` in the path is the catalog exercise id (`WorkoutByIdExercise.exerciseId`), as today's inputs
  document; the route param of the app is the workout-exercise `id`, resolved to it by the hook.

## Held State

- `useWorkoutExercise` -> the workout, loading / error, current exercise id from the route -> mount, `reload` after a
  save, route change -> resets on screen exit.
- `useSaveSetRecord` -> per-row `saving` and `errorCode` -> submit -> cleared on the next submit of that row and on exit.
- `useStartWorkout` -> `starting`, `errorCode` -> the start button -> cleared on the next press and on exit.
- `SetRecordRow` -> its input values (prefilled from the record when any) -> the athlete -> reset when the record
  changes after a reload. Nothing persists across screens.

## Forms And Structural Validation

- Reps done (`RepsRange`, `MaxReps`) -> required, integer ≥ 1 (the API's `RepCount` floor).
- Weight (rep-based targets) -> optional, number ≥ 0 with at most 2 decimals; empty sends no `weight`.
- Timed targets -> no inputs.
- Decimal separator follows the device locale; the value is sent as a number.

## Errors And Copy

- Locked results or kind mismatch -> `CONFLICT` -> row error "No se puede registrar esta serie." / "This set cannot be
  recorded."
- Bad values -> `VALIDATION` -> row error `errors:VALIDATION`.
- `NOT_FOUND`, `FORBIDDEN`, `UNAUTHORIZED`, `NETWORK`, `UNEXPECTED` -> existing `errors:*` copy (screen-level for the
  read, row-level for a save). No new mapper code.

New keys, `workouts` namespace, `exercise.*` (es -> en):

- `nameEn` is not a key: the subtitle is the `en` translation itself.
- `video` -> reuse `workout.video`.
- `navigator` -> "Ejercicios" -> "Exercises" (accessible label)
- `set` -> "Serie {{number}}" -> "Set {{number}}"
- `target.reps` -> "{{count}} reps" -> "{{count}} reps"
- `target.range` -> "{{min}}–{{max}} reps" -> "{{min}}–{{max}} reps"
- `target.max` -> "máx. reps" -> "max reps"
- `target.seconds` -> "{{count}} s" -> "{{count}} s"
- `target.minutes` -> "{{count}} min" -> "{{count}} min"
- `reps` -> "Reps" -> "Reps"; `weight` -> "Peso (kg)" -> "Weight (kg)"
- `save` -> "Guardar" -> "Save"; `saved` -> "Guardada" -> "Saved"
- `repsRequired` -> "Indica las repeticiones." -> "Enter the reps."
- `weightInvalid` -> "Peso no válido." -> "Invalid weight."
- `rest` -> "Descanso: {{minutes}} min" -> "Rest: {{minutes}} min"; `noRest` -> "Sin descanso" -> "No rest"
- `workout.start` -> "Iniciar" -> "Start"; `workout.started` -> "Iniciado" -> "Started"
- `errors.CONFLICT` -> as above.

## Platform Capabilities And Dependencies

None. No native module; Expo Go is enough.

## Out Of Scope

- The rest countdown (the card is static), finishing the workout, deleting or reordering set records, super-series
  grouping, per-side handling, pull to refresh, offline queueing.
- Any backoffice UI for set records beyond keeping it compiling.
- Must not change: the API, `core` use cases and ports, the workout list, other tabs.

## Notes

- **No stop case.** Every rule (which kinds are allowed, which transitions, locked results) stays in the API. The
  read-only state and the start button are presentation choices that mirror the API's published rules
  (`STARTED` / `COMPLETED` accept records; `PUBLISHED` -> `STARTED` is the athlete's transition); the API still
  enforces them with `409` / `403`. Which inputs to show per target is presentation, derived from `target.type`.
- **Row ↔ record pairing (decided)** is by position (`setRecords[i]` for row `i`), matching the API's "next
  prescribed set" rule. Records beyond the prescribed total are not shown (out of scope).
- **English subtitle** uses the `en` translation when `i18n.language !== "en"` and the `en` name differs from the
  shown name; hidden otherwise.
- **Snapshot refresh** is its own commit. Expect the backoffice typecheck to break on nullable `weight` / `reps`; fix in
  place without behaviour change.
- **Verification tiers:**
  - core unit: set-record use case and repository tests updated for the new shape (timed set, no weight, empty body).
  - app component: `workout-exercise-view.test.tsx` (header with and without English subtitle and video; navigator
    items with progress icons and selection; rows per target kind with the right inputs; `POST` body per kind including
    empty body; `PUT` when a record exists; reps required for range / max; `CONFLICT` row error; reload after save;
    read-only rows on `PUBLISHED` / `REVIEWED`; rest card with and without `restMinutes`; back; read error with
    retry); `set-rows.test.ts` for expansion and labels; `workout-view.test.tsx` gains "tapping a row navigates to
    the exercise", "Iniciar on PUBLISHED starts and becomes Iniciado", "Iniciado disabled on STARTED", "no button on
    DRAFT", "start failure shows the error".
  - Translation parity for the new keys. No E2E (app E2E is a known gap). Manual check in Expo Go.
- No deviation from the mapped references is allowed.
