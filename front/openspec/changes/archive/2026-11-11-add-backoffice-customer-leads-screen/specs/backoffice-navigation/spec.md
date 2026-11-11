# Spec Delta

## MODIFIED Requirements

### Requirement: The drawer lists every area in a fixed order

The drawer SHALL show the app name row at the top, then these entries in this order. Section titles SHALL be headers,
not actions, with their items below them. Account entries and logout SHALL NOT be in the drawer. For an `ADMIN` the
second section SHALL read "Usuarios: Todos los usuarios, Asignación de entrenadores, Invitaciones" instead of the
"Atletas" copy below; a `TRAINER`, or a visitor whose profile has not loaded yet, SHALL see the "Atletas" copy. For an
`ADMIN` only, a last section "Marketing: Leads" SHALL follow Seguimiento; a `TRAINER`, or a visitor whose profile has
not loaded yet, SHALL NOT see it.

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

#### Scenario: Admin sees the Marketing section last

- **WHEN** the drawer is shown in Spanish to an admin
- **THEN** after the "Seguimiento" section it shows the section "Marketing" with the single item "Leads"

#### Scenario: Trainer does not see the Marketing section

- **WHEN** the drawer is shown to a trainer
- **THEN** neither "Marketing" nor "Leads" is shown

#### Scenario: Marketing hidden while the profile loads

- **WHEN** the drawer is shown before the visitor's profile has loaded
- **THEN** neither "Marketing" nor "Leads" is shown

#### Scenario: Section titles do not act

- **WHEN** the visitor clicks the section title "Atletas"
- **THEN** nothing happens and the visitor stays on the current screen

### Requirement: Items without a screen do nothing

Every item other than Inicio, Mesociclos, Entrenamientos, Plantillas de entrenamiento, Catálogo de ejercicios, an
admin's Todos los usuarios, an admin's Asignación de entrenadores, an admin's Leads, a trainer's Todos los atletas,
Invitaciones and Cerrar sesión SHALL be shown but SHALL NOT navigate or trigger any operation until its screen exists.

#### Scenario: Placeholder item clicked

- **WHEN** a trainer clicks "Calendario"
- **THEN** the visitor stays on the current screen
- **AND** no request or operation is triggered

## ADDED Requirements

### Requirement: Leads goes to the leads screen for admins

For an `ADMIN`, the Leads entry SHALL link to `/customer-leads` and SHALL be shown as selected while the visitor is on
that screen. It SHALL NOT be selected on any other screen, and no other entry SHALL be selected on it. In English the
section and the entry SHALL read "Marketing" and "Leads".

#### Scenario: Admin clicks Leads

- **WHEN** an admin clicks "Leads" from home
- **THEN** the admin ends on `/customer-leads`

#### Scenario: Leads on its screen

- **WHEN** an admin is on `/customer-leads`
- **THEN** Leads is shown as selected
- **AND** Inicio is not shown as selected

#### Scenario: Entry in English

- **WHEN** the drawer is shown in English to an admin
- **THEN** the section reads "Marketing" and the entry reads "Leads"
