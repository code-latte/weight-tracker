# Spec Delta

## MODIFIED Requirements

### Requirement: The workout date is required and stored without time
A request without a mesocycle placement SHALL carry `workoutDate` as a calendar date. A request without a placement and without `workoutDate` SHALL be rejected with `400 Bad Request`. A value that is not a date SHALL be rejected with `400 Bad Request`, with or without a placement. The created workout SHALL keep only the date. With a placement, the date SHALL follow the workout mesocycle placement rules instead.

#### Scenario: Missing date
- **WHEN** an authenticated caller sends `POST /workouts` without `workoutDate` and without a placement
- **THEN** the response is `400 Bad Request`
- **AND** no workout is created

#### Scenario: Date is returned as sent
- **WHEN** a caller creates a workout with `workoutDate` `2026-10-20` and no placement
- **THEN** the response body's `workoutDate` is `2026-10-20`

#### Scenario: Placed workout without date
- **WHEN** the seeded trainer creates a workout for the seeded athlete in week 3 of the seeded mesocycle without `workoutDate`
- **THEN** the response is `201 Created`
