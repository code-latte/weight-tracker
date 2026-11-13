# exercise-history Specification

## Purpose

Lets a front read one user's per-session history of a catalog exercise through the API (the max weight, volume and
estimated one-rep max of each session), and get every failure back as a `DomainError` code.

## Requirements

### Requirement: Read a user's history of an exercise

`core` SHALL offer a use case that, given a user id, a catalog exercise id and a limit, requests
`GET /users/{userId}/exercises/{exerciseId}/history?limit={limit}` with no body. It SHALL return the sessions the API
answers with unchanged, in the order the API returns them, keeping every `null`.

#### Scenario: The user has sessions

- **WHEN** the API answers `200` with two sessions, the second with `maxWeight: null`
- **THEN** the use case succeeds with both sessions in the API's order, keeping `maxWeight: null`

#### Scenario: The request targets the user, the exercise and the limit

- **WHEN** the use case runs for user `u-1`, exercise `ex-1` and limit 5
- **THEN** it sends `GET /users/u-1/exercises/ex-1/history?limit=5`

#### Scenario: The user never did the exercise

- **WHEN** the API answers `200` with an empty list
- **THEN** the use case succeeds with an empty list

### Requirement: History failures come back as domain error codes

Every failure SHALL be returned as a `DomainError` through the HTTP error mapper and SHALL NOT be thrown. No new error
code is introduced.

#### Scenario: User or exercise missing

- **WHEN** the API answers `404`
- **THEN** the use case fails with `NOT_FOUND`

#### Scenario: Caller may not read that user

- **WHEN** the API answers `403`
- **THEN** the use case fails with `FORBIDDEN`

#### Scenario: Limit out of range

- **WHEN** the API answers `400`
- **THEN** the use case fails with `VALIDATION`

#### Scenario: Session expired

- **WHEN** the API answers `401`
- **THEN** the use case fails with `UNAUTHORIZED`

#### Scenario: Network failure

- **WHEN** the request cannot reach the API
- **THEN** the use case fails with `NETWORK`
