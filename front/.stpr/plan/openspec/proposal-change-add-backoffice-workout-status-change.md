# Change Proposal: Change A Workout's Status From The Backoffice Workout Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-workout-status-change/`.

## Change

Let trainers and admins move a workout between `DRAFT` and `PUBLISHED` from the backoffice workout screen, and lock
the workout's details once the athlete has started it. A `DRAFT` workout gets a **Publish** button to the right of
**Save**; a `PUBLISHED` workout gets a **Back to draft** button in the same place. Both ask for confirmation in a dialog before sending. A `STARTED`, `COMPLETED` or
`REVIEWED` workout loses its **Save** button, and a small banner takes its place: "The athlete started this workout
and you cannot edit it". The operation already exists in `core` (`ChangeWorkoutStatusUseCase`, registered in the
backoffice `container.ts`); this change only adds its presentation in the backoffice.

## Objective

A workout is created as `DRAFT` and the athlete only gets it once it is `PUBLISHED`, but the backoffice has no way
to publish it, nor to take it back while the athlete has not started it. And once the athlete starts it, the screen
still offers to save edits the trainer should no longer make. This change closes both gaps on the screen where the
trainer already prepares the workout.

## Capabilities

- The workout screen reads the workout's `status` from the `WorkoutById` it already loads, and keeps it as the
  current status of the screen.
- `DRAFT`: a **Publish** button sits to the right of **Save** in the details card's actions row. Pressing it opens a
  confirmation dialog; confirming sends `PUT /workouts/{workoutId}/status` with `status: "PUBLISHED"`.
- `PUBLISHED`: a **Back to draft** button sits in the same place. Pressing it opens a confirmation dialog;
  confirming sends the same request with `status: "DRAFT"`.
- The confirmation dialog has **Cancel** and a confirm button. Cancel, Escape or a click outside closes it and sends
  nothing. While the request is in flight both buttons are disabled and the dialog cannot be dismissed; it closes on
  success and on failure, and the outcome is shown on the screen.
- On success the screen takes the `status` from the returned `WorkoutDetail`, so the button swaps (Publish ↔ Back to
  draft) without reloading the screen, and a success alert says the workout was published / moved back to draft.
- On failure the status stays as it was and an error alert is shown above the content, like the save errors.
- While a status change is in flight, the status button and **Save** are disabled; while a save is in flight, the
  status button is disabled. Two requests never race.
- `STARTED`, `COMPLETED`, `REVIEWED`: **Save** and the status button are not rendered. In their place, in the
  actions row, a compact info banner says "The athlete started this workout and you cannot edit it". The details
  fields (name, description, week) are disabled.
- **Save as template** keeps working in every status: it copies the stored workout and does not edit it.
- Which transitions are allowed is decided by the API. The front only chooses which button to offer from the
  current status; it never validates a transition.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both publish, move back to draft, and see the lock
  banner on any workout the screen opens for them. The behaviour is the same for both roles.
- app -> lands here: no -> roles: `USER` -> nothing changes for the athlete in this change.

Neither role sees **Publish** or **Back to draft** on a started, completed or reviewed workout, nor **Save** on it.
A `DRAFT` workout never shows **Back to draft**, and a `PUBLISHED` workout never shows **Publish**.

## Classification

- `useChangeWorkoutStatus` -> state hook -> `backoffice/src/presentation/workouts/hooks/use-change-workout-status.ts`
- Status action (Publish / Back to draft button) -> composed in the view from the existing `Button` atom and passed
  to the details card -> `backoffice/src/presentation/workouts/views/workout-view.tsx`
- `ChangeWorkoutStatusDialog` -> organism (one dialog for both transitions, the target status chooses its copy; sends
  nothing itself) -> `backoffice/src/presentation/workouts/components/organisms/change-workout-status-dialog.tsx`
- Lock banner -> existing `Alert` atom (compact, `severity="info"`) rendered by the details card in place of Save ->
  `backoffice/src/presentation/workouts/components/organisms/workout-details-card.tsx`
- `WorkoutDetailsCard` -> organism (extended: a `locked` flag, the lock message, and a status action slot to the
  right of Save) -> `backoffice/src/presentation/workouts/components/organisms/`
- `LoadedWorkout` in `WorkoutView` -> view (holds the current status, wires the hook) ->
  `backoffice/src/presentation/workouts/views/workout-view.tsx`
- `ChangeWorkoutStatusUseCase`, `WorkoutRepository.changeStatus`, `ChangeWorkoutStatusInput`, `WorkoutStatus` ->
  use case / repository / entity types -> `core`, already present, not changed

## Canonical Template Mapping

- `useChangeWorkoutStatus` -> `backoffice/src/presentation/workouts/hooks/use-update-workout.ts` -> same front, same
  shape: one use case resolved from the container, `saving` / `errorCode` / success flag, returns whether it
  succeeded.
- `WorkoutDetailsCard` changes -> `backoffice/src/presentation/workouts/components/organisms/workout-details-card.tsx`
  -> the organism that already owns the actions row and the `secondaryAction` slot.
- Status button in the view -> the `saveAsTemplate` action in
  `backoffice/src/presentation/workouts/views/workout-view.tsx` -> same screen, an action composed in the view and
  handed to the card.
- `ChangeWorkoutStatusDialog` ->
  `backoffice/src/presentation/workouts/components/organisms/delete-workout-template-dialog.tsx` -> same front and
  area, a confirmation dialog with `open` / `pending` / `onCancel` / `onConfirm` that leaves the request to the screen.
- Success / error alerts -> the `saved` / `errorCode` alerts in `workout-view.tsx` -> same screen.
- View test -> `backoffice/test/presentation/workouts/views/workout-view.test.tsx` -> existing test of this view.
- E2E -> `backoffice/e2e/workout.spec.ts` -> existing flow of this screen.

The atom / molecule / organism / state hook / view test references are `pending` in `.stpr/template/README.md`;
the same-front files above are the references this change follows.

## Screens And Navigation

- backoffice -> `/workout?id=<id>` (existing) -> `WorkoutView` -> as today, from the mesocycle week or the assigned
  workouts list -> stays on the same screen on success and on every failure; no navigation changes.

## API Contract

- `PUT /workouts/{workoutId}/status` -> in `core/openapi/v1.json`: yes -> request `ChangeWorkoutStatusRequest`
  (`status: WorkoutStatus`), response `WorkoutDetailResponse` -> through `ChangeWorkoutStatusInput` /
  `WorkoutDetail`.
- `GET /workouts/{workoutId}` -> in `core/openapi/v1.json`: yes -> `WorkoutByIdResponse` already carries `status`.

No snapshot refresh is needed.

## Held State

- `LoadedWorkout` (view) -> the workout's current status, seeded from the loaded `WorkoutById` -> replaced by the
  status of a successful status change -> resets on screen exit or reload.
- `LoadedWorkout` (view) -> whether the confirmation dialog is open -> opened by the status button, closed by
  Cancel or when the request ends -> resets on screen exit.
- `useChangeWorkoutStatus` -> in-flight flag, last error code, last success -> set by the status button; cleared
  when a field changes or another action starts, like the save's flags -> resets on screen exit.

## Forms And Structural Validation

None. The status change sends no user input; the details form keeps its current validation.

## Errors And Copy

- The athlete started the workout meanwhile, or the transition is otherwise rejected -> `VALIDATION` (400) -> the
  existing `errors:VALIDATION` message in the error alert; the status stays as shown.
- Session expired -> `UNAUTHORIZED` -> handled as today by the session refresh.
- Network or unexpected failure -> the existing generic error codes and messages.

No new error code is needed in the HTTP error mapper.

- `workouts:workout.status.publish` -> "Publicar" -> "Publish"
- `workouts:workout.status.backToDraft` -> "Volver a borrador" -> "Back to draft"
- `workouts:workout.status.published` -> "Entrenamiento publicado" -> "Workout published"
- `workouts:workout.status.movedToDraft` -> "El entrenamiento ha vuelto a borrador" -> "Workout moved back to draft"
- `workouts:workout.status.publishConfirm.title` -> "¿Publicar el entrenamiento?" -> "Publish the workout?"
- `workouts:workout.status.publishConfirm.body` -> "El atleta podrá verlo y empezarlo." -> "The athlete will be able to
  see it and start it."
- `workouts:workout.status.publishConfirm.confirm` -> "Publicar" -> "Publish"
- `workouts:workout.status.backToDraftConfirm.title` -> "¿Volver a borrador?" -> "Move back to draft?"
- `workouts:workout.status.backToDraftConfirm.body` -> "El atleta dejará de verlo hasta que lo vuelvas a publicar." ->
  "The athlete will no longer see it until you publish it again."
- `workouts:workout.status.backToDraftConfirm.confirm` -> "Volver a borrador" -> "Back to draft"
- `workouts:workout.status.cancel` -> "Cancelar" -> "Cancel"
- `workouts:workout.status.locked` -> "El atleta ha empezado este entrenamiento y no puedes editarlo" -> "The athlete
  started this workout and you cannot edit it"

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- Any change to the exercises editor (add, edit, remove, reorder, super series) for started workouts. How the
  exercises render when the workout is not editable is a later change; this one must not touch the editor.
- Showing the status as a label or chip on the workout screen or in the workout lists.
- Moving a workout to `STARTED`, `COMPLETED` or `REVIEWED` from the backoffice (review is a separate flow).
- Saving unsaved edits automatically before a status change.
- Any change in `core`, in the API contract, or in the app.

## Notes

- Domain terms: `DRAFT` = being prepared, not visible to the athlete; `PUBLISHED` = sent to the athlete, not started.
  The lock covers `STARTED`, `COMPLETED` and `REVIEWED`.
- Choosing which button to show from the status is presentation, not a business rule: the API still enforces every
  transition and its rejection is shown as an error.
- Verification tiers:
  - component test (`workout-view.test.tsx`): `DRAFT` shows Publish and hides Back to draft; `PUBLISHED` shows Back
    to draft and hides Publish; each of `STARTED`, `COMPLETED`, `REVIEWED` hides Save, Publish and Back to draft,
    shows the banner and disables the fields; pressing Publish / Back to draft opens its dialog and sends nothing;
    Cancel closes it and the use case is never called; confirming a Publish swaps the button and shows the success
    alert; a failed change keeps the button and shows the error; the use case is called with the right `workoutId`
    and `status`.
  - backoffice E2E (`workout.spec.ts`): publish a draft workout and move it back to draft, confirming each dialog,
    with `page.route` intercepting `PUT /workouts/{workoutId}/status`.
  - core unit: none, `core` does not change.
- The behaviour is the same for `ADMIN` and `TRAINER`, so the component test does not split by role.
- No deviations from the mapped references are allowed.
