# Spec Delta

## MODIFIED Requirements

### Requirement: The screen has a page header

Above the cards, the screen SHALL show the page heading "Editar entrenamiento" and a "Volver" link to
`/assigned-workouts`. The "Guardar" button that saves the workout's data SHALL sit inside the properties card, after its
fields.

#### Scenario: Header shown

- **WHEN** workout "Pierna" is loaded
- **THEN** the page heading reads "Editar entrenamiento"
- **AND** a "Volver" link leads to `/assigned-workouts`
- **AND** the properties card shows "Guardar" after Descripción

### Requirement: The screen shows loading, not found and failure states

While the workout or the catalog loads, the screen SHALL show a loading placeholder. A missing `id`, or a workout the
API does not find, SHALL show "No hemos encontrado este entrenamiento." with a "Volver a entrenamientos" link
to `/assigned-workouts`. Any other failure SHALL show the error for its code with a retry that reads again what failed.

#### Scenario: Missing id

- **WHEN** the visitor opens `/workout` without `id`
- **THEN** "No hemos encontrado este entrenamiento." and the link back to `/assigned-workouts` are shown
- **AND** no workout read is sent

#### Scenario: Unknown workout

- **WHEN** the API answers 404 for the workout
- **THEN** "No hemos encontrado este entrenamiento." and the link back are shown

#### Scenario: Catalog fails and the user retries

- **WHEN** the catalog read fails with no response and the user clicks "Reintentar"
- **THEN** the catalog is read again and shown when it succeeds
