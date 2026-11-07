# Spec Delta

## ADDED Requirements

### Requirement: The dialog offers two sections on its left

The create workout dialog SHALL show, on its left, a list of two sections, each an icon and a name, in this order:
"Desde cero" and "Desde plantilla", laid out like the exercise catalog dialog's sections. Exactly one SHALL be
selected; "Desde cero" SHALL be selected every time the dialog opens.

#### Scenario: Dialog opens on Desde cero

- **WHEN** the user opens the create workout dialog
- **THEN** the sections "Desde cero" and "Desde plantilla" are shown on the left, in that order
- **AND** "Desde cero" is selected and the form of the dialog is shown as before this change

#### Scenario: Switching sections sends nothing by itself

- **WHEN** a trainer selects "Desde plantilla" and then "Desde cero"
- **THEN** no workout creation is sent

### Requirement: Desde cero keeps the current form unchanged

While "Desde cero" is selected, the dialog SHALL show the fields, validation, request and outcomes of every other
requirement of this capability, unchanged.

#### Scenario: Create from scratch

- **WHEN** a trainer fills Nombre, Semana and Atleta under "Desde cero" and clicks "Crear"
- **THEN** exactly one workout creation is sent, as before this change, and no creation from a template

### Requirement: Desde plantilla asks for the athlete, the week and a template

While "Desde plantilla" is selected, the dialog SHALL show Semana and Atleta as in "Desde cero", Entrenador for an
`ADMIN` only, and the list of templates. It SHALL NOT show Nombre or Descripción. Each template SHALL show its name, its
description when it has one, and its number of exercises ("3 ejercicios"); at most one SHALL be selected.

#### Scenario: Trainer sees their templates

- **WHEN** a trainer selects "Desde plantilla" and the API returns templates "Pierna A" (3 exercises) and "Torso"
- **THEN** exactly one templates request is sent, with no trainer id
- **AND** both are listed in that order with their descriptions and "3 ejercicios" under "Pierna A"
- **AND** Nombre and Descripción are not shown

#### Scenario: Select a template

- **WHEN** the user selects "Pierna A" and then "Torso"
- **THEN** only "Torso" is shown as selected

### Requirement: Admins see the templates of the chosen trainer

For an `ADMIN`, the templates list SHALL show "Elige un entrenador para ver sus plantillas." until a trainer is
chosen, and SHALL then request the templates of that trainer. Changing the trainer SHALL request that trainer's
templates and clear the selected template.

#### Scenario: Admin without trainer

- **WHEN** an admin selects "Desde plantilla" without a trainer chosen
- **THEN** "Elige un entrenador para ver sus plantillas." is shown and no templates request is sent

#### Scenario: Admin picks a trainer

- **WHEN** the admin chooses trainer `t-1`
- **THEN** exactly one templates request is sent with trainer id `t-1`

### Requirement: The templates list shows loading, empty and failed states

While the templates load, the list SHALL show a loading placeholder. An empty list SHALL show "No hay plantillas
disponibles.". A failed load SHALL show the message for the error code with "Reintentar", which requests them again.

#### Scenario: No templates

- **WHEN** the templates request returns none
- **THEN** "No hay plantillas disponibles." is shown

#### Scenario: Failed load

- **WHEN** the templates request fails with `NETWORK` and the user clicks "Reintentar"
- **THEN** exactly one more templates request is sent

### Requirement: Creating from a template sends it once and opens the workout

Under "Desde plantilla", "Crear" SHALL require a template ("Elige una plantilla."), Semana and Atleta and, for an
`ADMIN`, Entrenador, sending nothing when one is missing. When valid, it SHALL send exactly one creation from the chosen
template with the athlete's id and the Wednesday of the week as `YYYY-MM-DD`, and no trainer id.

#### Scenario: Create from a template

- **WHEN** a trainer selects "Pierna A" (`wt-1`), athlete `u-1`, "Semana del 2 nov – 8 nov" and clicks "Crear"
- **THEN** exactly one creation from template `wt-1` is sent with user `u-1` and date "2026-11-04", and no trainer id

#### Scenario: No template chosen

- **WHEN** the user clicks "Crear" with no template selected
- **THEN** "Elige una plantilla." is shown and no request is sent

#### Scenario: Created workout opens

- **WHEN** the creation from a template answers with workout `w-9`
- **THEN** the dialog closes and the user ends on `/workout?id=w-9`

#### Scenario: Creation from a template fails

- **WHEN** the creation fails with `FORBIDDEN`
- **THEN** the dialog stays open on "Desde plantilla" with the selection kept and shows the message for `FORBIDDEN`

### Requirement: The sections and the templates list are translated

The section names, the templates list and its messages SHALL be available in Spanish and English and SHALL follow the
active language.

#### Scenario: English

- **WHEN** the active language is English
- **THEN** the sections read "From scratch" and "From template", and a template with 3 exercises reads "3 exercises"
