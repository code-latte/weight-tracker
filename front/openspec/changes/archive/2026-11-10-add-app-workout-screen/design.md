# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. Current state that shapes the approach:

- `GetWorkoutByIdUseCase`, `GetExercisesUseCase` and `GetMesocycleByIdUseCase` exist in core and are registered in
  `app/src/infrastructure/container.ts` (`getWorkoutByIdUseCase`); the app no longer uses the other two.
- **Revised:** `GET /workouts/{id}` now carries, per entry (`WorkoutByIdExercise`), the exercise's `translations`,
  `videoUrl`, `muscle`, `bodyPart` and `type`, and the workout's `mesocycle` (`WorkoutMesocycle`, `null` when
  standalone). First planned as three reads (workout, catalog, mesocycle); measured as slow, since `GET /exercises`
  scans every workout the athlete can see. The API change `enrich-get-workout-by-id-response` makes it one read.
- The workouts tab is a single route, `app/src/app/(app)/(tabs)/workouts.tsx`. The profile tab is already a stack
  (`profile/_layout.tsx`, `index.tsx`, `settings.tsx`).
- The `Icon` atom has the tones `primary`, `secondary`, `default`. The app theme exposes the success colour under
  `custom` (read with `useAppTheme`) and Paper's `onPrimary`.

## Goals / Non-Goals

**Goals:**

- Map the screen onto the existing app building blocks with no core change.
- Keep the workouts list's state (viewed week) while the workout screen is on top.

**Non-Goals:**

- A shared "exercise name" helper across fronts: backoffice and app stay independent; the app gets its own.

## Decisions

### Core and container

No change. No new domain alias, port, repository, use case or `Dependencies` key; no `container.ts` change in either
front. No new error code: failures surface the codes the HTTP error mapper already returns, shown through the
existing `ErrorState` with its `errors` copy in `es` and `en`.

### Routing: the workouts tab becomes a stack

- `app/src/app/(app)/(tabs)/workouts.tsx` moves to `workouts/index.tsx` (same two statements).
- New `workouts/_layout.tsx` (`WorkoutsLayout`), modelled on `profile/_layout.tsx`: a `Stack` with `index` and `[id]`,
  both `headerShown: false` (the list has its own card; the workout screen's jumbo panel is its header).
- New `workouts/[id].tsx`: `import WorkoutView from "@/presentation/workouts/views/workout-view"; export default
WorkoutView;`. It reads no params.
- `WorkoutsView` passes `onPress` to each `WorkoutCard` and calls `router.push({ pathname: "/workouts/[id]", params:
{ id } })` (typed, absolute href with params, as the invariants require).
- `WorkoutView` reads `id` with `useLocalSearchParams<{ id: string }>()` and goes back with `router.back()`.

Alternative considered: a root-level `(app)/workout/[id]` outside the tabs, to hide the tab bar. Rejected: the
request keeps the athlete in Entrenamientos, and a nested stack keeps the list mounted, so the viewed week survives
without lifting state.

### State hook `useWorkout(id)`

Modelled on `use-week-workouts.ts`. Holds a discriminated state:

- `{ status: "loading" }`
- `{ status: "error"; errorCode }` when the workout read fails
- `{ status: "ready"; workout }`

Reads: `getWorkoutByIdUseCase.execute({ id })` only; names, type, video and the mesocycle name come from the workout.
`retry` sets `loading` and reads again. It
uses an `active` flag on unmount, like the list hook. It never navigates.

Derived values, exposed from the hook:

- `rows`: per workout exercise, in API order: `id`, `name` (active language, else first translation, else `null` for
  the view to show the `workout.unknownExercise` copy), `type`, `videoUrl` (or `null`)
  and `progress`.
- `progress` via an exported pure `exerciseProgressOf(exercise)`: `notStarted` when `setRecords.length === 0`,
  `done` when `setRecords.length >= ` the sum of `Number(set.count)` over `sets` (each `SetResponse` is a prescription
  `count × target`, so `sets.length` is not the number of sets), otherwise `inProgress`. It is a display grouping of
  API counts, not a business rule.
- `done` and `total` for the panel; `workout.mesocycle?.name` and `workout.mesocycleWeekNumber` for the subtitle (shown only
  when both are present).

Held state: the workout, loading and error. Changed by `retry`. Reset when the screen
unmounts (leaving and reopening reads again); logout unmounts it.

### Components (app only; imports flow atoms -> molecules -> view; only atoms import Paper widgets)

Atoms, `app/src/presentation/common/components/atoms/`:

- `Icon` (extended): adds tones `success` (`useAppTheme().custom` success colour) and `onPrimary`
  (`theme.colors.onPrimary`). Existing tones unchanged.
- `TextLink` (new): Paper `Button` in `text` mode, compact, with `label`, `onPress` and optional
  `accessibilityLabel` by props. Used for "Ver vídeo".

Molecules, `app/src/presentation/workouts/components/molecules/`:

- `WorkoutHeroPanel`: props `name`, `subtitle?`, `progressLabel`, `progress` (0..1), `backLabel`, `onBack`. Primary
  background, takes the top safe-area inset itself; back `IconButton` (`arrow-left`) and texts in `onPrimary`;
  `ProgressBar` with `onPrimary` fill on a `primaryContainer` track (the theme exposes no translucent `onPrimary`).
  Name, subtitle and progress are optional so the panel can show only the back button while loading or on failure. Reference: `week-progress-card.tsx`.
- `ExerciseProgressIcon`: `done` -> `check-circle` / `success`; `inProgress` -> `play-circle` / `primary`;
  `notStarted` -> `circle-outline` / `default`; `label` by prop. Reference: `workout-status-icon.tsx`.
- `ExerciseTypeIcon`: `STRENGTH` -> `dumbbell`, `CARDIO` -> `heart`, `STRETCHING` -> `meditation`, default tone;
  renders nothing for any other value; `label` by prop. Mirrors the backoffice's meaning (`FitnessCenter`,
  `Favorite`, `SelfImprovement`), not its code.
- `WorkoutExerciseRow`: progress icon, type icon, name and optional `TextLink`, trailing `chevron-right` `Icon`
  labelled "Ver detalle". No `onPress` on the row in this change. Reference: `workout-card.tsx`.
- `WorkoutCard` (existing): gains an optional `onPress`, passed to `Card`.

View `app/src/presentation/workouts/views/workout-view.tsx`: `WorkoutHeroPanel` fixed on top, `FlatList` of
`WorkoutExerciseRow` below; `LoadingState` / `ErrorState` in the list area while the panel shows only the back
button (no name yet). Translates every label and opens videos with React Native's `Linking.openURL`. Reference:
`workouts-view.tsx`.

No organism: no component holds behaviour or state hooks beyond the view.

### Video link

`Linking.openURL(videoUrl)` from `react-native`, called in the view. It is a UI-level call already present in React
Native on both iOS and Android, not a platform capability that differs per front, so it does not need a port. No
package, no development build.

### Translations

New keys in the existing app `workouts` namespace (`app/src/i18n/{es,en}/workouts.json`):

| Key                                 | es                                | en                               |
| ----------------------------------- | --------------------------------- | -------------------------------- |
| `workout.subtitle`                  | `{{mesocycle}} · Semana {{week}}` | `{{mesocycle}} · Week {{week}}`  |
| `workout.progress`                  | `{{done}} / {{total}} ejercicios` | `{{done}} / {{total}} exercises` |
| `workout.video`                     | `Ver vídeo`                       | `Watch video`                    |
| `workout.details`                   | `Ver detalle`                     | `View details`                   |
| `workout.unknownExercise`           | `Ejercicio`                       | `Exercise`                       |
| `workout.exerciseStatus.done`       | `Completado`                      | `Done`                           |
| `workout.exerciseStatus.inProgress` | `En curso`                        | `In progress`                    |
| `workout.exerciseStatus.notStarted` | `Sin empezar`                     | `Not started`                    |
| `workout.exerciseType.strength`     | `Fuerza`                          | `Strength`                       |
| `workout.exerciseType.cardio`       | `Cardio`                          | `Cardio`                         |
| `workout.exerciseType.stretching`   | `Estiramiento`                    | `Stretching`                     |

The back button reuses `common:back`. Errors reuse the existing `errors` namespace.

### Tests

- New `app/test/presentation/workouts/views/workout-view.test.tsx`, structured like `workouts-view.test.tsx`
  (`renderWithProviders`, container fakes returning chosen `Result`s, `expo-font` mock) and mocking `expo-router`
  (`useLocalSearchParams`, `router.back`) like `profile-view.test.tsx`; `Linking.openURL` spied. Renders in `es`
  except the English subtitle case. Arrange / Act / Assert phases with one Act.
- `workouts-view.test.tsx` gains the "tapping a card pushes `/workouts/[id]` with its id" case, with `router.push`
  mocked.
- Existing test helpers and patterns are unchanged.

## Risks / Trade-offs

- [`meditation` may not read as "stretching" to every athlete] → Mirrors the backoffice; swapping the icon name is a
  one-line change in `ExerciseTypeIcon`.
- [Moving `workouts.tsx` into a folder changes the route file path] → The URL `/workouts` is unchanged; the tab layout
  refers to the `workouts` segment, which now resolves to the folder.
