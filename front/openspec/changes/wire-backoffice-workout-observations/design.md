# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour.

- The metrics tab is driven by `WorkoutReviewView`. The view holds `useWorkoutSummary(workout.id)` and calls
  `metrics.load()` when the user first selects the tab. It then passes the summary state to `WorkoutReviewMetrics`,
  which renders `SessionObservationsCard` with no props.
- `SessionObservationsCard` maps over `SESSION_OBSERVATIONS_PREVIEW` from `session-observations.mock.ts`.
  `ObservationItem` takes the mock's `ObservationSeverity` (`positive | info | warning`).
- `GetWorkoutSummaryUseCase` / `WorkoutRepository.getSummary` is the latest read-only computed workout endpoint. Its
  key is declared in `core/src/application/dependencies.ts` and registered in both `container.ts` files.
- `GET /workouts/{workoutId}/observations` is on the current API branch. It is not yet in `core/openapi/v1.json`.

## Goals / Non-Goals

**Goals:**

- One use case and one hook feed both the card and the dialog.
- The card keeps its place, size and look in the tab. Only its content source changes.

**Non-Goals:**

- No change to the summary's loading or to the tab-level skeleton and error.
- No new atom, no new dependency, no new test pattern.

## Decisions

### D1. Core: workouts area, mirroring the summary

- `core/src/domain/workouts/workout.ts` gains these aliases:
  - `WorkoutObservations = components["schemas"]["WorkoutObservationsResponse"]`;
  - `WorkoutObservation = components["schemas"]["WorkoutObservationResponse"]`;
  - `WorkoutHighlight = components["schemas"]["WorkoutHighlightResponse"]`;
  - `GetWorkoutObservationsInput = { workoutId: string }`.

  They are exported through the domain barrel, as `WorkoutSummary` is.

- `WorkoutRepositoryInterface.getObservations(workoutId): Promise<Result<WorkoutObservations, DomainError>>`.
- `WorkoutRepository.getObservations` sends `GET /workouts/${workoutId}/observations` and maps failures with
  `mapHttpError`, the same as `getSummary`.
- `GetWorkoutObservationsUseCase` lives in `core/src/application/use-cases/workouts/` and is exported from the use-case
  barrel.
- `Dependencies` gains
  `getWorkoutObservationsUseCase: UseCaseInterface<GetWorkoutObservationsInput, WorkoutObservations>`.
- Registration: `getWorkoutObservationsUseCase: (c) => new GetWorkoutObservationsUseCase(c.resolve("workoutRepository"))`,
  beside `getWorkoutSummaryUseCase`, in **both** `backoffice/src/infrastructure/container.ts` and
  `app/src/infrastructure/container.ts`. Both build `Container<Dependencies>`, so leaving one out fails typecheck.
  The app never resolves the key.

_Alternative:_ a backoffice-only key outside `Dependencies`. Rejected, because the containers share one type and the
summary set the precedent.

### D2. State hook: `useWorkoutObservations`, held by the view

- `backoffice/src/presentation/workouts/hooks/use-workout-observations.ts` copies `use-workout-summary.ts`.
  - It returns `{ status, observations, errorCode, load, reload }`.
  - `load` reads once, `reload` reads again.
  - It runs through `runWithSession`, and a generation guard drops answers that arrive after unmount or after a newer
    read.
- `WorkoutReviewView` holds it beside `metrics` and calls its `load()` in the same tab-change branch as
  `metrics.load()`. It passes the state to `WorkoutReviewMetrics` as an `observations` prop, which forwards it to the
  card.
- It resets on screen exit.

_Alternative:_ load inside the card. Rejected, because the card is an organism, and the screen's hooks are held by the
view, like the summary and the history.

### D3. Card state, independent from the summary

- The tab still shows its skeleton and `LoadError` from the summary only.
- Once the summary is loaded, the card renders by the observations status alone:
  - `idle` / `loading` -> a rounded `Skeleton` inside the card;
  - `failed` -> `LoadError` with `reload` inside the card;
  - `loaded` -> the highlights or an empty state.
- The card owns `open: boolean` for the dialog. The dialog is mounted only while open, as `ExerciseVideoDialog` is,
  so it holds no state between openings.

### D4. Helpers in `presentation/workouts/workout-observations.ts`

These are pure functions with no React and no translation calls. They take values and return values.

- `highlightedObservationsOf(observations)`: the highlights sorted by `rank`, each joined to its observation by id,
  with dangling ones dropped.
- `highlightedIdsOf(observations)`: a `Set` of highlighted observation ids, for the dialog mark.
- `observationGroupsOf(observations)`: groups in first-appearance order. The key is `exerciseId` (`null` for the
  workout). Within a group the API's order is kept, and the workout group is moved to the front.
- `exerciseOf(workout, exerciseId)`: the first `workout.exercises` entry with that `exerciseId`, or `null`. The name
  is resolved by the caller with `useExerciseLabels().nameOf`, as `WorkoutReviewMetrics` does.
- `observationCopyOf(observation, format)`: returns the `{ titleKey, textKey | null, values, baselineKey | null }` for
  the observation.
  - `format` carries locale formatters for kg, signed percent, plain percent and integer, built by the caller.
  - Each value is narrowed structurally: a `number` for scalars, a `number[]` for lists. A required value that is
    missing makes `textKey` `null`.
  - For `REPS_BELOW_TARGET` / `REPS_ABOVE_TARGET`, the `setPositions`, `reps` and `targetMins`/`targetMaxes` lists
    are zipped into detail entries. The organism joins them with the `setDetail` key and " · ".
  - An unknown code returns `titleKey: "unknown.title"` and no text.
- Severity is mapped in `ObservationItem` (D5), not here.

This sits beside `workout-review.ts` with the same style. It gets its own file because it is a separate concern of
the same screen.

_Alternative:_ put the per-code copy logic in the component. Rejected, because the helper keeps it unit-testable
without rendering.

### D5. Components and atomic levels

All new and changed components are in `backoffice/src/presentation/workouts/components/`. Imports flow downward, and
only atoms import MUI widgets. `@mui/icons-material` and `Box` / `alpha` are already used by organisms and molecules
here.

- `molecules/observation-item.tsx` (changed):
  - `severity` becomes `string` (the generated type);
  - the internal map covers `SUCCESS -> success`, `INFO -> info` and `WARNING -> warning`, with any other value
    falling back to `info`;
  - it takes an optional `text` and an optional `mark?: ReactNode` rendered after the title, used for the
    "Destacada" `Chip`;
  - it keeps importing only atoms and icons.
- `organisms/session-observations-card.tsx` (changed):
  - it takes `workout` and the `observations` state;
  - the "Vista previa" chip is removed;
  - it renders the states of D3;
  - the highlight items are `ObservationItem`s;
  - the button is the `button` atom with `variant="text"`, at the bottom;
  - it opens the dialog.
- `organisms/workout-observations-dialog.tsx` (new): it copies the structure of `exercise-video-dialog.tsx`.
  - It uses the `Dialog`, `DialogTitle` (title plus a close `IconButton`) and `DialogContent` atoms, with
    `fullWidth maxWidth="sm"` and `aria-labelledby`.
  - Each group gets a `Typography` subtitle2 heading, followed by its `ObservationItem`s.
  - Escape, a click outside and the close button all call `onClose`.
- `organisms/workout-review-metrics.tsx` (changed): it takes the `observations` prop and passes `workout` and
  `observations` to the card.
- `presentation/workouts/session-observations.mock.ts`: deleted.

### D6. Formatting

- Formatting uses `Intl.NumberFormat(i18n.language)`, the same as D7 of the metrics change:
  - kg with no forced decimals and a " kg" suffix;
  - `changePercent` with exactly one decimal and a sign ("+8,3 %");
  - `completionPercent` as a plain percent with no decimals;
  - counts as integers.
- The formatters are built once per render in the card and dialog, and passed into `observationCopyOf`.

### D7. Copy

- New and changed keys go in `backoffice/src/i18n/{es,en}/workouts.json` under `review.metrics.observations`, as
  listed in the brief:
  - `viewAll`, `highlighted`, `none`, `noHighlights`, `workoutGroup`, `unknownExercise`, `unknown.title`, `setDetail`;
  - `codes.<CODE>.title` / `.text` for the 17 codes;
  - `baseline.weight|volume|reps`.
- Removed keys: `preview`, `LOAD_PROGRESSION.*`, `GOOD_COMPLIANCE.*`, `SET_BELOW_TARGET.*`.
- Kept keys: `title`, `subtitle`.
- The close button reuses `workout.catalog.close` ("Cerrar" / "Close"), as `ExerciseVideoDialog` does.
- The `en` texts mirror the brief's `es` templates. They are written in full in the JSON.

## Errors

There are no new error codes and no mapper change. Any `DomainError` from the observations read (`NOT_FOUND`,
`FORBIDDEN`, `UNAUTHORIZED`, `NETWORK`, `UNEXPECTED`) shows `LoadError` in the card with `reload`. The session refresh
is handled by `runWithSession`. `LoadError` already translates every code in `es` and `en`.

## Tests

- **Core:**
  - `get-workout-observations-use-case.test.ts` mirrors `get-workout-summary-use-case.test.ts`;
  - `workout-repository.test.ts` gains a `getObservations` block for `200` (with nulls and empty lists), `401`, `403`,
    `404`, network and the request path.
- **Backoffice:**
  - `test/presentation/workouts/workout-observations.test.ts` covers the D4 helpers, as listed in the brief's Notes;
  - `workout-view.test.tsx` covers the following, with `getWorkoutObservationsUseCase` faked through the container
    like `getWorkoutSummaryUseCase`:
    - the "shows the three sample observations marked as a preview" case is replaced by the spec's card and dialog
      scenarios;
    - one request on first opening and none on coming back;
    - a load error with retry;
    - every existing metrics test also registers the new fake, returning empty lists by default.
  - Translation parity covers the keys.
- No E2E. No new test pattern.

## Risks / Trade-offs

- [The generated `data` type may be `Record<string, unknown>` or similar, not a typed map] → D4 narrows each value at
  runtime and drops the text when a required value is missing. No hand-written domain type is added.
- [The API adds a new code before the front has copy for it] → it shows the generic title with its severity colour,
  and the item stays useful.
- [The same catalog exercise appears twice in a workout] → the name is taken from the first match. The name is the
  same either way.
- [The snapshot must come from the API branch with `add-workout-observations` running] → the refresh is the first
  task, and the diff is reviewed before generating.

## Migration Plan

This is a front-only release after the API change is deployed. Rollback is a revert of this change. The API endpoint
is additive, so an older front ignores it.
