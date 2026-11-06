# Spec Delta

## ADDED Requirements

### Requirement: Removing a week deletes a defined week without workouts
`DELETE /mesocycles/{id}/weeks/{weekNumber}` SHALL remove the defined week and answer `204` with no body. The other weeks SHALL keep their numbers, and the duration SHALL be unchanged. A week number that is not defined SHALL be `404`. A week in which any workout, whatever its status, is placed SHALL be `409`. Nothing SHALL change on failure.

#### Scenario: Remove a week without workouts
- **WHEN** the trainer removes week 4 of the seeded mesocycle
- **THEN** the response is `204`
- **AND** a following `GET /mesocycles/{id}` returns weeks 1 and 2 and a duration of 4

#### Scenario: Week with a planned workout
- **WHEN** the trainer removes week 1 of the seeded mesocycle, which holds a published workout
- **THEN** the response is `409` and week 1 is still defined

#### Scenario: Week with a started workout
- **WHEN** an admin removes week 2 of the seeded mesocycle, which holds a started workout
- **THEN** the response is `409` and week 2 is still defined

#### Scenario: Week not defined
- **WHEN** week 3 of the seeded mesocycle, which is not defined, is removed
- **THEN** the response is `404`

#### Scenario: Week can be defined again
- **WHEN** week 4 of the seeded mesocycle is removed and then defined again
- **THEN** the mesocycle has week 4 with the new training type and objective

## MODIFIED Requirements

### Requirement: Mesocycle writes require an authenticated caller
`POST /mesocycles`, `PUT /mesocycles/{id}`, `POST /mesocycles/{id}/weeks`, `PUT /mesocycles/{id}/weeks/{weekNumber}` and `DELETE /mesocycles/{id}/weeks/{weekNumber}` SHALL require a valid bearer token. A request without a token or with an invalid token SHALL be answered `401`.

#### Scenario: No token
- **WHEN** any of the five routes is called without a token
- **THEN** the response is `401`

#### Scenario: Invalid token
- **WHEN** any of the five routes is called with an invalid token
- **THEN** the response is `401`

### Requirement: Regular users cannot write mesocycles
A caller with the `USER` role SHALL be refused with `403` on every mesocycle write, including mesocycles where they are the athlete.

#### Scenario: Athlete edits their mesocycle
- **WHEN** the athlete of a mesocycle sends `PUT /mesocycles/{id}`
- **THEN** the response is `403` and the mesocycle is unchanged

#### Scenario: User creates a mesocycle
- **WHEN** a regular user sends `POST /mesocycles`
- **THEN** the response is `403` and no mesocycle is created

#### Scenario: Athlete removes a week
- **WHEN** the athlete of a mesocycle sends `DELETE /mesocycles/{id}/weeks/{weekNumber}` for a week without workouts
- **THEN** the response is `403` and the week is still defined

### Requirement: Only the mesocycle's trainer or an admin may change it
`PUT /mesocycles/{id}`, `POST /mesocycles/{id}/weeks`, `PUT /mesocycles/{id}/weeks/{weekNumber}` and `DELETE /mesocycles/{id}/weeks/{weekNumber}` SHALL be allowed to the mesocycle's trainer and to any admin, whether or not the trainer–athlete relationship is still active. Any other caller SHALL be refused with `403` and nothing SHALL change.

#### Scenario: Another trainer
- **WHEN** a trainer who is not the mesocycle's trainer updates it
- **THEN** the response is `403` and the mesocycle is unchanged

#### Scenario: Another trainer removes a week
- **WHEN** a trainer who is not the mesocycle's trainer removes one of its weeks
- **THEN** the response is `403` and the week is still defined

#### Scenario: Admin edits any mesocycle
- **WHEN** an admin defines a week on a mesocycle they are not part of
- **THEN** the week is defined

#### Scenario: Admin removes a week of any mesocycle
- **WHEN** an admin removes a week without workouts from a mesocycle they are not part of
- **THEN** the response is `204` and the week is gone

#### Scenario: Relationship ended
- **WHEN** a trainer whose relationship with the athlete has ended updates their mesocycle
- **THEN** the update is applied
