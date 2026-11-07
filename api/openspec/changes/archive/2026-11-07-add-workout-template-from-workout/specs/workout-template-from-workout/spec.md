# Spec Delta

## Purpose

Lets a trainer, or an admin on the trainer's behalf, save an existing workout as a new workout template owned by the
workout's trainer, by copying the workout's content.

## ADDED Requirements

### Requirement: Saving a workout as a template requires an authenticated caller
`POST /workouts/{workoutId}/templates` SHALL require a valid bearer token. A request without a token or with an invalid token SHALL be answered `401` and create nothing.

#### Scenario: No token
- **WHEN** the route is called without a token
- **THEN** the response is `401` and no template is created

#### Scenario: Invalid token
- **WHEN** the route is called with an invalid token
- **THEN** the response is `401` and no template is created

### Requirement: Regular users cannot save workouts as templates
A caller with the `USER` role SHALL be refused with `403`, including the owner of the workout, and nothing SHALL be created.

#### Scenario: Owner of a prescribed workout
- **WHEN** the athlete who owns a prescribed workout calls the route for it
- **THEN** the response is `403` and no template is created

#### Scenario: Owner of a self-planned workout
- **WHEN** a regular user calls the route for their own workout without a trainer
- **THEN** the response is `403`

### Requirement: The workout must exist
A `workoutId` that matches no workout SHALL be answered `404` and create nothing.

#### Scenario: Unknown workout
- **WHEN** an admin calls the route with an id that matches no workout
- **THEN** the response is `404`

### Requirement: A trainer saves only the workouts they prescribe
A trainer SHALL save a workout as a template only when they are the workout's trainer. A workout prescribed by another trainer, or a workout without a trainer, SHALL be refused with `403` and nothing SHALL be created.

#### Scenario: Trainer saves their prescribed workout
- **WHEN** a trainer calls the route for a workout they prescribe
- **THEN** the response is `201` and the template's `trainerId` is the caller

#### Scenario: Workout of another trainer
- **WHEN** trainer `A` calls the route for a workout prescribed by trainer `B`
- **THEN** the response is `403` and no template is created

#### Scenario: Trainer's own workout without trainer
- **WHEN** a trainer calls the route for a workout they own that has no trainer
- **THEN** the response is `403`

### Requirement: An admin saves workouts for their trainer
An admin SHALL save any workout that has a trainer. The template SHALL belong to the workout's trainer, never to the admin. A workout without a trainer SHALL be refused with `400` and nothing SHALL be created.

#### Scenario: Admin saves a prescribed workout
- **WHEN** an admin calls the route for a workout prescribed by trainer `T`
- **THEN** the response is `201` and the template's `trainerId` is `T`

#### Scenario: Admin on a self-planned workout
- **WHEN** an admin calls the route for a workout without a trainer
- **THEN** the response is `400` and no template is created

### Requirement: The workout's trainer must be able to own templates
The workout's trainer SHALL be an existing, active user with the `TRAINER` role. A workout whose trainer is an admin, or a deactivated trainer, SHALL be refused with `400` and nothing SHALL be created.

#### Scenario: Workout trainer is an admin
- **WHEN** an admin calls the route for a workout whose trainer is an admin
- **THEN** the response is `400`

#### Scenario: Workout trainer is deactivated
- **WHEN** an admin calls the route for a workout whose trainer has been deactivated
- **THEN** the response is `400` and no template is created

### Requirement: The template copies the workout
The template SHALL have the workout's name, description and exercises in the same order, each with the same exercise, effort, sets, per-side flag, rest and superseries grouping. Every copied entry SHALL get a new entry identifier. The request SHALL carry no body.

#### Scenario: Copy a workout with a superseries
- **WHEN** a workout with three exercises, two of them in a superseries, is saved as a template
- **THEN** the template has the same three exercises in the same order with the same prescriptions, and the same two share a superseries

#### Scenario: Entry identifiers are new
- **WHEN** a workout is saved as a template
- **THEN** no entry identifier of the template equals one of the workout's

#### Scenario: Workout without exercises
- **WHEN** a workout with no exercises is saved as a template
- **THEN** the response is `201` with an empty `exercises` list

#### Scenario: Archived exercise in the workout
- **WHEN** a workout uses a custom exercise its trainer later archived and the workout is saved as a template
- **THEN** the template contains that exercise

### Requirement: Any workout status may be saved
Saving SHALL only read the workout, so a workout SHALL be saveable in any status, and the workout SHALL stay unchanged.

#### Scenario: Started workout
- **WHEN** the trainer saves a workout that is `STARTED`
- **THEN** the response is `201` and the workout keeps its status, owner, date and exercises

### Requirement: Saving returns the new template
A successful call SHALL answer `201` with the template in the same shape as `GET /workout-templates/{id}`, and a `Location` header pointing at `GET /workout-templates/{id}`. The new template SHALL then be listed for its trainer and hidden from other trainers.

#### Scenario: Response and listing
- **WHEN** a trainer saves their workout as a template
- **THEN** the response is `201` with the template, `Location` points at `GET /workout-templates/{id}`, and the template appears in that trainer's `GET /workout-templates` but not in another trainer's

### Requirement: The template is independent of the workout
The template SHALL keep no link to the workout. Later changes to the workout SHALL NOT change the template, and saving the same workout again SHALL create another template.

#### Scenario: Workout edited afterwards
- **WHEN** a workout is saved as a template and an exercise is then removed from the workout
- **THEN** the template still has that exercise

#### Scenario: Saved twice
- **WHEN** the same workout is saved as a template twice
- **THEN** two templates with different ids exist
