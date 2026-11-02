# backoffice-athletes Specification

## Purpose
Gives a trainer one searchable table of their current athletes in the backoffice, so they can find any of them by
name or email.

## Requirements

### Requirement: Trainers reach the athletes screen

A signed-in visitor SHALL reach the athletes screen at `/athletes`, inside the session guard. A visitor without a
session SHALL be sent to login.

#### Scenario: Trainer opens the screen

- **WHEN** a signed-in trainer opens `/athletes`
- **THEN** the screen shows the heading "Todos los atletas"

#### Scenario: Visitor without a session

- **WHEN** a visitor without a session opens `/athletes`
- **THEN** the visitor is sent to the login screen

### Requirement: The screen lists the trainer's athletes

On opening, the screen SHALL request the trainer's athletes once and SHALL show one table row per athlete, in the
order the API returns them, until the visitor searches.

#### Scenario: Athletes shown

- **WHEN** the screen opens and the API returns three athletes
- **THEN** the table shows three rows in that order

### Requirement: Each row shows the full name, email and start date

Each row SHALL show the athlete's name and surname joined in one "Nombre completo" cell, the email, and "Atleta desde"
with the relationship's start date as `dd/mm/yyyy` in the active language. When both name and surname are empty, the
name cell SHALL show "—".

#### Scenario: Row content

- **WHEN** athlete "Luis" "Pérez", "luis@example.com", started on 3 October 2026 is listed
- **THEN** the row shows "Luis Pérez", "luis@example.com" and "03/10/2026"

#### Scenario: Athlete without a name

- **WHEN** an athlete with empty name and surname is listed
- **THEN** the "Nombre completo" cell shows "—"

### Requirement: The search field filters by full name or email

One search field labelled "Buscar por nombre o email" SHALL keep only the rows whose full name or email contains the
typed text, ignoring case and accents and surrounding spaces. Clearing it SHALL show every athlete again.

#### Scenario: Search by full name

- **WHEN** the visitor types "luis perez"
- **THEN** only "Luis Pérez" is shown

#### Scenario: Search by email

- **WHEN** the visitor types "marta@"
- **THEN** only the athlete with email "marta@example.com" is shown

#### Scenario: Search cleared

- **WHEN** the visitor clears the search
- **THEN** every athlete is shown again

### Requirement: The athletes screen shows loading, empty, no-match and failure states

While the athletes load, the screen SHALL show a loading placeholder instead of the table. An empty list SHALL show
"Todavía no tienes atletas.". A search that matches nothing SHALL show "Ningún atleta coincide con la búsqueda.". A
failed load SHALL show the error for its code with a retry action.

#### Scenario: Empty list

- **WHEN** the API returns no athletes
- **THEN** "Todavía no tienes atletas." is shown instead of rows

#### Scenario: No match

- **WHEN** athletes are loaded and the search matches none
- **THEN** "Ningún atleta coincide con la búsqueda." is shown instead of rows

#### Scenario: Load fails and is retried

- **WHEN** the listing fails
- **THEN** the error for its code is shown with a retry action
- **AND** activating retry requests the athletes again

#### Scenario: Admin opens the URL directly

- **WHEN** an admin opens `/athletes` and the listing fails with `FORBIDDEN`
- **THEN** the error for `FORBIDDEN` is shown with a retry action

### Requirement: The athletes screen is translated

Every text on the screen SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the heading reads "All athletes", the search reads "Search by name or email" and the columns read "Full
  name", "Email" and "Athlete since"

### Requirement: Each athlete row offers ending the relationship

Each row of the athletes table SHALL have an actions menu offering "Finalizar relación". The actions column SHALL have
an accessible name and no visible heading, as in the users table.

#### Scenario: Action offered

- **WHEN** a trainer opens the actions of athlete "Luis Pérez"
- **THEN** "Finalizar relación" is offered

### Requirement: Ending a relationship from the athletes screen

"Finalizar relación" SHALL open the end relationship confirmation for that athlete. Confirming SHALL end the
relationship once, for the signed-in trainer's id with the row's athlete id. On success the dialog SHALL close,
"Relación finalizada." SHALL show and the list SHALL reload.

#### Scenario: Trainer ends a relationship

- **WHEN** the signed-in trainer confirms "Finalizar relación" for athlete "Luis Pérez"
- **THEN** one end request is sent for the trainer's own id with Luis Pérez's athlete id
- **AND** the dialog closes, "Relación finalizada." is shown and the athletes are requested again

### Requirement: The athletes screen's end confirmation

The confirmation SHALL be titled "¿Finalizar la relación con {name}?", with a destructive "Finalizar" button and
"Cancelar". Cancelar, Escape or a click outside SHALL close it without a request. On failure it SHALL stay open with the
error for its code. Closing after `NOT_FOUND` or `CONFLICT` SHALL reload the list.

#### Scenario: Addressed to the trainer

- **WHEN** the trainer opens the confirmation for athlete "Luis Pérez"
- **THEN** it reads "Dejarás de entrenar a este atleta y ya no lo verás en tu lista."
- **AND** not the admin's "El atleta dejará de estar asignado a este entrenador."

#### Scenario: Cancelled

- **WHEN** the trainer presses "Cancelar"
- **THEN** the dialog closes and no request is sent

#### Scenario: Relationship no longer active

- **WHEN** ending fails with `NOT_FOUND`
- **THEN** the dialog shows "Esta relación ya no está activa. Recarga la lista."
- **AND** closing it requests the athletes again

#### Scenario: Other failure

- **WHEN** ending fails with `NETWORK`
- **THEN** the dialog stays open with the common error message for `NETWORK`

### Requirement: The end relationship flow is translated

The action, the confirmation and the success message SHALL be available in Spanish and English and SHALL follow the
active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the action reads "End relationship", the confirm button reads "End" and the success message reads
  "Relationship ended."
