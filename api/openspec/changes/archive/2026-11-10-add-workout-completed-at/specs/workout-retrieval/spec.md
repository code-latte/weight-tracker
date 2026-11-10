# Spec Delta

## MODIFIED Requirements

### Requirement: Workout responses carry review and results metadata
Every workout returned by `GET /workouts/{id}`, by `GET /workouts` and by the operations that respond with the whole workout SHALL carry:
- `resultsUpdatedAt`, the last time its set records changed, or `null`;
- `completedAt`, the last time it reached `COMPLETED`, or `null`;
- `reviewedAt`, when it was reviewed, or `null`;
- `reviewedBy`, the reviewer's user id, or `null`.

Timestamps are in UTC.

#### Scenario: Reviewed workout read by id
- **WHEN** the athlete reads a `REVIEWED` workout by id
- **THEN** the response carries its `resultsUpdatedAt`, `completedAt`, `reviewedAt` and `reviewedBy`

#### Scenario: New workout
- **WHEN** a caller reads a `DRAFT` workout that was just created
- **THEN** its `resultsUpdatedAt`, `completedAt`, `reviewedAt` and `reviewedBy` are `null`

#### Scenario: Reviewed workout in the list
- **WHEN** the prescribing trainer lists workouts and one of them is `REVIEWED`
- **THEN** that entry carries `status` `REVIEWED` with its `reviewedAt` and `reviewedBy`

#### Scenario: Completed workout in the list
- **WHEN** the athlete lists workouts and one of them is `COMPLETED`
- **THEN** that entry carries its `completedAt`
