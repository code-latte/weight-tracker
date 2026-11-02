# backoffice-navigation Specification

## Purpose

Gives every signed-in backoffice screen a common navigation drawer, so trainers and admins reach each area of the
backoffice and log out from one stable place.

## Requirements

### Requirement: Signed-in screens show the navigation drawer

Every signed-in backoffice screen SHALL show a navigation drawer on its left, always visible, with the screen's content
beside it. Trainers and admins SHALL see the same drawer.

#### Scenario: Signed-in visitor opens home

- **WHEN** a trainer or an admin with a stored session opens `/`
- **THEN** the navigation drawer is shown
- **AND** the home heading "Weight Tracker" is shown beside it

### Requirement: The drawer is hidden without a session

The drawer SHALL NOT be rendered for a visitor without a stored session, nor while the session is being read.

#### Scenario: Visitor without a session opens home

- **WHEN** a visitor with no stored session opens `/`
- **THEN** the visitor ends on `/login`
- **AND** neither the drawer nor any of its items is rendered

#### Scenario: Session not read yet

- **WHEN** a signed-in screen has not finished reading the session
- **THEN** only a progress indicator is shown and the drawer is not rendered

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

### Requirement: Inicio goes to home and shows where the visitor is

The Inicio entry SHALL link to `/`. The entry of the current screen SHALL be shown as selected.

#### Scenario: Inicio on home

- **WHEN** the visitor is on `/`
- **THEN** Inicio is shown as selected
- **AND** Inicio links to `/`

### Requirement: Items without a screen do nothing

Every item other than Inicio, Planes asignados, an admin's Todos los usuarios, an admin's Asignación de entrenadores,
a trainer's Todos los atletas, Invitaciones and Cerrar sesión SHALL be shown but SHALL NOT navigate or trigger any
operation until its screen exists.

#### Scenario: Placeholder item clicked

- **WHEN** a trainer clicks "Calendario"
- **THEN** the visitor stays on the current screen
- **AND** no request or operation is triggered

### Requirement: The drawer starts with the app name

The drawer SHALL start with a row that shows the logo and the app name "Weight Tracker". The row SHALL NOT be a menu
entry, and it SHALL have the same height as the header beside it.

#### Scenario: App name row

- **WHEN** the drawer is shown
- **THEN** its first row shows the logo and "Weight Tracker"
- **AND** clicking that row does nothing

### Requirement: Every drawer item has an icon

Every drawer item, Inicio included, SHALL show an icon before its text. Section titles SHALL NOT have an icon.

#### Scenario: Icons shown

- **WHEN** the drawer is shown
- **THEN** each of its items shows an icon before its text

### Requirement: Planes asignados goes to the assigned plans screen

The Planes asignados entry SHALL link to `/assigned-plans` and SHALL be shown as selected while the visitor is on that
screen. It SHALL NOT be selected on any other screen.

#### Scenario: Planes asignados clicked

- **WHEN** the visitor clicks "Planes asignados" from home
- **THEN** the visitor ends on `/assigned-plans`

#### Scenario: Planes asignados on its screen

- **WHEN** the visitor is on `/assigned-plans`
- **THEN** Planes asignados is shown as selected
- **AND** Inicio is not shown as selected

### Requirement: Todos los usuarios goes to the users screen for admins

For an `ADMIN`, the Todos los usuarios entry SHALL link to `/users` and SHALL be shown as selected while the visitor is
on that screen. It SHALL NOT be selected on any other screen. A `TRAINER`'s Todos los atletas entry SHALL NOT link to
`/users`.

#### Scenario: Admin clicks Todos los usuarios

- **WHEN** an admin clicks "Todos los usuarios" from home
- **THEN** the admin ends on `/users`

#### Scenario: Todos los usuarios on its screen

- **WHEN** an admin is on `/users`
- **THEN** Todos los usuarios is shown as selected
- **AND** Inicio is not shown as selected

#### Scenario: Trainer entry has no link

- **WHEN** the drawer is shown to a trainer
- **THEN** "Todos los atletas" has no link to `/users`

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
