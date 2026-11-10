# Change Proposal: Add App Workout Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-app-workout-screen/`.

## Change

Open a workout from the Entrenamientos list into its own screen: a primary-coloured jumbo panel at the top (back,
name, mesocycle and week, exercises done / total, progress bar) and, below it, the list of the workout's exercises,
each with its progress icon, its type icon, its name, a link to its video and a caret towards its (future) detail.

## Objective

The athlete can see their week's workouts but cannot open any of them. This screen is the step between the list and
recording sets: what the workout contains, how far along it is, and which exercises are done, under way or pending.
Core already reads everything needed (`GetWorkoutByIdUseCase`, `GetExercisesUseCase`, `GetMesocycleByIdUseCase`).

## Capabilities

1. **Open a workout:** tapping a workout card in the Entrenamientos list opens its workout screen.
2. **Jumbo panel** (top, fixed, does not scroll), primary background, text and icons in the on-primary colour,
   extending under the status bar:
   - **Row 1:** a back button (left arrow) that returns to the workouts list, and the workout name.
   - **Row 2, subtitle:** `<mesocycle name> · Semana <n>` when the workout belongs to a mesocycle (`mesocycleId` and
     `mesocycleWeekNumber` present). A standalone workout shows no subtitle and no empty space for it.
   - **Row 3:** the tracker `nDone / nTotal ejercicios` (e.g. `3 / 6 ejercicios`).
   - **Row 4:** a progress bar filled at `nDone / nTotal`; empty when the workout has no exercises.
3. **Exercise list** (scrollable, below the panel), one row per workout exercise, in the API order:
   - **Left, progress icon:**
     - done -> filled circle with tick, success (green) colour;
     - in progress -> circle with play, primary colour;
     - not started -> empty outlined circle, default colour.
   - **Then, type icon**, as the backoffice does: `STRENGTH` -> dumbbell, `CARDIO` -> heart, `STRETCHING` -> the
     meditation pose. No type icon when the exercise's type is unknown or the exercise is missing from the catalog.
   - **Middle, the widest column:**
     - **Title:** the exercise name in the active language (else the first translation).
     - **Below:** a "Ver vídeo" link when the exercise has a `videoUrl`, opening it outside the app; nothing otherwise.
   - **Right:** a right caret (`chevron-right`). It is shown but does nothing in this change (see Out Of Scope).
4. **Exercise progress** (per workout exercise):
   - done -> it has set records and their count equals the number of prescribed sets (`setRecords.length` against
     the sum of `sets[].count`: each `sets` entry is a prescription `count × target`);
   - in progress -> it has at least one set record, but not as many as sets;
   - not started -> it has no set records.
5. **Loading and failure:** a loading state while the screen reads its data; on failure, the existing error state with
   a retry, plus the back button so the athlete is never stuck.

## Fronts And Roles

- backoffice -> lands here: no -> unchanged.
- app -> lands here: yes -> roles: `USER` -> the athlete opens one of their workouts and sees its exercises and
  progress.

No role difference: only `USER` signs in to the app. The athlete sees what the API returns to them.

## Classification

Core: none. `GetWorkoutByIdUseCase`, `GetExercisesUseCase`, `GetMesocycleByIdUseCase` and the `WorkoutById`,
`WorkoutExerciseDetail`, `Exercise`, `Mesocycle` aliases exist and are already registered in
`app/src/infrastructure/container.ts`.

App:

- Workouts stack layout (list without header, workout screen without header: the jumbo panel is its header) -> route
  -> new `app/src/app/(app)/(tabs)/workouts/_layout.tsx`
- Workouts list route -> route -> `app/src/app/(app)/(tabs)/workouts.tsx` moves to
  `app/src/app/(app)/(tabs)/workouts/index.tsx` (unchanged content)
- Workout route -> route -> new `app/src/app/(app)/(tabs)/workouts/[id].tsx` (default-exports the view)
- `WorkoutView` (jumbo panel, exercise list; loading / error) -> view ->
  `app/src/presentation/workouts/views/workout-view.tsx`
- `useWorkout` (reads the workout by id, the exercise catalog and, when `mesocycleId` is set, the mesocycle; exposes
  the workout name, the subtitle parts, the rows with name, type, `videoUrl` and progress, `done`, `total`, `retry`,
  loading and error) -> state hook -> `app/src/presentation/workouts/hooks/use-workout.ts`
- `Icon` atom -> atom -> `app/src/presentation/common/components/atoms/icon.tsx`, gains a `success` tone (the theme's
  success colour) and an `onPrimary` tone for the panel
- `TextLink` (Paper text button / pressable text with link styling, label and `onPress` by props) -> atom ->
  `app/src/presentation/common/components/atoms/text-link.tsx`
- `WorkoutHeroPanel` (back `IconButton`, name, optional subtitle, tracker, `ProgressBar`, primary background; `onBack`
  by props) -> molecule -> `app/src/presentation/workouts/components/molecules/workout-hero-panel.tsx`
- `ExerciseProgressIcon` (progress -> icon and tone) -> molecule ->
  `app/src/presentation/workouts/components/molecules/exercise-progress-icon.tsx`
- `ExerciseTypeIcon` (type -> icon) -> molecule ->
  `app/src/presentation/workouts/components/molecules/exercise-type-icon.tsx`
- `WorkoutExerciseRow` (progress icon, type icon, name, video link, caret) -> molecule ->
  `app/src/presentation/workouts/components/molecules/workout-exercise-row.tsx`
- `WorkoutCard` -> molecule -> existing, gains an `onPress`; `WorkoutsView` navigates to `/workouts/<id>`
- `workout.*` keys -> i18n -> the existing `workouts` namespace in `app/src/i18n/{es,en}/workouts.json`

Existing atoms reused: `Card`, `Text`, `ProgressBar`, `IconButton`, `SafeArea`, `ListItem`; molecules `LoadingState`,
`ErrorState`.

## Canonical Template Mapping

- Stack layout -> `app/src/app/(app)/(tabs)/profile/_layout.tsx` -> the app's only nested stack inside a tab.
- Routes -> `app/src/app/(app)/(tabs)/profile/settings.tsx` -> two-statement route under a tab stack.
- `WorkoutView` -> `app/src/presentation/workouts/views/workouts-view.tsx` -> same area: fixed card over a scrollable
  list, loading / error states.
- `useWorkout` -> `app/src/presentation/workouts/hooks/use-week-workouts.ts` -> same area: use cases through the
  container, `Result` mapped to state, derived progress.
- `Icon` tones, `TextLink` -> `app/src/presentation/common/components/atoms/progress-bar.tsx` -> thin Paper wrappers,
  props only.
- `WorkoutHeroPanel` -> `app/src/presentation/workouts/components/molecules/week-progress-card.tsx` -> title,
  subtitle, `n / total` and bar.
- `ExerciseProgressIcon`, `ExerciseTypeIcon` ->
  `app/src/presentation/workouts/components/molecules/workout-status-icon.tsx` -> value -> icon and tone.
- `WorkoutExerciseRow` -> `app/src/presentation/workouts/components/molecules/workout-card.tsx` -> icon, text column,
  trailing element.
- Tests -> new `app/test/presentation/workouts/views/workout-view.test.tsx` modelled on
  `app/test/presentation/workouts/views/workouts-view.test.tsx`.

The backoffice is not a UI reference; only its type-to-icon mapping (`use-exercise-labels.ts`) is mirrored, as a
meaning, not as code. `.stpr/template/` and its README are not updated.

## Screens And Navigation

- app -> `/workouts/[id]` -> `WorkoutView` -> tapping a workout card in `/workouts` -> back button (and the platform
  back gesture) returns to `/workouts`, keeping the viewed week; on failure, the error state with retry and back.

The tab bar stays visible and the Entrenamientos tab stays selected. Tapping an exercise row or its caret does
nothing in this change.

## API Contract

- `GET /workouts/{id}` -> in `core/openapi/v1.json`: yes -> `WorkoutByIdResponse` (`WorkoutById`); uses `name`,
  `mesocycleId`, `mesocycleWeekNumber`, `exercises[].exerciseId`, `exercises[].sets`, `exercises[].setRecords`.
- `GET /exercises` -> yes -> `ExerciseResponse[]` (`Exercise`); uses `id`, `translations`, `type`, `videoUrl`. The
  workout exercise only carries `exerciseId`; name, type and video come from the catalog, which includes the
  exercises used in workouts the caller can see.
- `GET /mesocycles/{id}` -> yes -> `MesocycleResponse` (`Mesocycle`); uses `name`. The athlete of the mesocycle may
  read it.

No snapshot refresh.

## Held State

- `useWorkout` -> the workout, the exercise catalog, the mesocycle (when any), loading and error -> changed by retry
  -> resets when the screen unmounts (leaving it and opening it again re-reads); logout unmounts it.

The workouts list keeps its own state while the workout screen is on top of it (same stack).

## Forms And Structural Validation

None.

## Errors And Copy

- Reading the workout or the catalog fails (network, server, not found, forbidden) -> the codes the mapper already
  returns -> error state with retry and back, using the existing `errors` copy.
- Reading the mesocycle fails -> no error state: the screen shows without the subtitle (see Notes).

No new error code.

New keys in the `workouts` namespace (Spanish from the request; English proposed):

- `workout.subtitle` -> "{{mesocycle}} · Semana {{week}}" -> "{{mesocycle}} · Week {{week}}"
- `workout.progress` -> "{{done}} / {{total}} ejercicios" -> "{{done}} / {{total}} exercises"
- `workout.video` -> "Ver vídeo" -> "Watch video"
- `workout.details` -> "Ver detalle" -> "View details" (accessible label of the caret)
- `workout.exerciseStatus.done` / `.inProgress` / `.notStarted` -> "Completado" / "En curso" / "Sin empezar" ->
  "Done" / "In progress" / "Not started" (accessible labels of the progress icon)
- `workout.exerciseType.strength` / `.cardio` / `.stretching` -> "Fuerza" / "Cardio" / "Estiramiento" -> "Strength" /
  "Cardio" / "Stretching" (accessible labels of the type icon)

The back button reuses `common:back`.

## Platform Capabilities And Dependencies

None. Icons are Material Community Icons already shipped with Paper (`arrow-left`, `check-circle`, `play-circle`,
`circle-outline`, `dumbbell`, `heart`, `meditation`, `chevron-right`). The video opens with React Native's `Linking`;
no new package and no development build.

## Out Of Scope

- The exercise detail screen and recording, editing or removing set records. The caret is a placeholder for it.
- Everything else in `workout_details.png` not listed above: the `⋮` menu, total / remaining time, the
  Ejercicios / Detalles tabs, exercise thumbnails, the exercise number, the `4 × 8–10` prescription line, the
  highlighted row and the "Finalizar entrenamiento" button.
- Starting, finishing or changing the status of the workout.
- Grouping super-series; exercises are listed flat in the API order.
- Pull to refresh and refetch on focus.
- Must not change: core, the API, the backoffice, the list screen's behaviour other than opening a workout, other
  tabs.

## Notes

- **Progress is a display grouping (decided).** Done / in progress / not started is derived from counts the API
  returns, like the list's status grouping; it is not a business rule and is not sent anywhere.
- **More records than sets (decided).** The request says "match"; the brief uses `setRecords.length >=` the prescribed set total
  for done, so an extra record never turns a done exercise back into in progress. An exercise with zero sets and zero
  records is not started.
- **Mesocycle failure (decided).** The subtitle is secondary information: if `GET /mesocycles/{id}` fails, the screen
  still shows, without the subtitle. Confirm before `/opsx:propose` if it should block the screen instead.
- **Stretching icon (decided).** `meditation` mirrors the backoffice's `SelfImprovement`; `yoga` is the alternative.
- **Type icon colour (decided).** Default tone, not the type colour. Not in the request.
- **Panel colours.** Background `primary`; texts, back arrow and bar fill `onPrimary`, bar track a translucent
  `onPrimary`. The panel takes the top safe-area inset itself, so its colour runs under the status bar.
- **Data loading.** The three reads are independent of the list screen: the workout screen reads by `id` from the
  route, so it also works on a deep link. The catalog and the mesocycle are read in parallel once the workout arrives
  (the mesocycle only when `mesocycleId` is set).
- **Specs.** New `app-workout-detail` capability (panel, exercise list, exercise progress); the `app-workouts-list`
  capability gains "tapping a card opens the workout".
- **Verification tiers:**
  - core unit: none (no core change).
  - app component: `workout-view.test.tsx`, with the three use cases faked through the container:
    - mesocycle workout: name, `"<mesocycle> · Semana 3"`, `3 / 6 ejercicios`, bar at 50%;
    - standalone workout: no subtitle, and `getMesocycleByIdUseCase` is not called;
    - progress icons by accessible label: records = sets -> done, some records -> in progress, none -> not started;
    - type icons per type, and none for an exercise missing from the catalog;
    - "Ver vídeo" only for exercises with `videoUrl`, and pressing it opens the URL (`Linking` mocked);
    - names in `es`, falling back to the first translation;
    - back button returns to the list;
    - workout read fails: error state, back still shown, retry calls the use case again;
    - mesocycle read fails: screen shown without subtitle.
  - app component: `workouts-view.test.tsx` gains "tapping a card navigates to `/workouts/<id>`".
  - Translation parity covers the new keys.
  - No backoffice tests, no E2E (app E2E is a known gap).
  - Manual check in Expo Go: panel fixed while the list scrolls, colour under the status bar, back keeps the viewed
    week, light and dark themes.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.
- No deviations from the mapped references are allowed.
