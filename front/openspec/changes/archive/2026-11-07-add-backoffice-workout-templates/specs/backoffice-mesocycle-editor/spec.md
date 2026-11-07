# Spec Delta

## ADDED Requirements

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
