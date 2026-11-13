# workout-summary Specification

## Purpose

Lets a front read the summary the API computes for one workout: its sets in target and volume, and per exercise the
top set and the top set of its previous execution. Every failure comes back as a `DomainError` code.

## Requirements

### Requirement: Read a workout's summary

`core` SHALL offer a use case that, given a workout id, requests `GET /workouts/{workoutId}/summary` with no body and
returns the API's answer unchanged. The answer holds:

- the workout's `setsInTarget` and `volume`;
- per exercise, in the order the API returns them: `setsInTarget`, `volume`, `topSet` and `previous`, with every
  `null` kept.

#### Scenario: The workout has a summary

- **WHEN** the API answers `200` with `setsInTarget` 27 of 29, `volume` 16913.4 and two exercises,
  one with `volume: null`, `topSet: null` and `previous: null`
- **THEN** the use case succeeds with those values and both exercises in order, keeping every `null`

#### Scenario: The request targets the workout

- **WHEN** the use case runs for workout id `w-1`
- **THEN** it sends `GET /workouts/w-1/summary`

### Requirement: Summary failures come back as domain error codes

Every failure SHALL be returned as a `DomainError` through the HTTP error mapper and SHALL NOT be thrown. No new error
code is introduced.

#### Scenario: Workout missing

- **WHEN** the API answers `404`
- **THEN** the use case fails with `NOT_FOUND`

#### Scenario: Workout not visible to the caller

- **WHEN** the API answers `403`
- **THEN** the use case fails with `FORBIDDEN`

#### Scenario: Session expired

- **WHEN** the API answers `401`
- **THEN** the use case fails with `UNAUTHORIZED`

#### Scenario: Network failure

- **WHEN** the request cannot reach the API
- **THEN** the use case fails with `NETWORK`
