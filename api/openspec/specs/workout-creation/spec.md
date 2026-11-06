# workout-creation Specification

## Purpose

Lets authenticated callers create a workout, and decides who owns it and who prescribed it from the caller's role and the request.

## Requirements

### Requirement: Authenticated callers may create a workout
The system SHALL expose `POST /workouts` to every authenticated caller, whatever their role. A request without a valid access token SHALL be rejected with `401 Unauthorized` and create nothing.

#### Scenario: No token
- **WHEN** a caller sends `POST /workouts` without an access token
- **THEN** the response is `401 Unauthorized`
- **AND** no workout is created

#### Scenario: Invalid token
- **WHEN** a caller sends `POST /workouts` with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`
- **AND** no workout is created

### Requirement: The workout date is required and stored without time
A request without a mesocycle placement SHALL carry `workoutDate` as a calendar date. A request without a placement and without `workoutDate` SHALL be rejected with `400 Bad Request`. A value that is not a date SHALL be rejected with `400 Bad Request`, with or without a placement. The created workout SHALL keep only the date. With a placement, the date SHALL follow the workout mesocycle placement rules instead.

#### Scenario: Missing date
- **WHEN** an authenticated caller sends `POST /workouts` without `workoutDate` and without a placement
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: Date is returned as sent
- **WHEN** a caller creates a workout with `workoutDate` `2026-10-20` and no placement
- **THEN** the response body's `workoutDate` is `2026-10-20`

#### Scenario: Placed workout without date
- **WHEN** the seeded trainer creates a workout for the seeded athlete in week 3 of the seeded mesocycle without `workoutDate`
- **THEN** the response is `201 Created`

### Requirement: Creation returns the new workout
A successful creation SHALL respond `201 Created` with the new workout: a server-generated `id`, `name`, `description` (or `null`), `workoutDate`, `userId`, `trainerId` (or `null`), `status` and an empty `exercises` list. The response SHALL carry a `Location` header with the URL of `GET /workouts/{id}` for the new workout. The new workout SHALL be visible through `GET /workouts` to its trainer when set, to admins, and to its owner when it has no trainer. A prescribed workout SHALL stay hidden from its owner while it is `DRAFT`.

#### Scenario: Created workout is readable
- **WHEN** a caller creates a workout successfully
- **THEN** the response is `201 Created` with the workout, its `name` and `description`, `status` `DRAFT` and an empty `exercises` list
- **AND** `GET /workouts` by the caller includes a workout with that `id`

#### Scenario: Location points at the new workout
- **WHEN** a caller creates a workout successfully
- **THEN** the `Location` header ends with `/workouts/{id}` of the created workout
- **AND** following it as the same caller returns `200 OK` with that workout

#### Scenario: Prescribed draft is hidden from its owner
- **WHEN** a trainer prescribes a workout to a user
- **THEN** `GET /workouts` by that user does not include the new workout

### Requirement: A regular user creates only their own workout
When the caller has the `USER` role, the system SHALL make the caller the owner and leave the trainer empty. `userId` MAY be omitted or equal to the caller. A different `userId` or any `trainerId` SHALL be rejected with `400 Bad Request`.

#### Scenario: User creates a workout without ids
- **WHEN** a `USER` sends only `name` and `workoutDate`
- **THEN** the response is `201 Created`
- **AND** `userId` is the caller and `trainerId` is `null`

#### Scenario: User sends their own id
- **WHEN** a `USER` sends their own id as `userId`
- **THEN** the response is `201 Created` with the caller as owner

#### Scenario: User names another owner
- **WHEN** a `USER` sends another user's id as `userId`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: User names a trainer
- **WHEN** a `USER` sends any `trainerId`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

### Requirement: A trainer prescribes workouts to regular users
When the caller has the `TRAINER` role, the system SHALL require `userId`, make it the owner and make the caller the trainer. A missing `userId`, a `userId` equal to the caller, or any `trainerId` SHALL be rejected with `400 Bad Request`.

#### Scenario: Trainer prescribes a workout
- **WHEN** a `TRAINER` sends the id of an existing `USER` as `userId`
- **THEN** the response is `201 Created`
- **AND** `userId` is that user and `trainerId` is the caller

#### Scenario: Trainer omits the owner
- **WHEN** a `TRAINER` sends no `userId`
- **THEN** the response is `400 Bad Request`

#### Scenario: Trainer targets themselves
- **WHEN** a `TRAINER` sends their own id as `userId`
- **THEN** the response is `400 Bad Request`

#### Scenario: Trainer names a trainer
- **WHEN** a `TRAINER` sends any `trainerId`
- **THEN** the response is `400 Bad Request`

### Requirement: A trainer may only prescribe to users with the USER role
When a `TRAINER` names an owner who exists but does not have the `USER` role, the system SHALL reject the request with `403 Forbidden`.

#### Scenario: Trainer targets an admin or another trainer
- **WHEN** a `TRAINER` sends the id of an existing `ADMIN` or `TRAINER` as `userId`
- **THEN** the response is `403 Forbidden`
- **AND** no workout is created

### Requirement: An admin creates workouts for other users
When the caller has the `ADMIN` role, the system SHALL require `userId` and make it the owner. A missing `userId`, or a `userId` equal to the caller, SHALL be rejected with `400 Bad Request`. Any existing user other than the caller MAY be the owner.

#### Scenario: Admin creates a workout without a trainer
- **WHEN** an `ADMIN` sends the id of another existing user as `userId` and no `trainerId`
- **THEN** the response is `201 Created`
- **AND** `userId` is that user and `trainerId` is `null`

#### Scenario: Admin omits the owner
- **WHEN** an `ADMIN` sends no `userId`
- **THEN** the response is `400 Bad Request`

#### Scenario: Admin targets themselves
- **WHEN** an `ADMIN` sends their own id as `userId`
- **THEN** the response is `400 Bad Request`

### Requirement: Only an admin may assign a trainer, and it must be able to train
An `ADMIN` MAY send `trainerId`. It SHALL reference an existing user with the `TRAINER` or `ADMIN` role and SHALL differ from `userId`. Otherwise the request SHALL be rejected with `400 Bad Request`.

#### Scenario: Admin assigns a trainer
- **WHEN** an `ADMIN` sends an existing `USER` as `userId` and an existing `TRAINER` as `trainerId`
- **THEN** the response is `201 Created`
- **AND** `trainerId` is that trainer

#### Scenario: Admin assigns a user who cannot train
- **WHEN** an `ADMIN` sends as `trainerId` the id of a `USER`, or an id that matches no user
- **THEN** the response is `400 Bad Request`

#### Scenario: Admin makes the owner their own trainer
- **WHEN** an `ADMIN` sends the same id as `userId` and `trainerId`
- **THEN** the response is `400 Bad Request`

### Requirement: The owner must exist
When the request names an owner through `userId` and no user has that id, the system SHALL reject it with `404 Not Found` after the role-dependent `400` checks.

#### Scenario: Unknown owner
- **WHEN** a `TRAINER` or `ADMIN` sends a `userId` that matches no user
- **THEN** the response is `404 Not Found`
- **AND** no workout is created

### Requirement: New workouts start as drafts
Every workout created through `POST /workouts` SHALL have status `DRAFT`, whatever the caller's role. The request SHALL NOT be able to choose the initial status.

#### Scenario: User creates a draft
- **WHEN** a user creates their own workout
- **THEN** the response carries `status` `DRAFT`

#### Scenario: Trainer creates a draft
- **WHEN** a trainer prescribes a workout to a user
- **THEN** the response carries `status` `DRAFT`

### Requirement: The workout name is required
The request SHALL carry `name`. It SHALL be trimmed, and SHALL NOT be blank or longer than 256 characters after trimming. A missing, blank or too long `name` SHALL be rejected with `400 Bad Request` and create nothing.

#### Scenario: Name is stored trimmed
- **WHEN** a caller creates a workout with `name` `"  Leg day  "`
- **THEN** the response is `201 Created` with `name` `"Leg day"`

#### Scenario: Missing name
- **WHEN** an authenticated caller sends `POST /workouts` without `name`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: Blank name
- **WHEN** an authenticated caller sends `name` `"   "`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: Name at the limit
- **WHEN** a caller sends a `name` of exactly 256 characters
- **THEN** the response is `201 Created`

#### Scenario: Name too long
- **WHEN** a caller sends a `name` of 257 characters
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

### Requirement: The workout description is optional
The request MAY carry `description`. It SHALL be trimmed and SHALL NOT be longer than 1024 characters after trimming, or the request SHALL be rejected with `400 Bad Request`. A missing, `null` or blank `description` SHALL be stored and returned as `null`.

#### Scenario: Workout without description
- **WHEN** a caller creates a workout without `description`
- **THEN** the response is `201 Created` with `description` `null`

#### Scenario: Blank description
- **WHEN** a caller creates a workout with `description` `"   "`
- **THEN** the response is `201 Created` with `description` `null`

#### Scenario: Description at the limit
- **WHEN** a caller sends a `description` of exactly 1024 characters
- **THEN** the response is `201 Created` with that description

#### Scenario: Description too long
- **WHEN** a caller sends a `description` of 1025 characters
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

### Requirement: Existing workouts get a default name
Workouts that existed before names were introduced SHALL be named `Workout` and have no description.

#### Scenario: Migrated workout
- **WHEN** a workout created before this change is read
- **THEN** its `name` is `Workout` and its `description` is `null`

### Requirement: The owner and the trainer of a new workout must be active
When a `TRAINER` or `ADMIN` creates a workout, the named owner SHALL be active, and a trainer named by an `ADMIN` SHALL be active. Otherwise the request SHALL be rejected with `400 Bad Request` and no workout SHALL be created. An unknown owner keeps answering `404 Not Found`.

#### Scenario: Trainer prescribes to a deactivated user
- **WHEN** a `TRAINER` sends as `userId` the id of a deactivated `USER`
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: Admin creates a workout for a deactivated user
- **WHEN** an `ADMIN` sends as `userId` the id of a deactivated user
- **THEN** the response is `400 Bad Request`

#### Scenario: Admin assigns a deactivated trainer
- **WHEN** an `ADMIN` sends an active `USER` as `userId` and a deactivated `TRAINER` as `trainerId`
- **THEN** the response is `400 Bad Request`

#### Scenario: Reactivated user can be assigned again
- **WHEN** a `TRAINER` prescribes a workout to a `USER` who was deactivated and then activated
- **THEN** the response is `201 Created`
