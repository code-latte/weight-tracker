# Spec Delta

## ADDED Requirements

### Requirement: Plantillas de entrenamiento goes to the templates screen

The Plantillas de entrenamiento entry SHALL link to `/workout-templates` and SHALL be shown as selected while the
visitor is on that screen, for both admins and trainers. It SHALL NOT be selected on any other screen, and no other
entry SHALL be selected on it.

#### Scenario: Plantillas de entrenamiento clicked

- **WHEN** the visitor clicks "Plantillas de entrenamiento" from home
- **THEN** the visitor ends on `/workout-templates`

#### Scenario: Plantillas de entrenamiento on its screen

- **WHEN** the visitor is on `/workout-templates`
- **THEN** Plantillas de entrenamiento is shown as selected
- **AND** neither Inicio nor Catálogo de ejercicios is shown as selected

#### Scenario: Entry in English

- **WHEN** the drawer is shown in English
- **THEN** the entry reads "Workout templates"

## MODIFIED Requirements

### Requirement: Items without a screen do nothing

Every item other than Inicio, Mesociclos, Entrenamientos, Plantillas de entrenamiento, Catálogo de ejercicios, an
admin's Todos los usuarios, an admin's Asignación de entrenadores, a trainer's Todos los atletas, Invitaciones and
Cerrar sesión SHALL be shown but SHALL NOT navigate or trigger any operation until its screen exists.

#### Scenario: Placeholder item clicked

- **WHEN** a trainer clicks "Calendario"
- **THEN** the visitor stays on the current screen
- **AND** no request or operation is triggered
