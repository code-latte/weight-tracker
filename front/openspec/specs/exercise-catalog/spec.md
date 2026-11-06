# exercise-catalog Specification

## Purpose

Lets both fronts read the exercise catalog through `core`, with each exercise's type, owner and archive date, as the
API answers them, returning failures as results instead of throwing.

## Requirements

### Requirement: Listing the catalog returns every exercise

Listing the exercise catalog SHALL send `GET /exercises` with no body and SHALL return a successful result holding the exercises the API answers with, unchanged. It SHALL take no input and SHALL NOT cache the result.

#### Scenario: Catalog listed

- **WHEN** the API answers with a list of exercises
- **THEN** exactly one `GET /exercises` request is sent, with no body
- **AND** the result is successful and its value is that list, unchanged

#### Scenario: Empty catalog

- **WHEN** the API answers with an empty list
- **THEN** the result is successful and its value is an empty list

### Requirement: Listing the catalog needs no session

The listing SHALL work for a caller without a stored session, because the endpoint is public, and SHALL NOT fail only because no access token exists.

#### Scenario: No session

- **WHEN** the catalog is listed with no stored session and the API answers with exercises
- **THEN** the result is successful

### Requirement: Catalog failures are returned as results

Listing the catalog SHALL return a `DomainError` result instead of throwing: statuses other than success as `UNEXPECTED`, and a request that gets no response as `NETWORK`.

#### Scenario: Server error

- **WHEN** the API answers 500
- **THEN** the result is a failure with code `UNEXPECTED`

#### Scenario: No response

- **WHEN** the request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`

### Requirement: Listed exercises carry their type

Each exercise returned by listing the catalog SHALL carry its `type` as the API sends it (`STRENGTH`, `CARDIO` or
`STRETCHING`), unchanged, beside its muscle and body part.

#### Scenario: Type kept

- **WHEN** the API answers with an exercise whose `type` is `CARDIO`
- **THEN** the exercise in the successful result has `type` `CARDIO`

#### Scenario: Unknown type kept as it comes

- **WHEN** the API answers with an exercise whose `type` is a value the front does not know
- **THEN** the exercise in the result carries that value unchanged

### Requirement: Listed exercises carry their owner and archive date

Each exercise returned by listing the catalog SHALL carry its `ownerId` and `archivedAtUtc` as the API sends them,
unchanged: `ownerId` is `null` for a system exercise and the creator's user id for a custom one, and `archivedAtUtc`
is `null` while the exercise is not archived.

#### Scenario: System exercise

- **WHEN** the API answers with an exercise whose `ownerId` is `null`
- **THEN** the exercise in the successful result has `ownerId` `null`

#### Scenario: Custom exercise

- **WHEN** the API answers with an exercise whose `ownerId` is a user id and whose `archivedAtUtc` is a date
- **THEN** the exercise in the successful result carries that `ownerId` and that `archivedAtUtc`, unchanged

### Requirement: Listing the catalog sends the session when there is one

Listing the catalog SHALL send the stored access token when a session exists, so the API can include the caller's own
exercises, and SHALL still send the request without one when there is no session.

#### Scenario: Signed in

- **WHEN** the catalog is listed with a stored session
- **THEN** the `GET /exercises` request carries that session's access token
