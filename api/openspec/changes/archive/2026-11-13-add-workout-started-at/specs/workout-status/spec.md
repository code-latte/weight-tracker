# Spec Delta

## ADDED Requirements

### Requirement: Reaching started records when it happened
Every time a workout's status becomes `STARTED` through `PUT /workouts/{workoutId}/status`, through the lifecycle or an admin override, the system SHALL set its `startedAt` to the server time of the request, in UTC. The request cannot supply it. A later start SHALL overwrite the earlier value. A workout that was never started and received no estimate SHALL have `startedAt` `null`.

#### Scenario: Athlete starts a workout
- **WHEN** the athlete sets a `PUBLISHED` workout to `STARTED`
- **THEN** the response is `200 OK` with `startedAt` set to the time of the request

#### Scenario: Admin starts a workout skipping steps
- **WHEN** an admin sets a `DRAFT` workout to `STARTED`
- **THEN** the response carries `startedAt` set to the time of the request

#### Scenario: Starting again overwrites the date
- **WHEN** an admin moves a `STARTED` workout back to `PUBLISHED` and it later reaches `STARTED` again
- **THEN** its `startedAt` is the time of the latest start

#### Scenario: Never started
- **WHEN** a caller reads a workout created after this change that never reached `STARTED`
- **THEN** its `startedAt` is `null`

### Requirement: Other status changes keep the start date
A status change whose target is not `STARTED`, including completing the workout and an admin moving it out of `STARTED`, SHALL leave `startedAt` unchanged. A refused status change, a set record change and a review SHALL leave `startedAt` unchanged.

#### Scenario: Athlete completes a started workout
- **WHEN** the athlete sets a `STARTED` workout to `COMPLETED`
- **THEN** the response carries `status` `COMPLETED` and the same `startedAt` as before

#### Scenario: Admin moves a started workout back
- **WHEN** an admin sets a `STARTED` workout to `PUBLISHED`
- **THEN** the response carries `status` `PUBLISHED` and the same `startedAt` as before

#### Scenario: Refused change
- **WHEN** the athlete of a workout with no trainer sets a `COMPLETED` workout to `STARTED`
- **THEN** the response is `409 Conflict`
- **AND** its `startedAt` is unchanged

#### Scenario: Recording results keeps the start date
- **WHEN** the athlete records a set on a `STARTED` workout
- **THEN** its `startedAt` is unchanged

#### Scenario: Review keeps the start date
- **WHEN** the prescribing trainer reviews a `COMPLETED` workout
- **THEN** its `startedAt` is unchanged

### Requirement: Workouts started before the date existed receive an estimated start date
When start dates are introduced, the schema migration SHALL estimate `startedAt` for existing workouts, with an independent random offset per workout. A `COMPLETED` or `REVIEWED` workout with `completedAt` SHALL get `completedAt` minus 50 to 75 minutes. A `STARTED` workout with `resultsUpdatedAt` SHALL get `resultsUpdatedAt` minus 15 to 20 minutes. Every other workout SHALL keep `startedAt` `null`. No other field changes.

#### Scenario: Pre-existing completed workout
- **WHEN** the migration runs on a `COMPLETED` or `REVIEWED` workout with a `completedAt`
- **THEN** its `startedAt` is at least 50 and at most 75 minutes before its `completedAt`

#### Scenario: Pre-existing started workout with results
- **WHEN** the migration runs on a `STARTED` workout with a `resultsUpdatedAt`
- **THEN** its `startedAt` is at least 15 and at most 20 minutes before its `resultsUpdatedAt`

#### Scenario: No source date
- **WHEN** the migration runs on a `COMPLETED` or `REVIEWED` workout with `completedAt` `null`, or a `STARTED` workout with `resultsUpdatedAt` `null`
- **THEN** its `startedAt` is `null` and it is still readable and usable

#### Scenario: Not started
- **WHEN** the migration runs on a `DRAFT` or `PUBLISHED` workout
- **THEN** its `startedAt` is `null`

#### Scenario: Review concurrency is preserved
- **WHEN** the migration runs on a `COMPLETED` workout pending review
- **THEN** its `resultsUpdatedAt` and `completedAt` are unchanged, so a review sending the previously read `resultsUpdatedAt` still succeeds
