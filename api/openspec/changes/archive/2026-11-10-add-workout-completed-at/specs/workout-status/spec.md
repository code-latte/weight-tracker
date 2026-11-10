# Spec Delta

## ADDED Requirements

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
