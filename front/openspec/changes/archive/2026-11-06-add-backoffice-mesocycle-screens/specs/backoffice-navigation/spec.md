# Spec Delta

## ADDED Requirements

### Requirement: Entrenamientos goes to the workouts screen

The Entrenamientos entry SHALL link to `/assigned-workouts` and SHALL be shown as selected while the visitor
is on that screen. It SHALL NOT be selected on any other screen, and Mesociclos SHALL NOT be selected on it.

#### Scenario: Entrenamientos clicked

- **WHEN** the visitor clicks "Entrenamientos" from home
- **THEN** the visitor ends on `/assigned-workouts`

#### Scenario: Entrenamientos on its screen

- **WHEN** the visitor is on `/assigned-workouts`
- **THEN** Entrenamientos is shown as selected
- **AND** neither Inicio nor Mesociclos is shown as selected

#### Scenario: Entry in English

- **WHEN** the drawer is shown in English
- **THEN** the entry reads "Workouts"

### Requirement: Mesociclos goes to the mesocycles screen

The Mesociclos entry SHALL link to `/assigned-plans` and SHALL be shown as selected while the visitor is on that
screen. It SHALL NOT be selected on any other screen, including `/assigned-workouts`.

#### Scenario: Mesociclos clicked

- **WHEN** the visitor clicks "Mesociclos" from home
- **THEN** the visitor ends on `/assigned-plans`

#### Scenario: Mesociclos on its screen

- **WHEN** the visitor is on `/assigned-plans`
- **THEN** Mesociclos is shown as selected
- **AND** neither Inicio nor Entrenamientos is shown as selected

## MODIFIED Requirements

### Requirement: The drawer lists every area in a fixed order

The drawer SHALL show the app name row at the top, then these entries in this order. Section titles SHALL be headers,
not actions, with their items below them. Account entries and logout SHALL NOT be in the drawer. For an `ADMIN` the
second section SHALL read "Usuarios: Todos los usuarios, Asignación de entrenadores, Invitaciones" instead of the
"Atletas" copy below; a `TRAINER`, or a visitor whose profile has not loaded yet, SHALL see the "Atletas" copy.

- Inicio
- Atletas: Todos los atletas, Invitaciones
- Programación: Mesociclos, Entrenamientos, Calendario
- Biblioteca: Plantillas de entrenamiento, Catálogo de ejercicios
- Seguimiento: Feedback de entrenamientos, Revisiones semanales, Pendientes de revisión

#### Scenario: Drawer content in Spanish

- **WHEN** the drawer is shown in Spanish to a trainer
- **THEN** it shows the app name row, then every section title and item above, in that order
- **AND** it does not show "Configuración", "Mi perfil", "Preferencias", "Cerrar sesión" or "Invitaciones
  pendientes"

#### Scenario: Admin copy

- **WHEN** the drawer is shown in Spanish to an admin
- **THEN** the second section reads "Usuarios" with the items "Todos los usuarios", "Asignación de entrenadores" and
  "Invitaciones", in that order
- **AND** neither "Atletas" nor "Todos los atletas" is shown

#### Scenario: Trainer copy

- **WHEN** the drawer is shown to a trainer
- **THEN** the second section reads "Atletas" with "Todos los atletas" and "Invitaciones"
- **AND** neither "Usuarios", "Todos los usuarios" nor "Asignación de entrenadores" is shown

#### Scenario: Section titles do not act

- **WHEN** the visitor clicks the section title "Atletas"
- **THEN** nothing happens and the visitor stays on the current screen

### Requirement: Items without a screen do nothing

Every item other than Inicio, Mesociclos, Entrenamientos, Catálogo de ejercicios, an admin's Todos los
usuarios, an admin's Asignación de entrenadores, a trainer's Todos los atletas, Invitaciones and Cerrar sesión SHALL be
shown but SHALL NOT navigate or trigger any operation until its screen exists.

#### Scenario: Placeholder item clicked

- **WHEN** a trainer clicks "Calendario"
- **THEN** the visitor stays on the current screen
- **AND** no request or operation is triggered

## REMOVED Requirements

### Requirement: Planes asignados goes to the assigned plans screen

**Reason**: The entry is renamed "Mesociclos".
**Migration**: Covered by "Mesociclos goes to the mesocycles screen", same link to `/assigned-plans`.
