# Spec Delta

## MODIFIED Requirements

### Requirement: Every workout has a status
Every workout SHALL have exactly one status out of `DRAFT`, `PUBLISHED`, `STARTED`, `COMPLETED` or `REVIEWED`. Workouts that existed before statuses were introduced SHALL be `DRAFT`. Workouts that were `FINISHED` before the rename SHALL be `COMPLETED`.

#### Scenario: Status is part of the workout
- **WHEN** a caller allowed to see a workout reads it
- **THEN** the response carries its `status`

#### Scenario: Pre-existing workouts become drafts
- **WHEN** the schema migration runs on a database with existing workouts
- **THEN** every existing workout has status `DRAFT`

#### Scenario: Finished workouts become completed
- **WHEN** a workout stored as finished before the rename is read
- **THEN** its `status` is `COMPLETED`

### Requirement: The athlete of a workout with no trainer may make every business transition
When a workout has no prescribing trainer, its athlete (`userId`) SHALL be allowed every business transition.

#### Scenario: Self-planned lifecycle
- **WHEN** the athlete of a workout with no trainer moves it `DRAFT → PUBLISHED → DRAFT → PUBLISHED → STARTED → COMPLETED`
- **THEN** every step responds `200 OK`

## ADDED Requirements

### Requirement: Only the lifecycle transitions are allowed for non-admins
For a non-admin caller whom the role rules allow to act on the workout, the only valid transitions through `PUT /workouts/{workoutId}/status` SHALL be `DRAFT → PUBLISHED`, `PUBLISHED → DRAFT`, `PUBLISHED → STARTED` and `STARTED → COMPLETED`. Any other target, including the current status, SHALL be rejected with `409 Conflict`, and the workout SHALL be unchanged. Callers the role rules refuse get `403 Forbidden` instead, as the requirements below state.

#### Scenario: Skipping a step
- **WHEN** the athlete of a workout with no trainer sets a `DRAFT` workout to `STARTED`
- **THEN** the response is `409 Conflict`
- **AND** the status is still `DRAFT`

#### Scenario: Going back from started
- **WHEN** the athlete of a workout with no trainer sets a `STARTED` workout to `PUBLISHED`
- **THEN** the response is `409 Conflict`

#### Scenario: Completed is final without a trainer
- **WHEN** the athlete of a workout with no trainer sets a `COMPLETED` workout to any other status
- **THEN** the response is `409 Conflict`

#### Scenario: Same status
- **WHEN** the editor sets a `PUBLISHED` workout to `PUBLISHED`
- **THEN** the response is `409 Conflict`

### Requirement: Admins may set any status except reviewed
A caller with the `ADMIN` role SHALL be allowed to set any workout to any status except `REVIEWED` through `PUT /workouts/{workoutId}/status`, ignoring the business transitions, as long as the workout is not `REVIEWED`. Setting the current status SHALL succeed and change nothing.

#### Scenario: Admin reopens a completed workout
- **WHEN** an admin sets a `COMPLETED` workout to `DRAFT`
- **THEN** the response is `200 OK` with `status` `DRAFT`

#### Scenario: Admin skips steps
- **WHEN** an admin sets a `DRAFT` workout to `COMPLETED`
- **THEN** the response is `200 OK` with `status` `COMPLETED`

### Requirement: The athlete of a prescribed workout may only start and complete it
When a workout has a prescribing trainer, its athlete (`userId`) SHALL be allowed `PUBLISHED → STARTED` and `STARTED → COMPLETED` only. `PUBLISHED → DRAFT` SHALL be rejected with `403 Forbidden`. A `DRAFT` workout SHALL NOT be changeable by its athlete. The request SHALL be rejected with `403 Forbidden`.

#### Scenario: Athlete starts a published workout
- **WHEN** the athlete of a prescribed `PUBLISHED` workout sets it to `STARTED`
- **THEN** the response is `200 OK`

#### Scenario: Athlete completes a started workout
- **WHEN** the athlete of a prescribed `STARTED` workout sets it to `COMPLETED`
- **THEN** the response is `200 OK`

#### Scenario: Athlete cannot unpublish
- **WHEN** the athlete of a prescribed `PUBLISHED` workout sets it to `DRAFT`
- **THEN** the response is `403 Forbidden`

#### Scenario: Athlete cannot touch a draft
- **WHEN** the athlete of a prescribed `DRAFT` workout sets it to `PUBLISHED`
- **THEN** the response is `403 Forbidden`

### Requirement: Reviewed is reached only through the review
`PUT /workouts/{workoutId}/status` SHALL NOT set a workout to `REVIEWED` nor move a `REVIEWED` workout to another status, for every caller including an admin. A caller whom the role rules allow to act on the workout SHALL get `409 Conflict` and the workout SHALL be unchanged. `REVIEWED` is reached only through the review route.

#### Scenario: Trainer sets reviewed through the status route
- **WHEN** the prescribing trainer sets a `COMPLETED` workout to `REVIEWED` through the status route
- **THEN** the response is `409 Conflict`
- **AND** the status is still `COMPLETED`

#### Scenario: Admin sets reviewed through the status route
- **WHEN** an admin sets a `COMPLETED` prescribed workout to `REVIEWED` through the status route
- **THEN** the response is `409 Conflict`

#### Scenario: Admin reopens a reviewed workout
- **WHEN** an admin sets a `REVIEWED` workout to `COMPLETED`
- **THEN** the response is `409 Conflict`
- **AND** the status is still `REVIEWED` with its review metadata

## REMOVED Requirements

### Requirement: Only the business transitions are allowed for non-admins
**Reason**: The lifecycle now ends in `COMPLETED` instead of `FINISHED`.
**Migration**: Replaced by "Only the lifecycle transitions are allowed for non-admins".

### Requirement: Admins may set any status
**Reason**: Admins can no longer set or leave `REVIEWED`, and `FINISHED` is now `COMPLETED`.
**Migration**: Replaced by "Admins may set any status except reviewed".

### Requirement: The athlete of a prescribed workout may only move it forward
**Reason**: The forward moves now end in `COMPLETED` instead of `FINISHED`.
**Migration**: Replaced by "The athlete of a prescribed workout may only start and complete it".
