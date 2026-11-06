# Spec Delta

## ADDED Requirements

### Requirement: Duration is typed as a number

Duración (semanas) SHALL be a plain numeric text field with no increase or decrease buttons. It SHALL start at `4` in
create mode and hold the stored duration in edit mode.

#### Scenario: Duration typed

- **WHEN** the user clears Duración (semanas) and types `6`
- **THEN** Duración (semanas) holds `6`
- **AND** no "Reducir duración" or "Aumentar duración" button is shown

### Requirement: Edit mode shows who the mesocycle is for as person cards

In edit mode, between the page header and the cards, the screen SHALL show the athlete as a person card: a small card,
sized to its content, with an avatar holding the initials, the full name, or the email when there is no name, and the
caption "Atleta". Initials are the first letters of name and surname, or the email's first letter.

#### Scenario: Athlete card

- **WHEN** a trainer loads mesocycle "Hipertrofia" of athlete "Ana García"
- **THEN** a person card above "Información general" shows "AG", "Ana García" and "Atleta"

#### Scenario: Athlete without a name

- **WHEN** the mesocycle's athlete has no name and the email "ana@example.com"
- **THEN** the athlete's person card shows "A", "ana@example.com" and "Atleta"

#### Scenario: No person cards in create mode

- **WHEN** a trainer opens `/mesocycle`
- **THEN** no person card is shown

### Requirement: Only admins see the mesocycle's trainer card

In edit mode, for an `ADMIN` the screen SHALL show the mesocycle's trainer as a person card after the athlete's, with
the caption "Entrenador". For a `TRAINER`, no trainer person card, no "Entrenador" caption and no trainer name SHALL be
shown.

#### Scenario: Admin sees the trainer card

- **WHEN** an admin loads a mesocycle of trainer "Luis Pérez"
- **THEN** a person card with "LP", "Luis Pérez" and "Entrenador" is shown after the athlete's

#### Scenario: Trainer does not see the trainer card

- **WHEN** a trainer loads the same mesocycle
- **THEN** neither "Entrenador" nor "Luis Pérez" is shown

### Requirement: A week's workouts table shows each workout's exercise count

Inside an expanded week, the table SHALL have the columns "Nombre" and "Ejercicios" and one row per workout of the
week, showing its name as plain text and the number of exercises it holds.

#### Scenario: Counts shown

- **WHEN** week 1 holds "Pierna" with 3 exercises and "Empuje" with no exercises and the user expands "Semana 1"
- **THEN** the table shows a row "Pierna" with `3` and a row "Empuje" with `0`
- **AND** "Pierna" is not a link

### Requirement: Person cards and the workouts table are translated

The person cards' role captions and the workouts table's column headers and edit action SHALL be available in Spanish
and English and SHALL follow the active language.

#### Scenario: In English

- **WHEN** the active language is English, an admin loads a mesocycle and expands a week with workout "Pierna"
- **THEN** the screen shows "Athlete", "Trainer", "Name", "Exercises" and an action "Edit Pierna"

## MODIFIED Requirements

### Requirement: Información general asks for the mesocycle's data

The card SHALL show Nombre, Fecha de inicio, Duración (semanas) and Objetivo general (multiline). The first row SHALL hold Nombre, taking the remaining width, Fecha de inicio and Duración (semanas); Objetivo general SHALL fill the second row. Nombre and Objetivo general SHALL show a character counter against their
maximum. In create mode the card SHALL also show the Atleta picker before the other fields, and its save action SHALL
read "Crear mesociclo"; in edit mode it SHALL read "Guardar".

#### Scenario: Create form fields

- **WHEN** a trainer opens `/mesocycle`
- **THEN** Atleta, Nombre, Fecha de inicio, Duración (semanas) set to `4`, Objetivo general and "Crear mesociclo" are
  shown

#### Scenario: Counter

- **WHEN** the user types "Hipertrofia" in Nombre
- **THEN** the counter under Nombre reads "11 / 256"

#### Scenario: Edit card holds no names

- **WHEN** a trainer loads mesocycle "Hipertrofia" of athlete "Ana García"
- **THEN** the "Información general" card does not show "Ana García"

### Requirement: Invalid fields are reported and nothing is sent

On the save action, the card SHALL check its fields structurally and, if any fails, SHALL show the message under that
field and SHALL NOT send any request. Nombre is required and at most 256 characters after trimming; Objetivo general is
required and at most 4000 characters after trimming; Fecha de inicio is a required valid date; Duración (semanas) is a
required whole number from 1 to 52; Atleta, and Entrenador for an admin, are required in create mode.

#### Scenario: Blank required fields

- **WHEN** the user leaves Nombre, Fecha de inicio and Objetivo general empty and clicks "Crear mesociclo"
- **THEN** each of them shows "Este campo es obligatorio." and no request is sent

#### Scenario: Name too long

- **WHEN** Nombre has 257 characters after trimming and the user saves
- **THEN** Nombre shows "No puede superar los 256 caracteres." and no request is sent

#### Scenario: Objective too long

- **WHEN** Objetivo general has 4001 characters after trimming and the user saves
- **THEN** Objetivo general shows "No puede superar los 4000 caracteres." and no request is sent

#### Scenario: No athlete

- **WHEN** a trainer fills every field but Atleta and clicks "Crear mesociclo"
- **THEN** Atleta shows "Selecciona un atleta." and no request is sent

#### Scenario: Duration out of range

- **WHEN** Duración (semanas) holds `53` and the user saves
- **THEN** Duración (semanas) shows "Debe estar entre 1 y 52." and no request is sent

#### Scenario: Duration blank

- **WHEN** Duración (semanas) is empty and the user saves
- **THEN** Duración (semanas) shows "Este campo es obligatorio." and no request is sent

### Requirement: Edit mode shows the mesocycle's data

In edit mode, the general information card SHALL hold the mesocycle's name, start date, duration and objective. Who
the mesocycle is for SHALL be shown as person cards above the cards, not inside the general information card, and no
Atleta or Entrenador picker SHALL be rendered.

#### Scenario: Loaded for a trainer

- **WHEN** a trainer loads mesocycle "Hipertrofia" of athlete "Ana García" with trainer "Luis Pérez"
- **THEN** Nombre holds "Hipertrofia" and "Ana García" is shown in a person card as the athlete
- **AND** "Luis Pérez" is not shown and no Atleta or Entrenador picker is rendered

#### Scenario: Loaded for an admin

- **WHEN** an admin loads the same mesocycle
- **THEN** "Luis Pérez" is shown in a person card as the trainer

### Requirement: A week with workouts is a collapsible row

A defined week with at least one workout SHALL be a collapsible row, collapsed when the screen opens, keeping its type
chip, "Semana <n>", objective and edit action, and showing how many workouts it holds. Expanding it SHALL show a
contained "Nuevo entrenamiento" button followed by a table of its workouts.

#### Scenario: Collapsed by default

- **WHEN** week 1 holds the workouts "Pierna" and "Empuje"
- **THEN** the row of "Semana 1" shows "2 entrenamientos" and does not show "Pierna"

#### Scenario: Expanded

- **WHEN** the user expands "Semana 1"
- **THEN** "Nuevo entrenamiento" is shown inside it, before a table listing "Pierna" and "Empuje"

#### Scenario: Edit stays reachable

- **WHEN** "Semana 1" is collapsed and the user clicks "Editar semana 1"
- **THEN** the week dialog "Semana 1" opens and the row stays collapsed

### Requirement: A week without workouts is a plain row with a create button

A defined week with no workouts SHALL NOT be collapsible and SHALL show a contained "Nuevo entrenamiento" button in its
row.

#### Scenario: Empty week

- **WHEN** week 2 holds no workouts
- **THEN** the row of "Semana 2" offers no expand action and shows "Nuevo entrenamiento"

### Requirement: A listed workout opens its screen

Each row of a week's workouts table SHALL offer an edit icon button labelled "Editar <name>" that leads to
`/workout?id=<id>`.

#### Scenario: Workout opened

- **WHEN** the user expands "Semana 1" and clicks "Editar Pierna" of the workout with id `w-1`
- **THEN** the visitor ends on `/workout?id=w-1`

## REMOVED Requirements

### Requirement: Duration is chosen with a bounded stepper

**Reason**: The stepper is replaced by a plain numeric field, matching the other fields of the screen and the workout
screen.

**Migration**: See "Duration is typed as a number"; the 1–52 bounds are now reported as a field error on save, under
"Invalid fields are reported and nothing is sent".
