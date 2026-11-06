# Spec Delta

## ADDED Requirements

### Requirement: The mesocycle's workouts are read with it

In edit mode, the screen SHALL read the mesocycle's workouts once, after the mesocycle is shown, and SHALL show each
workout under the week whose number it carries. In create mode no workouts read SHALL be sent.

#### Scenario: Workouts read in edit mode

- **WHEN** a trainer opens `/mesocycle?id=m-1`
- **THEN** exactly one read of the workouts of mesocycle `m-1` is sent

#### Scenario: No read in create mode

- **WHEN** a trainer opens `/mesocycle`
- **THEN** no workouts read is sent

### Requirement: A week with workouts is a collapsible row

A defined week with at least one workout SHALL be a collapsible row, collapsed when the screen opens, keeping its type
chip, "Semana <n>", objective and edit action, and showing how many workouts it holds. Expanding it SHALL list its
workouts by name and a "Nuevo entrenamiento" button.

#### Scenario: Collapsed by default

- **WHEN** week 1 holds the workouts "Pierna" and "Empuje"
- **THEN** the row of "Semana 1" shows "2 entrenamientos" and does not show "Pierna"

#### Scenario: Expanded

- **WHEN** the user expands "Semana 1"
- **THEN** "Pierna", "Empuje" and "Nuevo entrenamiento" are shown inside it

#### Scenario: Edit stays reachable

- **WHEN** "Semana 1" is collapsed and the user clicks "Editar semana 1"
- **THEN** the week dialog "Semana 1" opens and the row stays collapsed

### Requirement: A week without workouts is a plain row with a create button

A defined week with no workouts SHALL NOT be collapsible and SHALL show a "Nuevo entrenamiento" button in its row.

#### Scenario: Empty week

- **WHEN** week 2 holds no workouts
- **THEN** the row of "Semana 2" offers no expand action and shows "Nuevo entrenamiento"

### Requirement: A listed workout opens its screen

Each workout listed under a week SHALL be a link to `/workout?id=<id>`.

#### Scenario: Workout opened

- **WHEN** the user expands "Semana 1" and clicks "Pierna" with id `w-1`
- **THEN** the visitor ends on `/workout?id=w-1`

### Requirement: The workouts list shows its failure without blocking the weeks

When the workouts read fails, the weeks card SHALL show the error for its code with "Reintentar", which reads them
again. Weeks SHALL stay listed and editable while the workouts are missing, without row expansion or create buttons.

#### Scenario: Read fails and the user retries

- **WHEN** the workouts read fails with no response
- **THEN** the network error message and "Reintentar" are shown in the weeks card and "Editar semana 1" still works
- **AND** when the user clicks "Reintentar" the workouts are read again and shown under their weeks

### Requirement: Nuevo entrenamiento asks only for a name and a description

"Nuevo entrenamiento" SHALL open a dialog titled "Nuevo entrenamiento · Semana <n>" with Nombre, Descripción (multiline),
"Cancelar" and "Crear". It SHALL NOT show an athlete picker, a trainer picker or a date. Nombre is required and at
most 256 characters after trimming; Descripción is at most 1024 characters. An invalid field SHALL show its message and
nothing SHALL be sent.

#### Scenario: Dialog fields

- **WHEN** the user clicks "Nuevo entrenamiento" in "Semana 2"
- **THEN** the dialog "Nuevo entrenamiento · Semana 2" shows Nombre and Descripción and no Atleta, Entrenador or date

#### Scenario: Blank name

- **WHEN** the user leaves Nombre empty and clicks "Crear"
- **THEN** Nombre shows "Este campo es obligatorio." and no request is sent

#### Scenario: Name too long

- **WHEN** Nombre has 257 characters after trimming and the user clicks "Crear"
- **THEN** Nombre shows "No puede superar los 256 caracteres." and no request is sent

### Requirement: Creating sends the workout placed in its week

When the fields are valid, "Crear" SHALL send exactly one workout creation with the trimmed name, the trimmed
description or none when blank, the mesocycle id, the week number, the mesocycle's athlete as the user, and no date.
For an `ADMIN` it SHALL also carry the mesocycle's trainer; for a `TRAINER` it SHALL NOT carry a trainer. "Crear" SHALL
be disabled while it is in progress.

#### Scenario: Trainer creates a workout in a week

- **WHEN** a trainer opens "Nuevo entrenamiento" in week 2 of mesocycle `m-1` for athlete `u-1`, types " Pierna " and
  clicks "Crear"
- **THEN** exactly one creation is sent with name "Pierna", mesocycle `m-1`, week 2, user `u-1`, no trainer, no
  description and no date

#### Scenario: Admin creates a workout in a week

- **WHEN** an admin does the same on a mesocycle of trainer `t-1`
- **THEN** the creation also carries trainer `t-1`

#### Scenario: Creation in progress

- **WHEN** the creation has been sent and has not answered yet
- **THEN** "Crear" is disabled

### Requirement: A created week workout opens its screen

When the creation succeeds, the visitor SHALL end on `/workout?id=<new id>`.

#### Scenario: Workout created

- **WHEN** the API accepts the creation with id `w-9`
- **THEN** the visitor ends on `/workout?id=w-9`

### Requirement: A failed week workout creation keeps the dialog

When the creation fails, the dialog SHALL stay open with the typed values and SHALL show the message for the error code.
"Cancelar" SHALL close the dialog without sending anything.

#### Scenario: Placement refused

- **WHEN** the API answers 409 to the creation
- **THEN** the conflict error message is shown in the dialog and Nombre keeps the typed value

#### Scenario: Cancel

- **WHEN** the user clicks "Cancelar"
- **THEN** the dialog closes and no request is sent

### Requirement: The week workouts are translated

The workout count, "Nuevo entrenamiento", the dialog and its fields SHALL be available in Spanish and English and SHALL
follow the active language.

#### Scenario: Weeks in English

- **WHEN** the active language is English and week 1 holds one workout
- **THEN** the row shows "1 workout" and, expanded, "New workout"
