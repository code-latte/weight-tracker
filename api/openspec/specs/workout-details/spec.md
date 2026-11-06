# workout-details Specification

## Purpose

Lets authorized callers update an existing workout's own properties (name, description, date) and, for admins, its owner and trainer, through `PUT /workouts/{id}`.

## Requirements

### Requirement: Authenticated callers may update a workout
The system SHALL expose `PUT /workouts/{id}` to authenticated callers. A request without a valid access token SHALL be rejected with `401 Unauthorized` and change nothing.

#### Scenario: No token
- **WHEN** a caller sends `PUT /workouts/{id}` without an access token
- **THEN** the response is `401 Unauthorized`
- **AND** the workout is unchanged

#### Scenario: Invalid token
- **WHEN** a caller sends `PUT /workouts/{id}` with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`
- **AND** the workout is unchanged

### Requirement: The request replaces every editable property
The body SHALL carry `name`, `description`, `workoutDate`, `userId` and `trainerId`, and the workout SHALL take exactly those values. A `null` or missing `description` clears it, and a `null` or missing `trainerId` leaves the workout without trainer. A body without `name` or `userId` SHALL be rejected with `400 Bad Request`. A body without `workoutDate` SHALL be rejected with `400 Bad Request` only when it sends no mesocycle placement. With a placement, `workoutDate` SHALL be ignored and the date SHALL follow the workout mesocycle placement rules.

#### Scenario: Owner renames and reschedules
- **WHEN** the owner of a workout without trainer sends a new `name`, `description` and `workoutDate` with the current `userId` and a `null` `trainerId`
- **THEN** the response is `200 OK`
- **AND** a subsequent `GET /workouts/{id}` returns the new `name`, `description` and `workoutDate`

#### Scenario: Description is cleared
- **WHEN** an authorized caller sends `description` `null` for a workout that has one
- **THEN** the response is `200 OK` with `description` `null`

#### Scenario: Missing required field
- **WHEN** an authorized caller sends a body without `name`, without `userId`, or without `workoutDate` and without a placement
- **THEN** the response is `400 Bad Request`
- **AND** the workout is unchanged

#### Scenario: Placed workout without date
- **WHEN** the trainer updates the published workout in week 1 of the seeded mesocycle, keeping its placement and sending no `workoutDate`
- **THEN** the response is `200 OK`

### Requirement: The update returns the whole workout
A successful update SHALL respond `200 OK` with the same shape as `GET /workouts/{id}`, including `name`, `description`, `status` and the unchanged `exercises`. The update SHALL NOT change the workout's status or exercises.

#### Scenario: Status and exercises are preserved
- **WHEN** the prescribing trainer updates the name of a `PUBLISHED` workout with exercises
- **THEN** the response is `200 OK`
- **AND** `status` is still `PUBLISHED` and `exercises` is unchanged

### Requirement: Name and description are validated
The `name` and `description` sent SHALL follow the same rules as on creation. A `name` that is blank or longer than 256 characters after trimming, or a `description` longer than 1024 characters after trimming, SHALL be rejected with `400 Bad Request` and change nothing.

#### Scenario: Blank name
- **WHEN** an authorized caller sends `name` `"   "`
- **THEN** the response is `400 Bad Request`
- **AND** the workout is unchanged

#### Scenario: Name too long
- **WHEN** an authorized caller sends a `name` of 257 characters
- **THEN** the response is `400 Bad Request`

#### Scenario: Description too long
- **WHEN** an authorized caller sends a `description` of 1025 characters
- **THEN** the response is `400 Bad Request`

### Requirement: Only an admin may change the owner or the trainer
A non-admin caller SHALL send the workout's current `userId` and `trainerId`. A `userId` or `trainerId` different from the current value SHALL be rejected with `403 Forbidden` and change nothing, even when the caller may edit the workout.

#### Scenario: Trainer tries to change the owner
- **WHEN** the prescribing trainer sends another user's id as `userId`
- **THEN** the response is `403 Forbidden`
- **AND** the workout is unchanged

#### Scenario: Owner tries to add a trainer
- **WHEN** the owner of a workout without trainer sends a `trainerId`
- **THEN** the response is `403 Forbidden`

#### Scenario: Trainer omits themselves as trainer
- **WHEN** the prescribing trainer sends `trainerId` `null`
- **THEN** the response is `403 Forbidden`

#### Scenario: Unchanged assignment is accepted
- **WHEN** the prescribing trainer sends the current `userId` and their own id as `trainerId` with a new `name`
- **THEN** the response is `200 OK`

### Requirement: An admin may reassign the owner and the trainer
An `ADMIN` MAY send a different `userId` or `trainerId`. A new owner SHALL exist (`404 Not Found` otherwise) and SHALL NOT be the calling admin. A new trainer SHALL be an existing `TRAINER` or `ADMIN`. The trainer SHALL differ from the owner. Breaking these rules SHALL return `400 Bad Request` and change nothing.

#### Scenario: Admin changes the owner
- **WHEN** an `ADMIN` sends another existing user as `userId`
- **THEN** the response is `200 OK` with that `userId`

#### Scenario: Admin assigns a trainer
- **WHEN** an `ADMIN` sends an existing `TRAINER` as `trainerId` for a workout without trainer
- **THEN** the response is `200 OK` with that `trainerId`

#### Scenario: Admin removes the trainer
- **WHEN** an `ADMIN` sends `trainerId` `null` for a prescribed workout
- **THEN** the response is `200 OK` with `trainerId` `null`

#### Scenario: Unknown new owner
- **WHEN** an `ADMIN` sends a `userId` that matches no user
- **THEN** the response is `404 Not Found`
- **AND** the workout is unchanged

#### Scenario: Admin makes themselves the owner
- **WHEN** an `ADMIN` sends their own id as a new `userId`
- **THEN** the response is `400 Bad Request`

#### Scenario: New trainer cannot train
- **WHEN** an `ADMIN` sends as `trainerId` the id of a `USER`, or an id that matches no user
- **THEN** the response is `400 Bad Request`

#### Scenario: Trainer equals owner
- **WHEN** an `ADMIN` sends the same id as `userId` and `trainerId`
- **THEN** the response is `400 Bad Request`

### Requirement: Read visibility follows the new assignment
After a reassignment, the workout SHALL be visible and hidden according to its new owner and trainer, with the same rules as any other workout.

#### Scenario: Previous owner loses access
- **WHEN** an `ADMIN` moves a workout from one `USER` to another
- **THEN** `GET /workouts/{id}` by the previous owner returns `403 Forbidden`
- **AND** `GET /workouts` by the previous owner does not include it
- **AND** `GET /workouts` by the new owner includes it, unless it is a prescribed `DRAFT`

### Requirement: Unknown workouts are not found
A well-formed id that matches no workout SHALL be rejected with `404 Not Found` for every authenticated caller.

#### Scenario: Missing workout
- **WHEN** an authenticated caller sends `PUT /workouts/{id}` for a GUID that matches no workout
- **THEN** the response is `404 Not Found`

### Requirement: A reassigned owner or trainer must be active
When an `ADMIN` changes a workout's owner or trainer, the new owner and the new trainer SHALL be active. Otherwise the request SHALL be rejected with `400 Bad Request` and change nothing. An owner or trainer that is not changed SHALL NOT be checked, so workouts of a deactivated user stay editable.

#### Scenario: Admin reassigns to a deactivated owner
- **WHEN** an `ADMIN` sends as `userId` the id of a deactivated user
- **THEN** the response is `400 Bad Request`
- **AND** the workout keeps its previous owner

#### Scenario: Admin assigns a deactivated trainer
- **WHEN** an `ADMIN` sends as `trainerId` the id of a deactivated `TRAINER`
- **THEN** the response is `400 Bad Request`
- **AND** the workout keeps its previous trainer

#### Scenario: Unchanged deactivated owner
- **WHEN** an `ADMIN` renames a workout whose owner is deactivated, sending the current `userId` and `trainerId`
- **THEN** the response is `200 OK` with the new name
