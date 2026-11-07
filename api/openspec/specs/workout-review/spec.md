# workout-review Specification

## Purpose

Lets the prescribing trainer, or an admin, mark a completed workout as reviewed. The review records who reviewed it and when, and locks its prescription and results.

## Requirements

### Requirement: Review a completed workout
The system SHALL expose `PUT /workouts/{workoutId}/review` with a body `{ "resultsUpdatedAt": <UTC timestamp or null> }`. A successful review SHALL set the status to `REVIEWED` and respond `200 OK` with the workout in the same shape as `GET /workouts/{id}`.

#### Scenario: Trainer reviews a completed workout
- **WHEN** the prescribing trainer reviews a `COMPLETED` workout sending its current `resultsUpdatedAt`
- **THEN** the response is `200 OK` with `status` `REVIEWED`
- **AND** a later `GET /workouts/{id}` returns `status` `REVIEWED`

#### Scenario: Admin reviews a completed workout
- **WHEN** an admin reviews a `COMPLETED` prescribed workout sending its current `resultsUpdatedAt`
- **THEN** the response is `200 OK` with `status` `REVIEWED`

### Requirement: The review records who reviewed and when
A successful review SHALL record the reviewer's user id as `reviewedBy` and the server time of the request, in UTC, as `reviewedAt`. The request cannot supply either value. A workout that was never reviewed SHALL have both as `null`.

#### Scenario: Review metadata
- **WHEN** the prescribing trainer reviews a `COMPLETED` workout
- **THEN** the response carries `reviewedBy` equal to the trainer's id and `reviewedAt` set to the time of the request

#### Scenario: Not reviewed yet
- **WHEN** a `COMPLETED` workout that was never reviewed is read
- **THEN** its `reviewedBy` and `reviewedAt` are `null`

### Requirement: Only completed workouts can be reviewed
A review of a workout that is not `COMPLETED` SHALL be rejected with `409 Conflict`, and the workout SHALL be unchanged. This includes a workout that is already `REVIEWED`.

#### Scenario: Started workout
- **WHEN** the prescribing trainer reviews a `STARTED` workout
- **THEN** the response is `409 Conflict`
- **AND** the status is still `STARTED`

#### Scenario: Already reviewed
- **WHEN** an admin reviews a `REVIEWED` workout
- **THEN** the response is `409 Conflict`
- **AND** its `reviewedBy` and `reviewedAt` are unchanged

### Requirement: Workouts without a trainer are not reviewed
A review of a workout with no prescribing trainer SHALL be rejected with `409 Conflict` for every caller, including an admin. For such a workout `COMPLETED` is the final status.

#### Scenario: Admin reviews a self-planned workout
- **WHEN** an admin reviews a `COMPLETED` workout with no trainer
- **THEN** the response is `409 Conflict`
- **AND** the status is still `COMPLETED`

### Requirement: A review cannot be based on outdated results
The `resultsUpdatedAt` sent with the review SHALL equal the workout's current `resultsUpdatedAt`, where `null` equals `null`. Otherwise the review SHALL be rejected with `409 Conflict` and the workout SHALL be unchanged.

#### Scenario: Results changed after the trainer looked
- **WHEN** the athlete edits a set record of a `COMPLETED` workout after the trainer read it, and the trainer then reviews sending the `resultsUpdatedAt` they read
- **THEN** the response is `409 Conflict`
- **AND** the status is still `COMPLETED`

#### Scenario: Workout without results
- **WHEN** the prescribing trainer reviews a `COMPLETED` workout with no set records sending `resultsUpdatedAt` `null`
- **THEN** the response is `200 OK` with `status` `REVIEWED`

### Requirement: Concurrent changes to a workout are refused
When two requests change the same workout at the same time, the system SHALL apply at most one of them. The other SHALL fail with `409 Conflict` and change nothing. A review and a concurrent set record change can therefore never both succeed.

#### Scenario: Concurrent save
- **WHEN** a workout is changed and saved while another request had already saved a change to it after loading it
- **THEN** the later save fails with `409 Conflict`
- **AND** the stored workout is the one saved first

### Requirement: Only the prescribing trainer or an admin may review
A review SHALL be allowed to an admin and to the workout's prescribing trainer. The athlete and any other caller SHALL be rejected with `403 Forbidden`. The permission check runs before the status and results checks, so a rejected caller never learns the workout's status.

#### Scenario: Athlete cannot review
- **WHEN** the athlete of a prescribed `COMPLETED` workout reviews it
- **THEN** the response is `403 Forbidden`
- **AND** the status is still `COMPLETED`

#### Scenario: Unrelated trainer cannot review
- **WHEN** a trainer who is not the workout's prescribing trainer reviews it
- **THEN** the response is `403 Forbidden`

#### Scenario: Rejected caller does not learn the status
- **WHEN** a user unrelated to a `STARTED` workout reviews it
- **THEN** the response is `403 Forbidden`, not `409 Conflict`

### Requirement: The review route requires authentication
The review route SHALL reject a request without a token or with an invalid token with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** a caller sends `PUT /workouts/{workoutId}/review` without an access token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** a caller sends `PUT /workouts/{workoutId}/review` with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`

### Requirement: Reviews of unknown workouts are not found
A review aimed at a well-formed id that matches no workout SHALL fail with `404 Not Found` for every authenticated caller.

#### Scenario: Unknown workout
- **WHEN** an authenticated caller reviews a GUID that matches no workout
- **THEN** the response is `404 Not Found`

### Requirement: The review request is structurally valid
A review request whose body is missing, or whose `resultsUpdatedAt` is neither `null` nor a valid timestamp, SHALL be rejected with `400 Bad Request`, and the workout SHALL be unchanged.

#### Scenario: Malformed timestamp
- **WHEN** an authenticated caller sends `resultsUpdatedAt` `"yesterday"`
- **THEN** the response is `400 Bad Request`

### Requirement: Reviewed workouts are locked
A `REVIEWED` workout SHALL refuse every content edit and every set record change with `409 Conflict`, for every caller including an admin, once the caller has passed the matching permission rule. It SHALL stay readable by everyone who could read it before.

#### Scenario: Admin edits a reviewed workout
- **WHEN** an admin updates an exercise of a `REVIEWED` workout
- **THEN** the response is `409 Conflict`
- **AND** the workout is unchanged

#### Scenario: Athlete corrects a set record of a reviewed workout
- **WHEN** the athlete edits a set record of a `REVIEWED` workout
- **THEN** the response is `409 Conflict`
- **AND** the set record is unchanged

#### Scenario: Athlete reads a reviewed workout
- **WHEN** the athlete reads a `REVIEWED` workout
- **THEN** the response is `200 OK` with its exercises, set records and review metadata
