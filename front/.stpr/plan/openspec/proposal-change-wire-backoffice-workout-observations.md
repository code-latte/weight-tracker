# Change Proposal: Wire The Backoffice Workout Observations

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/wire-backoffice-workout-observations/`.

## Change

Replace the mocked "Observaciones de la sesión" card in the "Resumen y métricas" tab of the backoffice workout
review with the real observations from the API. The card shows the highlights. A "Ver todas las observaciones"
button inside the card opens a dialog with every observation of the workout.

## Objective

The card has shown three fixed sample observations, marked "Vista previa", since the metrics tab landed
(`add-backoffice-workout-review-metrics`). The API now generates observations when a prescribed workout is completed
and selects up to 3 highlights (`api/openspec/changes/archive/2026-11-13-add-workout-observations`). The trainer can now
see the real key points at a glance and open the full list when they want the detail.

## Capabilities

- **One read, two lists.** `GET /workouts/{workoutId}/observations` returns both `observations` and `highlights`. It
  is a single endpoint, not two. The tab reads it the first time it is opened, together with the summary, and does
  not read it again while the screen stays open.
- **Card = highlights.** The card lists the highlights in `rank` order. Each highlight is resolved to its observation
  by `observationId`. A highlight whose observation is missing from `observations` is skipped.
- **"Ver todas las observaciones".** A text button at the bottom of the card, with the total count
  ("Ver todas las observaciones (12)"). It is shown whenever `observations` is not empty, even when there are no
  highlights.
- **Dialog = all observations.** It has the title "Observaciones de la sesión" and a close action. Observations are
  grouped by subject in the order the API returns them: first "Entrenamiento" (`exerciseId` null), then one group
  per exercise, headed by the exercise name. Highlighted observations carry a "Destacada" chip. The dialog is
  read-only.
- **Each observation** keeps the existing `ObservationItem` look: an icon and colour from its `severity`, a title
  from its `code` and a text built from its `data`.
  - `SUCCESS` -> success colour, trend icon.
  - `INFO` -> info colour, info icon.
  - `WARNING` -> warning colour, warning icon.
- **Copy from data.** Each code has a translated title and a text template interpolated with values from `data`.
  - Exercise-level texts start with the exercise name.
  - Kilograms and percentages use the active locale ("8,3 %" in `es`, "8.3%" in `en`). Changes are signed and shown
    with one decimal.
  - Optional values (`baselineMaxKg`, `baselineVolumeKg`, `baselineTotalReps`) add a trailing sentence only when present.
  - List values (`setPositions` with `reps` and `targetMins` / `targetMaxes`) are rendered as a joined detail
    ("serie 3: 6/8 · serie 4: 5/8").
  - A code the front does not know shows the generic title "Observación" with no text, instead of failing.
- **Exercise names.** `exerciseId` is the catalog exercise. Its name is taken, in the current language, from the
  first workout exercise with that `exerciseId` in the already loaded `WorkoutById`. If none matches, the generic
  "Ejercicio" is used.
- **Empty states.**
  - Both lists empty (workout not completed, no trainer, or completed before observations existed): the card shows
    "No hay observaciones para esta sesión" and no button.
  - No highlights but some observations: the card shows "Nada destacable en esta sesión" and the button.
- **Mock removed.** `session-observations.mock.ts`, the "Vista previa" chip and the sample copy keys go away.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both see the card and the dialog for any workout the
  API returns to them. There is no role difference in this tab.
- app -> lands here: no -> roles: `USER` -> no change. The use case lives in `core` but is only registered in the
  backoffice.

## Classification

- `WorkoutObservations`, `WorkoutObservation`, `WorkoutHighlight` -> entity types (aliases of the generated types) ->
  `core/src/domain/workouts`.
- `getObservations(workoutId)` -> repository contract + implementation ->
  `core/src/application/ports/workout-repository-interface.ts`, `core/src/infrastructure/repositories/workout-repository.ts`.
- `GetWorkoutObservationsUseCase` -> use case -> `core/src/application/use-cases/workouts/`.
- Registration -> composition root -> `backoffice/src/infrastructure/container.ts` only.
- `useWorkoutObservations` -> state hook -> beside `useWorkoutSummary`.
- `SessionObservationsCard` -> organism (rewired in place) -> it takes the observations and the workout, and owns
  the dialog's open state.
- `WorkoutObservationsDialog` -> organism (new) -> `presentation/workouts/components/organisms/workout-observations-dialog.tsx`.
- `ObservationItem` -> molecule (changed in place) -> its severity type moves from the mock to the API's
  `SUCCESS | INFO | WARNING`. It takes an optional trailing chip for "Destacada".
- Derivations -> pure presentation helpers -> `presentation/workouts/workout-observations.ts` (new). They cover:
  - resolving highlights to observations;
  - grouping by subject;
  - the exercise name lookup;
  - the interpolation values for each code, including list details and optional baselines.
- `WorkoutReviewMetrics` -> organism (changed in place) -> it loads the observations alongside the summary and passes
  them to the card.

## Canonical Template Mapping

- Core alias, contract method, implementation and use case -> `core/src/application/use-cases/workouts/get-workout-summary-use-case.ts`
  and its repository method -> the nearest read-only `Result` use case over a computed workout endpoint.
- Container wiring -> the `getWorkoutSummaryUseCase` entry in `backoffice/src/infrastructure/container.ts`.
- State hook -> `useWorkoutSummary` -> same lazy load, error and retry shape.
- Dialog -> `presentation/workouts/components/organisms/exercise-video-dialog.tsx` -> the closest read-only dialog
  in the same front, built on the `dialog*.tsx` atoms.
- Card, items and chips -> the existing `session-observations-card.tsx`, `observation-item.tsx` and the `chip` atom.
- Helpers -> `presentation/workouts/workout-review.ts` -> the same pure-function style and placement.
- No `pending` reference in `.stpr/template/README.md` applies.

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> review (status `STARTED`, `COMPLETED` or `REVIEWED`) -> the
  "Resumen y métricas" tab -> "Ver todas las observaciones" opens the dialog. Closing it returns to the tab. There is
  no new route.

## API Contract

- `GET /workouts/{workoutId}/observations` -> in `core/openapi/v1.json`: **not yet** -> `WorkoutObservationsResponse`
  (`observations`, `highlights`), `WorkoutObservationResponse` (`id`, `code`, `severity`, `exerciseId`, `priority`,
  `data`), `WorkoutHighlightResponse` (`observationId`, `rank`, `priority`).
- `GET /workouts/{id}` -> yes -> `WorkoutById`: `exercises[].exerciseId` and the exercise names are used for the
  lookup.

The endpoint is on the current API branch (`add-workout-observations`). The first task of this change runs
`openapi:refresh` (with that API running) and `openapi:generate`. It commits the snapshot and the generated types as
a step of their own. `code`, `severity` and `priority` are declared as strings, and `data` as a map of numbers or
number lists. The front treats them as such and never hand-writes a narrower domain type. The per-code
interpolation helper reads `data` structurally.

## Held State

- `useWorkoutObservations` -> the observations response -> loaded on first opening of the tab -> resets on screen exit.
- `SessionObservationsCard` -> whether the dialog is open -> the trainer -> resets on close and on screen exit.

## Forms And Structural Validation

None.

## Errors And Copy

No new error codes.

- Observations fail to load -> `NOT_FOUND`, `FORBIDDEN` or any other code -> the card shows an inline `LoadError`
  with retry. The rest of the tab stays.

Copy under `workouts:review.metrics.observations.*` (es / en). Keys removed: `preview`, `LOAD_PROGRESSION.*`,
`GOOD_COMPLIANCE.*`, `SET_BELOW_TARGET.*`. Keys kept: `title`, `subtitle`.

- `viewAll` -> Ver todas las observaciones ({{count}}) -> View all observations ({{count}})
- `highlighted` -> Destacada -> Highlighted
- `none` -> No hay observaciones para esta sesión -> No observations for this session
- `noHighlights` -> Nada destacable en esta sesión -> Nothing stands out in this session
- `workoutGroup` -> Entrenamiento -> Workout
- `unknownExercise` -> Ejercicio -> Exercise
- `unknown.title` -> Observación -> Observation
- `setDetail` -> serie {{position}}: {{reps}}/{{target}} -> set {{position}}: {{reps}}/{{target}}
- `close` -> reuse the existing common close copy, if one exists; otherwise Cerrar -> Close

Per code (`codes.<CODE>.title` / `codes.<CODE>.text`). `{{exercise}}` is the resolved name. Proposed copy is below;
the `en` texts mirror the `es` ones.

- `WORKOUT_INCOMPLETE` -> Entrenamiento incompleto / {{recordedSets}} de {{prescribedSets}} series registradas ({{completionPercent}}). -> Incomplete workout / {{recordedSets}} of {{prescribedSets}} sets recorded ({{completionPercent}}).
- `EXERCISE_SKIPPED` -> Ejercicio no realizado / {{exercise}}: ninguna de las {{prescribedSets}} series registrada. -> Exercise skipped / {{exercise}}: none of the {{prescribedSets}} sets recorded.
- `REPS_BELOW_TARGET` -> Por debajo del objetivo / {{exercise}}: {{detail}}. -> Below target / {{exercise}}: {{detail}}.
- `REPS_ABOVE_TARGET` -> Por encima del rango / {{exercise}}: {{detail}}. -> Above range / {{exercise}}: {{detail}}.
- `ALL_TARGETS_MET` -> Todos los objetivos cumplidos / Las {{prescribedSets}} series dentro del objetivo. -> All targets met / All {{prescribedSets}} sets within target.
- `WEIGHT_INCREASE` -> Subida de carga / {{exercise}}: de {{previousMaxKg}} a {{currentMaxKg}} ({{changePercent}}). -> Load increase / …
- `WEIGHT_DECREASE` -> Bajada de carga / {{exercise}}: de {{previousMaxKg}} a {{currentMaxKg}} ({{changePercent}}). -> Load decrease / …
- `WEIGHT_PERSONAL_RECORD` -> Récord de peso / {{exercise}}: {{currentMaxKg}}, antes {{previousMaxKg}} ({{changePercent}}). -> Weight record / …
- `REPS_PERSONAL_RECORD` -> Récord de repeticiones / {{exercise}}: {{currentReps}} reps con {{weightKg}}, antes {{previousBestReps}}. -> Rep record / …
- `VOLUME_INCREASE` -> Más volumen / {{exercise}}: de {{previousVolumeKg}} a {{currentVolumeKg}} ({{changePercent}}). -> Volume increase / …
- `VOLUME_DECREASE` -> Menos volumen / {{exercise}}: de {{previousVolumeKg}} a {{currentVolumeKg}} ({{changePercent}}). -> Volume decrease / …
- `REPS_INCREASE` -> Más repeticiones / {{exercise}}: de {{previousTotalReps}} a {{currentTotalReps}} reps en {{sets}} series ({{changePercent}}). -> Rep increase / …
- `REPS_DECREASE` -> Menos repeticiones / {{exercise}}: de {{previousTotalReps}} a {{currentTotalReps}} reps en {{sets}} series ({{changePercent}}). -> Rep decrease / …
- `REPEATED_TARGET_MISS` -> Objetivo fallado repetidamente / {{exercise}}: {{consecutiveMisses}} sesiones seguidas por debajo del objetivo. -> Repeated target miss / …
- `WEIGHT_PLATEAU` -> Carga estancada / {{exercise}}: {{executions}} sesiones sin superar {{baselineMaxKg}}. -> Load plateau / …
- `WEIGHT_UPTREND` -> Tendencia al alza / {{exercise}}: {{consecutiveIncreases}} subidas seguidas, hasta {{currentWeightKg}}. -> Upward trend / …
- `WEIGHT_DOWNTREND` -> Tendencia a la baja / {{exercise}}: {{consecutiveDecreases}} bajadas seguidas, hasta {{currentWeightKg}}. -> Downward trend / …
- `baseline.weight` / `baseline.volume` / `baseline.reps` -> Referencia previa: {{value}}. -> Earlier reference: {{value}}.

Values in kg are pre-formatted with their unit ("130 kg") and percentages with their sign ("+8,3 %"), so templates
carry no units.

## Platform Capabilities And Dependencies

None.

## Decisions Taken

- One endpoint feeds both the card and the dialog. There is no separate highlights endpoint.
- The front does not rank, filter or re-prioritise. Highlights and their order come from the API, and the dialog
  keeps the API's order.
- `priority` is not shown. Only "Destacada" marks the highlights in the dialog.
- Observations are not recomputed on the front after corrections. They reflect the completion, as the API states.
- The dialog shows the same items as the card plus the rest; it is not paginated.

## Out Of Scope

- Any per-observation action (dismiss, comment, link to the exercise in the "Ejercicios" tab).
- Showing observations in the app.
- Regenerating observations from the backoffice.
- Any other block of the metrics tab, the "Ejercicios" or "Feedback" tabs, the header or the editor.

## Notes

- Verification tiers:
  - core unit tests:
    - the use case;
    - the repository method, for success and for every error code it maps.
  - helper unit tests for `workout-observations.ts`:
    - highlights resolved in rank order, with a dangling `observationId` skipped;
    - grouping, workout-level first;
    - the exercise name lookup, including no match;
    - interpolation values per code family, including list details and present/absent baselines;
    - an unknown code.
  - the backoffice component test for the metrics tab (`workout-view.test.tsx`), with fakes through the container.
    The current "shows the three sample observations marked as a preview" case is replaced by:
    - highlights in rank order in the card, with no "Vista previa";
    - the button with its count, opening the dialog with every observation grouped, highlighted ones marked;
    - closing the dialog;
    - no highlights but observations present;
    - both lists empty, with no button;
    - load error with retry.
  - translation parity keeps passing. No new E2E flow.
- The exact shape of the generated `data` type depends on the refreshed snapshot. If it comes out as an untyped
  object, the helper narrows each value structurally (number or number array) and skips the text when a needed value
  is missing.
- No deviations from the mapped references are allowed.
