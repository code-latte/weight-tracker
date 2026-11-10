# backoffice-workout-templates Specification

## Purpose

Lets trainers and admins see, in one table, the workout templates they can use in the backoffice, as a starting point
to create a new template or open one to edit it.

## Requirements

### Requirement: The templates screen lists the caller's templates

The backoffice SHALL offer a signed-in screen at `/workout-templates` with the heading "Plantillas de entrenamiento".
On opening it SHALL request the templates once and show one row per template in the order the API returns them. Which
templates those are is the API's answer. A visitor without a session SHALL end on `/login`.

#### Scenario: Trainer opens the screen

- **WHEN** a signed-in trainer opens `/workout-templates` and the API returns two templates
- **THEN** the heading "Plantillas de entrenamiento" is shown
- **AND** exactly one templates request is sent, with no trainer id
- **AND** the table shows two rows in that order

#### Scenario: No session

- **WHEN** a visitor without a session opens `/workout-templates`
- **THEN** they end on `/login` and no templates request is sent

### Requirement: Each row shows the template's name, description and exercise count

Each row SHALL show the template's name, its description (empty when it has none) and the number of exercises it
holds, under the columns "Nombre", "Descripción" and "Ejercicios".

#### Scenario: Row content

- **WHEN** a template "Pierna A" with description "Fuerza" and three exercises is listed
- **THEN** its row shows "Pierna A", "Fuerza" and "3"

#### Scenario: Template without description

- **WHEN** a listed template has no description
- **THEN** its "Descripción" cell is empty

### Requirement: Only admins see the trainer column

For an `ADMIN`, the table SHALL show an "Entrenador" column with each template's trainer full name, or their email when
they have no name, taken from the users the screen reads once. The trainer's id SHALL NOT be shown. For a `TRAINER`,
neither the column header nor its cells SHALL be rendered and no users request SHALL be sent.

#### Scenario: Admin sees the trainer

- **WHEN** an admin opens the screen and a template belongs to trainer "Ana López"
- **THEN** the "Entrenador" column is shown and that row reads "Ana López"

#### Scenario: Trainer does not see the trainer

- **WHEN** a trainer opens the screen
- **THEN** no "Entrenador" header or cell is rendered
- **AND** no users request is sent

### Requirement: Loading, empty and failed states are shown

While the templates load, the screen SHALL show a loading placeholder instead of the table. An empty list SHALL show
"Todavía no hay plantillas." instead of rows. A failed load SHALL show the error for its code with a "Reintentar"
action that requests the templates again.

#### Scenario: Empty list

- **WHEN** the API returns no templates
- **THEN** "Todavía no hay plantillas." is shown and no rows

#### Scenario: Failed load and retry

- **WHEN** the templates request fails with `NETWORK` and the user clicks "Reintentar"
- **THEN** the error for `NETWORK` was shown and exactly one more templates request is sent

### Requirement: Each row opens its template

Each row SHALL end with an edit icon button labelled "Editar <template name>" that navigates to
`/workout-template?id=<template id>`, for both admins and trainers.

#### Scenario: Open a template

- **WHEN** the user clicks "Editar Pierna A" on the row of template `wt-1`
- **THEN** they end on `/workout-template?id=wt-1`

### Requirement: Nueva plantilla opens the create template dialog

The screen SHALL show a "Nueva plantilla" button with a plus icon above the table, for both admins and trainers.
Activating it SHALL open the create template dialog titled "Nueva plantilla" and SHALL NOT send any request by itself.

#### Scenario: Open the dialog

- **WHEN** the user clicks "Nueva plantilla"
- **THEN** the dialog "Nueva plantilla" opens and no template request is sent

### Requirement: The create template dialog asks only for a name, a description and, for admins, a trainer

The dialog SHALL ask for Nombre and Descripción (multiline), with "Cancelar" and "Crear". For an `ADMIN` it SHALL also
show the picker "Entrenador", offering the active users with role `TRAINER`. It SHALL NOT ask for a date, a week, a
mesocycle or an athlete. For a `TRAINER` no trainer picker SHALL be rendered.

#### Scenario: Trainer dialog

- **WHEN** a trainer opens the dialog
- **THEN** Nombre and Descripción are shown empty
- **AND** no Entrenador, Atleta, Semana, date or mesocycle field is rendered

#### Scenario: Admin dialog

- **WHEN** an admin opens the dialog
- **THEN** the picker Entrenador is shown with no selection, after Descripción

### Requirement: Invalid template fields are reported and nothing is sent

On "Crear", the dialog SHALL check each field structurally and, if any fails, SHALL show the message under that field
and SHALL NOT send any request. Nombre is required and at most 256 characters after trimming; Descripción is at most
1024 characters after trimming; for an `ADMIN`, Entrenador is required.

#### Scenario: Blank name

- **WHEN** Nombre is empty and the user clicks "Crear"
- **THEN** Nombre shows "Este campo es obligatorio." and no request is sent

#### Scenario: Description too long

- **WHEN** Descripción has 1025 characters after trimming and the user clicks "Crear"
- **THEN** Descripción shows "No puede superar los 1024 caracteres." and no request is sent

#### Scenario: Admin without trainer

- **WHEN** an admin leaves Entrenador empty and clicks "Crear"
- **THEN** Entrenador shows "Selecciona un entrenador." and no request is sent

### Requirement: Creating sends the template once and opens it

When every field is valid, "Crear" SHALL send exactly one template creation with the trimmed name, the trimmed
description only when it is not blank and, for an `ADMIN` only, the chosen trainer id. "Crear" SHALL be disabled while
it is in progress. On success the user SHALL end on `/workout-template?id=<new id>`.

#### Scenario: Trainer creates a template

- **WHEN** a trainer types " Pierna A " and no description and clicks "Crear", and the API answers with template `wt-9`
- **THEN** exactly one creation is sent with name "Pierna A", no description and no trainer id
- **AND** the user ends on `/workout-template?id=wt-9`

#### Scenario: Admin creates a template for a trainer

- **WHEN** an admin picks trainer `t-1` and creates a template
- **THEN** the creation carries trainer id `t-1`

### Requirement: A failed creation or a cancel keeps nothing sent twice

When the creation fails, the dialog SHALL stay open with the typed values and SHALL show the message for the error
code. "Cancelar", Escape and a click outside SHALL close the dialog without sending anything; opening it again SHALL
show empty fields and no error.

#### Scenario: Creation fails

- **WHEN** the creation fails with `FORBIDDEN`
- **THEN** the dialog stays open with the typed values and shows the message for `FORBIDDEN`

#### Scenario: Cancel

- **WHEN** the user types a name and clicks "Cancelar", then opens the dialog again
- **THEN** no creation was sent and Nombre is empty

### Requirement: The templates screen and dialog are translated

Every text of the screen and its dialog SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the heading reads "Workout templates" and the button "New template"

### Requirement: Each template row offers a delete action

Each row SHALL show, after the edit action, a trash icon button labelled "Eliminar <template name>". Admins and
trainers SHALL see it on every row. Pressing it SHALL open the confirmation "¿Eliminar esta plantilla?" with the text
"Los entrenamientos creados desde ella no cambian." and the buttons "Cancelar" and "Eliminar". Pressing it SHALL NOT
send any request.

#### Scenario: Trainer opens the confirmation

- **WHEN** a trainer presses "Eliminar Fuerza A" on the templates screen
- **THEN** the dialog "¿Eliminar esta plantilla?" opens with "Cancelar" and "Eliminar"
- **AND** no deletion is sent

#### Scenario: Admin sees the delete action

- **WHEN** an admin opens the templates screen with one template "Fuerza A"
- **THEN** its row shows both "Editar Fuerza A" and "Eliminar Fuerza A"

#### Scenario: Cancel sends nothing

- **WHEN** the user presses "Cancelar", presses Escape or clicks outside the dialog
- **THEN** the dialog closes and no deletion is sent

### Requirement: Confirming deletes the template and refreshes the list

"Eliminar" in the dialog SHALL send exactly one deletion of that row's template. While it is in progress, both
buttons SHALL be disabled and the dialog SHALL NOT close. On success the dialog SHALL close and the screen SHALL read
the templates again. On failure the dialog SHALL close, the screen SHALL show the message for the error code above
the table, and the list SHALL stay as it was.

#### Scenario: Template deleted

- **WHEN** the user confirms the deletion of template `t-1` and it succeeds
- **THEN** exactly one deletion of `t-1` is sent
- **AND** the dialog closes and the templates are read again

#### Scenario: Deletion fails

- **WHEN** the user confirms a deletion and it fails with `FORBIDDEN`
- **THEN** the dialog closes and the message for `FORBIDDEN` is shown above the table
- **AND** the rows are unchanged

### Requirement: The template delete action is translated

The delete action's label and its dialog SHALL be available in Spanish and English and SHALL follow the active
language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the row action is labelled "Delete <template name>" and the dialog is titled "Delete this template?"
