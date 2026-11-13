# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. Current state that shapes the approach:

- **Contract.** `core/openapi/v1.json` does not have the two routes yet. The API adds them in
  `add-workout-summary-and-exercise-history`, which is uncommitted in `api/`, so the snapshot refresh needs that API
  running.
  - The summary reuses `LastPerformanceSetResponse` (`reps`, `durationSeconds`, `weight`) for both top sets.
  - The history answers newest first, with `limit` in `1..50`.
- **Container.** `Dependencies` (`core/src/application/dependencies.ts`) is one interface, and both fronts build
  `Container<Dependencies>`, so every key is registered in both. This is the precedent of
  `show-exercise-last-performance`, D2.
- **Review helpers.** The review (`workout-review-view.tsx`) already computes, from the workout:
  - the counted records, completed exercises and the elapsed time (`reviewSummaryOf`, `countedRecordsOf`,
    `exerciseStateOf`, `elapsedMsOf`, `elapsedPartsOf` in `workout-review.ts`, and `useNow`);
  - volume formatting, with `Intl.NumberFormat` and the `review.volumeValue` key.
- **Tabs.** `WorkoutReviewTabs` renders its `children` only for `"exercises"`. The other two tabs render nothing.
- **Load errors.** `common/components/molecules/load-error.tsx` is the existing error-with-retry block.
- **Atoms.** The backoffice has no select atom. Existing selects are `text-field` with `select` and `menu-item`
  children (e.g. `exercise-configuration-dialog.tsx`). There is no chart library.

## Goals / Non-Goals

**Goals:**

- Two core reads in the exact shape of their references, with no logic beyond the HTTP call.
- A tab that reuses the review's own rules and formatters instead of adding new ones.
- Charts behind two thin atoms, so the rest of the presentation never sees the chart library.

**Non-Goals:**

- No caching across screens, no provider and no polling.
- No change to the "Ejercicios" tab, the header cards or the person strip.
- No real observations, and no `until` on the history.

## Decisions

### D1. Core: workouts area for the summary, users area for the history

- **Aliases.**
  - In `core/src/domain/workouts`: `WorkoutSummary`, `ExerciseSummary`, `SetsInTarget` and `PreviousExecution`, from
    the generated `components["schemas"][...]`.
  - In `core/src/domain/users`: `ExerciseHistorySession`.
  - Inputs, as interfaces next to them: `GetWorkoutSummaryInput { workoutId }` and
    `GetUserExerciseHistoryInput { userId, exerciseId, limit }`.
  - All of them are exported from the domain index.
- **Ports.**
  - `WorkoutRepositoryInterface.getSummary(workoutId): Promise<Result<WorkoutSummary, DomainError>>`.
  - `UserRepositoryInterface.getExerciseHistory(userId, exerciseId, limit): Promise<Result<ExerciseHistorySession[], DomainError>>`.
- **Implementations.**
  - Shaped like `getById` and `getMyExerciseLastPerformance`: one `request` and `ok(response)`, with
    `fail(mapHttpError(error))` in the `catch`.
  - The history path is `/users/${userId}/exercises/${exerciseId}/history?limit=${limit}`. If `HttpClientInterface`
    takes a `query` option, use it instead of building the string. Check its request type and follow existing usage.
- **Use cases.** `GetWorkoutSummaryUseCase` (in `use-cases/workouts/`) and `GetUserExerciseHistoryUseCase` (in
  `use-cases/users/`) delegate to the repository. They are exported from each area's index.
- **`Dependencies` keys.**
  - `getWorkoutSummaryUseCase: UseCaseInterface<GetWorkoutSummaryInput, WorkoutSummary>`;
  - `getUserExerciseHistoryUseCase: UseCaseInterface<GetUserExerciseHistoryInput, ExerciseHistorySession[]>`.
- **Container registration.** Both fronts' `container.ts` register both keys, resolving `workoutRepository` and
  `userRepository`. In the app this is wiring only.

_Why these areas:_ repositories follow the API's route groups (`/workouts/...` and `/users/...`), as decided in
`show-exercise-last-performance` D1.

### D2. Tab panels: the tabs organism takes the metrics content as a prop

`WorkoutReviewTabs` gains a `metrics: ReactNode` prop, rendered when `value === "metrics"`. `children` stays the
"Ejercicios" content, and "Feedback" stays empty. `WorkoutReviewView` passes
`<WorkoutReviewMetrics workout={workout} elapsed={elapsed} />`.

The metrics organism mounts only while its tab is selected. Its state must survive leaving and coming back (spec: no
second summary read), so the state hooks live in the view, not in the organism (D3).

_Alternative:_ a render-prop per tab. Rejected because one prop is enough for one more panel.

### D3. State hooks, held by the view

- **`useWorkoutSummary(workoutId, enabled)`** (`presentation/workouts/hooks/use-workout-summary.ts`):
  - modelled on `useWorkout`: `status` (`idle` | `loading` | `loaded` | `failed`), `summary`, `errorCode`, `reload`,
    and the generation guard;
  - it reads once, the first time `enabled` becomes true, and keeps the result while the view lives;
  - the view passes `enabled = tab === "metrics"` and hands the hook's state to `WorkoutReviewMetrics`.
- **`useExerciseHistory(userId, exerciseId)`** (`hooks/use-exercise-history.ts`):
  - same shape;
  - it reads with `limit: 5` whenever `exerciseId` changes and is not `null`, and stays `idle` with `null`;
  - the answer is reversed to oldest first in the hook's consumer, through a helper (D5), never in core.
- **The selected chart exercise** is held by the view next to the tab, so it also survives a tab switch.
  - It starts as `null`. Once the summary loads, it becomes the first weighted exercise's `exerciseId` (the catalog
    id, which the history route needs).
  - The view passes it down, with an `onSelect` callback.

| State                   | Holder               | Changed by                 | Reset       |
| ----------------------- | -------------------- | -------------------------- | ----------- |
| summary, status, error  | `useWorkoutSummary`  | first metrics tab, retry   | screen exit |
| history, status, error  | `useExerciseHistory` | selected exercise, retry   | screen exit |
| selected chart exercise | `WorkoutReviewView`  | summary load, the selector | screen exit |

_Alternative:_ hooks inside `WorkoutReviewMetrics`. Rejected because the panel unmounts on a tab change, and the
summary would be read again.

### D4. Charts: two atoms over `@mui/x-charts`

- Install it with `pnpm --filter backoffice add @mui/x-charts`. Version 9 accepts `@mui/material` 9 and React 19. The
  user asked for `npm install`; the workspace uses pnpm.
- **`LineChart` atom** (`common/components/atoms/line-chart.tsx`):
  - props: `labels: string[]`, `values: number[]`, `valueFormatter`, `height`, `ariaLabel`;
  - it wraps the library's `LineChart`, with an area, the theme's primary colour and the last point's value marked.
- **`BarChart` atom** (`bar-chart.tsx`): the same props, wrapping `BarChart`. The last bar is in the full primary
  colour and the others in a softer primary.
- The atoms hold no copy. Labels and formatted values come by props, as in every other atom.
- `ExerciseEvolutionCard` (organism) takes `variant: "load" | "volume"`, the points, the selector props and the
  history state. It renders, in order:
  - the title, the subtitle and the selector (`text-field` with `select` and `menu-item`);
  - then, by state:
    - `skeleton` while loading;
    - `LoadError` with retry on failure;
    - "Sin datos de este ejercicio" when empty;
    - otherwise the chart atom.

_Why `@mui/x-charts`:_ the user chose it, and it themes from the MUI theme, so light and dark mode work without extra
tokens.

### D5. Presentation helpers in `workout-review.ts`

Each helper is pure, has no copy, and is unit-tested:

- `isWeighted(exercise: ExerciseSummary)`: `volume !== null`.
- `weightedExercisesOf(summary, workout)`: the weighted summary rows paired with their workout exercise by
  `workoutExerciseId`, each with its position (1-based in the workout), in workout order.
- `weightChangeOf(previous, current)`: `null` when either weight is missing. Otherwise `{ direction: "up" | "down" |
"equal", percent }`, with `percent = (current − previous) / previous × 100`. It returns `null` when `previous` is 0.
- `percentOf(part, total)`: rounded, or `null` when `total` is 0.
- `muscleDistributionOf(workout)`: `{ muscle, sets }[]`. It sums `countedRecordsOf(exercise).length` by `exercise.muscle`,
  drops 0, and sorts by `sets` descending, then by muscle code for stability.
- `historyPointsOf(sessions, field)`: reversed to oldest first, dropping sessions whose `field` is `null`.

Elapsed time and the counted records are not duplicated: the view passes `elapsed` (already formatted) and the
metrics reuse `reviewSummaryOf`.

### D6. Components and atomic levels

All of these live under `backoffice/src/presentation/workouts/components/` unless noted. Imports flow downward, and
only atoms import `@mui/material` widgets or `@mui/x-charts`.

- **Organisms:**
  - `WorkoutReviewMetrics`: the layout of the tab, with summary loading and error handling;
  - `SessionResultsCard`: five `ReviewStatCard`s;
  - `ExerciseEvolutionCard`;
  - `ExerciseComparisonCard`: a table over the `table*` atoms, like `review-sets-table.tsx`;
  - `MuscleDistributionCard`;
  - `SessionObservationsCard`.
- **Molecules:**
  - `ReviewStatCard` (existing) gains an optional `caption` with a `captionColor`;
  - `WeightChangeChip`: the `chip` atom, success or warning, with "▲" / "▼" / "=" / "—";
  - `ComplianceChip`: the `chip` atom with a check icon at 100%;
  - `MuscleSetsRow`: label, `linear-progress` and count;
  - `ObservationItem`: icon on a tinted circle, title and text.
- **Atoms (new):** `line-chart.tsx` and `bar-chart.tsx` in `common/components/atoms/`.
- **Fixture:** `presentation/workouts/session-observations.mock.ts` exports
  `SESSION_OBSERVATIONS_PREVIEW: readonly { code; severity: "positive" | "info" | "warning" }[]`. The copy is read
  from `review.metrics.observations.<code>.{title,text}`.

### D7. Formatting

- **Numbers** use `Intl.NumberFormat(i18n.language)`:
  - volume with at most one decimal, the same as the existing "Volumen registrado";
  - change with exactly one decimal and a sign;
  - weights with no forced decimals.
- **Dates** on the chart axis use `Intl.DateTimeFormat(language, { day: "numeric", month: "short" })`. The
  `YYYY-MM-DD` value is read as a local date, as in the app's `formatDay`, so it does not shift a day.
- **Top sets** read "<reps> × <weight> kg" through the existing `review.done.weighted` key.

### D8. Copy

New keys go in `backoffice/src/i18n/{es,en}/workouts.json` under `review.metrics`, as listed in the brief.

- Reused keys:
  - `review.volumeValue`;
  - `review.elapsed.*` (via the view);
  - `review.done.weighted`;
  - `muscles.<CODE>`;
  - the existing `LoadError` copy.
- `review.tabs.metrics` already exists.

## Errors

There are no new error codes and no mapper change.

- **Summary:** any `DomainError` (`NOT_FOUND`, `FORBIDDEN`, `UNAUTHORIZED`, `NETWORK`, `UNEXPECTED`) shows `LoadError`
  in the tab with `reload`. The session refresh is handled by `runWithSession`, as in `useWorkout`.
- **History:** any `DomainError` shows `LoadError` in each evolution card with the history hook's `reload`.
  `VALIDATION` (`400`) cannot happen with `limit: 5`.

## Tests

- **Core:**
  - `get-workout-summary-use-case.test.ts` mirrors `get-workout-by-id-use-case.test.ts`;
  - `get-user-exercise-history-use-case.test.ts` mirrors `get-my-exercise-last-performance-use-case.test.ts`;
  - `workout-repository.test.ts` and `user-repository.test.ts` gain blocks for `200`, `401`, `403`, `404`, network
    and the request path. The history also covers the `limit` query and `400`.
- **Backoffice:**
  - `workout-review.test.ts` gains the D5 helpers;
  - `workout-view.test.tsx` gains the metrics scenarios from the spec, faking both use cases through the container
    like `getWorkoutByIdUseCase`;
  - jsdom does not lay out SVG, so the test mocks the two chart atoms with `vi.mock` and asserts the `labels` /
    `values` they receive. This is the only new test pattern, and it is documented here as the exception.
  - Translation parity covers the keys.
- No E2E.

## Risks / Trade-offs

- [The refresh pulls other pending API changes] → Review the `v1.json` diff. If an unrelated change breaks a
  typecheck, stop and report.
- [The generated types make `volume` / `maxWeight` `number | string`, as other decimals are] → Convert them with
  `Number(...)` in the helpers, as the existing review does for weights. Check after the refresh.
- [History not anchored to the reviewed workout] → Accepted (proposal, Conflicts). A later API change can add
  `until`.
- [`@mui/x-charts` adds bundle size to a static SPA] → Accepted. It is loaded by the workout screen only, since Next
  splits it per route.
- [Mocked observations could be read as real] → The "Vista previa" mark, and the same three items on every workout.
- [The "Volumen registrado" card (front-computed) and "Volumen de fuerza" (API) can differ] → Accepted. They measure
  different things: the header card counts every weighted record, and the API applies its own rules. Aligning the
  header card is out of scope.

## Migration Plan

The snapshot refresh and regeneration form their own commit, first. The dependency install is part of the backoffice
commit. There is no data migration. Rolling back means reverting the commits; the endpoints are additive.
