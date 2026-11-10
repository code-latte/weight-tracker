# backoffice-mesocycle-editor Specification

## Purpose

Lets trainers and admins create a mesocycle for an athlete in the backoffice, edit its general information and plan
its weeks, on a screen that shares the layout of the workout screen.

## Requirements

### Requirement: The mesocycle screen opens in create or edit mode

The backoffice SHALL offer a signed-in screen at `/mesocycle`. Without `id` it SHALL open in create mode; with
`?id=<id>` it SHALL read that mesocycle once and open in edit mode. A visitor without a session SHALL end on `/login`.

#### Scenario: Create mode

- **WHEN** a trainer opens `/mesocycle`
- **THEN** the create form is shown with empty fields
- **AND** no mesocycle read is sent

#### Scenario: Edit mode

- **WHEN** a trainer opens `/mesocycle?id=m-1`
- **THEN** exactly one read of mesocycle `m-1` is sent and its data is shown

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/mesocycle?id=m-1`
- **THEN** the visitor ends on `/login`
- **AND** no mesocycle read is sent

### Requirement: The screen has a page header

Above the cards, the screen SHALL show the heading "Crear mesociclo" in create mode or "Editar mesociclo" in edit
mode, the line "Define el objetivo del mesociclo y planifica sus semanas" and a "Volver" link to `/assigned-plans`.

#### Scenario: Header in create mode

- **WHEN** the visitor opens `/mesocycle`
- **THEN** the heading reads "Crear mesociclo"
- **AND** a "Volver" link leads to `/assigned-plans`

#### Scenario: Header in edit mode

- **WHEN** mesocycle `m-1` is loaded
- **THEN** the heading reads "Editar mesociclo"

### Requirement: The screen shows two stacked cards at full width

The screen SHALL use the full width beside the navigation drawer and SHALL show two cards, one above the other and each
taking the full width: "Información general" on top and "Planificación semanal" below. The page SHALL NOT scroll
horizontally.

#### Scenario: Cards in order

- **WHEN** the screen is shown in either mode
- **THEN** "Información general" is shown above "Planificación semanal", both at full width

### Requirement: Información general asks for the mesocycle's data

The card SHALL show Nombre, Fecha de inicio, Duración (semanas) and Objetivo general (multiline). The first row SHALL hold Nombre, taking the remaining width, Fecha de inicio and Duración (semanas); Objetivo general SHALL fill the second row. Nombre and Objetivo general SHALL show a character counter against their
maximum. In create mode the card SHALL also show the Atleta picker before the other fields, and its save action SHALL
read "Crear mesociclo"; in edit mode it SHALL read "Guardar".

#### Scenario: Create form fields

- **WHEN** a trainer opens `/mesocycle`
- **THEN** Atleta, Nombre, Fecha de inicio, Duración (semanas) set to `4`, Objetivo general and "Crear mesociclo" are
  shown

#### Scenario: Counter

- **WHEN** the user types "Hipertrofia" in Nombre
- **THEN** the counter under Nombre reads "11 / 256"

#### Scenario: Edit card holds no names

- **WHEN** a trainer loads mesocycle "Hipertrofia" of athlete "Ana García"
- **THEN** the "Información general" card does not show "Ana García"

### Requirement: Duration is typed as a number

Duración (semanas) SHALL be a plain numeric text field with no increase or decrease buttons. It SHALL start at `4` in
create mode and hold the stored duration in edit mode.

#### Scenario: Duration typed

- **WHEN** the user clears Duración (semanas) and types `6`
- **THEN** Duración (semanas) holds `6`
- **AND** no "Reducir duración" or "Aumentar duración" button is shown

### Requirement: Only admins are asked for a trainer

In create mode, for an `ADMIN` the card SHALL show an Entrenador picker after Atleta, and the creation SHALL carry the
chosen trainer. For a `TRAINER` the Entrenador picker SHALL NOT be rendered and the creation SHALL NOT carry a trainer.

#### Scenario: Admin sees the trainer picker

- **WHEN** an admin opens `/mesocycle`
- **THEN** the Entrenador picker is shown after Atleta

#### Scenario: Trainer does not see the trainer picker

- **WHEN** a trainer opens `/mesocycle`
- **THEN** no Entrenador picker is shown
- **AND** a valid creation is sent without a trainer

### Requirement: The pickers offer the same people as the create workout dialog

For a `TRAINER`, the Atleta picker SHALL offer the trainer's current athletes. For an `ADMIN`, Atleta SHALL offer the
active users with role `USER` and Entrenador the active users with role `TRAINER`. Each option SHALL show and match the
person's name and email. While the people load the pickers SHALL be disabled; a failure SHALL show its message with
"Reintentar" and disable "Crear mesociclo" until they load.

#### Scenario: Trainer's athletes offered

- **WHEN** a trainer opens `/mesocycle` and the API returns the athletes "Ana García" and "Bea López"
- **THEN** the Atleta picker offers exactly "Ana García" and "Bea López", in that order

#### Scenario: People fail to load

- **WHEN** the people request fails with no response
- **THEN** the network error message and "Reintentar" are shown
- **AND** "Crear mesociclo" is disabled

### Requirement: Invalid fields are reported and nothing is sent

On the save action, the card SHALL check its fields structurally and, if any fails, SHALL show the message under that
field and SHALL NOT send any request. Nombre is required and at most 256 characters after trimming; Objetivo general is
required and at most 4000 characters after trimming; Fecha de inicio is a required valid date; Duración (semanas) is a
required whole number from 1 to 52; Atleta, and Entrenador for an admin, are required in create mode.

#### Scenario: Blank required fields

- **WHEN** the user leaves Nombre, Fecha de inicio and Objetivo general empty and clicks "Crear mesociclo"
- **THEN** each of them shows "Este campo es obligatorio." and no request is sent

#### Scenario: Name too long

- **WHEN** Nombre has 257 characters after trimming and the user saves
- **THEN** Nombre shows "No puede superar los 256 caracteres." and no request is sent

#### Scenario: Objective too long

- **WHEN** Objetivo general has 4001 characters after trimming and the user saves
- **THEN** Objetivo general shows "No puede superar los 4000 caracteres." and no request is sent

#### Scenario: No athlete

- **WHEN** a trainer fills every field but Atleta and clicks "Crear mesociclo"
- **THEN** Atleta shows "Selecciona un atleta." and no request is sent

#### Scenario: Duration out of range

- **WHEN** Duración (semanas) holds `53` and the user saves
- **THEN** Duración (semanas) shows "Debe estar entre 1 y 52." and no request is sent

#### Scenario: Duration blank

- **WHEN** Duración (semanas) is empty and the user saves
- **THEN** Duración (semanas) shows "Este campo es obligatorio." and no request is sent

### Requirement: Creating sends the mesocycle once and opens it

When the fields are valid, "Crear mesociclo" SHALL send exactly one creation with the athlete, the trainer for an
admin, the trimmed name and objective, the start date and the duration, and SHALL be disabled while it is in progress.
On success the visitor SHALL end on `/mesocycle?id=<new id>` in edit mode, without a new history entry.

#### Scenario: Trainer creates a mesocycle

- **WHEN** a trainer picks "Ana García", types " Hipertrofia ", 2026-11-03, duration 4 and the objective "Ganar masa"
  and clicks "Crear mesociclo"
- **THEN** exactly one creation is sent with Ana's id, name "Hipertrofia", objective "Ganar masa", start date
  "2026-11-03", duration 4 and no trainer
- **AND** when it succeeds with id `m-1`, the visitor ends on `/mesocycle?id=m-1` with the heading "Editar mesociclo"

#### Scenario: Creation in progress

- **WHEN** the creation has been sent and has not answered yet
- **THEN** "Crear mesociclo" is disabled

### Requirement: A failed creation keeps the form

When the creation fails, the screen SHALL show the message for the error code in the general information card, SHALL
keep the typed values and SHALL stay in create mode.

#### Scenario: Creation forbidden

- **WHEN** the API answers 403 to the creation
- **THEN** "No tienes permiso para realizar esta acción." is shown
- **AND** the fields keep the typed values and the visitor stays on `/mesocycle`

### Requirement: Weekly planning is locked until the mesocycle exists

In create mode, the "Planificación semanal" card SHALL show "Guarda el mesociclo para planificar sus semanas." and its
"Añadir semana" button SHALL be disabled.

#### Scenario: Create mode weeks card

- **WHEN** a trainer opens `/mesocycle`
- **THEN** the weeks card shows "Guarda el mesociclo para planificar sus semanas."
- **AND** "Añadir semana" is disabled

### Requirement: Edit mode shows the mesocycle's data

In edit mode, the general information card SHALL hold the mesocycle's name, start date, duration and objective. Who
the mesocycle is for SHALL be shown as person cards above the cards, not inside the general information card, and no
Atleta or Entrenador picker SHALL be rendered.

#### Scenario: Loaded for a trainer

- **WHEN** a trainer loads mesocycle "Hipertrofia" of athlete "Ana García" with trainer "Luis Pérez"
- **THEN** Nombre holds "Hipertrofia" and "Ana García" is shown in a person card as the athlete
- **AND** "Luis Pérez" is not shown and no Atleta or Entrenador picker is rendered

#### Scenario: Loaded for an admin

- **WHEN** an admin loads the same mesocycle
- **THEN** "Luis Pérez" is shown in a person card as the trainer

### Requirement: Guardar replaces the mesocycle's general data

In edit mode, when the fields are valid, "Guardar" SHALL send exactly one update of the mesocycle with its id and the
trimmed name, the trimmed objective, the start date and the duration. While it is in progress, "Guardar" SHALL be
disabled.

#### Scenario: Mesocycle saved

- **WHEN** mesocycle `m-1` is loaded, the user changes the duration to 6 and clicks "Guardar"
- **THEN** exactly one update of `m-1` is sent with its name, objective, start date and duration 6

### Requirement: The save outcome is shown

When the update succeeds, the screen SHALL show "Mesociclo guardado." and the fields SHALL hold the values the API
returned. Editing a field again SHALL hide the confirmation. When it fails, the screen SHALL show the message for the
error code and keep the typed values.

#### Scenario: Saved

- **WHEN** the API accepts the update
- **THEN** "Mesociclo guardado." is shown

#### Scenario: Duration shorter than a defined week

- **WHEN** week 4 is defined, the user sets the duration to 3, clicks "Guardar" and the API answers 409
- **THEN** the conflict error message is shown
- **AND** Duración keeps `3`

### Requirement: Defined weeks are listed in order

In edit mode, the weeks card SHALL list each defined week in ascending week number with its type label as a coloured
chip, "Semana <n>", its objective and an edit icon button labelled "Editar semana <n>". With no weeks defined it SHALL
show "Todavía no hay semanas definidas.".

#### Scenario: Weeks listed

- **WHEN** the mesocycle has week 2 (ACCUMULATION, "Subir volumen") and week 1 (ADAPTATION, "Cargas iniciales")
- **THEN** the card lists "Adaptación", "Semana 1", "Cargas iniciales" first and "Acumulación", "Semana 2", "Subir
  volumen" second

#### Scenario: No weeks

- **WHEN** the mesocycle has no weeks
- **THEN** "Todavía no hay semanas definidas." is shown

### Requirement: Añadir semana defines the next free week

In edit mode, "Añadir semana" SHALL open the week dialog titled "Semana <n>", where n is the lowest number from 1 to
the loaded duration that has no week yet. When every week up to the duration is defined, "Añadir semana" SHALL be
disabled.

#### Scenario: Gap filled first

- **WHEN** the mesocycle lasts 4 weeks with weeks 1 and 3 defined and the user clicks "Añadir semana"
- **THEN** the dialog titled "Semana 2" opens with an empty type and objective

#### Scenario: All weeks defined

- **WHEN** the mesocycle lasts 2 weeks with weeks 1 and 2 defined
- **THEN** "Añadir semana" is disabled

### Requirement: The week dialog asks for a type and an objective

The week dialog SHALL show Tipo de semana, offering Adaptación, Acumulación, Intensificación, Realización, Descarga and
Recuperación, and Objetivo (multiline, at most 4000 characters after trimming), plus "Cancelar" and "Guardar". Both
fields are required; an invalid field SHALL show its message and nothing SHALL be sent.

#### Scenario: Missing type

- **WHEN** the user types an objective, leaves Tipo de semana empty and clicks "Guardar"
- **THEN** Tipo de semana shows "Este campo es obligatorio." and no request is sent

### Requirement: Saving a new week defines it

From "Añadir semana", a valid "Guardar" SHALL send exactly one week definition with the dialog's week number, type and
trimmed objective. On success the dialog SHALL close and the list SHALL show the weeks the API returned.

#### Scenario: Week defined

- **WHEN** the dialog "Semana 2" is saved with Acumulación and " Subir volumen "
- **THEN** exactly one definition is sent with week 2, type ACCUMULATION and objective "Subir volumen"
- **AND** the dialog closes and "Semana 2" appears in the list

### Requirement: Editing a week redefines it

The edit action of a week SHALL open the week dialog titled "Semana <n>" with that week's type and objective. A valid
"Guardar" SHALL send exactly one redefinition of that week number with the type and trimmed objective. On success the
dialog SHALL close and the list SHALL show the weeks the API returned.

#### Scenario: Week redefined

- **WHEN** the user clicks "Editar semana 1", changes the type to Descarga and clicks "Guardar"
- **THEN** exactly one redefinition of week 1 is sent with type DELOAD and the unchanged objective
- **AND** the list shows "Descarga" for "Semana 1"

### Requirement: A failed week save keeps the dialog

When a week definition or redefinition fails, the dialog SHALL stay open with the typed values and SHALL show the
message for the error code. "Guardar" SHALL be disabled while the request is in progress. "Cancelar" SHALL close the
dialog without sending anything.

#### Scenario: Duplicate week

- **WHEN** the API answers 409 to a week definition
- **THEN** the conflict error message is shown in the dialog and the typed values are kept

#### Scenario: Cancel

- **WHEN** the user clicks "Cancelar" in the week dialog
- **THEN** the dialog closes and no request is sent

### Requirement: The screen shows loading, not found and failure states

In edit mode, while the mesocycle loads, the screen SHALL show a loading placeholder. A mesocycle the API does not find
SHALL show "No hemos encontrado este mesociclo." with a "Volver a mesociclos" link to `/assigned-plans`. Any
other read failure SHALL show the error for its code with a retry that reads it again.

#### Scenario: Unknown mesocycle

- **WHEN** the API answers 404 for the mesocycle
- **THEN** "No hemos encontrado este mesociclo." and the link back to `/assigned-plans` are shown

#### Scenario: Read fails and the user retries

- **WHEN** the mesocycle read fails with no response and the user clicks "Reintentar"
- **THEN** the mesocycle is read again and shown when it succeeds

### Requirement: The mesocycle screen is translated

Every text of the screen and its dialog, including the week type labels, SHALL be available in Spanish and English and
SHALL follow the active language.

#### Scenario: Screen in English

- **WHEN** the active language is English
- **THEN** the screen shows "Create mesocycle", "General information", "Weekly planning", "Add week" and the week type
  "Deload"

### Requirement: The mesocycle's workouts are read with it

In edit mode, the screen SHALL read the mesocycle's workouts once, after the mesocycle is shown, and SHALL show each
workout under the week whose number it carries. In create mode no workouts read SHALL be sent.

#### Scenario: Workouts read in edit mode

- **WHEN** a trainer opens `/mesocycle?id=m-1`
- **THEN** exactly one read of the workouts of mesocycle `m-1` is sent

#### Scenario: No read in create mode

- **WHEN** a trainer opens `/mesocycle`
- **THEN** no workouts read is sent

### Requirement: A week with workouts is a collapsible row

A defined week with at least one workout SHALL be a collapsible row, collapsed when the screen opens, keeping its type
chip, "Semana <n>", objective and edit action, and showing how many workouts it holds. Expanding it SHALL show a
contained "Nuevo entrenamiento" button followed by a table of its workouts.

#### Scenario: Collapsed by default

- **WHEN** week 1 holds the workouts "Pierna" and "Empuje"
- **THEN** the row of "Semana 1" shows "2 entrenamientos" and does not show "Pierna"

#### Scenario: Expanded

- **WHEN** the user expands "Semana 1"
- **THEN** "Nuevo entrenamiento" is shown inside it, before a table listing "Pierna" and "Empuje"

#### Scenario: Edit stays reachable

- **WHEN** "Semana 1" is collapsed and the user clicks "Editar semana 1"
- **THEN** the week dialog "Semana 1" opens and the row stays collapsed

### Requirement: A week without workouts is a plain row with a create button

A defined week with no workouts SHALL NOT be collapsible and SHALL show a contained "Nuevo entrenamiento" button in its
row.

#### Scenario: Empty week

- **WHEN** week 2 holds no workouts
- **THEN** the row of "Semana 2" offers no expand action and shows "Nuevo entrenamiento"

### Requirement: A listed workout opens its screen

Each row of a week's workouts table SHALL offer an edit icon button labelled "Editar <name>" that leads to
`/workout?id=<id>`.

#### Scenario: Workout opened

- **WHEN** the user expands "Semana 1" and clicks "Editar Pierna" of the workout with id `w-1`
- **THEN** the visitor ends on `/workout?id=w-1`

### Requirement: The workouts list shows its failure without blocking the weeks

When the workouts read fails, the weeks card SHALL show the error for its code with "Reintentar", which reads them
again. Weeks SHALL stay listed and editable while the workouts are missing, without row expansion or create buttons.

#### Scenario: Read fails and the user retries

- **WHEN** the workouts read fails with no response
- **THEN** the network error message and "Reintentar" are shown in the weeks card and "Editar semana 1" still works
- **AND** when the user clicks "Reintentar" the workouts are read again and shown under their weeks

### Requirement: Nuevo entrenamiento asks only for a name and a description

"Nuevo entrenamiento" SHALL open a dialog titled "Nuevo entrenamiento · Semana <n>" with Nombre, Descripción (multiline),
"Cancelar" and "Crear". It SHALL NOT show an athlete picker, a trainer picker or a date. Nombre is required and at
most 256 characters after trimming; Descripción is at most 1024 characters. An invalid field SHALL show its message and
nothing SHALL be sent.

#### Scenario: Dialog fields

- **WHEN** the user clicks "Nuevo entrenamiento" in "Semana 2"
- **THEN** the dialog "Nuevo entrenamiento · Semana 2" shows Nombre and Descripción and no Atleta, Entrenador or date

#### Scenario: Blank name

- **WHEN** the user leaves Nombre empty and clicks "Crear"
- **THEN** Nombre shows "Este campo es obligatorio." and no request is sent

#### Scenario: Name too long

- **WHEN** Nombre has 257 characters after trimming and the user clicks "Crear"
- **THEN** Nombre shows "No puede superar los 256 caracteres." and no request is sent

### Requirement: Creating sends the workout placed in its week

When the fields are valid, "Crear" SHALL send exactly one workout creation with the trimmed name, the trimmed
description or none when blank, the mesocycle id, the week number, the mesocycle's athlete as the user, and no date.
For an `ADMIN` it SHALL also carry the mesocycle's trainer; for a `TRAINER` it SHALL NOT carry a trainer. "Crear" SHALL
be disabled while it is in progress.

#### Scenario: Trainer creates a workout in a week

- **WHEN** a trainer opens "Nuevo entrenamiento" in week 2 of mesocycle `m-1` for athlete `u-1`, types " Pierna " and
  clicks "Crear"
- **THEN** exactly one creation is sent with name "Pierna", mesocycle `m-1`, week 2, user `u-1`, no trainer, no
  description and no date

#### Scenario: Admin creates a workout in a week

- **WHEN** an admin does the same on a mesocycle of trainer `t-1`
- **THEN** the creation also carries trainer `t-1`

#### Scenario: Creation in progress

- **WHEN** the creation has been sent and has not answered yet
- **THEN** "Crear" is disabled

### Requirement: A created week workout opens its screen

When the creation succeeds, the visitor SHALL end on `/workout?id=<new id>`.

#### Scenario: Workout created

- **WHEN** the API accepts the creation with id `w-9`
- **THEN** the visitor ends on `/workout?id=w-9`

### Requirement: A failed week workout creation keeps the dialog

When the creation fails, the dialog SHALL stay open with the typed values and SHALL show the message for the error code.
"Cancelar" SHALL close the dialog without sending anything.

#### Scenario: Placement refused

- **WHEN** the API answers 409 to the creation
- **THEN** the conflict error message is shown in the dialog and Nombre keeps the typed value

#### Scenario: Cancel

- **WHEN** the user clicks "Cancelar"
- **THEN** the dialog closes and no request is sent

### Requirement: The week workouts are translated

The workout count, "Nuevo entrenamiento", the dialog and its fields SHALL be available in Spanish and English and SHALL
follow the active language.

#### Scenario: Weeks in English

- **WHEN** the active language is English and week 1 holds one workout
- **THEN** the row shows "1 workout" and, expanded, "New workout"

### Requirement: Edit mode shows who the mesocycle is for as person cards

In edit mode, between the page header and the cards, the screen SHALL show the athlete as a person card: a small card,
sized to its content, with an avatar holding the initials, the full name, or the email when there is no name, and the
caption "Atleta". Initials are the first letters of name and surname, or the email's first letter.

#### Scenario: Athlete card

- **WHEN** a trainer loads mesocycle "Hipertrofia" of athlete "Ana García"
- **THEN** a person card above "Información general" shows "AG", "Ana García" and "Atleta"

#### Scenario: Athlete without a name

- **WHEN** the mesocycle's athlete has no name and the email "ana@example.com"
- **THEN** the athlete's person card shows "A", "ana@example.com" and "Atleta"

#### Scenario: No person cards in create mode

- **WHEN** a trainer opens `/mesocycle`
- **THEN** no person card is shown

### Requirement: Only admins see the mesocycle's trainer card

In edit mode, for an `ADMIN` the screen SHALL show the mesocycle's trainer as a person card after the athlete's, with
the caption "Entrenador". For a `TRAINER`, no trainer person card, no "Entrenador" caption and no trainer name SHALL be
shown.

#### Scenario: Admin sees the trainer card

- **WHEN** an admin loads a mesocycle of trainer "Luis Pérez"
- **THEN** a person card with "LP", "Luis Pérez" and "Entrenador" is shown after the athlete's

#### Scenario: Trainer does not see the trainer card

- **WHEN** a trainer loads the same mesocycle
- **THEN** neither "Entrenador" nor "Luis Pérez" is shown

### Requirement: A week's workouts table shows each workout's exercise count

Inside an expanded week, the table SHALL have the columns "Nombre" and "Ejercicios" and one row per workout of the
week, showing its name as plain text and the number of exercises it holds.

#### Scenario: Counts shown

- **WHEN** week 1 holds "Pierna" with 3 exercises and "Empuje" with no exercises and the user expands "Semana 1"
- **THEN** the table shows a row "Pierna" with `3` and a row "Empuje" with `0`
- **AND** "Pierna" is not a link

### Requirement: Person cards and the workouts table are translated

The person cards' role captions and the workouts table's column headers and edit action SHALL be available in Spanish
and English and SHALL follow the active language.

#### Scenario: In English

- **WHEN** the active language is English, an admin loads a mesocycle and expands a week with workout "Pierna"
- **THEN** the screen shows "Athlete", "Trainer", "Name", "Exercises" and an action "Edit Pierna"

### Requirement: The week workout dialog offers two sections on its left

The "Nuevo entrenamiento · Semana <n>" dialog SHALL show, on its left, the sections "Desde cero" and "Desde
plantilla", in that order and laid out like the exercise catalog dialog's sections. "Desde cero" SHALL be selected
every time the dialog opens and SHALL keep the current Nombre / Descripción form, its validation and its request.

#### Scenario: Dialog opens on Desde cero

- **WHEN** the user clicks "Nuevo entrenamiento" in "Semana 2"
- **THEN** the sections "Desde cero" and "Desde plantilla" are shown, "Desde cero" selected
- **AND** Nombre and Descripción are shown as before this change

### Requirement: Desde plantilla lists the mesocycle trainer's templates

While "Desde plantilla" is selected, the dialog SHALL list the templates with name, description and number of
exercises, as the create workout dialog does, without Nombre, Descripción, Atleta, Entrenador or date. A `TRAINER` SHALL
get their own templates; an `ADMIN` SHALL get those of the mesocycle's trainer.

#### Scenario: Trainer lists templates

- **WHEN** a trainer selects "Desde plantilla"
- **THEN** exactly one templates request is sent with no trainer id, and the templates are listed

#### Scenario: Admin lists the mesocycle trainer's templates

- **WHEN** an admin selects "Desde plantilla" on a mesocycle of trainer `t-1`
- **THEN** exactly one templates request is sent with trainer id `t-1`

#### Scenario: Empty or failed list

- **WHEN** the templates request returns none, or fails with `NETWORK`
- **THEN** "No hay plantillas disponibles." is shown, or the message for `NETWORK` with "Reintentar"

### Requirement: Creating a week workout from a template places it in the week

Under "Desde plantilla", "Crear" SHALL require a template ("Elige una plantilla.") and send nothing otherwise. When
one is chosen, it SHALL send exactly one creation from that template with the mesocycle's athlete as the user, the
mesocycle id and the week number, and no date or trainer id. "Crear" SHALL be disabled while it is in progress.

#### Scenario: Create in a week from a template

- **WHEN** a trainer selects template `wt-1` in week 2 of mesocycle `m-1` for athlete `u-1` and clicks "Crear"
- **THEN** exactly one creation from template `wt-1` is sent with user `u-1`, mesocycle `m-1` and week 2
- **AND** it carries no date and no trainer id

#### Scenario: Created week workout opens

- **WHEN** the creation answers with workout `w-9`
- **THEN** the user ends on `/workout?id=w-9`

#### Scenario: Creation fails

- **WHEN** the creation fails with `CONFLICT`
- **THEN** the dialog stays open on "Desde plantilla" with the selection kept and shows the message for `CONFLICT`

### Requirement: The week dialog sections are translated

The section names and the templates list of the week dialog SHALL be available in Spanish and English and SHALL follow
the active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the sections read "From scratch" and "From template"

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
