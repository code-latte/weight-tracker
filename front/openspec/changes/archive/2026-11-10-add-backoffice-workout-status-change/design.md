# Design

## Context

- `core` already has everything this change uses: `ChangeWorkoutStatusUseCase` (`ChangeWorkoutStatusInput` =
  `{ workoutId, status }` → `Result<WorkoutDetail, DomainError>`), `WorkoutRepository.changeStatus` over
  `PUT /workouts/{workoutId}/status`, and the `WorkoutStatus` alias. The backoffice `container.ts` registers it as
  `changeWorkoutStatusUseCase`. `WorkoutById` (the screen's read) carries `status`.
- `WorkoutView`'s `LoadedWorkout` owns the details form and its save through `useUpdateWorkout`, and hands
  `WorkoutDetailsCard` a `secondaryAction` ("Guardar como plantilla") shown before "Guardar".
- `WorkoutDetailsCard` is also used by the workout template screen, which has no status.
- The backoffice already has confirmation dialogs as organisms that send nothing themselves
  (`delete-workout-template-dialog.tsx`).

See `proposal.md` for the motivation and `specs/` for the behaviour.

## Goals / Non-Goals

**Goals:**

- Add the status change to the backoffice workout screen with no change to `core`, the container or the contract.
- Keep `WorkoutDetailsCard` backwards compatible for the template screen.

**Non-Goals:**

- No change to `WorkoutExercisesEditor` or any exercise component.
- No shared presentation with the app.

## Decisions

### `core` and wiring: nothing new

`ChangeWorkoutStatusUseCase` is reused as is. No new domain alias, port, repository method, `Dependencies` key or
`container.ts` registration. The front does not decide transitions: it maps the current status to the action to
offer (`DRAFT` → `PUBLISHED`, `PUBLISHED` → `DRAFT`, anything else → locked) and sends whatever the user confirms.
That mapping is presentation, not a business rule; the API still validates the transition.

### State hook `useChangeWorkoutStatus`

`backoffice/src/presentation/workouts/hooks/use-change-workout-status.ts`, shaped like `useUpdateWorkout`:

- holds `changing: boolean`, `errorCode: DomainErrorCode | null`, `changed: WorkoutStatus | null` (the status of the
  last successful change, used to pick the success message);
- `change(workoutId, status)` resolves `changeWorkoutStatusUseCase` through the container, runs it inside
  `runWithSession`, and resolves to the returned `WorkoutDetail`'s `status`, or `null` on failure;
- `clear()` resets `changed` and `errorCode`.

Alternative considered: folding the change into `useUpdateWorkout`. Rejected: they are separate operations with
separate outcomes, and every existing hook here wraps one use case.

### View state in `LoadedWorkout`

- `status`: seeded from `workout.status`, replaced by the status `change` resolves to. It drives the action, the lock
  and the fields' `disabled`. Reset on screen exit or reload.
- `confirming: boolean`: the dialog is open. Set by the status action; cleared by Cancelar or when `change` settles
  (success or failure).
- Editing a field calls `clear()` of the status hook next to the existing `clearSaved()` and `asTemplate.clear()`.
  Starting a save clears it too, and starting a status change clears the save's `saved`, so only the latest outcome
  is shown.
- Disabling: the status action is disabled while `saving || changing`; "Guardar" receives `saving || changing`.
- The status-change alerts sit with the save alerts above the cards: `errorCode` → `errors:<code>`; `changed ===
"PUBLISHED"` → `workout.status.published`; `changed === "DRAFT"` → `workout.status.movedToDraft`.

Alternative considered: re-reading the workout after a change. Rejected: the response already carries the status,
and a re-read would reset the editor's in-progress exercise state.

### `WorkoutDetailsCard` (organism, extended)

Two optional props, so the template screen is unaffected:

- `statusAction?: ReactNode`: rendered right after "Guardar", mirroring `secondaryAction`.
- `locked?: boolean`: when true, "Guardar" and `statusAction` are not rendered; a compact `Alert` atom
  (`severity="info"`) with `workout.status.locked` is rendered at the end of the actions row in their place, after
  `secondaryAction`; and every field is disabled (as with `disabled`).

The card already translates its own copy, so it translates the banner itself.

### `ChangeWorkoutStatusDialog` (organism, new)

`backoffice/src/presentation/workouts/components/organisms/change-workout-status-dialog.tsx`, modelled on
`DeleteWorkoutTemplateDialog`: props `open`, `target: "PUBLISHED" | "DRAFT"`, `pending`, `onCancel`, `onConfirm`.
`target` selects the `publishConfirm` or `backToDraftConfirm` keys. `onClose` is ignored while `pending`. The confirm
button is `variant="contained"` with the default colour (not `error`: neither action is destructive). It uses the
`Dialog`, `DialogTitle`, `DialogContent`, `DialogActions`, `Typography` and `Button` atoms.

Alternative considered: one dialog per transition. Rejected: they differ only in copy.

### Status action in the view

Composed in `LoadedWorkout` from the `Button` atom with `color="secondary"`: `variant="contained"` for "Publicar" and
`variant="outlined"` for "Volver a borrador", with
`workout.status.publish` or `workout.status.backToDraft`, passed as `statusAction`. Not rendered when locked.

### Atomic levels and imports

- Atoms reused: `Button`, `Alert`, `Dialog`, `DialogTitle`, `DialogContent`, `DialogActions`, `Typography`,
  `TextField`. No new atom or molecule.
- Organisms: `WorkoutDetailsCard` (extended), `ChangeWorkoutStatusDialog` (new), both in
  `backoffice/src/presentation/workouts/components/organisms/`.
- View: `WorkoutView`. Imports flow view → organism → molecule → atom; only atoms import MUI.

### Translations

New keys under `workout.status` in `backoffice/src/i18n/es/workouts.json` and `en/workouts.json`:

| Key                          | es                                                            | en                                                            |
| ---------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------- |
| `publish`                    | Publicar                                                      | Publish                                                       |
| `backToDraft`                | Volver a borrador                                             | Back to draft                                                 |
| `published`                  | Entrenamiento publicado.                                      | Workout published.                                            |
| `movedToDraft`               | El entrenamiento ha vuelto a borrador.                        | Workout moved back to draft.                                  |
| `locked`                     | El atleta ha empezado este entrenamiento y no puedes editarlo | The athlete started this workout and you cannot edit it       |
| `cancel`                     | Cancelar                                                      | Cancel                                                        |
| `publishConfirm.title`       | ¿Publicar el entrenamiento?                                   | Publish the workout?                                          |
| `publishConfirm.body`        | El atleta podrá verlo y empezarlo.                            | The athlete will be able to see it and start it.              |
| `publishConfirm.confirm`     | Publicar                                                      | Publish                                                       |
| `backToDraftConfirm.title`   | ¿Volver a borrador?                                           | Move back to draft?                                           |
| `backToDraftConfirm.body`    | El atleta dejará de verlo hasta que lo vuelvas a publicar.    | The athlete will no longer see it until you publish it again. |
| `backToDraftConfirm.confirm` | Volver a borrador                                             | Back to draft                                                 |

### Errors

No new code. The endpoint answers 400 (`VALIDATION`, e.g. a transition the API refuses because the athlete started
the workout meanwhile) and 401 (`UNAUTHORIZED`, handled by `runWithSession`); transport failures map to the existing
codes. All are already translated in `errors.json` in `es` and `en`.

### Tests

- Component (`backoffice/test/presentation/workouts/views/workout-view.test.tsx`): follow its existing fakes through
  the container, adding a fake `changeWorkoutStatusUseCase` with chosen `Result` values, and vary the fixture's
  `status`. One scenario per spec scenario of `backoffice-workout-status`, plus "Started workout has no Guardar".
  Each status branch asserts what is hidden, not only what is shown.
- E2E (`backoffice/e2e/workout.spec.ts`): one flow that opens a `DRAFT` workout, publishes it through the dialog,
  then moves it back to draft, with `page.route` answering `PUT /workouts/{workoutId}/status` with the updated
  workout. It reuses the file's existing route helpers.
- No core test: `core` does not change. No change to test helpers or patterns.

## Risks / Trade-offs

- [Unsaved edits are left as they are by a status change] → the change does not touch the form values; the
  user still sees "Guardar" while the workout is `DRAFT` or `PUBLISHED`. Auto-saving is out of scope.
- [The athlete starts the workout while the trainer has the screen open] → the screen still shows the old status;
  a "Volver a borrador" or a save then fails with the API's error, and a reload shows the lock.
- [The exercises editor stays editable on a locked workout while the banner says it cannot be edited] → accepted
  by the user; a later change decides how exercises render when the workout is not editable.
