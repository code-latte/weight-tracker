# workout-status Specification

## Purpose

Gives every workout a lifecycle status (Draft, Published, Started, Finished) and decides who may move a workout between statuses and which moves are allowed.

## Requirements

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

### Requirement: Authenticated callers may change a workout's status
The system SHALL expose `PUT /workouts/{workoutId}/status` with a body `{ "status": "<STATUS>" }`. A successful change SHALL respond `200 OK` with the workout in the same shape as `GET /workouts/{id}`, carrying the new status. A request without a valid access token SHALL be rejected with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** a caller sends `PUT /workouts/{workoutId}/status` without an access token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** a caller sends `PUT /workouts/{workoutId}/status` with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Successful change
- **WHEN** the editor of a `DRAFT` workout sets its status to `PUBLISHED`
- **THEN** the response is `200 OK` with `status` `PUBLISHED`
- **AND** a later `GET /workouts/{id}` returns `status` `PUBLISHED`

### Requirement: The status request is structurally valid
A request whose `status` is missing or is not one of the defined statuses SHALL be rejected with `400 Bad Request`, and the workout SHALL be unchanged.

#### Scenario: Missing status
- **WHEN** an authenticated caller sends the request without `status`
- **THEN** the response is `400 Bad Request`

#### Scenario: Unknown status
- **WHEN** an authenticated caller sends `status` `ARCHIVED`
- **THEN** the response is `400 Bad Request`

### Requirement: Status changes on unknown workouts are not found
A status change aimed at a well-formed id that matches no workout SHALL fail with `404 Not Found` for every authenticated caller.

#### Scenario: Unknown workout
- **WHEN** an authenticated caller changes the status of a GUID that matches no workout
- **THEN** the response is `404 Not Found`

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

### Requirement: The prescribing trainer may only publish and unpublish
When a workout has a prescribing trainer, that trainer SHALL be allowed `DRAFT → PUBLISHED` and `PUBLISHED → DRAFT` only. Any other valid business transition attempted by the trainer SHALL be rejected with `403 Forbidden`.

#### Scenario: Trainer publishes
- **WHEN** the prescribing trainer sets a `DRAFT` workout to `PUBLISHED`
- **THEN** the response is `200 OK`

#### Scenario: Trainer unpublishes
- **WHEN** the prescribing trainer sets a `PUBLISHED` workout to `DRAFT`
- **THEN** the response is `200 OK`

#### Scenario: Trainer cannot start the workout
- **WHEN** the prescribing trainer sets a `PUBLISHED` workout to `STARTED`
- **THEN** the response is `403 Forbidden`
- **AND** the status is still `PUBLISHED`

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

### Requirement: The athlete of a workout with no trainer may make every business transition
When a workout has no prescribing trainer, its athlete (`userId`) SHALL be allowed every business transition.

#### Scenario: Self-planned lifecycle
- **WHEN** the athlete of a workout with no trainer moves it `DRAFT → PUBLISHED → DRAFT → PUBLISHED → STARTED → COMPLETED`
- **THEN** every step responds `200 OK`

### Requirement: Unrelated callers may not change the status
A non-admin caller who is neither the prescribing trainer nor the athlete of the workout SHALL be rejected with `403 Forbidden`.

#### Scenario: Another trainer
- **WHEN** a trainer unrelated to the workout changes its status
- **THEN** the response is `403 Forbidden`

#### Scenario: Another user
- **WHEN** a user who is not the workout's athlete changes its status
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

### Requirement: Reaching completed records when it happened
Every time a workout's status becomes `COMPLETED` through `PUT /workouts/{workoutId}/status`, through the lifecycle or an admin override, the system SHALL set its `completedAt` to the server time of the request, in UTC. The request cannot supply it. A later completion SHALL overwrite the earlier value. A workout that was never completed SHALL have `completedAt` `null`.

#### Scenario: Athlete completes a workout
- **WHEN** the athlete sets a `STARTED` workout to `COMPLETED`
- **THEN** the response is `200 OK` with `completedAt` set to the time of the request

#### Scenario: Admin completes a workout skipping steps
- **WHEN** an admin sets a `DRAFT` workout to `COMPLETED`
- **THEN** the response carries `completedAt` set to the time of the request

#### Scenario: Completing again overwrites the date
- **WHEN** an admin reopens a `COMPLETED` workout and it later reaches `COMPLETED` again
- **THEN** its `completedAt` is the time of the latest completion

#### Scenario: Never completed
- **WHEN** a caller reads a workout that never reached `COMPLETED`
- **THEN** its `completedAt` is `null`

### Requirement: Other status changes keep the completion date
A status change whose target is not `COMPLETED`, including an admin moving a workout out of `COMPLETED`, SHALL leave `completedAt` unchanged. A refused status change and a review SHALL leave `completedAt` unchanged.

#### Scenario: Admin reopens a completed workout
- **WHEN** an admin sets a `COMPLETED` workout to `DRAFT`
- **THEN** the response carries `status` `DRAFT` and the same `completedAt` as before

#### Scenario: Refused change
- **WHEN** the athlete of a workout with no trainer sets a `COMPLETED` workout to `STARTED`
- **THEN** the response is `409 Conflict`
- **AND** its `completedAt` is unchanged

#### Scenario: Review keeps the completion date
- **WHEN** the prescribing trainer reviews a `COMPLETED` workout
- **THEN** its `completedAt` is unchanged

### Requirement: Workouts completed before the date existed have no completion date
Workouts that were already `COMPLETED` or `REVIEWED` when completion dates were introduced SHALL have `completedAt` `null` and SHALL still be readable and usable.

#### Scenario: Pre-existing completed workout
- **WHEN** the schema migration runs on a database with a `COMPLETED` workout
- **THEN** that workout is returned with `completedAt` `null`
