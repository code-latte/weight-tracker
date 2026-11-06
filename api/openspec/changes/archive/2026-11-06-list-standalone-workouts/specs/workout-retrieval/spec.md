# Spec Delta

## ADDED Requirements

### Requirement: The workout list can be limited to standalone workouts

`GET /workouts` SHALL accept an optional `standalone` boolean query parameter. With `standalone=true` the list SHALL
include only the workouts in the caller's role scope that are not placed in a mesocycle. Without the parameter, or with
`standalone=false`, the list SHALL be the same as before. The parameter SHALL NOT widen what the caller may see.

#### Scenario: Admin lists standalone workouts

- **WHEN** an ADMIN lists workouts with `standalone=true` and two workouts are placed in a mesocycle
- **THEN** the list includes every other workout
- **AND** the list does not include the two placed workouts

#### Scenario: Trainer's standalone list stays within their scope

- **WHEN** a TRAINER lists workouts with and without `standalone=true`
- **THEN** every workout in the standalone list is also in the full list
- **AND** no workout placed in a mesocycle is in the standalone list

#### Scenario: Placed workouts are still listed without the parameter

- **WHEN** a caller lists workouts without `standalone`
- **THEN** workouts placed in a mesocycle are included as before
