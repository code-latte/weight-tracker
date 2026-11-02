# backoffice-trainer-assignment Specification

## Purpose
Gives an admin one backoffice screen listing every athlete with their current trainer, from which they assign a
trainer to an athlete without one or end the relationship of an athlete who has one.

## Requirements

### Requirement: Admins reach the trainer assignment screen

A signed-in visitor SHALL reach the trainer assignment screen at `/trainer-assignments`, inside the session guard. A
visitor without a session SHALL be sent to login.

#### Scenario: Admin opens the screen

- **WHEN** a signed-in admin opens `/trainer-assignments`
- **THEN** the screen shows the heading "Asignación de entrenadores"

#### Scenario: Visitor without a session

- **WHEN** a visitor without a session opens `/trainer-assignments`
- **THEN** the visitor is sent to the login screen

### Requirement: The screen lists only athletes

On opening, the screen SHALL request the users once and SHALL show one row per user whose role is `USER`, in the order
the API returns them, until the visitor searches or filters. Users with role `TRAINER` or `ADMIN` SHALL NOT be shown
as rows.

#### Scenario: Athletes only

- **WHEN** the API returns two `USER`s, one `TRAINER` and one `ADMIN`
- **THEN** the table shows the two athletes, in the API's order
- **AND** neither the trainer nor the admin is shown as a row

### Requirement: Each row shows the athlete and their current trainer

Each row SHALL show the athlete's full name (or "—" when name and surname are empty), the email, and in "Entrenador"
the trainer's full name as the API returns it, or "Sin entrenador" when the athlete has no trainer.

#### Scenario: Athlete with a trainer

- **WHEN** athlete "Luis Pérez" is listed with trainer "Ana García"
- **THEN** the row shows "Luis Pérez", the athlete's email and "Ana García"

#### Scenario: Athlete without a trainer

- **WHEN** an athlete is listed with no trainer
- **THEN** the "Entrenador" cell shows "Sin entrenador"

### Requirement: Each row offers exactly the action that fits its trainer

A row whose athlete has a trainer SHALL offer "Finalizar relación" and SHALL NOT offer "Asignar entrenador". A row
whose athlete has no trainer SHALL offer "Asignar entrenador" and SHALL NOT offer "Finalizar relación". Only the
trainer value returned by the API SHALL decide which.

#### Scenario: Row with a trainer

- **WHEN** the admin opens the actions of an athlete with a trainer
- **THEN** "Finalizar relación" is offered
- **AND** "Asignar entrenador" is not

#### Scenario: Row without a trainer

- **WHEN** the admin opens the actions of an athlete without a trainer
- **THEN** "Asignar entrenador" is offered
- **AND** "Finalizar relación" is not

### Requirement: The search field filters by athlete or trainer

One search field labelled "Buscar por atleta, email o entrenador" SHALL keep only the rows whose athlete full name,
athlete email or trainer full name contains the typed text, ignoring case, accents and surrounding spaces. Clearing it
SHALL show every athlete again.

#### Scenario: Search by athlete name

- **WHEN** the admin types "luis perez"
- **THEN** only "Luis Pérez" is shown

#### Scenario: Search by trainer name

- **WHEN** the admin types "garcia"
- **THEN** only the athletes whose trainer is "Ana García" are shown

#### Scenario: Search by email

- **WHEN** the admin types "marta@"
- **THEN** only the athlete with email "marta@example.com" is shown

### Requirement: Assignment pills filter by having a trainer

Two pills, "Con entrenador" and "Sin entrenador", SHALL keep only the athletes with or without a trainer. At most one
SHALL be selected; pressing the selected one again SHALL clear it. Pills and search SHALL combine.

#### Scenario: Without trainer

- **WHEN** the admin selects "Sin entrenador"
- **THEN** only athletes without a trainer are shown

#### Scenario: Pill and search combined

- **WHEN** the admin selects "Con entrenador" and types "luis"
- **THEN** only athletes with a trainer whose name, email or trainer name contains "luis" are shown

#### Scenario: Pill cleared

- **WHEN** the admin presses the selected pill again
- **THEN** every athlete matching the search is shown

### Requirement: Assigning a trainer asks for one trainer

"Asignar entrenador" SHALL open a dialog titled "Asignar entrenador a {name}" with one searchable "Entrenador" field.
Its options SHALL be the active users with role `TRAINER` from the same listing, each shown as full name and email and
filterable by either. Accepting without a selection SHALL show "Selecciona un entrenador." and SHALL NOT send a request.

#### Scenario: Options are trainers

- **WHEN** the admin opens the dialog and the listing holds two active trainers, one inactive trainer and an admin
- **THEN** exactly the two active trainers are offered

#### Scenario: Filter options

- **WHEN** the admin types "ana@" in the field
- **THEN** only the trainers whose name or email contains it are offered

#### Scenario: No selection

- **WHEN** the admin accepts without choosing a trainer
- **THEN** "Selecciona un entrenador." is shown
- **AND** no request is sent

### Requirement: Accepting the assignment links the athlete to the chosen trainer

Accepting with a trainer chosen SHALL link the row's athlete to that trainer once. On success the dialog SHALL close,
"Entrenador asignado." SHALL show and the list SHALL reload. Cancelar, Escape or a click outside SHALL close it without
a request.

#### Scenario: Trainer assigned

- **WHEN** the admin chooses "Ana García" for athlete "Luis Pérez" and accepts
- **THEN** one link request is sent for Ana García's id with Luis Pérez's id as the athlete
- **AND** the dialog closes, "Entrenador asignado." is shown and the list is requested again

#### Scenario: Assignment cancelled

- **WHEN** the admin presses "Cancelar"
- **THEN** the dialog closes and no request is sent

### Requirement: A refused assignment keeps the dialog open

When linking fails, the dialog SHALL stay open with the chosen trainer kept and SHALL show the message for the error
code: `CONFLICT`, `NOT_FOUND` and `VALIDATION` have their own messages, any other code the common error message.
Closing after a `CONFLICT` or `NOT_FOUND` SHALL reload the list.

#### Scenario: Athlete already has a trainer

- **WHEN** linking fails with `CONFLICT`
- **THEN** the dialog shows "No se puede asignar este entrenador: el atleta ya tiene uno o la relación no es válida."
- **AND** the chosen trainer is still selected

#### Scenario: Stale list closed

- **WHEN** the admin closes the dialog after a `NOT_FOUND`
- **THEN** the list is requested again

### Requirement: Ending a relationship from the assignment screen

"Finalizar relación" on a row SHALL open the end relationship confirmation for that athlete and the row's trainer. On
success the list SHALL reload and show the athlete without a trainer.

#### Scenario: Admin ends a relationship

- **WHEN** the admin confirms "Finalizar relación" for athlete "Luis Pérez" with trainer "Ana García"
- **THEN** one end request is sent for Ana García's id with Luis Pérez's id as the athlete
- **AND** the dialog closes, "Relación finalizada." is shown and the list is requested again

### Requirement: The end relationship confirmation

The end relationship confirmation SHALL be titled "¿Finalizar la relación con {name}?" with a destructive "Finalizar"
button and "Cancelar". Cancelar, Escape or a click outside SHALL close it without a request. On failure it SHALL stay
open with the error for its code. Closing after `NOT_FOUND` or `CONFLICT` SHALL reload the list.

#### Scenario: Cancelled

- **WHEN** the admin presses "Cancelar"
- **THEN** the dialog closes and no request is sent

#### Scenario: Relationship no longer active

- **WHEN** ending fails with `NOT_FOUND`
- **THEN** the dialog shows "Esta relación ya no está activa. Recarga la lista."
- **AND** closing it requests the list again

### Requirement: The assignment screen shows loading, empty, no-match and failure states

While the users load, the screen SHALL show a loading placeholder instead of the table. No athletes SHALL show
"Todavía no hay atletas.". A search or pill that matches nothing SHALL show "Ningún atleta coincide con la
búsqueda.". A failed load SHALL show the error for its code with a retry action.

#### Scenario: No athletes

- **WHEN** the API returns only trainers and admins
- **THEN** "Todavía no hay atletas." is shown instead of rows

#### Scenario: No match

- **WHEN** athletes are loaded and the search matches none
- **THEN** "Ningún atleta coincide con la búsqueda." is shown instead of rows

#### Scenario: Trainer opens the URL directly

- **WHEN** a trainer opens `/trainer-assignments` and the listing fails with `FORBIDDEN`
- **THEN** the error for `FORBIDDEN` is shown with a retry action
- **AND** no assign or end action is shown

### Requirement: The assignment screen is translated

Every text on the screen and in its dialogs SHALL be available in Spanish and English and SHALL follow the active
language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the heading reads "Trainer assignment", the columns read "Full name", "Email" and "Trainer", and a row
  without a trainer reads "No trainer"
