# backoffice-assigned-plans Specification

## Purpose

Lets trainers and admins see, in one table, the workouts they have assigned in the backoffice, as a starting point to
create new ones.

## Requirements

### Requirement: Signed-in staff reach the assigned plans screen

The backoffice SHALL offer a signed-in screen at `/assigned-plans` with the heading "Planes asignados", shown with the
navigation drawer and header like every signed-in screen. A visitor without a session SHALL end on `/login`.

#### Scenario: Trainer opens the screen

- **WHEN** a trainer with a stored session opens `/assigned-plans`
- **THEN** the heading "Planes asignados" is shown beside the navigation drawer

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/assigned-plans`
- **THEN** the visitor ends on `/login`
- **AND** no workout request is sent

### Requirement: The screen lists every workout the API returns

On opening, the screen SHALL request the caller's workouts once and SHALL show one table row per workout, in the order
the API returns them. It SHALL NOT filter, sort or hide rows itself.

#### Scenario: Two workouts returned

- **WHEN** the API returns two workouts
- **THEN** the table shows exactly two rows, in that order

### Requirement: Each row shows the workout's name, date, athlete and exercise count

Each row SHALL show the workout's name, its date as the label of the week that contains it (under the "Semana"
column), the athlete's full name as returned with the workout (their email when they have no name) and the number of
exercises in the workout. The
athlete's id SHALL NOT be shown.

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

For an `ADMIN`, the table SHALL show a trainer column with each workout's trainer full name as returned with the
workout (their email when they have no name), or "Sin entrenador" when the workout has none. The trainer's id SHALL NOT be shown. For a `TRAINER`, the
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
message "Todavía no hay entrenamientos asignados." instead of rows. A failed load SHALL show the error for its code with
a retry action that requests the workouts again.

#### Scenario: Empty list

- **WHEN** the API returns no workouts
- **THEN** "Todavía no hay entrenamientos asignados." is shown and no workout row is rendered

#### Scenario: Load fails and the user retries

- **WHEN** the workout request fails with no response
- **THEN** the network error message and a retry action are shown
- **AND** activating retry sends the workout request again and shows the table when it succeeds

### Requirement: The screen offers a new workout button

The screen SHALL show a "Nuevo entrenamiento" button with a plus icon above the table, for both admins and trainers.
Activating it SHALL open the create workout dialog and SHALL NOT send any request by itself.

#### Scenario: Button shown and inert

- **WHEN** a trainer clicks "Nuevo entrenamiento"
- **THEN** the create workout dialog titled "Nuevo entrenamiento" is shown
- **AND** the trainer stays on `/assigned-plans`
- **AND** no request is sent

### Requirement: The screen is translated

Every text on the screen SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: Screen in English

- **WHEN** the active language is English
- **THEN** the screen shows "Assigned plans", "New workout" and the column headers "Name", "Week", "Athlete" and
  "Exercises"

#### Scenario: Workout without trainer in English

- **WHEN** the active language is English and an admin sees a workout with no trainer
- **THEN** that row's trainer cell shows "No trainer"

### Requirement: Each row offers an edit action

Each workout row SHALL end with an edit icon button labelled "Editar <workout name>" that navigates to that workout's
screen at `/workout?id=<workout id>`, for both admins and trainers.

#### Scenario: Edit a workout from the table

- **WHEN** a trainer clicks "Editar Pierna" on the row of the workout "Pierna"
- **THEN** the trainer ends on `/workout?id=<id of Pierna>`

#### Scenario: Edit action in English

- **WHEN** the active language is English
- **THEN** the edit action of the workout "Pierna" is labelled "Edit Pierna"
