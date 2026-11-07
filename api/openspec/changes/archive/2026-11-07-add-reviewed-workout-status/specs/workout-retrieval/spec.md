# Spec Delta

## MODIFIED Requirements

### Requirement: Visibility depends on the caller's role
An ADMIN SHALL read any workout. A TRAINER SHALL read workouts they own and workouts where they are the trainer. A USER SHALL read only workouts they own. A caller SHALL NOT read a `DRAFT` workout they own when it has a prescribing trainer. An existing workout outside the caller's scope SHALL be rejected with `403 Forbidden` and its content SHALL NOT be disclosed.

#### Scenario: Admin reads any workout
- **WHEN** an ADMIN reads a workout owned by another user with another trainer
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads a prescribed workout
- **WHEN** a TRAINER reads a workout where they are the trainer
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads a prescribed draft
- **WHEN** a TRAINER reads a `DRAFT` workout where they are the trainer
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads their own workout
- **WHEN** a TRAINER reads a workout they own
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads an unrelated workout
- **WHEN** a TRAINER reads a workout they neither own nor train
- **THEN** the response is `403 Forbidden`

#### Scenario: User reads their own workout
- **WHEN** a USER reads a workout they own
- **THEN** the response is `200 OK`

#### Scenario: User reads their own self-planned draft
- **WHEN** a USER reads a `DRAFT` workout they own that has no trainer
- **THEN** the response is `200 OK`

#### Scenario: User reads a prescribed draft
- **WHEN** a USER reads a `DRAFT` workout they own that has a prescribing trainer
- **THEN** the response is `403 Forbidden`

#### Scenario: User reads a published prescribed workout
- **WHEN** a USER reads a `PUBLISHED`, `STARTED`, `COMPLETED` or `REVIEWED` workout they own that has a prescribing trainer
- **THEN** the response is `200 OK`

#### Scenario: User reads another user's workout
- **WHEN** a USER reads a workout owned by someone else, even one where their trainer is set
- **THEN** the response is `403 Forbidden`

## ADDED Requirements

### Requirement: Workout responses carry review and results metadata
Every workout returned by `GET /workouts/{id}`, by `GET /workouts` and by the operations that respond with the whole workout SHALL carry:
- `resultsUpdatedAt`, the last time its set records changed, or `null`;
- `reviewedAt`, when it was reviewed, or `null`;
- `reviewedBy`, the reviewer's user id, or `null`.

Timestamps are in UTC.

#### Scenario: Reviewed workout read by id
- **WHEN** the athlete reads a `REVIEWED` workout by id
- **THEN** the response carries its `resultsUpdatedAt`, `reviewedAt` and `reviewedBy`

#### Scenario: New workout
- **WHEN** a caller reads a `DRAFT` workout that was just created
- **THEN** its `resultsUpdatedAt`, `reviewedAt` and `reviewedBy` are `null`

#### Scenario: Reviewed workout in the list
- **WHEN** the prescribing trainer lists workouts and one of them is `REVIEWED`
- **THEN** that entry carries `status` `REVIEWED` with its `reviewedAt` and `reviewedBy`
