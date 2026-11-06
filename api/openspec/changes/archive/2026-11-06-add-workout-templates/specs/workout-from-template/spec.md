# Spec Delta

## Purpose

Lets a trainer, or an admin on their behalf, turn a workout template into a real workout for an athlete by copying its
content, standalone or placed in a mesocycle week, under the same rules as prescribing a workout directly.

## ADDED Requirements

### Requirement: Creating a workout from a template follows the template access rules
`POST /workout-templates/{id}/workouts` SHALL be allowed to the template's trainer and to admins only: `401` without a valid token, `403` for a regular user, `404` for an unknown template or another trainer's template. Nothing SHALL be created on refusal.

#### Scenario: No token
- **WHEN** the route is called without a token
- **THEN** the response is `401`

#### Scenario: Invalid token
- **WHEN** the route is called with an invalid token
- **THEN** the response is `401`

#### Scenario: Regular user
- **WHEN** a regular user calls the route
- **THEN** the response is `403` and no workout is created

#### Scenario: Another trainer's template
- **WHEN** trainer `A` calls the route for a template owned by trainer `B`
- **THEN** the response is `404` and no workout is created

### Requirement: The created workout copies the template
The created workout SHALL have the template's name, description and exercises in the same order, each with the same exercise, effort, sets, per-side flag, rest and superseries grouping. Every copied entry SHALL get a new entry identifier.

#### Scenario: Copy a full template
- **WHEN** a workout is created from a template with three exercises, two of them in a superseries
- **THEN** the workout has the same three exercises in the same order with the same prescriptions, and the same two share a superseries

#### Scenario: Entry identifiers are new
- **WHEN** a workout is created from a template
- **THEN** no exercise entry identifier of the workout equals one of the template's

#### Scenario: Empty template
- **WHEN** a workout is created from a template with no exercises
- **THEN** the response is `201` with an empty `exercises` list

### Requirement: The workout's trainer is the template's trainer
The created workout SHALL have the athlete named by `userId` as owner and the template's trainer as trainer, whoever the caller is. The request SHALL NOT carry a trainer. The workout SHALL start as `DRAFT`.

#### Scenario: Trainer uses their template
- **WHEN** trainer `T` creates a workout from their template for athlete `A`
- **THEN** the workout's owner is `A`, its trainer is `T` and its status is `DRAFT`

#### Scenario: Admin uses a trainer's template
- **WHEN** an admin creates a workout from trainer `T`'s template for athlete `A`
- **THEN** the workout's trainer is `T`, not the admin

### Requirement: The athlete must be a valid owner
`userId` SHALL be required and name an existing, active user with the `USER` role. A missing `userId` SHALL be `400` and an unknown one `404`. A non-regular user SHALL be `403` for a trainer, as in `POST /workouts`, and `400` for an admin. A deactivated user SHALL be `400`.

#### Scenario: Missing user
- **WHEN** the request has no `userId`
- **THEN** the response is `400` and no workout is created

#### Scenario: Unknown user
- **WHEN** `userId` matches no user
- **THEN** the response is `404`

#### Scenario: Trainer names another trainer
- **WHEN** a trainer creates a workout from their template with `userId` of another trainer
- **THEN** the response is `403`

#### Scenario: Admin names a trainer
- **WHEN** an admin creates a workout from a template with `userId` of a trainer
- **THEN** the response is `400`

#### Scenario: Deactivated athlete
- **WHEN** `userId` names a deactivated user
- **THEN** the response is `400`

### Requirement: The template's trainer must be active
When the template's trainer is deactivated, creating a workout from the template SHALL be refused with `400`, as `POST /workouts` refuses a deactivated trainer.

#### Scenario: Admin uses a deactivated trainer's template
- **WHEN** an admin creates a workout from a template whose trainer is deactivated
- **THEN** the response is `400` and no workout is created

### Requirement: The workout is standalone or placed in a mesocycle week
The request SHALL carry either `workoutDate`, or `mesocycleId` and `mesocycleWeekNumber` together, with the same rules as `POST /workouts`. A placed workout SHALL take the date of its week and SHALL pass the mesocycle placement checks, with the template's trainer as the workout's trainer.

#### Scenario: Standalone
- **WHEN** a workout is created from a template with `workoutDate` and no placement
- **THEN** the workout has that date and no mesocycle

#### Scenario: Placed in a week
- **WHEN** the template's trainer creates a workout from it in week 2 of their mesocycle for that mesocycle's athlete
- **THEN** the workout is placed in week 2 and dated by that week

#### Scenario: No date and no placement
- **WHEN** the request has neither `workoutDate` nor a placement
- **THEN** the response is `400`

#### Scenario: Only half the placement
- **WHEN** the request has `mesocycleId` without `mesocycleWeekNumber`
- **THEN** the response is `400`

#### Scenario: Mesocycle of another trainer
- **WHEN** an admin creates a workout from trainer `T`'s template in a mesocycle prepared by trainer `U`
- **THEN** the request is refused as `POST /workouts` refuses a trainer mismatch, and no workout is created

#### Scenario: Mesocycle of another athlete
- **WHEN** the placement names a mesocycle whose athlete is not `userId`
- **THEN** the request is refused as `POST /workouts` refuses an athlete mismatch

### Requirement: Creation returns the new workout
A successful creation SHALL answer `201` with the new workout in the same shape as `POST /workouts`, including its copied exercises, and a `Location` header pointing at `GET /workouts/{id}`.

#### Scenario: Response
- **WHEN** a workout is created from a template
- **THEN** the response is `201`, its body is the workout and `Location` points at `GET /workouts/{id}`

### Requirement: The workout is independent of its template
A workout created from a template SHALL keep no link to it. Later changes to the template's name, description or content, or deleting the template, SHALL NOT change the workout.

#### Scenario: Template edited afterwards
- **WHEN** a workout is created from a template and an exercise is then removed from the template
- **THEN** the workout still has that exercise

#### Scenario: Template deleted afterwards
- **WHEN** a workout is created from a template and the template is then deleted
- **THEN** `GET /workouts/{id}` still returns the workout with all its exercises

### Requirement: Copied exercises are not re-checked
The copy SHALL include every exercise of the template as it is, including an exercise archived after it was added to the template.

#### Scenario: Archived exercise in the template
- **WHEN** a template uses an exercise its trainer later archived and a workout is created from it
- **THEN** the workout contains that exercise
