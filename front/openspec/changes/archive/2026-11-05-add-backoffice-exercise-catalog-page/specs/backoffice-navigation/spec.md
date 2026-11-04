# Spec Delta

## ADDED Requirements

### Requirement: Catálogo de ejercicios goes to the exercise catalog screen

The Catálogo de ejercicios entry SHALL link to `/exercises` and SHALL be shown as selected while the visitor is on
that screen. It SHALL NOT be selected on any other screen, and no other entry SHALL be selected on it.

#### Scenario: Catálogo de ejercicios clicked

- **WHEN** the visitor clicks "Catálogo de ejercicios" from home
- **THEN** the visitor ends on `/exercises`

#### Scenario: Catálogo de ejercicios on its screen

- **WHEN** the visitor is on `/exercises`
- **THEN** Catálogo de ejercicios is shown as selected
- **AND** Inicio is not shown as selected

## MODIFIED Requirements

### Requirement: Items without a screen do nothing

Every item other than Inicio, Planes asignados, Catálogo de ejercicios, an admin's Todos los usuarios, an admin's
Asignación de entrenadores, a trainer's Todos los atletas, Invitaciones and Cerrar sesión SHALL be shown but SHALL NOT
navigate or trigger any operation until its screen exists.

#### Scenario: Placeholder item clicked

- **WHEN** a trainer clicks "Calendario"
- **THEN** the visitor stays on the current screen
- **AND** no request or operation is triggered
