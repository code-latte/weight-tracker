# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-workout-status-change.md`.

## Why

A workout is created as `DRAFT` and the athlete only gets it once it is `PUBLISHED`, but the backoffice offers no way
to publish it or to take it back before the athlete starts it. Once the athlete has started it, the workout screen
still offers "Guardar" for edits the trainer should no longer make. The API already exposes the status change and
`core` already wires it, so only the backoffice screen is missing.

## What Changes

- On the backoffice workout screen (`/workout?id=<id>`), the properties card's actions row offers a status action to
  the right of "Guardar", chosen from the workout's current status:
  - `DRAFT` → "Publicar", which moves the workout to `PUBLISHED`;
  - `PUBLISHED` → "Volver a borrador", which moves it back to `DRAFT`.
- Both actions ask for confirmation in a dialog first. Cancelling sends nothing. While the request is in flight the
  dialog cannot be dismissed and its buttons are disabled.
- On success the screen takes the status the API returns, so the action swaps without a reload, and a success alert
  is shown. On failure the status is kept and the error message is shown.
- `STARTED`, `COMPLETED`, `REVIEWED` → "Guardar" and the status action are not rendered; a compact info banner
  "El atleta ha empezado este entrenamiento y no puedes editarlo" takes their place, and the details fields (Nombre,
  Descripción, Semana) are disabled. "Guardar como plantilla" stays available.
- The exercises editor does not change in any status.
- Which transitions are allowed stays an API decision: the front only picks which action to offer from the status and
  shows the API's rejection as an error. No business rule is added.

## Capabilities

### New Capabilities

- `backoffice-workout-status`: changing a workout's status from the backoffice workout screen (publish, back to draft,
  confirmation, outcome) and locking the workout's details once the athlete has started it.

### Modified Capabilities

- `backoffice-workout-editor`: the requirement "The screen has a page header" states that "Guardar" always sits in
  the properties card; it now sits there only while the workout is `DRAFT` or `PUBLISHED`.

## Fronts And Roles

- backoffice → lands here → `ADMIN` and `TRAINER` behave the same: both publish, move back to draft, and see the lock
  on any workout the screen opens for them. Neither role sees "Publicar" or "Volver a borrador" on a started,
  completed or reviewed workout, nor "Guardar" on it. A `DRAFT` workout never shows "Volver a borrador"; a
  `PUBLISHED` workout never shows "Publicar".
- app → does not land here. Nothing changes for the athlete.

## Canonical References

The atom, molecule, organism, state hook and view-test rows of `.stpr/template/README.md` are `pending`; this change
follows the closest same-front files instead, with no deviations:

- `useChangeWorkoutStatus` (state hook) → `backoffice/src/presentation/workouts/hooks/use-update-workout.ts`.
- `ChangeWorkoutStatusDialog` (organism, new) →
  `backoffice/src/presentation/workouts/components/organisms/delete-workout-template-dialog.tsx`.
- `WorkoutDetailsCard` (organism, extended) → itself, which already owns the actions row and `secondaryAction`.
- Status action and alerts in `WorkoutView` (view) → the `saveAsTemplate` action and the save alerts in
  `backoffice/src/presentation/workouts/views/workout-view.tsx`.
- Reused atoms: `Button`, `Alert`, `Dialog`, `DialogTitle`, `DialogContent`, `DialogActions`, `Typography`. No new
  atom or molecule.
- Tests → `backoffice/test/presentation/workouts/views/workout-view.test.tsx` and `backoffice/e2e/workout.spec.ts`.

## Impact

- API: `PUT /workouts/{workoutId}/status` (`ChangeWorkoutStatusRequest` → `WorkoutDetailResponse`) and
  `GET /workouts/{workoutId}` (`WorkoutByIdResponse.status`), both in `core/openapi/v1.json`. No snapshot refresh.
- `core`: unchanged. `ChangeWorkoutStatusUseCase` is already registered in the backoffice `container.ts` as
  `changeWorkoutStatusUseCase`.
- backoffice: one new state hook, one new organism, `WorkoutDetailsCard` and `WorkoutView` extended, new keys in
  `src/i18n/es/workouts.json` and `src/i18n/en/workouts.json`.
- No new error code, no new dependency, no app change.
- Tests: backoffice component test for the view and one backoffice E2E flow.

## Out Of Scope

- Any change to the exercises editor in any status; how exercises render when the workout is not editable is a later
  change.
- Showing the status as a label in the screen or the lists.
- Moving a workout to `STARTED`, `COMPLETED` or `REVIEWED` from the backoffice.
- Saving unsaved edits before a status change.
