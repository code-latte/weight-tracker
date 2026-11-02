# backoffice-invitations Specification

## Purpose
Gives trainers a screen to follow, send and revoke their athlete invitations, and gives admins a read-only view of
every invitation, each searchable and filterable by status.

## Requirements

### Requirement: Staff reach the invitations screen

A signed-in visitor SHALL reach the invitations screen at `/invitations`, inside the session guard. A visitor without
a session SHALL be sent to login.

#### Scenario: Trainer opens the screen

- **WHEN** a signed-in trainer opens `/invitations`
- **THEN** the screen shows the heading "Invitaciones"

#### Scenario: Visitor without a session

- **WHEN** a visitor without a session opens `/invitations`
- **THEN** the visitor is sent to the login screen

### Requirement: The screen lists the invitations the API returns

On opening, the screen SHALL request the invitations once and SHALL show one table row per invitation, in the order
the API returns them, until the visitor searches or picks a status.

#### Scenario: Invitations shown

- **WHEN** the screen opens and the API returns three invitations
- **THEN** the table shows three rows in that order

### Requirement: A trainer's rows show the email, status and dates

For a trainer, each row SHALL show the invited email, the translated status (Pendiente, Aceptada, Rechazada or
Revocada), "Enviada" with the creation date and "Caduca" with the expiry date, both as `dd/mm/yyyy` in the active
language. The table SHALL NOT show an "Entrenador" column.

#### Scenario: Trainer row content

- **WHEN** a trainer's invitation to "ana@example.com", `PENDING`, created on 28 October 2026 and expiring on
  4 November 2026, is listed
- **THEN** the row shows "ana@example.com", "Pendiente", "28/10/2026" and "04/11/2026"
- **AND** there is no "Entrenador" column

### Requirement: An admin's rows show the trainer as well

For an admin, each row SHALL show "Entrenador" with the trainer's name and surname joined ("—" when both are empty),
then "Email del atleta", the translated status, "Enviada" and "Caduca".

#### Scenario: Admin row content

- **WHEN** an admin is shown an invitation from trainer "Luis" "Pérez" to "ana@example.com"
- **THEN** the row shows "Luis Pérez" in the "Entrenador" column and "ana@example.com" in "Email del atleta"

### Requirement: The search field filters by email, and by trainer for admins

One search field SHALL keep only the rows that contain the typed text, ignoring case, accents and surrounding
spaces. For a trainer it is labelled "Buscar por email" and matches the invited email. For an admin it is labelled
"Buscar por entrenador o email" and matches the trainer's full name or the invited email.

#### Scenario: Trainer searches by email

- **WHEN** a trainer types "ana@"
- **THEN** only the invitations to emails containing "ana@" are shown

#### Scenario: Admin searches by trainer

- **WHEN** an admin types "luis perez"
- **THEN** only the invitations sent by "Luis Pérez" are shown

#### Scenario: Admin searches by email

- **WHEN** an admin types "marta@"
- **THEN** only the invitations to emails containing "marta@" are shown

### Requirement: Status pills filter by status

Status pills labelled "Estado" (Pendiente, Aceptada, Rechazada, Revocada) SHALL keep only the invitations with the
selected status. Selecting the selected pill again SHALL clear the filter. Pills and search SHALL combine.

#### Scenario: Pill selected

- **WHEN** the visitor selects "Revocada"
- **THEN** only revoked invitations are shown

#### Scenario: Pill cleared

- **WHEN** "Revocada" is selected and the visitor selects it again
- **THEN** every invitation is shown again

#### Scenario: Pill and search combined

- **WHEN** the visitor selects "Pendiente" and types "ana@"
- **THEN** only pending invitations to emails containing "ana@" are shown

### Requirement: The invitations screen shows loading, empty, no-match and failure states

While the invitations load, the screen SHALL show a loading placeholder instead of the table. An empty list SHALL show
"Todavía no hay invitaciones.". Filters that match nothing SHALL show "Ninguna invitación coincide con la búsqueda.".
A failed load SHALL show the error for its code with a retry action.

#### Scenario: Empty list

- **WHEN** the API returns no invitations
- **THEN** "Todavía no hay invitaciones." is shown instead of rows

#### Scenario: No match

- **WHEN** invitations are loaded and the filters match none
- **THEN** "Ninguna invitación coincide con la búsqueda." is shown instead of rows

#### Scenario: Load fails and is retried

- **WHEN** the listing fails
- **THEN** the error for its code is shown with a retry action
- **AND** activating retry requests the invitations again

### Requirement: Only trainers can invite and revoke

A trainer SHALL see an "Invitar atleta" button and a "Revocar" action on each `PENDING` row only. An admin SHALL see
neither the button, the invite modal, any "Revocar" action nor the revoke dialog.

#### Scenario: Trainer controls

- **WHEN** a trainer is shown one pending and one accepted invitation
- **THEN** "Invitar atleta" is shown
- **AND** only the pending row has a "Revocar" action

#### Scenario: Admin sees no controls

- **WHEN** an admin is shown pending invitations
- **THEN** no "Invitar atleta" button, "Revocar" action or dialog is shown

### Requirement: The invite form checks the email before sending

"Invitar atleta" SHALL open a modal titled "Invitar atleta" with one "Email" field. Submitting it SHALL report an empty
email as "Este campo es obligatorio." and a malformed one as "Introduce un email válido.", and SHALL send nothing
while either is reported. A valid email SHALL be sent trimmed.

#### Scenario: Empty email

- **WHEN** the trainer submits the form without an email
- **THEN** "Este campo es obligatorio." is shown under the field
- **AND** no invitation is sent

#### Scenario: Email trimmed

- **WHEN** the trainer submits " ana@example.com "
- **THEN** the invitation is sent for "ana@example.com"

### Requirement: A sent invitation closes the modal and refreshes the list

When the API accepts the invitation, the modal SHALL close, "Invitación enviada." SHALL be shown and the invitations
SHALL be requested again.

#### Scenario: Invitation sent

- **WHEN** the trainer submits a valid email and the API accepts it
- **THEN** the modal closes, "Invitación enviada." is shown and the invitations are requested again

### Requirement: A failed invitation keeps the modal open

When the API refuses the invitation, the modal SHALL stay open with the typed email and SHALL show the message for the
error code. "Cancelar" SHALL close the modal without a request.

#### Scenario: Email cannot be invited

- **WHEN** the invitation fails with `CONFLICT`
- **THEN** the modal stays open with the email and shows "No se puede invitar a este email: ya tiene una invitación
  pendiente, ya entrena con alguien o no es una cuenta de atleta."

#### Scenario: Rejected by the API

- **WHEN** the invitation fails with `VALIDATION`
- **THEN** the modal stays open and shows "Introduce un email válido."

#### Scenario: Cancelled

- **WHEN** the trainer chooses "Cancelar"
- **THEN** the modal closes and no invitation is sent

### Requirement: Revoking is confirmed before it is sent

"Revocar" SHALL open a confirmation dialog titled "¿Revocar la invitación a <email>?" with a destructive "Revocar"
button, and SHALL send nothing until confirmed. "Cancelar" SHALL close it without a request.

#### Scenario: Revoke dialog

- **WHEN** the trainer chooses "Revocar" on the invitation to "ana@example.com"
- **THEN** a dialog titled "¿Revocar la invitación a ana@example.com?" is shown
- **AND** no request has been sent

#### Scenario: Revoke cancelled

- **WHEN** the dialog is open and the trainer chooses "Cancelar"
- **THEN** the dialog closes and no revoke request is sent

### Requirement: A confirmed revoke refreshes the list

Confirming SHALL revoke that invitation. On success the dialog SHALL close, "Invitación revocada." SHALL be shown and
the invitations SHALL be requested again.

#### Scenario: Invitation revoked

- **WHEN** the trainer confirms and the API accepts it
- **THEN** the dialog closes, "Invitación revocada." is shown and the invitations are requested again

### Requirement: A failed revoke keeps the dialog open

On failure the dialog SHALL stay open, show the message for the error code and keep the confirm button usable.
Closing it after a `CONFLICT` or `NOT_FOUND` failure SHALL request the invitations again.

#### Scenario: No longer pending

- **WHEN** the revoke fails with `CONFLICT`
- **THEN** the dialog stays open and shows "Esta invitación ya no está pendiente. Recarga la lista."

#### Scenario: Invitation no longer exists

- **WHEN** the revoke fails with `NOT_FOUND`
- **THEN** the dialog stays open and shows "Esta invitación ya no existe."

#### Scenario: Closed after a stale-list failure

- **WHEN** the revoke failed with `CONFLICT` and the trainer closes the dialog
- **THEN** the invitations are requested again

### Requirement: The invitations screen is translated

Every text on the screen, in the invite modal and in the revoke dialog SHALL be available in Spanish and English and
SHALL follow the active language.

#### Scenario: English

- **WHEN** the active language is English and a trainer views the screen
- **THEN** the heading reads "Invitations", the button reads "Invite athlete" and the pills read "Pending",
  "Accepted", "Rejected" and "Revoked"
