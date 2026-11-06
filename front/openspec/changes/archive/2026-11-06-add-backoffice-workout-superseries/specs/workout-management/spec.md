# Spec Delta

## ADDED Requirements

### Requirement: Creating a superseries sends the workout entry ids

Creating a superseries SHALL send `POST /workouts/{workoutId}/superseries` with the workout id in the path and a body
`{ exerciseIds }` holding the given workout entry ids (not catalog ids), in the given order, unchanged. It SHALL return
the superseries id the API answers with 201.

#### Scenario: Superseries created

- **WHEN** a superseries is created in workout `w-1` with entries `[e-1, e-2]` and the API answers 201 with `{ "id": 3 }`
- **THEN** exactly one `POST /workouts/w-1/superseries` request is sent with body `{ "exerciseIds": ["e-1", "e-2"] }`
- **AND** the result is successful and holds the superseries id `3`

#### Scenario: Creation rejected

- **WHEN** the API answers 400, 403, 404 or 409
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT`

#### Scenario: Creation fails without a response

- **WHEN** the create request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`

### Requirement: Breaking a superseries sends its id

Breaking a superseries SHALL send `DELETE /workouts/{workoutId}/superseries/{superSeriesId}` with the workout id and
the superseries id in the path and no body, and SHALL return a successful empty result when the API answers 204.

#### Scenario: Superseries broken

- **WHEN** superseries `3` of workout `w-1` is broken and the API answers 204
- **THEN** exactly one `DELETE /workouts/w-1/superseries/3` request is sent without a body
- **AND** the result is successful

#### Scenario: Break rejected

- **WHEN** the API answers 400, 403, 404 or 409
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT`

#### Scenario: Break fails without a response

- **WHEN** the break request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`
