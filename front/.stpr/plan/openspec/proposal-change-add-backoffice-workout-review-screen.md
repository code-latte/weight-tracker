# Change Proposal: Add The Backoffice Workout Review Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-workout-review-screen/`.

Visual reference: `brand/workout_review.png`.

## Change

Add a read-only "workout review" screen to the backoffice, opened on the same route as the editor (`/workout?id=<id>`).
A `DRAFT` or `PUBLISHED` workout keeps opening the current editor. A `STARTED`, `COMPLETED` or `REVIEWED` workout opens
the new review screen. It shows what the athlete has recorded against the plan: a header, a person strip, summary
cards, a progress bar, and three tabs. Only the "Exercises" tab has content: one expandable card per exercise with a
plan-versus-done table.

## Objective

Once the athlete starts a workout, the backoffice locks the editor and shows the trainer a form they can no longer use.
The set records the athlete logs are not shown anywhere. The trainer needs one screen to follow and review the session:
how far the athlete got, and what they did against what was planned.

## Capabilities

- **Routing by status.** `/workout?id=<id>` reads the workout once, as today. `DRAFT` and `PUBLISHED` show the editor,
  unchanged. `STARTED`, `COMPLETED` and `REVIEWED` show the review screen. Any other status value shows the editor, as
  today.
- **Counting rule (as the app does):**
  - an exercise's planned sets are the sum of its `sets[].count`;
  - its counted records are its first `setRecords`, in order, up to its planned sets, and any record beyond that is
    discarded everywhere on this screen;
  - an exercise is **finished** when its counted records equal its planned sets, **in progress** when it has some, and
    **pending** when it has none.
- **Placeholders** for the data the API will provide later. Each one renders its slot with the value "—" and no
  invented data; the API change that fills them is a separate plan.
  - start time;
  - elapsed time;
  - live state.
- **Header:**
  - a back link to where the workout lives (mesocycle or the workouts list, same rule as the editor);
  - the title "Revisión del entrenamiento";
  - a subtitle "<workout name> · <mesocycle name> · Semana <n>", which keeps only the name for a standalone workout;
  - on the right, a status chip with the workout status, using the existing status colours;
  - for `STARTED`, the status chip is outlined red with a live icon and fades slowly in and out (no separate "En
    directo" chip); it stands still under reduced motion. The screen reads the workout once and does not poll.
- **Person strip** (one outlined card):
  - the athlete's avatar with initials, full name (email when nameless) and "Atleta";
  - the trainer's name beside "Atleta", only for `ADMIN`, the same rule as the editor's strip;
  - on the right, "Inicio: —" (start time placeholder) with a calendar icon;
  - then "Última actualización: <relative time>" from `resultsUpdatedAt`, hidden when it is `null`.
- **Summary cards** (a row of four, which wraps on narrow widths; icon, label and big value):
  - "Tiempo transcurrido": "—" (placeholder);
  - "Series completadas": `<counted records> / <planned sets>`, summed over every exercise;
  - "Ejercicios terminados": `<finished> / <total>`;
  - "Volumen registrado": the sum of `reps × weight` over the counted records that have both, in kg. Timed and
    weightless records add nothing, and a `perSide` exercise is not doubled.
- **Session progress** (one outlined card):
  - the title "Progreso de la sesión" and the percentage (counted records / planned sets, rounded, with 0% when
    nothing is planned);
  - a linear progress bar;
  - "<n> series realizadas" on the left and "<n> pendientes" on the right (planned minus counted).
- **Tabs:** "Ejercicios", "Resumen y métricas" and "Feedback", with icons as in the reference. "Ejercicios" is
  selected first. The other two tabs show an empty panel. The tab row has no "Expandir todo" button and no ⋮ menu.
- **Exercise cards** (one per exercise, flat in workout order; `superSeriesId` is ignored):
  - **Collapsed row:** a state icon (check when finished, play when in progress, empty circle when pending), the
    position number in a primary circle, the exercise name in the current language, a filled body-part chip, an
    outlined muscle chip, a state chip ("Completado" / "En progreso" / "Pendiente") and an expand chevron.
  - **Initially expanded:** the first exercise in progress, or else the first one not finished. The rest start
    collapsed, and all start collapsed when every exercise is finished.
  - **Expanded body:**
    - **Effort, at exercise scope:** a line "Esfuerzo: <effort>" above the table, with the effort translated by
      exercise type through the existing `effortsByType` / `efforts` copy. It is hidden when `effort` is empty.
    - **A table** with the columns "Serie", "Objetivo (plan)", "Realizado" and "Descanso". There is one row per
      planned set: the `sets` groups are expanded by `count`, and the counted records are paired with them in order.
      There are no rows for discarded records.
    - **Objetivo:** the planned target only, with no weight:
      - `Reps`: "N reps";
      - `RepsRange`: "N–M reps";
      - `MaxReps`: "Máx. reps";
      - `Duration`: "N s";
      - `Minutes`: "N min".
    - **Realizado:** a check and the record on a light success tint:
      - "<reps> × <weight> kg" when the record has both;
      - "<reps> reps" without a weight;
      - the duration for a timed record.

      A planned set with no record shows an empty circle and "Pendiente".

    - **Descanso:** the exercise's `restMinutes` as `m:ss` on every row, or "—" when it is `null`.
  - There is no athlete comment block; it will live in the Feedback tab later.
- **Read-only.** The review offers no edit or status change action. It keeps "Guardar como plantilla" in the header, as in the editor.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both open the review of any workout the API returns to
  them.
  - The only role difference is the trainer's name in the person strip: `ADMIN` sees it and `TRAINER` must not, as in
    the editor.
- app -> lands here: no -> roles: `USER` -> no change.

## Classification

Backoffice only. `core` is unchanged: the screen reuses `getWorkoutByIdUseCase`.

- Status branch -> view (changed in place) -> `backoffice/src/presentation/workouts/views/workout-view.tsx`.
  - `WorkoutEditor` renders the editor or the review once the workout has loaded.
- `WorkoutReview` -> view section (new, beside the editor) -> `backoffice/src/presentation/workouts/views/workout-review-view.tsx`.
  - It has no route of its own; `workout-view.tsx` renders it.
- `WorkoutReviewHeader` -> organism -> `presentation/workouts/components/organisms/workout-review-header.tsx`.
  - It holds the back link, the title, the subtitle and the status chip.
- `WorkoutReviewPersonStrip` -> organism -> `presentation/workouts/components/organisms/workout-review-person-strip.tsx`.
- `ReviewStatCard` -> molecule -> `presentation/workouts/components/molecules/review-stat-card.tsx`.
- `SessionProgressCard` -> molecule -> `presentation/workouts/components/molecules/session-progress-card.tsx`.
- `WorkoutReviewTabs` -> organism -> `presentation/workouts/components/organisms/workout-review-tabs.tsx`.
- `ReviewExerciseCard` -> organism -> `presentation/workouts/components/organisms/review-exercise-card.tsx`.
  - It holds the collapsed row, the effort line and the `Collapse` with the table.
- `ReviewSetsTable` -> molecule -> `presentation/workouts/components/molecules/review-sets-table.tsx`.
- Derivations -> pure presentation helpers -> `presentation/workouts/workout-review.ts`.
  - They cover the counting rule (planned sets, counted records, discarded extras), the plan-to-record row pairing,
    exercise state, the initially expanded card, volume and the rest format.
- `Tabs`, `Tab`, `LinearProgress` -> atoms (new) -> `backoffice/src/presentation/common/components/atoms/`.
  - Only atoms import MUI widgets.

## Canonical Template Mapping

- Status branch and review view -> `backoffice/src/presentation/workouts/views/workout-view.tsx` -> the same screen,
  loading and error handling, back-link rule and strip rule.
- Person strip -> `components/organisms/workout-context-strip.tsx` and `common/components/molecules/person-card.tsx` ->
  the existing way the backoffice shows the athlete and the trainer of a workout.
- Exercise card and its chips -> `components/organisms/workout-exercises-card.tsx` and
  `components/molecules/exercise-list-item.tsx` -> the existing exercise rows, names per language and chips.
- Sets table -> the backoffice table atoms (`table*.tsx`) as used by `components/organisms/workouts-table.tsx`.
- Effort line -> the existing `effortLine`, `effortsByType` and `efforts` copy in `src/i18n/*/workouts.json`, reused.
- Counting rule -> the app's equivalent rule for when an exercise is done, mirrored and not imported: the fronts share
  nothing.
- Status chip -> `presentation/common/workout-status.ts` -> the existing status-to-colour map.
- New atoms -> any existing atom in `common/components/atoms` (thin MUI wrapper).
- No `pending` reference in `.stpr/template/README.md` applies.

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> any workout link (the workouts list, the mesocycle week table) ->
  the status decides editor or review.
  - The back link goes to the mesocycle or to `/assigned-workouts`.
  - `NOT_FOUND` shows the existing not-found block, and other failures show `LoadError` with retry, as today.

## API Contract

- `GET /workouts/{id}` -> in `core/openapi/v1.json`: yes -> `WorkoutById`.
  - From it the screen uses `status`, `exercises[].sets`, `exercises[].setRecords`, `type`, `restMinutes`, `effort`,
    `resultsUpdatedAt`, `athlete`, `trainer` and `mesocycle`.

No snapshot refresh and no new endpoint. `PUT /workouts/{workoutId}/review` exists but is not used here.

The start time, the elapsed time and the live state will come from a later API change, planned in `api/`. That change
then replaces the three placeholders in a front change of its own.

## Held State

- `useWorkout` -> the workout -> read once on open -> resets on screen exit (unchanged; no polling).
- The review view -> the selected tab and the set of expanded exercise cards -> the trainer -> resets on screen exit.

## Forms And Structural Validation

None.

## Errors And Copy

No new error codes. Load failures reuse the existing handling.

New keys under `workouts:review.*` (es / en):

- `title` -> Revisión del entrenamiento -> Workout review
- `back` -> reuse `workout.back`
- `athlete` -> reuse `person.role.athlete`
- `startedAt` -> Inicio: {{time}} -> Start: {{time}} (rendered with "—" for now)
- `notAvailable` -> — -> — (the placeholder value)
- `lastUpdate` -> Última actualización: {{when}} -> Last update: {{when}}
- `stats.setsDone` -> Series completadas -> Sets completed
- `stats.exercisesDone` -> Ejercicios terminados -> Exercises finished
- `stats.volume` -> Volumen registrado -> Volume recorded
- `stats.elapsed` -> Tiempo transcurrido -> Time elapsed
- `progress.title` -> Progreso de la sesión -> Session progress
- `progress.done` -> {{count}} series realizadas -> {{count}} sets done
- `progress.pending` -> {{count}} pendientes -> {{count}} pending
- `tabs.exercises` -> Ejercicios -> Exercises
- `tabs.metrics` -> Resumen y métricas -> Summary and metrics
- `tabs.feedback` -> Feedback -> Feedback
- `exercise.completed` -> Completado -> Completed
- `exercise.inProgress` -> En progreso -> In progress
- `exercise.pending` -> Pendiente -> Pending
- `table.set` -> Serie -> Set
- `table.target` -> Objetivo (plan) -> Target (plan)
- `table.done` -> Realizado -> Done
- `effort` -> reuse `exercise.effortLine` ("Esfuerzo: {{effort}}")
- `target.Reps` -> {{value}} reps -> {{value}} reps
- `target.RepsRange` -> {{value}}–{{max}} reps -> {{value}}–{{max}} reps
- `target.MaxReps` -> Máx. reps -> Max reps
- `target.Duration` -> {{value}} s -> {{value}} s
- `target.Minutes` -> {{value}} min -> {{value}} min
- `done.weighted` -> {{reps}} × {{weight}} kg -> {{reps}} × {{weight}} kg
- `done.reps` -> {{reps}} reps -> {{reps}} reps
- `table.rest` -> Descanso -> Rest
- `table.pending` -> Pendiente -> Pending
- `expand` / `collapse` (a11y labels for the chevron) -> Expandir / Contraer -> Expand / Collapse

## Platform Capabilities And Dependencies

None. The relative time ("hace 30 s") uses `Intl.RelativeTimeFormat` and needs no new package.

## Colour Scheme

Everything uses theme tokens, so light and dark mode both work:

- **Primary teal:** the active tab and its underline, the progress bar, the stat-card icons and the position circles.
- **Exercise chips:** the backoffice's current per-body-part colours; the mockup's chip colours are ignored.
- **Status chip in the header:** `workoutStatusColorOf`, which gives `STARTED` warning, `COMPLETED` success and
  `REVIEWED` secondary. This keeps the screen consistent with the workouts list, except that `STARTED` is an outlined
  red chip with a live icon and a slow fade.
- **Exercise states:**
  - finished: a success chip and a success check;
  - in progress: a primary chip and a primary play icon;
  - pending: a default (grey) chip and an outlined circle.
- **"Realizado" cell:** a success check and bold text on `alpha(success, 0.08)` when recorded; secondary text and an
  outlined circle when pending.
- **Cards:** outlined `background.paper` cards, the theme's 14 px radius and no heavy shadows. The table header uses
  `action.hover` as its background.

## Decisions Taken

- Start time, elapsed time and live state: placeholders with "—" for now; the API will provide them.
- Planned weight: discarded. "Objetivo" shows only the target.
- RIR: at exercise scope, as the effort line above the table, not per set.
- Athlete comment: discarded. It will live in the Feedback tab.
- Exercise state: the app's rule. Planned sets reached means finished, and records beyond the plan are discarded.
- Volume: `reps × weight` over the counted records that have both; timed and weightless records add nothing; no
  `perSide` doubling.
- ⋮ menu, "mark as reviewed" and superseries grouping: left out.
- Initially expanded card: the first one in progress, else the first one not finished.

## Out Of Scope

- Content of the "Resumen y métricas" and "Feedback" tabs: both are empty panels.
- The "Expandir todo" button.
- Editing a started workout, changing its status or saving it as a template from the review.
- Any change to `core`, the API contract or the app.
- The editor for `DRAFT` and `PUBLISHED`, which must not change.
- Exercise editing for started workouts. The locked editor offered it, and the review drops it.

## Notes

- Verification tiers:
  - a backoffice component test for the review screen: the status branch, every summary value, the progress, the
    tabs, the expand and collapse, the initially expanded card, the placeholders, the effort line, the plan-versus-done
    rows including a discarded extra record and a pending set, and an
    `ADMIN` versus `TRAINER` strip that asserts the trainer is hidden for `TRAINER`;
  - unit tests for the `workout-review.ts` helpers;
  - the existing editor tests keep passing for `DRAFT` and `PUBLISHED`;
  - no new E2E flow.
- The derivations are display arithmetic over data the API already returns, with the same counting rule as the app.
  This keeps no business rule of the front's own.
- The editor's current "locked" mode (`statusTarget === null`) becomes unreachable for the three review statuses. Leave
  it as is: removing it is a separate clean-up.
- No deviations from the mapped references are allowed.
