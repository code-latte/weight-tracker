# Proposal

## Why

The "Observaciones de la sesión" card in the backoffice review's "Resumen y métricas" tab has shown three fixed sample
observations, marked "Vista previa", since `add-backoffice-workout-review-metrics`. The API now generates observations
when a prescribed workout is completed, and selects up to 3 of them as highlights. Both come from
`GET /workouts/{workoutId}/observations`, API change `add-workout-observations`. The trainer can now see the real key
points of the session at a glance and open the full list when they want the detail.

Source brief: `.stpr/plan/openspec/proposal-change-wire-backoffice-workout-observations.md`.

## What Changes

- **Contract sync.** Refresh `core/openapi/v1.json` from the running API and regenerate `core/src/contract/generated`,
  in a commit of its own. The refresh adds the route and `WorkoutObservationsResponse`, `WorkoutObservationResponse` and
  `WorkoutHighlightResponse`.
- **Core.**
  - Domain aliases `WorkoutObservations`, `WorkoutObservation` and `WorkoutHighlight`.
  - `WorkoutRepositoryInterface.getObservations` and its implementation.
  - `GetWorkoutObservationsUseCase`.
  - The `getWorkoutObservationsUseCase` key in `Dependencies`, registered in both fronts' `container.ts`, because both
    build `Container<Dependencies>`. Only the backoffice reads it. (The brief said "backoffice only". The previous
    summary change set this precedent of registering in both, and the shared `Dependencies` type requires it.)
- **Backoffice, "Observaciones de la sesión" card.**
  - The observations are read together with the summary, the first time the tab is opened.
  - The card lists the highlights in `rank` order, each resolved to its observation by `observationId`.
  - A "Ver todas las observaciones (N)" button opens a read-only dialog. The dialog lists every observation, grouped
    by subject in the API's order (workout first, then each exercise), and marks the highlighted ones "Destacada".
  - Each observation has a colour and icon from its `severity`, a title from its `code` and a text built from its
    `data`. An unknown code shows a generic title.
  - The card has two empty states and an inline load error with retry.
- **Removal.** The mock `session-observations.mock.ts`, the "Vista previa" chip and the sample copy keys are removed.

No breaking change.

## Capabilities

### New Capabilities

- `workout-observations`: core reads a workout's observations and highlights through the API, and returns every
  failure as a `DomainError` code.
  - Lands in `core`. Only the backoffice uses it. The app registers the key and has no screen for it.
  - Canonical reference: `GetWorkoutSummaryUseCase` and `WorkoutRepository.getSummary`, the nearest read-only `Result`
    use case over a computed workout endpoint in the same repository.

### Modified Capabilities

- `backoffice-workout-review-metrics`: the requirement "Session observations are a marked preview" is replaced. The
  card now shows the API's highlights, offers the full list in a dialog, and has empty and error states.
  - Lands in the backoffice only.
  - `ADMIN` and `TRAINER` see the same card and dialog for any workout the API returns to them. Neither role is shown
    anything the other is not, and the dialog offers no action.
  - Canonical references, all in the backoffice:
    - card and item -> the existing `session-observations-card.tsx` (organism) and `observation-item.tsx` (molecule);
    - dialog -> `exercise-video-dialog.tsx` (organism), the closest read-only dialog;
    - hook -> `use-workout-summary.ts`;
    - helpers -> `workout-review.ts`.

## Components

- Reused, unchanged: the atoms `card`, `chip`, `button`, `dialog`, `dialog-title`, `dialog-content`, `icon-button`,
  `typography`; the molecules `review-section-header` and `load-error`.
- Changed in place:
  - `ObservationItem` (molecule): its severity becomes the API's `SUCCESS | INFO | WARNING`, and it takes an
    optional trailing mark;
  - `SessionObservationsCard` (organism);
  - `WorkoutReviewMetrics` (organism).
- New: `WorkoutObservationsDialog` (organism).

## Constraints

- No business rule in the front. Eligibility, priority, ranking and the selection of highlights come from the API.
  The front only resolves ids, groups in the received order and formats values.
- No hand-written domain type. `code`, `severity`, `priority` and `data` are used as generated.
- No conflict with the template, the invariants or a stop case was found.

## Acceptance

- The card shows the highlights in rank order with no "Vista previa" mark.
- The button shows the total count and opens a dialog with every observation, grouped, with the highlighted ones marked.
- Both empty states and the load error with retry behave as specified.
- Every new string exists in `es` and `en`, and the removed keys are gone from both.
- `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. No E2E flow is added.

## Impact

- `core`: `openapi/v1.json`, `src/contract/generated`, `src/domain/workouts`, the workout repository port and
  implementation, a new use case, `src/application/dependencies.ts`, and their tests.
- `backoffice`: `container.ts`, a new hook, the review view, the metrics organism, the card, the item, a new dialog
  and helper, `es`/`en` translations, and the view's component test.
- `app`: `container.ts` registration only.
- API: depends on `add-workout-observations`, on the current API branch. No API change is requested.
