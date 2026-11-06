# Spec Delta

## MODIFIED Requirements

### Requirement: Creating a workout sends its data without exercises

Creating a workout SHALL send `POST /workouts` with the given `name`, the optional `workoutDate` and `description`, the optional `userId` and `trainerId` and the optional `mesocycleId` and `mesocycleWeekNumber`, unchanged, and SHALL return a successful result holding the workout the API answers with. It SHALL NOT send any exercises, SHALL NOT fill in a date the caller omitted and SHALL NOT check the caller's role.

#### Scenario: Workout created with every field

- **WHEN** a workout is created with name "Pierna", a date, description "Día 1", a user id and a trainer id and the API accepts it
- **THEN** exactly one `POST /workouts` request is sent with that body, unchanged
- **AND** the result is successful and its value is the workout the API answered with

#### Scenario: Optional fields omitted

- **WHEN** a workout is created with only a name and a date
- **THEN** the request body holds only those two fields

#### Scenario: Workout created in a mesocycle week

- **WHEN** a workout is created with a `mesocycleId` and a `mesocycleWeekNumber` and the API accepts it
- **THEN** the request body holds both values unchanged
- **AND** the result's workout holds the `mesocycleId` and `mesocycleWeekNumber` the API answered with

#### Scenario: Placed workout created without a date

- **WHEN** a workout is created with a name, a user id, a `mesocycleId` and a `mesocycleWeekNumber` and no date, and the API accepts it
- **THEN** the request body holds no `workoutDate`
- **AND** the result's workout holds the date the API answered with

#### Scenario: Rejected by the API

- **WHEN** the API refuses the creation (missing name, missing date without a placement, missing user or trainer for the caller's role, unknown user or trainer)
- **THEN** the result is a failure holding the matching `DomainError` code (`VALIDATION`, `FORBIDDEN` or `NOT_FOUND`) with the API's detail kept in the message

#### Scenario: Placement rejected by the API

- **WHEN** the API refuses the placement with 400 (half-set pair), 403 (caller may not place it), 404 (unknown mesocycle) or 409 (athlete or trainer mismatch, week beyond the duration)
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively

### Requirement: Reading a workout by id returns it with its exercises

Reading a workout SHALL send `GET /workouts/{id}` with the given workout id in the path and no body, and SHALL return a
successful result holding the workout, its exercises, its placement and its athlete and trainer profiles as the API
answers them.

#### Scenario: Workout read

- **WHEN** a workout is read by id and the API answers with it
- **THEN** exactly one `GET /workouts/{id}` request is sent with that id in the path
- **AND** the result is successful and its value is the API's answer

#### Scenario: Workout read with its people

- **WHEN** the API answers with a workout whose athlete is "Ana García" and whose trainer is "Luis Pérez"
- **THEN** the result's workout holds both profiles unchanged

#### Scenario: Unknown workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Workout of someone else

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`
