# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-app-workout-screen.md`.

## Why

The athlete can see their week's workouts in the app but cannot open any of them. A workout screen is the step
between the list and recording sets: what the workout contains, how far along it is, and which exercises are done,
under way or not started. Core already reads everything it needs, so this is an app-only change.

## What Changes

- Tapping a workout card in the Entrenamientos list opens a new workout screen at `/workouts/[id]`. The workouts tab
  becomes a stack (list, then workout); the tab bar stays visible.
- The workout screen has a fixed jumbo panel on a primary background: back button and workout name; the mesocycle
  name and week as subtitle (nothing for a standalone workout); `done / total ejercicios`; a progress bar.
- Below the panel, a scrollable list of the workout's exercises in API order. Each row shows a progress icon (done,
  in progress, not started), a type icon (strength, cardio, stretching), the exercise name, a "Ver vídeo" link when
  the exercise has a video, and a right caret that does nothing yet.
- Exercise progress is derived from counts the API returns: done when its set records reach its prescribed sets, in
  progress with some records, not started with none.
- Loading state while reading; error state with retry and back on failure. A failed mesocycle read only hides the
  subtitle.
- The screen reads only `GET /workouts/{id}`: the API (change `enrich-get-workout-by-id-response` in `api/`) now
  embeds each entry's exercise data (names, video, muscle, body part, type) and the workout's mesocycle. The contract
  snapshot is refreshed, core gains the `WorkoutByIdExercise` and `WorkoutMesocycle` aliases, and the backoffice
  workout screen takes its mesocycle from the workout instead of reading it (it keeps the catalog for its add-exercise
  dialog).
- No new error code, package or development build.

## Capabilities

### New Capabilities

- `app-workout-detail`: the athlete's workout screen in the app: the jumbo panel, the exercise list with progress and
  type, the video link, and loading / failure.

### Modified Capabilities

- `app-workouts-list`: "Each workout of the week is a card" changes: tapping a card opens that workout's screen
  instead of doing nothing, and coming back keeps the viewed week.

## Fronts, Roles And References

- **Fronts:** app only, role `USER`. The backoffice is unchanged. No role difference: only `USER` signs in to the app
  and the screen shows what the API returns to them.
- **Canonical references** (all in the app; the backoffice is not a UI reference; none is `pending`):
  - stack layout -> `app/src/app/(app)/(tabs)/profile/_layout.tsx`;
  - routes -> `app/src/app/(app)/(tabs)/profile/settings.tsx`;
  - view -> `app/src/presentation/workouts/views/workouts-view.tsx`;
  - state hook -> `app/src/presentation/workouts/hooks/use-week-workouts.ts`;
  - atoms (`Icon` new tones, new `TextLink`) -> `app/src/presentation/common/components/atoms/progress-bar.tsx`;
  - molecules: `WorkoutHeroPanel` -> `week-progress-card.tsx`; `ExerciseProgressIcon`, `ExerciseTypeIcon` ->
    `workout-status-icon.tsx`; `WorkoutExerciseRow` -> `workout-card.tsx` (all in
    `app/src/presentation/workouts/components/molecules/`);
  - view test -> `app/test/presentation/workouts/views/workouts-view.test.tsx`.
- **Components:** atoms `Icon` (extended), `TextLink` (new); molecules `WorkoutHeroPanel`, `ExerciseProgressIcon`,
  `ExerciseTypeIcon`, `WorkoutExerciseRow` (new), `WorkoutCard` (gains `onPress`). Reused: atoms `Text`,
  `ProgressBar`, `IconButton`, `SafeArea`, `Card`; molecules `LoadingState`, `ErrorState`. No organism.
- **API endpoints**: `GET /workouts/{id}` (`WorkoutById`, with `WorkoutByIdExercise` entries and an embedded
  `WorkoutMesocycle`), in `core/openapi/v1.json` after a snapshot refresh from the API change
  `enrich-get-workout-by-id-response`. The app no longer reads `GET /exercises` or `GET /mesocycles/{id}`; the
  backoffice workout screen no longer reads `GET /mesocycles/{id}`.
- **Business rules:** none. The exercise progress is a display grouping of counts the API returns, like the list's
  status grouping; nothing is validated or sent.
- **Conflicts with the template or invariants:** none. The route stays two statements; the view reads `id` with
  `useLocalSearchParams`, as the invariants require.

## Impact

- App: `src/app/(app)/(tabs)/workouts.tsx` moves to `workouts/index.tsx`; new `workouts/_layout.tsx` and
  `workouts/[id].tsx`; new view, state hook and molecules in `presentation/workouts`; `Icon` atom gains tones; new
  `TextLink` atom; `WorkoutCard` and `WorkoutsView` gain the tap; new keys in `i18n/{es,en}/workouts.json`.
- Tests: new `app/test/presentation/workouts/views/workout-view.test.tsx`; `workouts-view.test.tsx` gains the
  navigation case; translation parity covers the new keys.
- Not affected: core, the API, the backoffice, other tabs.
