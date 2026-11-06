# Spec Delta

## ADDED Requirements

### Requirement: Listing a mesocycle's workouts sends the mesocycle id

Listing a mesocycle's workouts SHALL send `GET /mesocycles/{id}/workouts` with the given mesocycle id in the path and no
body, and SHALL return a successful result holding the workouts the API answers with, in the order the API gives, each
with its week number. It SHALL NOT filter, group or sort them, nor check the caller's role.

#### Scenario: Workouts listed

- **WHEN** the workouts of mesocycle `m-1` are listed and the API answers 200 with two workouts in weeks 1 and 2
- **THEN** exactly one `GET /mesocycles/m-1/workouts` request is sent without a body
- **AND** the result is successful and its value is those two workouts, in the same order, with their week numbers

#### Scenario: No workouts

- **WHEN** the API answers 200 with an empty list
- **THEN** the result is successful and its value is an empty list

#### Scenario: Unknown or invisible mesocycle

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Mesocycle of someone else

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

#### Scenario: No response

- **WHEN** the request gets no response
- **THEN** the result is a failure with code `NETWORK`
