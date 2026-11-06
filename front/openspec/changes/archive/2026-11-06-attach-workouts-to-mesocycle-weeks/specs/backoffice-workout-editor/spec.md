# Spec Delta

## MODIFIED Requirements

### Requirement: The screen has a page header

Above the cards, the screen SHALL show the page heading "Editar entrenamiento" and a "Volver" link. For a workout
placed in a mesocycle, "Volver" SHALL lead to `/mesocycle?id=<mesocycleId>`; for a standalone workout it SHALL lead to
`/assigned-workouts`. The "Guardar" button that saves the workout's data SHALL sit inside the properties card, after its
fields.

#### Scenario: Header shown

- **WHEN** standalone workout "Pierna" is loaded
- **THEN** the page heading reads "Editar entrenamiento"
- **AND** a "Volver" link leads to `/assigned-workouts`
- **AND** the properties card shows "Guardar" after Descripción

#### Scenario: Back to the mesocycle

- **WHEN** a workout placed in mesocycle `m-1` is loaded
- **THEN** the "Volver" link leads to `/mesocycle?id=m-1`

### Requirement: Guardar replaces the workout's editable data

When the fields are valid, "Guardar" SHALL send exactly one update of the workout with its id, the trimmed name, the
trimmed description or none when blank, the athlete and trainer exactly as they were read, and the mesocycle and week
exactly as they were read. A standalone workout SHALL carry the Wednesday of the selected week as its date; a placed
workout SHALL carry no date. While it is in progress, "Guardar" SHALL be disabled.

#### Scenario: Workout saved

- **WHEN** standalone workout `w-1` dated 2026-11-02 is loaded, the user changes the name to " Pierna B " and clicks
  "Guardar"
- **THEN** exactly one update of workout `w-1` is sent with name "Pierna B", date "2026-11-04" (the Wednesday of the
  loaded week), description "Día 1", the loaded athlete and trainer, and no mesocycle or week

#### Scenario: Week changed

- **WHEN** standalone workout `w-1` dated 2026-11-02 is loaded, the user clicks "Semana anterior" and clicks "Guardar"
- **THEN** the update carries the date "2026-10-28"

#### Scenario: Placed workout saved

- **WHEN** workout `w-2` placed in week 3 of mesocycle `m-1` is loaded, the user changes the name and clicks "Guardar"
- **THEN** the update carries mesocycle `m-1`, week 3, the loaded athlete and trainer, and no date

#### Scenario: Description cleared

- **WHEN** the user empties Descripción and clicks "Guardar"
- **THEN** the update carries no description

#### Scenario: Save in progress

- **WHEN** the update has been sent and has not answered yet
- **THEN** "Guardar" is disabled

### Requirement: The properties card shows the workout's data

The "Propiedades del entrenamiento" card SHALL hold the workout's Nombre and Descripción in editable fields and its
week in the week field Semana, filled with the loaded values, and no exercises. For a workout placed in a mesocycle,
Semana SHALL be shown but SHALL NOT be changeable. Editing the fields SHALL change nothing until "Guardar" is pressed.

#### Scenario: Card filled

- **WHEN** workout "Pierna" dated 2026-11-02 with description "Día 1" is loaded, in 2026
- **THEN** the card's fields hold "Pierna" and "Día 1", and the week field shows "Semana del 2 nov – 8 nov"
- **AND** the card lists no exercises

#### Scenario: Placed workout week is read-only

- **WHEN** a workout placed in a mesocycle week and dated 2026-11-04 is loaded
- **THEN** the week field shows "Semana del 2 nov – 8 nov"
- **AND** "Semana anterior" and "Semana siguiente" are disabled

## ADDED Requirements

### Requirement: Saving keeps the athlete and trainer shown

When an update, a reorder or a status change answers with the workout, the screen SHALL keep showing the athlete and
trainer it read when it opened.

#### Scenario: Names kept after saving

- **WHEN** a workout of athlete "Ana García" is loaded and "Guardar" succeeds
- **THEN** "Ana García" is still shown as the athlete
