# Spec Delta

## MODIFIED Requirements

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

### Requirement: The screen is translated

Every text on the screen SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: Screen in English

- **WHEN** the active language is English
- **THEN** the screen shows "Assigned plans", "New workout" and the column headers "Name", "Week", "Athlete" and
  "Exercises"

#### Scenario: Workout without trainer in English

- **WHEN** the active language is English and an admin sees a workout with no trainer
- **THEN** that row's trainer cell shows "No trainer"
