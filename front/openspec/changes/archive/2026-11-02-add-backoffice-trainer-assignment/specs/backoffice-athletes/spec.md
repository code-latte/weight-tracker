# Spec Delta

## ADDED Requirements

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
