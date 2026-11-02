# backoffice-workout-creation Specification

## Purpose

Lets trainers and admins create a workout for an athlete from the backoffice and continue straight to that workout's
screen.

## Requirements

### Requirement: The dialog asks for the workout's data

The create workout dialog SHALL be titled "Nuevo entrenamiento" and SHALL ask for Nombre, Descripción, Semana and
Atleta, with "Cancelar" and "Crear" actions. Semana SHALL be the week field and SHALL start on the current week; Atleta
SHALL be a searchable picker with no selection; every other field SHALL start empty.

#### Scenario: Trainer opens the dialog

- **WHEN** a trainer opens the dialog during the week of 26 Oct – 1 Nov 2026
- **THEN** the fields Nombre and Descripción are shown empty
- **AND** the picker Atleta is shown with no athlete selected
- **AND** the week field Semana shows "Semana del 26 oct – 1 nov"
- **AND** the actions "Cancelar" and "Crear" are shown
- **AND** no field asks for an athlete id

### Requirement: Only admins are asked for a trainer

For an `ADMIN`, the dialog SHALL also show the picker "Entrenador", with no selection. For a `TRAINER`, that picker
SHALL NOT be rendered and the request SHALL NOT carry a trainer id.

#### Scenario: Admin sees the trainer field

- **WHEN** an admin opens the dialog
- **THEN** the picker "Entrenador" is shown with no trainer selected

#### Scenario: Trainer does not see the trainer field

- **WHEN** a trainer opens the dialog
- **THEN** no "Entrenador" picker is shown

### Requirement: Invalid fields are reported and nothing is sent

On "Crear", the dialog SHALL check each field structurally and, if any fails, SHALL show the message under that field
and SHALL NOT send any request. Nombre is required and at most 256 characters after trimming; Descripción is optional
and at most 1024 characters; Semana always holds a week, so it is never reported; Atleta, and Entrenador for an admin,
require a selected person.

#### Scenario: Empty required fields

- **WHEN** a trainer clicks "Crear" with Nombre empty and no athlete selected
- **THEN** Nombre shows "Este campo es obligatorio."
- **AND** Atleta shows "Selecciona un atleta."
- **AND** Descripción and Semana show no message
- **AND** no request is sent

#### Scenario: Blank name

- **WHEN** the name holds only spaces
- **THEN** Nombre shows "Este campo es obligatorio." and no request is sent

#### Scenario: Name too long

- **WHEN** the name has 257 characters after trimming
- **THEN** Nombre shows "No puede superar los 256 caracteres." and no request is sent

#### Scenario: Description too long

- **WHEN** the description has 1025 characters
- **THEN** Descripción shows "No puede superar los 1024 caracteres." and no request is sent

#### Scenario: Athlete id is not a UUID

- **WHEN** the selected athlete's id, as returned by the API, is not a UUID
- **THEN** Atleta shows "Introduce un identificador válido." and no request is sent

#### Scenario: Admin without trainer

- **WHEN** an admin fills every field and selects an athlete but no trainer, and clicks "Crear"
- **THEN** Entrenador shows "Selecciona un entrenador." and no request is sent

#### Scenario: Selecting clears the message

- **WHEN** Atleta shows "Selecciona un atleta." and the user selects an athlete
- **THEN** the message under Atleta is no longer shown

### Requirement: Creating sends the workout once

When every field is valid, "Crear" SHALL send exactly one workout creation with the trimmed name, the Wednesday of the
selected week as `YYYY-MM-DD`, the selected athlete's id, the description only when it is not blank, and the selected
trainer's id only for an admin. While it is in progress, "Crear" SHALL be disabled.

#### Scenario: Trainer creates a workout

- **WHEN** a trainer enters " Pierna ", no description, selects the athlete "Ana García" (id `u-1`), moves Semana to
  "Semana del 2 nov – 8 nov" and clicks "Crear"
- **THEN** exactly one creation is sent with name "Pierna", date "2026-11-04" and athlete id `u-1`
- **AND** it carries neither a description nor a trainer id

#### Scenario: Admin creates a workout

- **WHEN** an admin also enters the description "Día 1", selects the trainer "Luis Pérez" (id `t-1`) and clicks
  "Crear"
- **THEN** the creation carries that description and trainer id `t-1`

#### Scenario: Create in progress

- **WHEN** the creation has been sent and has not answered yet
- **THEN** "Crear" is disabled

### Requirement: A created workout opens its screen

When the creation succeeds, the dialog SHALL close and the user SHALL end on `/workout?id=<id of the created workout>`.

#### Scenario: Navigate after creating

- **WHEN** the API answers with the created workout whose id is `w-1`
- **THEN** the dialog is closed
- **AND** the user ends on `/workout?id=w-1`

### Requirement: A failed creation keeps the dialog

When the creation fails, the dialog SHALL stay open with the typed values and selected people and SHALL show the
message for the error code above the fields. The user SHALL be able to correct the data and try again.

#### Scenario: Unknown athlete

- **WHEN** the API answers 404
- **THEN** the dialog stays open showing "No hemos encontrado lo que buscas."
- **AND** the typed values and the selected athlete are kept

#### Scenario: Rejected data

- **WHEN** the API answers 400
- **THEN** the dialog stays open showing "Revisa los datos introducidos."

### Requirement: Cancel closes without creating

"Cancelar", Escape and a click outside the dialog SHALL close it without sending any workout creation. Opening it again
SHALL show empty fields, no selected people and no error.

#### Scenario: Cancel

- **WHEN** a trainer types a name, selects an athlete and clicks "Cancelar"
- **THEN** the dialog is closed and no workout creation is sent
- **AND** opening the dialog again shows an empty Nombre and no athlete selected

### Requirement: The dialog is translated

Every text of the dialog SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: Dialog in English

- **WHEN** the active language is English and an admin opens the dialog
- **THEN** it shows "New workout", "Name", "Description", "Week", "Athlete", "Trainer", "Cancel" and "Create"

### Requirement: A trainer picks among their current athletes

For a `TRAINER`, opening the dialog SHALL request the trainer's current athletes once, and the Atleta picker SHALL
offer exactly those athletes, in the order the API returns them.

#### Scenario: Trainer's athletes offered

- **WHEN** a trainer opens the dialog and the API returns the athletes "Ana García" and "Bea López"
- **THEN** the Atleta picker offers exactly "Ana García" and "Bea López", in that order
- **AND** no request for every user is sent

### Requirement: An admin picks among active athletes and trainers

For an `ADMIN`, opening the dialog SHALL request the users once. The Atleta picker SHALL offer the active users with
role `USER`, and the Entrenador picker the active users with role `TRAINER`, each in the order the API returns them.
Inactive users and other roles SHALL NOT be offered.

#### Scenario: Users split by role

- **WHEN** an admin opens the dialog and the API returns the active athlete "Ana García", the inactive athlete "Carla
  Ruiz", the active trainer "Luis Pérez" and the admin "Marta Gil"
- **THEN** the Atleta picker offers only "Ana García"
- **AND** the Entrenador picker offers only "Luis Pérez"
- **AND** exactly one request for the users is sent

### Requirement: Picker options show and match the person's name and email

Each option in the Atleta and Entrenador pickers SHALL read "<full name> · <email>", or only the email when the person
has no name. Typing in a picker SHALL keep the options whose full name or email contain the typed text, ignoring case
and accents, and SHALL show "Ningún atleta coincide." or "Ningún entrenador coincide." when none match.

#### Scenario: Option label

- **WHEN** the athlete "Ana García" with email `ana@example.com` is offered
- **THEN** the option reads "Ana García · ana@example.com"

#### Scenario: Person without a name

- **WHEN** an athlete with no name and email `new@example.com` is offered
- **THEN** the option reads "new@example.com"

#### Scenario: Filter by name ignoring accents

- **WHEN** the user types "garcia" in Atleta
- **THEN** "Ana García · ana@example.com" is offered

#### Scenario: Filter by email

- **WHEN** the user types "bea@" in Atleta
- **THEN** only the athlete whose email contains "bea@" is offered

#### Scenario: No match

- **WHEN** the user types "zzz" in Atleta
- **THEN** "Ningún atleta coincide." is shown

### Requirement: The pickers show loading and failure states

While the people load, the pickers SHALL be disabled. If loading fails, the dialog SHALL show the message for the error
code with a "Reintentar" action that requests the people again, and "Crear" SHALL be disabled until they load. Nombre,
Descripción and Semana SHALL stay editable meanwhile.

#### Scenario: Loading

- **WHEN** a trainer opens the dialog and the athletes have not answered yet
- **THEN** the Atleta picker is disabled
- **AND** Nombre can be typed in

#### Scenario: Load fails and the user retries

- **WHEN** the people request fails with no response
- **THEN** the network error message and "Reintentar" are shown in the dialog
- **AND** "Crear" is disabled
- **AND** activating "Reintentar" sends the people request again and enables the pickers and "Crear" when it succeeds

#### Scenario: Load forbidden

- **WHEN** the people request answers 403
- **THEN** the dialog shows the forbidden error message and "Crear" is disabled
