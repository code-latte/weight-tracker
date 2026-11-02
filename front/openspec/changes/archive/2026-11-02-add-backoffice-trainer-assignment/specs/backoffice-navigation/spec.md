# Spec Delta

## MODIFIED Requirements

### Requirement: The drawer lists every area in a fixed order

The drawer SHALL show the app name row at the top, then these entries in this order. Section titles SHALL be headers,
not actions, with their items below them. Account entries and logout SHALL NOT be in the drawer. For an `ADMIN` the
second section SHALL read "Usuarios: Todos los usuarios, Asignación de entrenadores, Invitaciones" instead of the
"Atletas" copy below; a `TRAINER`, or a visitor whose profile has not loaded yet, SHALL see the "Atletas" copy.

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

Every item other than Inicio, Planes asignados, an admin's Todos los usuarios, an admin's Asignación de entrenadores,
a trainer's Todos los atletas, Invitaciones and Cerrar sesión SHALL be shown but SHALL NOT navigate or trigger any
operation until its screen exists.

#### Scenario: Placeholder item clicked

- **WHEN** a trainer clicks "Calendario"
- **THEN** the visitor stays on the current screen
- **AND** no request or operation is triggered

## ADDED Requirements

### Requirement: Asignación de entrenadores goes to the trainer assignment screen for admins

For an `ADMIN`, the Asignación de entrenadores entry SHALL link to `/trainer-assignments` and SHALL be shown as
selected while the visitor is on that screen. It SHALL NOT be selected on any other screen.

#### Scenario: Admin clicks Asignación de entrenadores

- **WHEN** an admin clicks "Asignación de entrenadores" from home
- **THEN** the admin ends on `/trainer-assignments`

#### Scenario: Asignación de entrenadores on its screen

- **WHEN** an admin is on `/trainer-assignments`
- **THEN** Asignación de entrenadores is shown as selected
- **AND** neither Todos los usuarios nor Invitaciones is shown as selected
