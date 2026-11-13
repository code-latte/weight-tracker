# backoffice-workout-status Specification

## Purpose

Lets trainers and admins publish a workout or move it back to draft from the backoffice workout screen, and stops
them from editing the workout's details once the athlete has started it.

## Requirements

### Requirement: The status action follows the workout's status

The properties card SHALL offer, to the right of "Guardar", "Publicar" when the loaded workout is `DRAFT` and "Volver
a borrador" when it is `PUBLISHED`. It SHALL never offer both, and SHALL offer neither for any other status. Admins and
trainers SHALL see the same.

#### Scenario: Draft workout

- **WHEN** a trainer opens a workout whose status is `DRAFT`
- **THEN** the properties card shows "Guardar" and, to its right, "Publicar"
- **AND** "Volver a borrador" is not shown

#### Scenario: Published workout

- **WHEN** a trainer opens a workout whose status is `PUBLISHED`
- **THEN** the properties card shows "Guardar" and, to its right, "Volver a borrador"
- **AND** "Publicar" is not shown

#### Scenario: Admin sees the same action

- **WHEN** an admin opens a workout whose status is `DRAFT`
- **THEN** the properties card shows "Publicar"

### Requirement: A status change is confirmed before it is sent

Pressing "Publicar" or "Volver a borrador" SHALL open a confirmation dialog and SHALL NOT send any request.
"Cancelar", Escape or a click outside SHALL close the dialog and send nothing.

#### Scenario: Publish asks first

- **WHEN** the user clicks "Publicar" on a `DRAFT` workout
- **THEN** a dialog titled "¿Publicar el entrenamiento?" says "El atleta podrá verlo y empezarlo." and offers
  "Cancelar" and "Publicar"
- **AND** no request is sent

#### Scenario: Back to draft asks first

- **WHEN** the user clicks "Volver a borrador" on a `PUBLISHED` workout
- **THEN** a dialog titled "¿Volver a borrador?" says "El atleta dejará de verlo hasta que lo vuelvas a publicar." and
  offers "Cancelar" and "Volver a borrador"
- **AND** no request is sent

#### Scenario: Cancelled

- **WHEN** the dialog is open and the user clicks "Cancelar"
- **THEN** the dialog closes, no request is sent, and the workout keeps its status

### Requirement: Confirming sends exactly one status change

Confirming the dialog SHALL send exactly one status change of the workout, to `PUBLISHED` from the publish dialog and
to `DRAFT` from the back-to-draft dialog. While it is in flight, both dialog buttons and "Guardar" SHALL be disabled
and the dialog SHALL NOT close on Escape or a click outside. While a save is in flight, the status action SHALL be
disabled.

#### Scenario: Publish confirmed

- **WHEN** the user confirms "Publicar" on workout `w-1`
- **THEN** exactly one status change of workout `w-1` to `PUBLISHED` is sent

#### Scenario: Back to draft confirmed

- **WHEN** the user confirms "Volver a borrador" on workout `w-1`
- **THEN** exactly one status change of workout `w-1` to `DRAFT` is sent

#### Scenario: Change in progress

- **WHEN** the status change has been sent and has not answered yet
- **THEN** "Cancelar", the dialog's confirm button and "Guardar" are disabled
- **AND** pressing Escape does not close the dialog

#### Scenario: Save in progress

- **WHEN** a save of the workout has been sent and has not answered yet
- **THEN** the status action is disabled

### Requirement: The status change outcome is shown

When the API accepts the change, the dialog SHALL close, the screen SHALL take the status the API returned, and SHALL
show "Entrenamiento publicado." or "El entrenamiento ha vuelto a borrador.". When it fails, the dialog SHALL close, the
status SHALL stay as it was, and the screen SHALL show the message for the error code. Editing a field SHALL hide either
message.

#### Scenario: Published

- **WHEN** the API accepts the publish and returns the workout with status `PUBLISHED`
- **THEN** the dialog closes and "Entrenamiento publicado." is shown
- **AND** the properties card shows "Volver a borrador" and no longer shows "Publicar"

#### Scenario: Moved back to draft

- **WHEN** the API accepts the change and returns the workout with status `DRAFT`
- **THEN** "El entrenamiento ha vuelto a borrador." is shown
- **AND** the properties card shows "Publicar" and no longer shows "Volver a borrador"

#### Scenario: Rejected

- **WHEN** the user confirms "Volver a borrador" and the API answers 400
- **THEN** the dialog closes and "Revisa los datos introducidos." is shown
- **AND** the properties card still shows "Volver a borrador"
