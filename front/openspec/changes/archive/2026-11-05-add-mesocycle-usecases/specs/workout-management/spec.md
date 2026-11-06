# Spec Delta

## MODIFIED Requirements

### Requirement: Creating a workout sends its data without exercises

Creating a workout SHALL send `POST /workouts` with the given `name`, `workoutDate`, optional `description`, the optional `userId` and `trainerId` and the optional `mesocycleId` and `mesocycleWeekNumber`, unchanged, and SHALL return a successful result holding the workout the API answers with. It SHALL NOT send any exercises and SHALL NOT check the caller's role.

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

#### Scenario: Rejected by the API

- **WHEN** the API refuses the creation (missing name, missing user or trainer for the caller's role, unknown user or trainer)
- **THEN** the result is a failure holding the matching `DomainError` code (`VALIDATION`, `FORBIDDEN` or `NOT_FOUND`) with the API's detail kept in the message

#### Scenario: Placement rejected by the API

- **WHEN** the API refuses the placement with 400 (half-set pair), 403 (caller may not place it), 404 (unknown mesocycle) or 409 (athlete or trainer mismatch, week beyond the duration)
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively

### Requirement: Updating a workout replaces its editable data

Updating a workout SHALL send `PUT /workouts/{id}` with the given workout id in the path and the given body unchanged, including the optional `mesocycleId` and `mesocycleWeekNumber`, and SHALL return a successful result holding the updated workout. It SHALL NOT decide which fields a role may change and SHALL NOT fill in a placement the caller omitted.

#### Scenario: Workout updated

- **WHEN** a workout is updated with a new name, description and date and the API accepts it
- **THEN** exactly one `PUT /workouts/{id}` request is sent with that id in the path and the body unchanged
- **AND** the result is successful and its value is the workout the API answered with

#### Scenario: Workout moved to a mesocycle week

- **WHEN** a workout is updated with a `mesocycleId` and a `mesocycleWeekNumber` and the API accepts it
- **THEN** the request body holds both values unchanged

#### Scenario: Placement omitted

- **WHEN** a workout is updated without `mesocycleId` and `mesocycleWeekNumber`
- **THEN** the request body holds neither field
- **AND** the result's workout holds the placement the API answered with

#### Scenario: Placement locked

- **WHEN** the placement of a started or finished workout is changed and the API answers 409
- **THEN** the result is a failure with code `CONFLICT`

#### Scenario: Reassignment by a non-admin

- **WHEN** a non-admin sends a different `userId` or `trainerId` and the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

#### Scenario: Unknown workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`
