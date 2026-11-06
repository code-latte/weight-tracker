# Spec Delta

## ADDED Requirements

### Requirement: The screen lists every mesocycle the API returns

On opening, the screen SHALL request the caller's mesocycles once and SHALL show one table row per mesocycle, in the
order the API returns them. It SHALL NOT filter, sort or hide rows itself, and SHALL NOT request workouts.

#### Scenario: Two mesocycles returned

- **WHEN** the API returns two mesocycles
- **THEN** the table shows exactly two rows, in that order
- **AND** no workout request is sent

### Requirement: Each row shows the mesocycle's name, athlete, start date, duration and defined weeks

Each row SHALL show the mesocycle's name, the athlete's full name (their email when they have no name), the start date
formatted in the active language, the duration in weeks and the number of defined weeks out of the duration. The
athlete's id SHALL NOT be shown.

#### Scenario: Row content in Spanish

- **WHEN** the API returns the mesocycle "Hipertrofia" for athlete "Ana" "García" (id `u-1`), starting 2026-11-03,
  lasting 4 weeks with 2 weeks defined
- **THEN** its row shows "Hipertrofia", "Ana García", "3 nov 2026", "4 semanas" and "2 / 4"
- **AND** `u-1` is not shown anywhere in the table

#### Scenario: Athlete without a name

- **WHEN** a mesocycle's athlete has no name and the email `new@example.com`
- **THEN** its row shows `new@example.com` under "Atleta"

#### Scenario: Start date does not shift with the time zone

- **WHEN** a mesocycle starting 2026-11-03 is shown in a time zone behind UTC
- **THEN** its row shows "3 nov 2026", not the day before

#### Scenario: One-week mesocycle

- **WHEN** a mesocycle lasts 1 week
- **THEN** its duration shows "1 semana"

### Requirement: The screen offers a new mesocycle button

The screen SHALL show a "Nuevo mesociclo" button with a plus icon above the table, for both admins and trainers.
Activating it SHALL navigate to `/mesocycle` and SHALL NOT send any request by itself.

#### Scenario: Button opens the create screen

- **WHEN** a trainer clicks "Nuevo mesociclo"
- **THEN** the trainer ends on `/mesocycle`
- **AND** no mesocycle is created

### Requirement: Only admins see the mesocycle trainer column

For an `ADMIN`, the table SHALL show a trainer column with each mesocycle's trainer full name (their email when they
have no name). The trainer's id SHALL NOT be shown. For a `TRAINER`, the table SHALL NOT render the trainer column,
neither its header nor its cells.

#### Scenario: Admin sees the trainer

- **WHEN** an admin opens the screen and a mesocycle has trainer "Luis" "Pérez" (id `t-1`)
- **THEN** the table shows the header "Entrenador" and "Luis Pérez" in that mesocycle's row
- **AND** `t-1` is not shown anywhere in the table

#### Scenario: Trainer does not see the trainer column

- **WHEN** a trainer opens the screen and a mesocycle has trainer "Luis" "Pérez"
- **THEN** no "Entrenador" header is shown
- **AND** "Luis Pérez" is not shown anywhere in the table

### Requirement: The assigned plans screen is translated

Every text on the screen SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: Screen in English

- **WHEN** the active language is English
- **THEN** the screen shows "Mesocycles", "New mesocycle" and the column headers "Name", "Athlete", "Start date",
  "Duration" and "Defined weeks"

#### Scenario: Row values in English

- **WHEN** the active language is English and a mesocycle starts 2026-11-03 and lasts 4 weeks
- **THEN** its row shows "Nov 3, 2026" and "4 weeks"

### Requirement: Each mesocycle row offers an edit action

Each mesocycle row SHALL end with an edit icon button labelled "Editar <mesocycle name>" that navigates to that
mesocycle's screen at `/mesocycle?id=<mesocycle id>`, for both admins and trainers.

#### Scenario: Edit a mesocycle from the table

- **WHEN** a trainer clicks "Editar Hipertrofia" on the row of the mesocycle "Hipertrofia"
- **THEN** the trainer ends on `/mesocycle?id=<id of Hipertrofia>`

#### Scenario: Edit action in English

- **WHEN** the active language is English
- **THEN** the edit action of the mesocycle "Hipertrofia" is labelled "Edit Hipertrofia"

## MODIFIED Requirements

### Requirement: Signed-in staff reach the assigned plans screen

The backoffice SHALL offer a signed-in screen at `/assigned-plans` with the heading "Mesociclos", shown with the
navigation drawer and header like every signed-in screen. A visitor without a session SHALL end on `/login`.

#### Scenario: Trainer opens the screen

- **WHEN** a trainer with a stored session opens `/assigned-plans`
- **THEN** the heading "Mesociclos" is shown beside the navigation drawer

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/assigned-plans`
- **THEN** the visitor ends on `/login`
- **AND** no mesocycle request is sent

### Requirement: The screen shows loading, empty and failure states

While the mesocycles load, the screen SHALL show a loading placeholder instead of the table. An empty list SHALL show
the message "Todavía no hay mesociclos." instead of rows. A failed load SHALL show the error for its code with a
retry action that requests the mesocycles again.

#### Scenario: Empty list

- **WHEN** the API returns no mesocycles
- **THEN** "Todavía no hay mesociclos." is shown and no mesocycle row is rendered

#### Scenario: Load fails and the user retries

- **WHEN** the mesocycle request fails with no response
- **THEN** the network error message and a retry action are shown
- **AND** activating retry sends the mesocycle request again and shows the table when it succeeds

## REMOVED Requirements

### Requirement: The screen lists every workout the API returns

**Reason**: `/assigned-plans` now lists mesocycles.
**Migration**: The workout list moves unchanged to `/assigned-workouts` (capability `backoffice-assigned-workouts`).

### Requirement: Each row shows the workout's name, date, athlete and exercise count

**Reason**: `/assigned-plans` now lists mesocycles.
**Migration**: Moved to `backoffice-assigned-workouts`.

### Requirement: The screen offers a new workout button

**Reason**: Creating a workout belongs to the workouts list.
**Migration**: "Nuevo entrenamiento" moves to `/assigned-workouts` (capability `backoffice-assigned-workouts`).

### Requirement: Only admins see the trainer column

**Reason**: It described the workout table.
**Migration**: Moved to `backoffice-assigned-workouts`; the mesocycle table is covered by "Only admins see the mesocycle trainer column".

### Requirement: The screen is translated

**Reason**: It described the workout table.
**Migration**: Moved to `backoffice-assigned-workouts`; the mesocycle table is covered by "The assigned plans screen is translated".

### Requirement: Each row offers an edit action

**Reason**: It described the workout table.
**Migration**: Moved to `backoffice-assigned-workouts`; the mesocycle table is covered by "Each mesocycle row offers an edit action".
