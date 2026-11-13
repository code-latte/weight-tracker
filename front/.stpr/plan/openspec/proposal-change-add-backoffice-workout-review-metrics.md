# Change Proposal: Add The Backoffice Workout Review Metrics Tab

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-workout-review-metrics/`.

Visual reference: `brand/review.png`.

## Change

Fill the "Resumen y métricas" tab of the backoffice workout review screen. Today it is an empty panel. It gets six
blocks: session results, load evolution, volume evolution, a per-exercise comparison, the muscle distribution, and
the session observations. Observations are mocked until the API provides them.

## Objective

The review shows what the athlete recorded, set by set, but not how the session went as a whole or against the
previous executions. The API now computes the summary and the exercise history
(`api/openspec/changes/add-workout-summary-and-exercise-history`), so the trainer can read compliance, volume and
progression without working it out by hand.

## Capabilities

- **Lazy load.** The tab reads `GET /workouts/{workoutId}/summary` the first time it is opened, and not again while the
  screen stays open. The workout itself is already loaded by the screen (`useWorkout`).
- **Weighted exercises.** An exercise counts as weighted when its summary `volume` is not `null`. Only weighted
  exercises appear in the comparison table and in the chart selector.
- **Session results** (one outlined card, title "Resultados de la sesión", subtitle "Cumplimiento, volumen e
  intensidad del entrenamiento"). It holds five stat tiles in a row that wraps on narrow widths. None of them shows
  a change against another workout.
  - "Series completadas": `<counted records> / <planned sets>`, with the percentage below it. The existing counting
    rule in `workout-review.ts` is reused.
  - "Ejercicios completados": `<finished> / <total>`, with the percentage below it. The existing rule is reused.
  - "Series en rango objetivo": summary `setsInTarget.inTarget / evaluated`, with the percentage below it, or "—"
    when `evaluated` is 0.
  - "Volumen de fuerza": summary `volume` in kg (`16.913,4 kg`), or "—" when it is `null`.
  - "Duración": `completedAt − startedAt` as "1 h 12 min" (or "N min" under an hour). It shows "—" when either value
    is `null`.
- **Exercise evolution** (two cards side by side, which stack on narrow widths). Both cards share one exercise
  selector, defaulting to the first weighted exercise in workout order.
  - Data: `GET /users/{athleteId}/exercises/{exerciseId}/history?limit=5`. It is read again on every change of the
    selection, and the sessions are shown oldest to newest.
  - "Evolución de la carga" ("Peso máximo por sesión"): a line chart of `maxWeight`, with the area under it tinted.
    The last point carries a "130 kg" label.
  - "Evolución del volumen" ("Volumen total del ejercicio (peso × repeticiones)"): a bar chart of `volume`. The last
    bar is highlighted and carries a label.
  - The x axis shows the session date ("12 nov"). Points with a `null` value are skipped.
  - With no sessions, each card shows "Sin datos de este ejercicio".
- **Comparativa por ejercicio** (one card, subtitle "Resultados de esta sesión frente a la ejecución anterior"). It
  shows one row per weighted exercise, in workout order.
  - Position number, in the primary circle that the Ejercicios tab already uses.
  - Name in the current language, taken from the workout's exercise by `workoutExerciseId`.
  - "Anterior": `previous.topSet` as "<reps> × <weight> kg", or "—" when `previous` is `null`.
  - "Actual": `topSet`, in the same format and in bold.
  - "Cambio": the weight change from `previous.topSet` to `topSet` as a percentage with one decimal ("▲ +8,3 %"
    in a success chip, "▼ −5,0 %" in a warning chip). It shows "=" when the weights are equal and "—" when there is no
    previous.
  - "Volumen (actual)": the exercise `volume` in kg.
  - "Cumplimiento": `inTarget / evaluated` as a percentage in a chip. The chip is success at 100 %, warning below
    100 %, and shows "—" when `evaluated` is 0.
- **Distribución de series por grupo muscular** (subtitle "Series efectivas de la sesión").
  - One row per muscle, from each exercise's own `muscle`, with only muscles that have at least one counted record.
    Each row shows the muscle name (existing muscle copy), a bar, and "<n> series".
  - The count is the counted records of the exercises with that muscle, under the screen's existing counting rule.
  - Rows are sorted by count, descending. Bars are relative to the largest count.
- **Observaciones de la sesión** (subtitle "Puntos clave y recomendaciones"). These are **mocked** until the API
  returns them.
  - A fixed list of three observations lives in the presentation layer, shaped as the planned contract
    (`code`, `severity`):
    - `LOAD_PROGRESSION` / positive: "Progresión destacada";
    - `GOOD_COMPLIANCE` / info: "Buen cumplimiento";
    - `SET_BELOW_TARGET` / warning: "Revisar".
  - Each one uses fixed example copy, translated.
  - A "Vista previa" chip beside the card title marks the content as sample data.
  - Each observation is an outlined row with a tinted icon (trend / info / warning), a title in the severity colour,
    and its text.
- **Read-only.** The tab has no actions.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both see the same tab for any workout the API returns
  to them. There is no role difference in this tab.
- app -> lands here: no -> roles: `USER` -> no change.

## Classification

- `WorkoutSummary`, `ExerciseSummary`, `ExerciseHistorySession` -> entity types (aliases of generated types) ->
  `core/src/domain/workouts`, `core/src/domain/users`.
- `getSummary(workoutId)` -> repository contract + implementation -> `core/src/application/ports/workout-repository-interface.ts`,
  `core/src/infrastructure/repositories/workout-repository.ts`.
- `getExerciseHistory(userId, exerciseId, limit)` -> repository contract + implementation ->
  `core/src/application/ports/user-repository-interface.ts`, `core/src/infrastructure/repositories/user-repository.ts`.
- `GetWorkoutSummaryUseCase` -> use case -> `core/src/application/use-cases/workouts/`.
- `GetUserExerciseHistoryUseCase` -> use case -> `core/src/application/use-cases/users/`.
- Registration -> composition root -> `backoffice/src/infrastructure/container.ts` only. The app does not use them.
- `useWorkoutSummary`, `useExerciseHistory` -> state hooks -> beside `useWorkout` in `backoffice/src/presentation/workouts`.
- `WorkoutReviewMetrics` -> organism holding the six blocks, rendered by the "Resumen y métricas" tab panel ->
  `presentation/workouts/components/organisms/workout-review-metrics.tsx`.
- Each of the six blocks is its own organism, in `components/organisms/`:
  - `SessionResultsCard`;
  - `ExerciseEvolutionCard`, used twice: line and bar;
  - `ExerciseComparisonCard`;
  - `MuscleDistributionCard`;
  - `SessionObservationsCard`.
- New molecules, in `components/molecules/`:
  - `WeightChangeChip`;
  - `ComplianceChip`;
  - `MuscleSetsRow`;
  - `ObservationItem`.
- `ReviewStatCard` -> molecule (extended in place) -> an optional caption line for the percentage.
- `LineChart`, `BarChart` -> atoms (new) -> `backoffice/src/presentation/common/components/atoms/`. They are the only
  importers of `@mui/x-charts`.
- Derivations -> pure presentation helpers -> `presentation/workouts/workout-review.ts` (extended).
  - They cover duration, percentages, the weight change, the weighted filter and the muscle distribution.
- Mocked observations -> presentation fixture -> `presentation/workouts/session-observations.mock.ts`.

## Canonical Template Mapping

- Core aliases, contract methods, implementations and use cases -> `core/src/application/use-cases/users/get-my-exercise-last-performance-use-case.ts`
  and its repository method -> the latest read-only `Result` use case over a computed endpoint.
- Container wiring -> the existing `getWorkoutByIdUseCase` entry in `backoffice/src/infrastructure/container.ts`.
- State hooks -> `useWorkout` -> same load, error and retry shape.
- Tab content and cards -> the review's own organisms (`workout-review-header.tsx`, `review-exercise-card.tsx`) and
  `review-stat-card.tsx` -> the same screen's card, title and stat-tile style.
- Comparison table -> `components/molecules/review-sets-table.tsx` and the backoffice `table*.tsx` atoms.
- Exercise selector -> the backoffice's existing select atom.
- Muscle bars -> the `LinearProgress` atom added by the review screen.
- Chart atoms -> any thin MUI-wrapping atom in `common/components/atoms`. Charts are new to the backoffice, so this
  change establishes the chart atom pattern.
- No `pending` reference in `.stpr/template/README.md` applies.

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> review (status `STARTED`, `COMPLETED` or `REVIEWED`) -> the
  "Resumen y métricas" tab. There is no new route and no navigation out of the tab.

## API Contract

- `GET /workouts/{workoutId}/summary` -> in `core/openapi/v1.json`: **not yet** -> `WorkoutSummaryResponse`,
  `ExerciseSummaryResponse`, `SetsInTargetResponse`, `PreviousExecutionResponse`, `LastPerformanceSetResponse`.
- `GET /users/{userId}/exercises/{exerciseId}/history?limit=` -> in `core/openapi/v1.json`: **not yet** ->
  `ExerciseHistorySessionResponse[]`, newest first, with `limit` in `1..50`.
- `GET /workouts/{id}` -> yes -> `WorkoutById`: `startedAt`, `completedAt`, `athlete.id` and `exercises[].muscle` are
  used here.

Both endpoints have landed in the API (`add-workout-summary-and-exercise-history`). The first task of this change
runs `openapi:refresh` + `openapi:generate` and commits the snapshot and the generated types as a step of their own.
Observations are not in the contract; they stay mocked (see Capabilities).

## Held State

- `useWorkoutSummary` -> the summary -> loaded on first opening of the tab -> resets on screen exit.
- `useExerciseHistory` -> the history of the selected exercise -> read again when the selection changes -> resets on
  screen exit.
- `WorkoutReviewMetrics` -> the selected chart exercise -> the trainer -> resets on screen exit.

## Forms And Structural Validation

None.

## Errors And Copy

No new error codes. A `400` (limit out of range) cannot happen with the fixed `limit=5`.

- Summary fails -> `NOT_FOUND`, `FORBIDDEN` or any other code -> the tab shows `LoadError` with retry; the rest of the
  screen stays.
- History fails -> any code -> both evolution cards show an inline `LoadError` with retry; the other blocks stay.

New keys under `workouts:review.metrics.*` (es / en):

- `results.title` -> Resultados de la sesión -> Session results
- `results.subtitle` -> Cumplimiento, volumen e intensidad del entrenamiento -> Compliance, volume and intensity of the session
- `results.setsDone` -> Series completadas -> Sets completed
- `results.exercisesDone` -> Ejercicios completados -> Exercises completed
- `results.setsInTarget` -> Series en rango objetivo -> Sets in target range
- `results.volume` -> Volumen de fuerza -> Strength volume
- `results.duration` -> Duración -> Duration
- `duration.hoursMinutes` -> {{hours}} h {{minutes}} min -> {{hours}} h {{minutes}} min
- `duration.minutes` -> {{minutes}} min -> {{minutes}} min
- `load.title` -> Evolución de la carga -> Load progression
- `load.subtitle` -> Peso máximo por sesión -> Max weight per session
- `volume.title` -> Evolución del volumen -> Volume progression
- `volume.subtitle` -> Volumen total del ejercicio (peso × repeticiones) -> Total exercise volume (weight × reps)
- `history.empty` -> Sin datos de este ejercicio -> No data for this exercise
- `comparison.title` -> Comparativa por ejercicio -> Exercise comparison
- `comparison.subtitle` -> Resultados de esta sesión frente a la ejecución anterior -> This session against the previous execution
- `comparison.exercise` / `previous` / `current` / `change` / `volume` / `compliance` -> Ejercicio / Anterior /
  Actual / Cambio / Volumen (actual) / Cumplimiento -> Exercise / Previous / Current / Change / Volume (current) /
  Compliance
- `comparison.topSet` -> {{reps}} × {{weight}} kg -> reuse `review.done.weighted`
- `muscles.title` -> Distribución de series por grupo muscular -> Sets per muscle group
- `muscles.subtitle` -> Series efectivas de la sesión -> Effective sets of the session
- `muscles.sets` -> {{count}} series -> {{count}} sets
- `observations.title` -> Observaciones de la sesión -> Session observations
- `observations.subtitle` -> Puntos clave y recomendaciones -> Key points and recommendations
- `observations.preview` -> Vista previa -> Preview
- `observations.LOAD_PROGRESSION.title` / `.text` -> Progresión destacada / Aumento de carga en Peso muerto rumano (+8,3 %) y Sentadilla hack (+12,5 %). ->
  Notable progression / Load increase in Romanian deadlift (+8.3%) and Hack squat (+12.5%).
- `observations.GOOD_COMPLIANCE.title` / `.text` -> Buen cumplimiento / 27 de 29 series dentro del rango de repeticiones objetivo (93 %). ->
  Good compliance / 27 of 29 sets within the target rep range (93%).
- `observations.SET_BELOW_TARGET.title` / `.text` -> Revisar / La tercera serie de Peso muerto rumano quedó por debajo del objetivo (7/8 reps). Valorar mantener la carga o ajustar el RIR en la próxima sesión. ->
  Review / The third set of Romanian deadlift fell below target (7/8 reps). Consider keeping the load or adjusting the RIR next session.
- Muscle names -> reuse the existing muscle copy.

## Platform Capabilities And Dependencies

- `@mui/x-charts` -> `backoffice` -> the backoffice has no chart library. This one is MUI's own, so it follows the
  theme in light and dark mode. Only the `LineChart` / `BarChart` atoms import it.

## Colour Scheme

Everything uses theme tokens, so light and dark mode both work:

- **Primary teal:**
  - stat-tile icons, card title icons and position circles;
  - the chart line, its area at low alpha, and the bars (the last bar at full strength, the others softer);
  - the muscle bars on an `action.hover` track.
- **Percentages under the tiles:** success at 100 %, otherwise `text.secondary`.
- **Weight change:** a success chip with ▲ for an increase, a warning chip with ▼ for a decrease, and plain "=" when
  equal.
- **Compliance chip:** success with a check at 100 %, warning below it.
- **Observations:** positive uses success, info uses info, and warning uses warning. Each icon sits on its colour at
  low alpha.
- **Cards:** outlined `background.paper` cards with the theme's radius, as in the rest of the review.

## Decisions Taken

- No workout-to-workout comparison: the tiles show no deltas. Comparisons are per exercise against its previous
  execution only.
- Only weighted exercises (summary `volume` not `null`) are listed in the comparison table and the chart selector.
- Effective sets = counted records, under the screen's existing counting rule.
- Compliance = sets in the target range, as computed by the API (a max-reps target is always compliant).
- Muscle distribution uses each exercise's own `muscle` and lists only muscles with counted records.
- Volume comes from the API only, never computed by the front, for the tile, the table and the charts.
- Observations are mocked with fixed sample copy and a "Vista previa" chip. The RIR mention is trainer-facing advice
  about the exercise's planned effort.
- History shows the last 5 sessions as the API returns them. The endpoint is not anchored to the reviewed workout's
  date, so reviewing an older workout can show later sessions.

## Out Of Scope

- Real observations. A later API change provides them, and a front change of its own then replaces the mock.
- An `until` parameter on the history endpoint, and anchoring the charts to the reviewed workout.
- The estimated one-rep max from the history (not in the mockup).
- The "Feedback" tab, which stays an empty panel.
- Any change to the "Ejercicios" tab, the header, the person strip or the editor.
- The app.

## Notes

- Verification tiers:
  - core unit tests:
    - both use cases;
    - both repository methods, for success and for every error code they map.
  - helper unit tests for the new `workout-review.ts` derivations:
    - duration, including `null` start or end and under/over an hour;
    - percentages, including 0 evaluated;
    - weight change: up, down, equal and no previous;
    - the weighted filter;
    - the muscle distribution: only trained muscles, sorted, counted records.
  - a backoffice component test for the metrics tab, with fakes through the container:
    - the five tiles and their "—" cases;
    - the comparison rows, with a non-weighted exercise hidden and a row without previous;
    - the muscle rows, with an untrained muscle absent;
    - the observations with the preview chip;
    - a selector change that reads the history again;
    - the empty history;
    - summary and history load errors, each with retry.
  - translation parity keeps passing. No new E2E flow.
- `@mui/x-charts` renders SVG that jsdom handles poorly. Component tests may replace the chart atoms with a test
  double and assert the data passed to them.
- No deviations from the mapped references are allowed.
