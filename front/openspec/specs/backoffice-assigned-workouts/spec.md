# backoffice-assigned-workouts Specification

## Purpose

Lets trainers and admins see, in one table, the workouts they have assigned in the backoffice, as a starting point to
create new ones or open one to edit it.

## Requirements

### Requirement: Signed-in staff reach the assigned workouts screen

The backoffice SHALL offer a signed-in screen at `/assigned-workouts` with the heading "Entrenamientos", shown
with the navigation drawer and header like every signed-in screen. A visitor without a session SHALL end on `/login`.

#### Scenario: Trainer opens the screen

- **WHEN** a trainer with a stored session opens `/assigned-workouts`
- **THEN** the heading "Entrenamientos" is shown beside the navigation drawer

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/assigned-workouts`
- **THEN** the visitor ends on `/login`
- **AND** no workout request is sent

### Requirement: The screen lists the workouts outside any mesocycle

On opening, the screen SHALL request once the caller's workouts that are not placed in a mesocycle, and SHALL show one
table row per workout, in the order the API returns them. Which workouts those are is the API's answer; the screen
SHALL NOT filter, sort or hide rows itself.

#### Scenario: Two workouts returned

- **WHEN** the API returns two workouts
- **THEN** the table shows exactly two rows, in that order

#### Scenario: Workouts in a mesocycle are not asked for

- **WHEN** the screen opens
- **THEN** exactly one workout list request is sent, asking only for the workouts outside any mesocycle

### Requirement: Each row shows the workout's name, date, athlete and exercise count

Each row SHALL show the workout's name, its date as the label of the week that contains it (under the "Semana"
column), the athlete's full name as returned with the workout (their email when they have no name) and the number of
exercises in the workout. The athlete's id SHALL NOT be shown.

#### Scenario: Row content in Spanish

- **WHEN** the API returns a workout named "Pierna" dated 2026-10-27 for athlete "Ana" "García" (id `u-1`) with three
  exercises, in 2026
- **THEN** its row shows "Pierna", "Semana del 26 oct – 1 nov", "Ana García" and `3`
- **AND** `u-1` is not shown anywhere in the table

#### Scenario: Athlete without a name

- **WHEN** a workout's athlete has no name and the email `new@example.com`
- **THEN** its row shows `new@example.com` under "Atleta"

#### Scenario: Date does not shift with the time zone

- **WHEN** a workout dated 2026-10-26 is shown in a time zone behind UTC
- **THEN** its row shows "Semana del 26 oct – 1 nov", not the week before

#### Scenario: Workout without exercises

- **WHEN** a workout has no exercises
- **THEN** its exercise count shows `0`

### Requirement: Only admins see the trainer column

For an `ADMIN`, the table SHALL show a trainer column with each workout's trainer full name (their email when they
have no name), or "Sin entrenador" when the workout has none. The trainer's id SHALL NOT be shown. For a `TRAINER`, the
table SHALL NOT render the trainer column, neither its header nor its cells.

#### Scenario: Admin sees the trainer

- **WHEN** an admin opens the screen and a workout has trainer "Luis" "Pérez" (id `t-1`)
- **THEN** the table shows the header "Entrenador" and "Luis Pérez" in that workout's row
- **AND** `t-1` is not shown anywhere in the table

#### Scenario: Workout without trainer for an admin

- **WHEN** an admin opens the screen and a workout has no trainer
- **THEN** that row's trainer cell shows "Sin entrenador"

#### Scenario: Trainer does not see the trainer column

- **WHEN** a trainer opens the screen and a workout has trainer "Luis" "Pérez"
- **THEN** no "Entrenador" header is shown
- **AND** "Luis Pérez" is not shown anywhere in the table

### Requirement: The screen shows loading, empty and failure states

While the workouts load, the screen SHALL show a loading placeholder instead of the table. An empty list SHALL show the
message "Todavía no hay entrenamientos." instead of rows. A failed load SHALL show the error for its code with
a retry action that requests the workouts again.

#### Scenario: Empty list

- **WHEN** the API returns no workouts
- **THEN** "Todavía no hay entrenamientos." is shown and no workout row is rendered

#### Scenario: Load fails and the user retries

- **WHEN** the workout request fails with no response
- **THEN** the network error message and a retry action are shown
- **AND** activating retry sends the workout request again and shows the table when it succeeds

### Requirement: The screen offers a new workout button

The screen SHALL show a "Nuevo entrenamiento" button with a plus icon above the table, for both admins and trainers.
Activating it SHALL open the create workout dialog and SHALL NOT send any request by itself.

#### Scenario: Button opens the dialog

- **WHEN** a trainer clicks "Nuevo entrenamiento"
- **THEN** the create workout dialog titled "Nuevo entrenamiento" is shown
- **AND** the trainer stays on `/assigned-workouts`
- **AND** no request is sent

### Requirement: Each row offers an edit action

Each workout row SHALL end with an edit icon button labelled "Editar <workout name>" that navigates to that workout's
screen at `/workout?id=<workout id>`, for both admins and trainers.

#### Scenario: Edit a workout from the table

- **WHEN** a trainer clicks "Editar Pierna" on the row of the workout "Pierna"
- **THEN** the trainer ends on `/workout?id=<id of Pierna>`

### Requirement: The screen is translated

Every text on the screen SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: Screen in English

- **WHEN** the active language is English
- **THEN** the screen shows "Workouts", "New workout" and the column headers "Name", "Week", "Athlete" and
  "Exercises"

#### Scenario: Labels in English

- **WHEN** the active language is English and an admin sees the workout "Pierna" with no trainer
- **THEN** that row's trainer cell shows "No trainer"
- **AND** its edit action is labelled "Edit Pierna"

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
