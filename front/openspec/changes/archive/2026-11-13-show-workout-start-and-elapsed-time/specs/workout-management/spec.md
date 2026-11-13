# Spec Delta

## MODIFIED Requirements

### Requirement: Reading a workout by id returns it with its exercises

Reading a workout SHALL send `GET /workouts/{id}` with the given workout id in the path and no body, and SHALL return a
successful result holding the workout, its exercises with their set records, its placement, its session dates
(`startedAt`, `completedAt`), its review fields (`reviewedAt`, `reviewedBy`, `resultsUpdatedAt`) and its athlete and
trainer profiles as the API answers them.

#### Scenario: Workout read

- **WHEN** a workout is read by id and the API answers with it
- **THEN** exactly one `GET /workouts/{id}` request is sent with that id in the path
- **AND** the result is successful and its value is the API's answer

#### Scenario: Workout read with its people

- **WHEN** the API answers with a workout whose athlete is "Ana García" and whose trainer is "Luis Pérez"
- **THEN** the result's workout holds both profiles unchanged

#### Scenario: Workout read with its results

- **WHEN** the API answers with a reviewed workout whose exercise holds two set records
- **THEN** the result's workout holds both set records in the order the API answered, and its `reviewedAt`, `reviewedBy` and `resultsUpdatedAt` unchanged

#### Scenario: Workout read with its session dates

- **WHEN** the API answers with a completed workout whose `startedAt` and `completedAt` are set
- **THEN** the result's workout holds both dates unchanged

#### Scenario: Unknown workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Workout of someone else

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`
