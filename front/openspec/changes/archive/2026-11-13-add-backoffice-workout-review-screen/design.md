# Design

## Context

See `proposal.md` for the why and `specs/` for the behaviour. Today `WorkoutEditor` in
`backoffice/src/presentation/workouts/views/workout-view.tsx`:

- calls `useWorkout(id)`, `useExercises()` (the session's catalog cache) and `useExerciseFavorites()`;
- waits for the workout and the catalog;
- renders `LoadedWorkout`, which locks the properties card when the status is not `DRAFT` or `PUBLISHED`.

`WorkoutById` already carries everything the review shows:

- `exercises[].sets` and `exercises[].setRecords`;
- `type`, `effort`, `restMinutes`, `bodyPart`, `muscle` and `translations`;
- `resultsUpdatedAt`, `athlete`, `trainer` and `mesocycle`.

`core` is unchanged. There are no new `Dependencies` keys and nothing new in either `container.ts`.

## Goals / Non-Goals

**Goals:**

- One route and one workout read, with the branch made in the view once the workout is loaded.
- All the figures of the review come from one pure, unit-tested helper module, so the components only render.

**Non-Goals:**

- Removing the editor's locked mode (`locked`, banner copy), which becomes unreachable. Its clean-up is a separate
  change.
- Polling, live data, and any new API field.

## Decisions

### Branch in `WorkoutEditor`, after the workout guard and before the catalog guard

`WorkoutEditor` keeps its three hooks; hooks cannot be called conditionally, and the catalog comes from the session
cache. Once `workout.status === "loaded"`, a `STARTED`, `COMPLETED` or `REVIEWED` workout returns
`<WorkoutReview workout={...} />` before the catalog checks run. A catalog failure therefore never blocks the review.

The status test is a small exported helper, `isReviewStatus(status)` in `presentation/workouts/workout-review.ts`. Any
other status falls through to the editor as today.

- _Alternative:_ a separate route (`/workout-review?id=`). It was rejected because every existing link points to
  `/workout?id=`, and the status is only known after the read.
- _Alternative:_ moving the catalog and favorites hooks into an inner editor component, so that the review sends no
  favorites read. It was rejected because the editor's three reads would stop running in parallel. The extra favorites
  read is harmless.

### `WorkoutReview` lives in `views/workout-review-view.tsx`

It is a view-level component with no route of its own, rendered by `workout-view.tsx`. It owns the review's held state:

- the selected tab (`"exercises" | "metrics" | "feedback"`), starting on `"exercises"`;
- the set of expanded exercise ids, initialised once from `initiallyExpandedOf(...)`.

Both reset when the screen is left, and the trainer is the only one who changes them. It uses `useCurrentUser()` for
the `ADMIN` rule and `useExerciseLabels()` for names, chips and effort labels.

### Pure helpers in `presentation/workouts/workout-review.ts`

| Helper                           | Returns                                                                                                                        |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `plannedSetsOf(exercise)`        | sum of `sets[].count` (numbers or numeric strings, as the contract types them)                                                 |
| `countedRecordsOf(exercise)`     | `setRecords.slice(0, plannedSetsOf(exercise))` — the counting rule, the only place records are trimmed                         |
| `exerciseStateOf(exercise)`      | `"completed" \| "inProgress" \| "pending"` from counted vs planned (`completed` needs planned > 0)                             |
| `reviewSummaryOf(workout)`       | `{ planned, counted, pending, percent, completedExercises, totalExercises, volumeKg }`                                         |
| `plannedRowsOf(exercise)`        | one row per planned set: `{ number, target, record \| null }`, with groups expanded by `count` and paired with counted records |
| `initiallyExpandedOf(exercises)` | the id to expand first, or `null`                                                                                              |
| `restOf(restMinutes)`            | `"m:ss"` or `null`                                                                                                             |

- **`percent`** is `Math.round(counted / planned * 100)`, and `0` when `planned` is `0`.
- **`volumeKg`** sums `reps × weight` over counted records where both are non-null; there is no `perSide` factor.
- The contract types numbers as `number | string`, so every helper reads them through `Number(...)`, as the editor
  already does.

### Copy and formatting

- **New keys** under `workouts:review.*` in `backoffice/src/i18n/{es,en}/workouts.json`, as listed in the brief.
- **Target wording:** it gets its own `review.target.<Type>` keys ("8 reps", "8–10 reps", "Máx. reps", "30 s",
  "1 min"), because the editor's `setSummary` always prefixes the series count ("2 × 8 repeticiones") and a table row
  has no count.
- **Reused copy:**
  - the effort line uses `effortLabel(code, type)` with the existing `exercise.effortLine` wording;
  - the status chip text uses the existing status copy and colours (`workoutStatusColorOf`);
  - "Atleta" uses `person.role.athlete`.
- **Volume:** formatted with `Intl.NumberFormat(language, { useGrouping: "always", maximumFractionDigits: 1 })`.
  Spanish leaves four-digit numbers ungrouped by default, but the reference shows "3.240 kg".
- **Relative time:** `Intl.RelativeTimeFormat(language, { numeric: "auto" })`, in seconds, minutes, hours or days,
  computed once at render. It is not a ticking clock, since the screen does not refresh.
- **Unknown target type:** falls back to the raw value, like the editor's `other` summary.

### Components and atomic levels

Imports flow downward, and only atoms import MUI widgets. `Box` for layout follows the existing practice in the
backoffice's molecules and organisms.

- **Atoms (new)** in `common/components/atoms`: `tabs.tsx`, `tab.tsx` and `linear-progress.tsx`, which are thin MUI
  wrappers like `collapse.tsx`.
- **Molecules** in `workouts/components/molecules`:
  - `review-stat-card.tsx`: an icon, a label and a value;
  - `session-progress-card.tsx`;
  - `review-sets-table.tsx`: the effort line and the table.
- **Organisms** in `workouts/components/organisms`:
  - `workout-review-header.tsx`;
  - `workout-review-person-strip.tsx`;
  - `workout-review-tabs.tsx`: the tab row and the panels;
  - `review-exercise-card.tsx`: the collapsed row, the `Collapse` and `ReviewSetsTable`.
- **Reused:**
  - `Card`, `CardContent`, `Chip`, `Avatar`, `Collapse`, `IconButton`, `Typography`, `Link`, the `table*` atoms;
  - `HomeSkeleton` and `LoadError`, through the existing guards.

### Guardar como plantilla on the review

The header of the review gets a "Guardar como plantilla" button, to the right of the status chips. `WorkoutReview` uses
the existing `useSaveWorkoutAsTemplate()` hook and shows its outcome as the editor does: an error `Alert`, or a success
`Alert` "Plantilla creada." with a "Ver plantilla" link. The review has no form, so the button has no "unsaved" state
and no hint. The editor's copy keys (`workout.saveAsTemplate`, `workout.savedAsTemplate`, `workout.viewTemplate`) are
reused.

### Colours

Only theme tokens are used, so dark mode follows.

- **Primary:** the active tab, the progress bar, the stat icons and the position circles.
- **Status chip:** `workoutStatusColorOf`. For `STARTED` it is an outlined `error` chip with the `Sensors` icon and a
  2.4 s opacity fade (1 to 0.4), switched off under `prefers-reduced-motion`.
- **Exercise states:**
  - completed: `success`;
  - in progress: `primary`;
  - pending: `default`, with an outlined circle icon.
- **"Realizado":** `alpha(theme.palette.success.main, 0.08)` behind a recorded cell; the pending text is
  `text.secondary`.
- **Table header:** `action.hover`.
- **Cards:** outlined.
- **Exercise chips:** they keep the backoffice's current per-body-part colours from `exerciseLabelsOf`, and the
  mockup's teal chips are ignored.

### Errors

There are no new error codes. The existing guards are unchanged: not found, `LoadError` with retry, and the skeleton
while loading.

### Tests

- `backoffice/test/presentation/workouts/workout-review.test.ts`: unit tests for every helper, including discarded
  records, the empty plan, volume exclusions and the initially expanded choice.
- `backoffice/test/presentation/workouts/views/workout-view.test.tsx`: a new `describe("review")` block on the same
  `setup` and fakes, covering the scenarios in `specs/backoffice-workout-review` and the branch scenarios in
  `specs/backoffice-workout-editor`, with `ADMIN` and `TRAINER`.
- Existing tests that open a `STARTED`, `COMPLETED` or `REVIEWED` workout in the editor are updated or removed,
  following the REMOVED requirement in `specs/backoffice-workout-status`.
- The translation parity test covers the new keys. No E2E.

## Risks / Trade-offs

- **[Lost actions]** Exercise editing is gone for started workouts → this is flagged as BREAKING in the proposal. It
  can be brought back as an action on the review later.
- **[Counting rule drift]** The app and the backoffice each implement the same rule, because they cannot share code →
  both are unit-tested against the same examples. If the API ever returns an exercise state, both switch to it.
- **[Stale view]** The figures do not refresh while a `STARTED` workout is open → the "Última actualización" time and
  the live-styled status chip make that visible. Live data comes with the later API change.
- **[Dead code]** The editor's locked mode stays, unreachable for these statuses → leave it for a separate clean-up so
  that this change stays scoped.

## Migration Plan

This is a frontend-only static export. Deploying is enough, and rolling back means redeploying the previous build.
There is no data migration.
