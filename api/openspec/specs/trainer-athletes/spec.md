# trainer-athletes Specification

## Purpose

Records which trainer prepares which athlete over time: an administrator links them, the relationship can be ended by
either side or an administrator, and trainers and administrators list a trainer's current athletes.

## Requirements

### Requirement: Trainer–athlete routes require authentication
`POST /trainer-athletes/{trainerId}`, `PUT /trainer-athletes/{trainerId}/end`, `GET /trainer-athletes` and `GET /trainer-athletes/{trainerId}` SHALL respond `401 Unauthorized` without an access token or with an invalid one, and SHALL change no relationship.

#### Scenario: No token
- **WHEN** any trainer–athlete route is called without an access token
- **THEN** the response is `401 Unauthorized` and no relationship is created or changed

#### Scenario: Invalid token
- **WHEN** any trainer–athlete route is called with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized` and no relationship is created or changed

### Requirement: Only administrators link athletes directly
`POST /trainer-athletes/{trainerId}` SHALL be available only to `ADMIN` callers. `TRAINER` and `USER` callers SHALL get `403 Forbidden` and no relationship SHALL be created.

#### Scenario: Trainer caller
- **WHEN** a `TRAINER` calls `POST /trainer-athletes/{trainerId}`, including with their own id
- **THEN** the response is `403 Forbidden` and no relationship is created

#### Scenario: Athlete caller
- **WHEN** a `USER` calls `POST /trainer-athletes/{trainerId}` with their own id as `athleteId`
- **THEN** the response is `403 Forbidden` and no relationship is created

### Requirement: An administrator links an athlete to a trainer
For an `ADMIN`, `POST /trainer-athletes/{trainerId}` with body `{ "athleteId": "..." }` SHALL create an `ACTIVE` relationship starting now and respond `201 Created` with `id`, `trainerId`, `athleteId`, `status`, `startedAtUtc` and `finishedAtUtc` (`null`).

#### Scenario: Link
- **WHEN** an `ADMIN` links an active `USER` without an active trainer to an active `TRAINER`
- **THEN** the response is `201 Created` with status `ACTIVE` and `finishedAtUtc` `null`, and the athlete appears in that trainer's list

### Requirement: Link targets must exist
`POST /trainer-athletes/{trainerId}` SHALL respond `404 Not Found` and create nothing when no user has the trainer id or the athlete id.

#### Scenario: Unknown trainer
- **WHEN** an `ADMIN` links an athlete to a trainer id that does not exist
- **THEN** the response is `404 Not Found` and no relationship is created

#### Scenario: Unknown athlete
- **WHEN** an `ADMIN` links an `athleteId` that does not exist
- **THEN** the response is `404 Not Found` and no relationship is created

### Requirement: Roles of the linked users
`POST /trainer-athletes/{trainerId}` SHALL respond `409 Conflict` and create nothing when the trainer's role is not `TRAINER`, the athlete's role is not `USER`, or either user is deactivated. An `ADMIN` SHALL NOT qualify as trainer.

#### Scenario: Trainer is an admin
- **WHEN** an `ADMIN` links an athlete to a user whose role is `ADMIN`
- **THEN** the response is `409 Conflict` and no relationship is created

#### Scenario: Athlete is a trainer
- **WHEN** an `ADMIN` links a `TRAINER` as athlete
- **THEN** the response is `409 Conflict` and no relationship is created

#### Scenario: Deactivated user
- **WHEN** an `ADMIN` links a deactivated athlete, or links to a deactivated trainer
- **THEN** the response is `409 Conflict` and no relationship is created

### Requirement: An athlete has at most one active trainer
`POST /trainer-athletes/{trainerId}` SHALL respond `409 Conflict` and create nothing when the athlete already has an `ACTIVE` relationship, with that trainer or any other.

#### Scenario: Already linked to another trainer
- **WHEN** an `ADMIN` links an athlete who has an active relationship with a different trainer
- **THEN** the response is `409 Conflict` and no relationship is created

#### Scenario: Already linked to the same trainer
- **WHEN** an `ADMIN` links an athlete to the trainer they already have an active relationship with
- **THEN** the response is `409 Conflict` and no relationship is created

### Requirement: A pair can be linked again after ending
Ended relationships SHALL be kept, and SHALL NOT block a new relationship between the same trainer and athlete or between the athlete and another trainer.

#### Scenario: Same pair again
- **WHEN** an `ADMIN` ends a relationship and then links the same athlete to the same trainer
- **THEN** the response is `201 Created` with a new `id`, and the ended relationship stays `ENDED`

### Requirement: Invalid link input is rejected
`POST /trainer-athletes/{trainerId}` SHALL respond `400 Bad Request` and create nothing when the body or `athleteId` is missing or `athleteId` is not a valid id.

#### Scenario: Missing athlete id
- **WHEN** an `ADMIN` calls `POST /trainer-athletes/{trainerId}` with an empty body
- **THEN** the response is `400 Bad Request` and no relationship is created

### Requirement: Who can end a relationship
`PUT /trainer-athletes/{trainerId}/end` with body `{ "athleteId": "..." }` SHALL be available to an `ADMIN`, to the caller whose id is `trainerId`, and to the caller whose id is `athleteId`. Anyone else SHALL get `403 Forbidden` before any lookup, and nothing SHALL change.

#### Scenario: Another trainer
- **WHEN** a `TRAINER` ends a relationship of a different trainer
- **THEN** the response is `403 Forbidden` and the relationship stays `ACTIVE`

#### Scenario: Another athlete
- **WHEN** a `USER` ends a relationship whose `athleteId` is not theirs
- **THEN** the response is `403 Forbidden` and the relationship stays `ACTIVE`

### Requirement: Ending an active relationship
For an allowed caller, `PUT /trainer-athletes/{trainerId}/end` SHALL move the `ACTIVE` relationship between that trainer and athlete to `ENDED`, set `finishedAtUtc` to now and respond `200 OK` with the relationship. The relationship SHALL be kept, not deleted.

#### Scenario: Ended by the trainer
- **WHEN** the trainer ends their relationship with an athlete
- **THEN** the response is `200 OK` with status `ENDED` and a non-null `finishedAtUtc`, and the athlete leaves the trainer's list

#### Scenario: Ended by the athlete
- **WHEN** the athlete ends their relationship with their trainer
- **THEN** the response is `200 OK` with status `ENDED`

#### Scenario: Ended by an admin
- **WHEN** an `ADMIN` ends any active relationship
- **THEN** the response is `200 OK` with status `ENDED`

### Requirement: Ending needs an active relationship
`PUT /trainer-athletes/{trainerId}/end` SHALL respond `404 Not Found` when there is no `ACTIVE` relationship between that trainer and athlete, including when the only ones are already `ENDED`. Invalid body input SHALL get `400 Bad Request`.

#### Scenario: Already ended
- **WHEN** an allowed caller ends a relationship that is already `ENDED`
- **THEN** the response is `404 Not Found` and the relationship keeps its original `finishedAtUtc`

#### Scenario: Missing athlete id
- **WHEN** an `ADMIN` calls `PUT /trainer-athletes/{trainerId}/end` with an empty body
- **THEN** the response is `400 Bad Request`

### Requirement: A trainer lists their own athletes
`GET /trainer-athletes` SHALL be available only to `TRAINER` callers and return `200 OK` with the caller's `ACTIVE` relationships, ordered by athlete email ascending. `USER` and `ADMIN` callers SHALL get `403 Forbidden`.

#### Scenario: Own active athletes only
- **WHEN** a `TRAINER` with active and ended relationships calls `GET /trainer-athletes`
- **THEN** the response lists only their active athletes, and excludes ended relationships and other trainers' athletes

#### Scenario: Not a trainer
- **WHEN** a `USER` or an `ADMIN` calls `GET /trainer-athletes`
- **THEN** the response is `403 Forbidden`

### Requirement: An administrator lists a trainer's athletes
`GET /trainer-athletes/{trainerId}` SHALL be available only to `ADMIN` callers and return `200 OK` with that trainer's `ACTIVE` relationships, ordered by athlete email ascending. `TRAINER` and `USER` callers SHALL get `403 Forbidden`. An unknown `trainerId` SHALL get `404 Not Found`.

#### Scenario: Admin lists a trainer
- **WHEN** an `ADMIN` calls `GET /trainer-athletes/{trainerId}`
- **THEN** the response lists that trainer's active athletes, and excludes ended relationships and other trainers' athletes

#### Scenario: Trainer caller
- **WHEN** a `TRAINER` calls `GET /trainer-athletes/{trainerId}`, including with their own id
- **THEN** the response is `403 Forbidden`

#### Scenario: Unknown trainer
- **WHEN** an `ADMIN` calls `GET /trainer-athletes/{trainerId}` with an id no user has
- **THEN** the response is `404 Not Found`

### Requirement: Listed athletes expose relationship and profile fields
Each listed item SHALL contain exactly `id` (the relationship), `athleteId`, `email`, `name`, `surname`, `trainerName`, `trainerSurname` and `startedAtUtc`. It SHALL NOT contain the password hash, the date of birth or any token.

#### Scenario: Item shape
- **WHEN** a `TRAINER` or an `ADMIN` lists a trainer's athletes
- **THEN** every item contains exactly `id`, `athleteId`, `email`, `name`, `surname`, `trainerName`, `trainerSurname` and `startedAtUtc`

### Requirement: Relationship responses carry the names of both sides
The responses of `POST /trainer-athletes/{trainerId}` and `PUT /trainer-athletes/{trainerId}/end` SHALL include `trainerName` and `trainerSurname` of the trainer and `athleteName` and `athleteSurname` of the athlete, as currently stored.

#### Scenario: Link response
- **WHEN** an `ADMIN` links an athlete to a trainer
- **THEN** the `201` response carries `trainerName`, `trainerSurname`, `athleteName` and `athleteSurname`

#### Scenario: End response
- **WHEN** an allowed caller ends a relationship
- **THEN** the `200` response carries `trainerName`, `trainerSurname`, `athleteName` and `athleteSurname`
