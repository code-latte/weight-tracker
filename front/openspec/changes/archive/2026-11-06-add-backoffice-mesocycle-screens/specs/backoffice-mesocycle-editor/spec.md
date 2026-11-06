# Spec Delta

## Purpose

Lets trainers and admins create a mesocycle for an athlete in the backoffice, edit its general information and plan
its weeks, on a screen that shares the layout of the workout screen.

## ADDED Requirements

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

The card SHALL show Nombre, Fecha de inicio, Duración (semanas) and Objetivo general (multiline). Nombre and Objetivo
general SHALL show a character counter against their maximum. In create mode the card SHALL also show the Atleta
picker before the other fields, and its save action SHALL read "Crear mesociclo"; in edit mode it SHALL read "Guardar".

#### Scenario: Create form fields

- **WHEN** a trainer opens `/mesocycle`
- **THEN** Atleta, Nombre, Fecha de inicio, Duración (semanas) set to `4`, Objetivo general and "Crear mesociclo" are
  shown

#### Scenario: Counter

- **WHEN** the user types "Hipertrofia" in Nombre
- **THEN** the counter under Nombre reads "11 / 256"

### Requirement: Duration is chosen with a bounded stepper

Duración (semanas) SHALL offer a decrease and an increase button around its value. The decrease button SHALL be
disabled at 1 and the increase button at 52.

#### Scenario: Increase

- **WHEN** Duración shows `4` and the user clicks "Aumentar duración"
- **THEN** Duración shows `5`

#### Scenario: Lower bound

- **WHEN** Duración shows `1`
- **THEN** "Reducir duración" is disabled

#### Scenario: Upper bound

- **WHEN** Duración shows `52`
- **THEN** "Aumentar duración" is disabled

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
required and at most 4000 characters after trimming; Fecha de inicio is a required valid date; Atleta, and Entrenador
for an admin, are required in create mode.

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

In edit mode, the general information card SHALL hold the mesocycle's name, start date, duration and objective, and
SHALL show the athlete's full name (their email when they have no name) as read-only text. For an `ADMIN` it SHALL also
show the trainer the same way; for a `TRAINER` the trainer SHALL NOT be shown.

#### Scenario: Loaded for a trainer

- **WHEN** a trainer loads mesocycle "Hipertrofia" of athlete "Ana García" with trainer "Luis Pérez"
- **THEN** Nombre holds "Hipertrofia" and "Ana García" is shown as the athlete
- **AND** "Luis Pérez" is not shown and no Atleta or Entrenador picker is rendered

#### Scenario: Loaded for an admin

- **WHEN** an admin loads the same mesocycle
- **THEN** "Luis Pérez" is shown as the trainer

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
