# Spec Delta

## MODIFIED Requirements

### Requirement: The screen has a page header

Above the cards, the screen SHALL show the page heading "Editar entrenamiento" and a "Volver" link. For a workout
placed in a mesocycle, "Volver" SHALL lead to `/mesocycle?id=<mesocycleId>`; for a standalone workout it SHALL lead to
`/assigned-workouts`. While the workout is `DRAFT` or `PUBLISHED`, the "Guardar" button that saves the workout's data
SHALL sit inside the properties card, after its fields; for any other status it SHALL NOT be shown.

#### Scenario: Header shown

- **WHEN** standalone `DRAFT` workout "Pierna" is loaded
- **THEN** the page heading reads "Editar entrenamiento"
- **AND** a "Volver" link leads to `/assigned-workouts`
- **AND** the properties card shows "Guardar" after Descripción

#### Scenario: Back to the mesocycle

- **WHEN** a workout placed in mesocycle `m-1` is loaded
- **THEN** the "Volver" link leads to `/mesocycle?id=m-1`

#### Scenario: Started workout has no Guardar

- **WHEN** a `STARTED` workout is loaded
- **THEN** the page heading and the "Volver" link are shown
- **AND** the properties card does not show "Guardar"
