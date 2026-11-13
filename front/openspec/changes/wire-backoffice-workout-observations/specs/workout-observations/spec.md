# Spec Delta

## Purpose

Lets a front read the observations the API generated for one completed workout, and which of them are highlighted.
Every failure comes back as a `DomainError` code.

## ADDED Requirements

### Requirement: Read a workout's observations

`core` SHALL offer a use case that, given a workout id, requests `GET /workouts/{workoutId}/observations` with no body
and returns the API's answer unchanged: `observations` (each with `id`, `code`, `severity`, `exerciseId`, `priority`
and `data`) and `highlights` (each with `observationId`, `rank` and `priority`), in the order the API returns them and
with every `null` kept.

#### Scenario: The workout has observations

- **WHEN** the API answers `200` with three observations, one with `exerciseId: null` and `priority: null`, and one
  highlight
- **THEN** the use case succeeds with both lists in order, keeping every `null` and every `data` value

#### Scenario: The workout has none

- **WHEN** the API answers `200` with empty `observations` and `highlights`
- **THEN** the use case succeeds with both lists empty

#### Scenario: The request targets the workout

- **WHEN** the use case runs for workout id `w-1`
- **THEN** it sends `GET /workouts/w-1/observations`

### Requirement: Observation failures come back as domain error codes

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
