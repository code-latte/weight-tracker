# Spec Delta

## MODIFIED Requirements

### Requirement: The request replaces every editable property
The body SHALL carry `name`, `description`, `workoutDate`, `userId` and `trainerId`, and the workout SHALL take exactly those values. A `null` or missing `description` clears it, and a `null` or missing `trainerId` leaves the workout without trainer. A body without `name` or `userId` SHALL be rejected with `400 Bad Request`. A body without `workoutDate` SHALL be rejected with `400 Bad Request` only when it sends no mesocycle placement. With a placement, `workoutDate` SHALL be ignored and the date SHALL follow the workout mesocycle placement rules.

#### Scenario: Owner renames and reschedules
- **WHEN** the owner of a workout without trainer sends a new `name`, `description` and `workoutDate` with the current `userId` and a `null` `trainerId`
- **THEN** the response is `200 OK`
- **AND** a subsequent `GET /workouts/{id}` returns the new `name`, `description` and `workoutDate`

#### Scenario: Description is cleared
- **WHEN** an authorized caller sends `description` `null` for a workout that has one
- **THEN** the response is `200 OK` with `description` `null`

#### Scenario: Missing required field
- **WHEN** an authorized caller sends a body without `name`, without `userId`, or without `workoutDate` and without a placement
- **THEN** the response is `400 Bad Request`
- **AND** the workout is unchanged

#### Scenario: Placed workout without date
- **WHEN** the trainer updates the published workout in week 1 of the seeded mesocycle, keeping its placement and sending no `workoutDate`
- **THEN** the response is `200 OK`
