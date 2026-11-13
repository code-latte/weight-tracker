# Spec Delta

## ADDED Requirements

### Requirement: The editor is shown only for draft and published workouts

Once the workout is loaded, the workout screen SHALL show the editor when the workout is `DRAFT` or `PUBLISHED`, or
has a status the backoffice does not know. For `STARTED`, `COMPLETED` and `REVIEWED`, it SHALL show the workout review
instead of the editor.

#### Scenario: Draft and published open the editor

- **WHEN** the loaded workout is `DRAFT`, or it is `PUBLISHED`
- **THEN** the page heading reads "Editar entrenamiento"

#### Scenario: Started opens the review

- **WHEN** the loaded workout is `STARTED`
- **THEN** "Editar entrenamiento" is not shown and "Revisión del entrenamiento" is shown

#### Scenario: Unknown status opens the editor

- **WHEN** the loaded workout's status is `ARCHIVED`
- **THEN** the page heading reads "Editar entrenamiento"

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

- **WHEN** a `DRAFT` workout placed in mesocycle `m-1` is loaded
- **THEN** the "Volver" link leads to `/mesocycle?id=m-1`

#### Scenario: Started workout has no Guardar

- **WHEN** a `STARTED` workout is loaded
- **THEN** the review is shown instead of the editor
- **AND** neither "Editar entrenamiento" nor "Guardar" is shown

### Requirement: The screen shows loading, not found and failure states

While the workout loads, and for an editor workout while the catalog loads, the screen SHALL show a loading
placeholder. A missing `id`, or a workout the API does not find, SHALL show "No hemos encontrado este entrenamiento."
with a "Volver a entrenamientos" link to `/assigned-workouts`. Any other failure SHALL show the error for its code with
a retry that reads again what failed. A catalog failure SHALL NOT block the review of a started workout.

#### Scenario: Missing id

- **WHEN** the visitor opens `/workout` without `id`
- **THEN** "No hemos encontrado este entrenamiento." and the link back to `/assigned-workouts` are shown
- **AND** no workout read is sent

#### Scenario: Unknown workout

- **WHEN** the API answers 404 for the workout
- **THEN** "No hemos encontrado este entrenamiento." and the link back are shown

#### Scenario: Catalog fails and the user retries

- **WHEN** the workout is `DRAFT`, the catalog read fails with no response and the user clicks "Reintentar"
- **THEN** the catalog is read again and shown when it succeeds

#### Scenario: Catalog failure does not block the review

- **WHEN** the workout is `STARTED` and the catalog read fails
- **THEN** the review is shown and no catalog error is shown
