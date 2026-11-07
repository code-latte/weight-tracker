# backoffice-workout-template-editor Specification

## Purpose

Gives trainers and admins one backoffice screen per workout template, where they edit the template's name,
description and exercises exactly as they edit a workout, and delete the template.

## Requirements

### Requirement: The template screen is reached by the template id

The backoffice SHALL offer a signed-in screen at `/workout-template?id=<template id>` that reads that template, the
exercise catalog and the user's favorites once on opening. A visitor without a session SHALL end on `/login`.

#### Scenario: Trainer opens a template

- **WHEN** a trainer with a stored session opens `/workout-template?id=wt-1`
- **THEN** exactly one read of template `wt-1`, one of the exercise catalog and one of the favorites are sent
- **AND** the screen is shown beside the navigation drawer

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/workout-template?id=wt-1`
- **THEN** the visitor ends on `/login` and no template is read

### Requirement: The template screen has a page header and no workout context

Above the cards, the screen SHALL show the heading "Editar plantilla", a "Volver" link to `/workout-templates` and an
"Eliminar plantilla" action. It SHALL NOT show athlete or trainer person cards, a week, a date or a mesocycle.

#### Scenario: Header shown

- **WHEN** template "Pierna A" is loaded
- **THEN** the heading reads "Editar plantilla", "Volver" leads to `/workout-templates` and "Eliminar plantilla" is shown
- **AND** no person card, Semana field, date or mesocycle is shown

### Requirement: The template screen has the workout screen's two cards

The screen SHALL use the workout screen's layout: "Propiedades de la plantilla" on top with Nombre and Descripción in
editable fields, filled with the loaded values, and its "Guardar" after them; and the "Ejercicios" card below. It SHALL
NOT show the week field.

#### Scenario: Cards shown

- **WHEN** template "Pierna A" with description "Fuerza" is loaded
- **THEN** "Propiedades de la plantilla" shows Nombre "Pierna A", Descripción "Fuerza" and "Guardar", and no Semana
- **AND** the "Ejercicios" card is shown below it at full width

### Requirement: Guardar checks and replaces the template's name and description

On "Guardar", the screen SHALL apply the workout screen's structural checks to Nombre (required, at most 256 characters
after trimming) and Descripción (at most 1024), sending nothing when one fails. When valid, it SHALL send exactly one
template update with the trimmed name and the trimmed description or none when blank, disabled while in progress.

#### Scenario: Blank name

- **WHEN** the user clears Nombre and clicks "Guardar"
- **THEN** Nombre shows "Este campo es obligatorio." and no request is sent

#### Scenario: Template saved

- **WHEN** the user changes Nombre to " Pierna B " and clicks "Guardar"
- **THEN** exactly one update of the template is sent with name "Pierna B"

### Requirement: The template save outcome is shown

When the update succeeds, the screen SHALL show "Plantilla guardada." and editing a field again SHALL hide it. When it
fails, the screen SHALL show the message for the error code and keep the typed values.

#### Scenario: Saved

- **WHEN** the update succeeds
- **THEN** "Plantilla guardada." is shown

#### Scenario: Refused

- **WHEN** the update fails with `FORBIDDEN`
- **THEN** the message for `FORBIDDEN` is shown and the typed values stay

### Requirement: Template exercises are edited as workout exercises are

The "Ejercicios" card SHALL behave as the workout screen's, with every requirement of `backoffice-workout-editor` on
the catalog dialog, the configuration dialog, removal, reordering, superseries and favorites, except that each request
targets the template instead of a workout.

#### Scenario: Add an exercise

- **WHEN** the user clicks "Añadir ejercicio", chooses "Sentadilla" and saves a valid configuration
- **THEN** exactly one exercise addition to template `wt-1` is sent and no workout request
- **AND** "Sentadilla" is appended to the template's list and leaves the catalog

#### Scenario: Reorder

- **WHEN** the user drops the second exercise in first place
- **THEN** exactly one reorder of template `wt-1` is sent with the catalog ids in the new order

#### Scenario: Group in a superseries

- **WHEN** the user checks two exercises and clicks "Agrupar en superserie"
- **THEN** exactly one superseries creation in template `wt-1` is sent with their entry ids in list order

#### Scenario: Remove an exercise

- **WHEN** the user clicks "Quitar ejercicio" on "Sentadilla"
- **THEN** exactly one removal of that exercise from template `wt-1` is sent

### Requirement: A template is deleted after confirmation

"Eliminar plantilla" SHALL open a confirmation dialog. Cancelling SHALL close it without a request. Confirming SHALL
send exactly one deletion of the template, with confirm disabled while in progress; on success the user SHALL end on
`/workout-templates`; on failure the dialog SHALL close and the screen SHALL show the message for the error code.

#### Scenario: Delete confirmed

- **WHEN** the user clicks "Eliminar plantilla" and confirms
- **THEN** exactly one deletion of the template is sent
- **AND** the user ends on `/workout-templates`

#### Scenario: Delete cancelled

- **WHEN** the user clicks "Eliminar plantilla" and then "Cancelar"
- **THEN** no deletion is sent and the template stays on screen

#### Scenario: Delete refused

- **WHEN** the deletion fails with `FORBIDDEN`
- **THEN** the user stays on the template screen and the message for `FORBIDDEN` is shown

### Requirement: Loading, not-found and failure states of the template screen

While the template, the catalog or the favorites load, the screen SHALL show a loading placeholder. A missing `id`, or
a template the API does not find, SHALL show "No hemos encontrado esta plantilla." with a "Volver a plantillas" link to
`/workout-templates`. Any other failure SHALL show the error for its code with a retry.

#### Scenario: Missing id

- **WHEN** the user opens `/workout-template` without `id`
- **THEN** "No hemos encontrado esta plantilla." and the link back to `/workout-templates` are shown
- **AND** no template read is sent

#### Scenario: Unknown template

- **WHEN** the template read fails with `NOT_FOUND`
- **THEN** "No hemos encontrado esta plantilla." is shown

### Requirement: The template screen is translated

Every text of the screen and its dialogs SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the heading reads "Edit template" and the delete action "Delete template"
