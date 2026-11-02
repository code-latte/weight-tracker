# Spec Delta

## MODIFIED Requirements

### Requirement: The drawer lists every area in a fixed order

The drawer SHALL show the app name row at the top, then these entries in this order. Section titles SHALL be headers,
not actions, with their items below them. Account entries and logout SHALL NOT be in the drawer. For an `ADMIN` the
second section SHALL read "Usuarios: Todos los usuarios, Invitaciones" instead of the "Atletas" copy below; a
`TRAINER`, or a visitor whose profile has not loaded yet, SHALL see the "Atletas" copy.

- Inicio
- Atletas: Todos los atletas, Invitaciones
- Programación: Planes asignados, Calendario
- Biblioteca: Plantillas de entrenamiento, Catálogo de ejercicios
- Seguimiento: Feedback de entrenamientos, Revisiones semanales, Pendientes de revisión

#### Scenario: Drawer content in Spanish

- **WHEN** the drawer is shown in Spanish to a trainer
- **THEN** it shows the app name row, then every section title and item above, in that order
- **AND** it does not show "Configuración", "Mi perfil", "Preferencias", "Cerrar sesión" or "Invitaciones
  pendientes"

#### Scenario: Admin copy

- **WHEN** the drawer is shown in Spanish to an admin
- **THEN** the second section reads "Usuarios" with the items "Todos los usuarios" and "Invitaciones"
- **AND** neither "Atletas" nor "Todos los atletas" is shown

#### Scenario: Trainer copy

- **WHEN** the drawer is shown to a trainer
- **THEN** the second section reads "Atletas" with "Todos los atletas" and "Invitaciones"
- **AND** neither "Usuarios" nor "Todos los usuarios" is shown

#### Scenario: Section titles do not act

- **WHEN** the visitor clicks the section title "Atletas"
- **THEN** nothing happens and the visitor stays on the current screen

### Requirement: Items without a screen do nothing

Every item other than Inicio, Planes asignados, an admin's Todos los usuarios, a trainer's Todos los atletas,
Invitaciones and Cerrar sesión SHALL be shown but SHALL NOT navigate or trigger any operation until its screen exists.

#### Scenario: Placeholder item clicked

- **WHEN** a trainer clicks "Calendario"
- **THEN** the visitor stays on the current screen
- **AND** no request or operation is triggered

## ADDED Requirements

### Requirement: Todos los atletas goes to the athletes screen for trainers

For a `TRAINER`, the Todos los atletas entry SHALL link to `/athletes` and SHALL be shown as selected while the
visitor is on that screen. It SHALL NOT be selected on any other screen.

#### Scenario: Trainer clicks Todos los atletas

- **WHEN** a trainer clicks "Todos los atletas" from home
- **THEN** the trainer ends on `/athletes`

#### Scenario: Todos los atletas on its screen

- **WHEN** a trainer is on `/athletes`
- **THEN** Todos los atletas is shown as selected
- **AND** Inicio is not shown as selected

### Requirement: Invitaciones goes to the invitations screen

For a `TRAINER` and for an `ADMIN`, the Invitaciones entry SHALL link to `/invitations` and SHALL be shown as
selected while the visitor is on that screen.

#### Scenario: Trainer clicks Invitaciones

- **WHEN** a trainer clicks "Invitaciones" from home
- **THEN** the trainer ends on `/invitations`

#### Scenario: Admin's Invitaciones on its screen

- **WHEN** an admin is on `/invitations`
- **THEN** Invitaciones is shown as selected
- **AND** Todos los usuarios is not shown as selected
