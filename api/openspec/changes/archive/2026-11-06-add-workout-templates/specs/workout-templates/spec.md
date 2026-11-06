# Spec Delta

## Purpose

Lets trainers keep reusable workout templates that only they and admins can see and change, and lets admins manage
templates on a trainer's behalf without ever owning one.

## ADDED Requirements

### Requirement: Template routes require an authenticated caller
Every route under `/workout-templates` SHALL require a valid bearer token. A request without a token or with an invalid token SHALL be answered `401` and change nothing.

#### Scenario: No token
- **WHEN** any `/workout-templates` route is called without a token
- **THEN** the response is `401`

#### Scenario: Invalid token
- **WHEN** any `/workout-templates` route is called with an invalid token
- **THEN** the response is `401`

### Requirement: Regular users cannot use templates
A caller with the `USER` role SHALL be refused with `403` on every `/workout-templates` route, and nothing SHALL change.

#### Scenario: User lists templates
- **WHEN** a regular user sends `GET /workout-templates`
- **THEN** the response is `403`

#### Scenario: User creates a template
- **WHEN** a regular user sends `POST /workout-templates`
- **THEN** the response is `403` and no template is created

### Requirement: A template is private to its trainer
A trainer SHALL see and act only on templates they own. Another trainer's template SHALL be answered `404` on every route that names it, exactly as an unknown id, and SHALL stay unchanged.

#### Scenario: Trainer reads another trainer's template
- **WHEN** trainer `A` sends `GET /workout-templates/{id}` for a template owned by trainer `B`
- **THEN** the response is `404`

#### Scenario: Trainer deletes another trainer's template
- **WHEN** trainer `A` sends `DELETE /workout-templates/{id}` for a template owned by trainer `B`
- **THEN** the response is `404` and the template still exists

### Requirement: An admin may act on any template
An admin SHALL read, update, delete and use any template, and change its content, whoever its trainer is. The template's trainer SHALL stay unchanged.

#### Scenario: Admin renames a trainer's template
- **WHEN** an admin sends `PUT /workout-templates/{id}` for a template owned by trainer `T`
- **THEN** the response is `200` with the new name and the trainer is still `T`

### Requirement: An unknown template is not found
Any route naming a template id that matches no template SHALL be answered `404` and change nothing.

#### Scenario: Unknown id
- **WHEN** an admin sends `GET /workout-templates/{id}` with an id that matches no template
- **THEN** the response is `404`

### Requirement: A trainer creates templates for themselves
`POST /workout-templates` from a trainer SHALL create a template owned by the caller. A trainer sending `trainerId` SHALL be refused with `400` and nothing SHALL be created.

#### Scenario: Trainer creates a template
- **WHEN** a trainer creates a template with a name and no `trainerId`
- **THEN** the response is `201` with the template, its trainer is the caller and it has no exercises

#### Scenario: Trainer sends a trainer id
- **WHEN** a trainer creates a template sending any `trainerId`
- **THEN** the response is `400` and no template is created

### Requirement: An admin creates templates for an active trainer
`POST /workout-templates` from an admin SHALL require `trainerId`, naming an existing, active user with the `TRAINER` role, who SHALL own the template. A missing `trainerId`, or one naming an admin, a regular user, a deactivated trainer or no user, SHALL be refused with `400`.

#### Scenario: Admin creates a template for a trainer
- **WHEN** an admin creates a template with `trainerId` of active trainer `T`
- **THEN** the response is `201` and the template's trainer is `T`, not the admin

#### Scenario: Admin omits the trainer
- **WHEN** an admin creates a template without `trainerId`
- **THEN** the response is `400` and no template is created

#### Scenario: Admin names themselves or another admin
- **WHEN** an admin creates a template whose `trainerId` is an admin
- **THEN** the response is `400` and no template is created

#### Scenario: Admin names a regular user
- **WHEN** an admin creates a template whose `trainerId` is a regular user
- **THEN** the response is `400`

#### Scenario: Admin names a deactivated trainer
- **WHEN** an admin creates a template whose `trainerId` is a deactivated trainer
- **THEN** the response is `400`

### Requirement: Template name and description follow the workout rules
A template SHALL have a `name` trimmed and between 1 and 256 characters, and an optional `description` with the same rules and limits as a workout description. A missing, blank or too long name, or an invalid description, SHALL be refused with `400` and change nothing.

#### Scenario: Blank name
- **WHEN** a trainer creates a template with a blank name
- **THEN** the response is `400` and no template is created

#### Scenario: No description
- **WHEN** a trainer creates a template without a description
- **THEN** the response is `201` with `description` `null`

### Requirement: Update replaces name and description
`PUT /workout-templates/{id}` SHALL replace the template's name and description and leave its trainer and content unchanged. Omitting the description SHALL clear it.

#### Scenario: Owner renames the template
- **WHEN** the owning trainer sends a new name and no description
- **THEN** the response is `200` with the new name, `description` `null`, and the same exercises

### Requirement: Delete removes the template and its content
`DELETE /workout-templates/{id}` SHALL remove the template with all its exercises and sets and answer `204`. Workouts created from it SHALL be unaffected.

#### Scenario: Owner deletes a template
- **WHEN** the owning trainer deletes their template
- **THEN** the response is `204` and `GET /workout-templates/{id}` answers `404`

### Requirement: Template responses carry the template and its content
Reading, creating or updating a template SHALL return its `id`, `name`, `description` (or `null`), `trainerId` and its exercises in order, each with effort, sets, per side, rest and superseries. A template SHALL carry no athlete, date, status or mesocycle.

#### Scenario: Read a template
- **WHEN** the owning trainer sends `GET /workout-templates/{id}` for a template with two exercises in a superseries
- **THEN** the response is `200` with both exercises in order, each showing the same superseries

#### Scenario: Create returns a location
- **WHEN** a template is created
- **THEN** the response carries a `Location` header pointing at `GET /workout-templates/{id}`

### Requirement: Listing templates is scoped by role
`GET /workout-templates` SHALL return to a trainer only the templates they own, and to an admin every template. An admin MAY filter by `trainerId`. A trainer sending `trainerId` SHALL still receive only their own templates.

#### Scenario: Trainer lists templates
- **WHEN** trainer `A` lists templates while trainer `B` also has templates
- **THEN** the response contains every template of `A` and none of `B`

#### Scenario: Admin lists templates
- **WHEN** an admin lists templates
- **THEN** the response contains the templates of both `A` and `B`

#### Scenario: Admin filters by trainer
- **WHEN** an admin lists templates with `trainerId` of `A`
- **THEN** the response contains every template of `A` and none of `B`

#### Scenario: Trainer tries another trainer's filter
- **WHEN** trainer `A` lists templates with `trainerId` of `B`
- **THEN** the response contains only templates of `A`

### Requirement: Templates are persisted with their content
Saving and loading a template SHALL preserve its name, description, trainer, and exercises in order with effort, sets, per side, rest and superseries membership.

#### Scenario: Round trip
- **WHEN** a template with exercises and a superseries is saved and loaded again
- **THEN** the loaded template equals the saved one
