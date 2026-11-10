# Spec Delta

## ADDED Requirements

### Requirement: Each row of a week's workouts offers a delete action

Each row of an expanded week's workouts table SHALL show, after the edit action, a trash icon button labelled
"Eliminar <name>". Admins and trainers SHALL see it on every row, whatever the workout's status. Pressing it SHALL open
the confirmation "¿Eliminar entrenamiento?", the same one the "Entrenamientos" screen uses, and SHALL NOT send any
request.

#### Scenario: Trainer opens the confirmation

- **WHEN** a trainer expands "Semana 1" and presses "Eliminar Pierna"
- **THEN** the dialog "¿Eliminar entrenamiento?" opens with "Cancelar" and "Eliminar"
- **AND** no deletion is sent

#### Scenario: Admin sees the delete action

- **WHEN** an admin expands a week holding the workout "Pierna"
- **THEN** its row shows both "Editar Pierna" and "Eliminar Pierna"

#### Scenario: Cancel sends nothing

- **WHEN** the user presses "Cancelar", presses Escape or clicks outside the dialog
- **THEN** the dialog closes and no deletion is sent

### Requirement: Confirming deletes the workout and reads the mesocycle's workouts again

"Eliminar" SHALL send exactly one deletion of that workout, with both buttons disabled while it is in progress. On
success the dialog SHALL close and the screen SHALL read the mesocycle's workouts again. The mesocycle itself SHALL
NOT be read again. On failure the dialog SHALL close and the weeks card SHALL show the error, with the weeks unchanged.

#### Scenario: Workout deleted

- **WHEN** the user confirms the deletion of workout `w-1` and it succeeds
- **THEN** exactly one deletion of `w-1` is sent
- **AND** the dialog closes and the mesocycle's workouts are read again

#### Scenario: Last workout of a week deleted

- **WHEN** the deleted workout was the only one of "Semana 2" and the workouts are read again
- **THEN** "Semana 2" is no longer collapsible and shows its "Nuevo entrenamiento" button

#### Scenario: Deletion fails for another reason

- **WHEN** the user confirms a deletion and it fails with `FORBIDDEN`
- **THEN** the dialog closes and the weeks card shows the message for `FORBIDDEN`

### Requirement: A refused deletion in a week explains the draft rule

When a workout deletion from a week fails with `CONFLICT`, the weeks card SHALL show "Solo se pueden eliminar
entrenamientos en borrador." instead of the generic conflict message.

#### Scenario: Workout no longer a draft

- **WHEN** the user confirms the deletion of a published workout in a week and it fails with `CONFLICT`
- **THEN** the weeks card shows "Solo se pueden eliminar entrenamientos en borrador."

### Requirement: The week workout delete action is translated

The delete action's label in a week's workouts table SHALL be available in Spanish and English and SHALL follow the
active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the row action is labelled "Delete <name>"

### Requirement: Each workout of a week shows its status next to the name

Each row of an expanded week's workouts table SHALL show, right after the workout's name, a small label with its status: "Borrador" in grey for
`DRAFT`, "Publicado" in blue (info) for `PUBLISHED`, "En curso" in amber (warning) for `STARTED`, "Completado" in green
(success) for `COMPLETED` and "Revisado" in purple (secondary) for `REVIEWED`. A status it does not know SHALL show its
raw value in grey. The labels SHALL be available in Spanish and English.

#### Scenario: Draft workout

- **WHEN** an expanded week holds the `DRAFT` workout "Pierna"
- **THEN** its name cell shows "Pierna" followed by the grey label "Borrador"

#### Scenario: Published workout

- **WHEN** an expanded week holds a `PUBLISHED` workout
- **THEN** its name cell shows the blue label "Publicado"

#### Scenario: English

- **WHEN** the active language is English
- **THEN** a `DRAFT` workout's label reads "Draft"
