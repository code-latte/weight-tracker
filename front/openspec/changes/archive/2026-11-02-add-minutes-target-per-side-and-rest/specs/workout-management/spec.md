## MODIFIED Requirements

### Requirement: Adding an exercise to a workout sends its prescription

Adding an exercise SHALL send `POST /workouts/{workoutId}/exercises`, with the workout id in the path. The body SHALL
hold, unchanged:

- `exerciseId`, `effort` and the list of set prescriptions;
- `perSide`, and `restMinutes` when given.

A set prescription of type `Minutes` SHALL carry `targetMinutes`. The result SHALL be successful and hold what the API
answers with, including `perSide`, `restMinutes` and each minutes target.

#### Scenario: Exercise added

- **WHEN** an exercise is added with an exercise id, an effort and two set prescriptions and the API accepts it
- **THEN** exactly one `POST /workouts/{workoutId}/exercises` request is sent with that body, unchanged
- **AND** the result is successful and its value is the API's answer

#### Scenario: Exercise added with minutes, rest and per side

- **WHEN** an exercise is added with `perSide` true, `restMinutes` 1.5 and a set prescription of type `Minutes` with
  `targetMinutes` 2, and the API accepts it
- **THEN** exactly one `POST /workouts/{workoutId}/exercises` request is sent with that body, unchanged
- **AND** the result's value holds `perSide`, `restMinutes` and the minutes target as the API answered them

#### Scenario: Exercise already in the workout

- **WHEN** the API answers 409
- **THEN** the result is a failure with code `CONFLICT`

#### Scenario: Invalid prescription

- **WHEN** the API answers 400
- **THEN** the result is a failure with code `VALIDATION` and the API's detail kept

### Requirement: Updating an exercise of a workout replaces its prescription

Updating an exercise of a workout SHALL send `PUT /workouts/{workoutId}/exercises/{exerciseId}`, with the workout id
and the catalog exercise id in the path. The body SHALL hold, unchanged:

- that same `exerciseId`, the `effort` and the list of set prescriptions;
- `perSide`, and `restMinutes` when given.

The result SHALL be successful and hold what the API answers with.

#### Scenario: Exercise updated

- **WHEN** the exercise of a workout is updated with an effort and two set prescriptions and the API accepts it
- **THEN** exactly one `PUT /workouts/{workoutId}/exercises/{exerciseId}` request is sent with the catalog exercise id in
  the path and the body unchanged
- **AND** the result is successful and its value is the API's answer

#### Scenario: Exercise updated with rest and per side

- **WHEN** the exercise of a workout is updated with `perSide` true and `restMinutes` 0.75 and the API accepts it
- **THEN** exactly one `PUT /workouts/{workoutId}/exercises/{exerciseId}` request is sent with that body, unchanged

#### Scenario: Exercise not in the workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Invalid prescription

- **WHEN** the API answers 400
- **THEN** the result is a failure with code `VALIDATION` and the API's detail kept
