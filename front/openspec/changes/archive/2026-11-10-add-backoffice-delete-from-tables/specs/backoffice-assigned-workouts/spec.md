# Spec Delta

## ADDED Requirements

### Requirement: Each workout row offers a delete action

Each workout row SHALL show, after the edit action, a trash icon button labelled "Eliminar <workout name>". Admins
and trainers SHALL see it on every row, whatever the workout's status. Pressing it SHALL open the confirmation
"¿Eliminar entrenamiento?" with the text "Se eliminará el entrenamiento con sus ejercicios. Esta acción no se puede
deshacer." and the buttons "Cancelar" and "Eliminar". Pressing it SHALL NOT send any request.

#### Scenario: Trainer opens the confirmation

- **WHEN** a trainer presses "Eliminar Pierna" on the "Entrenamientos" screen
- **THEN** the dialog "¿Eliminar entrenamiento?" opens with "Cancelar" and "Eliminar"
- **AND** no deletion is sent

#### Scenario: Shown whatever the status

- **WHEN** an admin opens the screen with a `PUBLISHED` workout "Pierna"
- **THEN** its row shows both "Editar Pierna" and "Eliminar Pierna"

#### Scenario: Cancel sends nothing

- **WHEN** the user presses "Cancelar", presses Escape or clicks outside the dialog
- **THEN** the dialog closes and no deletion is sent

### Requirement: Confirming deletes the workout and refreshes the list

"Eliminar" SHALL send exactly one deletion of that row's workout. While it is in progress, both buttons SHALL be
disabled and the dialog SHALL NOT close. On success the dialog SHALL close and the screen SHALL read the workouts
again. On failure the dialog SHALL close, the screen SHALL show an error above the table, and the list SHALL stay.

#### Scenario: Workout deleted

- **WHEN** the user confirms the deletion of workout `w-1` and it succeeds
- **THEN** exactly one deletion of `w-1` is sent
- **AND** the dialog closes and the workouts are read again

#### Scenario: Deletion fails for another reason

- **WHEN** the user confirms a deletion and it fails with `FORBIDDEN`
- **THEN** the dialog closes and the message for `FORBIDDEN` is shown above the table
- **AND** the rows are unchanged

### Requirement: A refused workout deletion explains the draft rule

When a workout deletion fails with `CONFLICT`, the error above the table SHALL read "Solo se pueden eliminar
entrenamientos en borrador." instead of the generic conflict message.

#### Scenario: Workout no longer a draft

- **WHEN** the user confirms the deletion of a published workout and it fails with `CONFLICT`
- **THEN** "Solo se pueden eliminar entrenamientos en borrador." is shown above the table

### Requirement: The workout delete action is translated

The delete action's label, its dialog and the draft-only message SHALL be available in Spanish and English and SHALL
follow the active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the row action is labelled "Delete <workout name>", the dialog is titled "Delete workout?" and a refused
  deletion reads "Only draft workouts can be deleted."

### Requirement: Each workout row shows its status next to the name

Each row of the "Entrenamientos" table SHALL show, right after the workout's name, a small label with its status: "Borrador" in grey for
`DRAFT`, "Publicado" in blue (info) for `PUBLISHED`, "En curso" in amber (warning) for `STARTED`, "Completado" in green
(success) for `COMPLETED` and "Revisado" in purple (secondary) for `REVIEWED`. A status it does not know SHALL show its
raw value in grey. The labels SHALL be available in Spanish and English.

#### Scenario: Draft workout

- **WHEN** the table holds the `DRAFT` workout "Pierna"
- **THEN** its name cell shows "Pierna" followed by the grey label "Borrador"

#### Scenario: Published workout

- **WHEN** the table holds a `PUBLISHED` workout
- **THEN** its name cell shows the blue label "Publicado"

#### Scenario: English

- **WHEN** the active language is English
- **THEN** a `DRAFT` workout's label reads "Draft"
