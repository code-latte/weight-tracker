# exercise-last-performance Specification

## Purpose

Lets a front read the signed-in user's last performance of a catalog exercise through the API (the date, the
prescribed effort and the recorded sets), and get every failure back as a `DomainError` code instead of an exception.

## Requirements

### Requirement: Read the signed-in user's last performance of an exercise

`core` SHALL offer a use case that, given a catalog exercise id, requests `GET /users/me/exercises/{exerciseId}/last-performance`
and returns the API's answer unchanged: the workout date, the effort and the sets in the order the API returns them,
each with `reps`, `durationSeconds` and `weight` as answered, including `null`.

#### Scenario: The user has a last performance

- **WHEN** the API answers `200` with a date, an effort and two sets, one of them with `weight: null`
- **THEN** the use case succeeds with that date, that effort and both sets in order, keeping `weight: null`

#### Scenario: The request targets the catalog exercise

- **WHEN** the use case runs for exercise id `ex-1`
- **THEN** it sends `GET /users/me/exercises/ex-1/last-performance`

### Requirement: No last performance is an empty success

When the API answers `204` with no body, the use case SHALL succeed with `null`. It SHALL NOT report an error.

#### Scenario: The user never finished a workout with the exercise

- **WHEN** the API answers `204`
- **THEN** the use case succeeds with `null`

### Requirement: Failures come back as domain error codes

Every failure SHALL be returned as a `DomainError` through the HTTP error mapper and SHALL NOT be thrown. No new error
code is introduced.

#### Scenario: Exercise missing or not visible

- **WHEN** the API answers `404`
- **THEN** the use case fails with `NOT_FOUND`

#### Scenario: Session expired

- **WHEN** the API answers `401`
- **THEN** the use case fails with `UNAUTHORIZED`

#### Scenario: Network failure

- **WHEN** the request cannot reach the API
- **THEN** the use case fails with `NETWORK`
