# exercise-favorites Specification

## Purpose

Lists, marks and unmarks the signed-in caller's favorite exercises through the API, and returns every outcome,
including failures, as a result that a front can show.

## Requirements

### Requirement: Listing favorites returns the caller's favorites

Listing favorites SHALL send `GET /exercise-favorites` with no body and SHALL return a successful result holding the
favorites the API answers with, unchanged and in the order the API sends them. Each favorite SHALL carry its
`exerciseId` and `createdAtUtc`. It SHALL take no input and SHALL NOT cache the result.

#### Scenario: Favorites listed

- **WHEN** the API answers with two favorites
- **THEN** exactly one `GET /exercise-favorites` request is sent, with no body
- **AND** the result is successful and its value is those two favorites, unchanged and in the same order

#### Scenario: No favorites

- **WHEN** the API answers with an empty list
- **THEN** the result is successful and its value is an empty list

### Requirement: Marking a favorite sends the exercise id

Marking an exercise as a favorite SHALL take the exercise id and SHALL send `PUT /exercise-favorites/{exerciseId}`
with no body. On success it SHALL return the favorite the API answers with, unchanged.

#### Scenario: Exercise marked

- **WHEN** an exercise is marked and the API answers 200 with its favorite
- **THEN** exactly one `PUT /exercise-favorites/<exercise id>` request is sent, with no body
- **AND** the result is successful and its value is that favorite

#### Scenario: Already a favorite

- **WHEN** an exercise that already is a favorite is marked and the API answers 200 with its original date
- **THEN** the result is successful and its value carries that original `createdAtUtc`

### Requirement: Unmarking a favorite sends the exercise id

Unmarking an exercise SHALL take the exercise id and SHALL send `DELETE /exercise-favorites/{exerciseId}` with no
body. A `204` answer SHALL be a successful result with no value.

#### Scenario: Exercise unmarked

- **WHEN** an exercise is unmarked and the API answers 204
- **THEN** exactly one `DELETE /exercise-favorites/<exercise id>` request is sent, with no body
- **AND** the result is successful

### Requirement: Favorite failures are returned as results

Listing, marking and unmarking SHALL return a `DomainError` result instead of throwing: `401` as `UNAUTHORIZED`,
`404` as `NOT_FOUND`, `409` as `CONFLICT`, any other status as `UNEXPECTED`, and a request that gets no response as
`NETWORK`.

#### Scenario: No session

- **WHEN** favorites are listed and the API answers 401
- **THEN** the result is a failure with code `UNAUTHORIZED`

#### Scenario: Unknown or invisible exercise

- **WHEN** an exercise is marked or unmarked and the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Archived exercise

- **WHEN** an exercise is marked and the API answers 409
- **THEN** the result is a failure with code `CONFLICT`

#### Scenario: Server error

- **WHEN** the API answers 500
- **THEN** the result is a failure with code `UNEXPECTED`

#### Scenario: No response

- **WHEN** the request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`
