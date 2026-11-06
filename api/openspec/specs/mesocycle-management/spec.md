# mesocycle-management Specification

## Purpose

Lets trainers and admins create mesocycles for athletes, change their details, and define and redefine their weeks
over HTTP, with each operation limited to the callers allowed to plan for that athlete.

## Requirements

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

### Requirement: A trainer creates mesocycles for their active athletes
A trainer SHALL create a mesocycle with themselves as trainer for an athlete with whom they have an active trainer–athlete relationship. Without an active relationship the request SHALL be refused with `403`. A trainer SHALL NOT send a trainer id; doing so SHALL be refused with `400`.

#### Scenario: Active relationship
- **WHEN** a trainer creates a mesocycle for an athlete they actively train
- **THEN** the response is `201` with the mesocycle, its trainer is the caller and it has no weeks

#### Scenario: Ended or missing relationship
- **WHEN** a trainer creates a mesocycle for a user they have no active relationship with
- **THEN** the response is `403` and no mesocycle is created

#### Scenario: Trainer sends a trainer id
- **WHEN** a trainer creates a mesocycle sending a trainer id
- **THEN** the response is `400` and no mesocycle is created

### Requirement: An admin creates mesocycles for any trainer and athlete
An admin SHALL create a mesocycle naming both a trainer and an athlete, with no trainer–athlete relationship required. A missing trainer id SHALL be refused with `400`.

#### Scenario: Admin creates a mesocycle
- **WHEN** an admin creates a mesocycle for trainer `T` and athlete `A` who have no relationship
- **THEN** the response is `201` with trainer `T` and athlete `A`

#### Scenario: Admin omits the trainer
- **WHEN** an admin creates a mesocycle without a trainer id
- **THEN** the response is `400` and no mesocycle is created

### Requirement: The trainer and athlete of a new mesocycle must be valid
The athlete SHALL exist, have the `USER` role and be active. The trainer SHALL have the `TRAINER` role and be active; an admin cannot be a mesocycle's trainer. An unknown athlete SHALL be `404`; any other violation SHALL be `400`. Nothing SHALL be created on failure.

#### Scenario: Unknown athlete
- **WHEN** an admin creates a mesocycle for an athlete id that matches no user
- **THEN** the response is `404`

#### Scenario: Athlete is not a regular user
- **WHEN** an admin creates a mesocycle whose athlete is a trainer
- **THEN** the response is `400`

#### Scenario: Admin as trainer
- **WHEN** an admin creates a mesocycle naming an admin as trainer
- **THEN** the response is `400`

#### Scenario: Deactivated athlete
- **WHEN** an admin creates a mesocycle for a deactivated athlete
- **THEN** the response is `400`

#### Scenario: Unknown trainer
- **WHEN** an admin creates a mesocycle naming a trainer id that matches no user
- **THEN** the response is `400`

### Requirement: Creation returns the new mesocycle
A successful `POST /mesocycles` SHALL answer `201` with the mesocycle in the shape of `GET /mesocycles/{id}` and a `Location` header pointing to it.

#### Scenario: Location header
- **WHEN** a mesocycle is created
- **THEN** the `Location` header points to `GET /mesocycles/{id}` of the new mesocycle

### Requirement: Mesocycle details are validated
On creation and update, the name, objective, start date and duration SHALL follow the mesocycle rules: name and objective required and within their lengths, start date a calendar date, duration from 1 to 52. A violation SHALL be refused with `400` and nothing SHALL change.

#### Scenario: Blank name
- **WHEN** a mesocycle is created with a blank name
- **THEN** the response is `400`

#### Scenario: Duration out of range
- **WHEN** a mesocycle is updated with duration 53
- **THEN** the response is `400` and the mesocycle is unchanged

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

### Requirement: Writes on unknown mesocycles are not found
A write on a mesocycle id that matches no mesocycle SHALL be answered `404`, checked before permissions.

#### Scenario: Unknown mesocycle
- **WHEN** a trainer updates a mesocycle id that does not exist
- **THEN** the response is `404`

### Requirement: Updating a mesocycle replaces its details
`PUT /mesocycles/{id}` SHALL replace the name, objective, start date and duration, keep the trainer, athlete and weeks, and answer `200` with the mesocycle. Shrinking the duration below the highest defined week SHALL be refused with `409` and nothing SHALL change.

#### Scenario: Update details
- **WHEN** the trainer updates the name, objective, start date and duration of their mesocycle
- **THEN** the response is `200` with the new values and the same weeks

#### Scenario: Shrink below a defined week
- **WHEN** the seeded mesocycle with week 4 defined is updated to 3 weeks
- **THEN** the response is `409` and the duration stays 4

### Requirement: Defining a week adds a new week
`POST /mesocycles/{id}/weeks` SHALL add a week with the given number, training type and objective, and answer `201` with the mesocycle. A number already defined SHALL be `409`; a number above the duration SHALL be `409`; an invalid number, type or objective SHALL be `400`. Nothing SHALL change on failure.

#### Scenario: Define a gap
- **WHEN** the trainer defines week 3 of the seeded mesocycle as `INTENSIFICATION`
- **THEN** the response is `201` and the mesocycle has weeks 1, 2, 3 and 4

#### Scenario: Week already defined
- **WHEN** week 2 of the seeded mesocycle is defined again
- **THEN** the response is `409` and week 2 is unchanged

#### Scenario: Week beyond the duration
- **WHEN** week 5 of a four-week mesocycle is defined
- **THEN** the response is `409`

#### Scenario: Unknown training type
- **WHEN** a week is defined with training type `PEAKING`
- **THEN** the response is `400`

### Requirement: Redefining a week changes its type and objective
`PUT /mesocycles/{id}/weeks/{weekNumber}` SHALL replace the training type and objective of a defined week, keep its number, and answer `200` with the mesocycle. A week number that is not defined SHALL be `404`; an invalid type or objective SHALL be `400`.

#### Scenario: Redefine a week
- **WHEN** the trainer redefines week 2 of the seeded mesocycle as `DELOAD`
- **THEN** the response is `200` and week 2 is `DELOAD`

#### Scenario: Week not defined
- **WHEN** week 3 of the seeded mesocycle, which is not defined, is redefined
- **THEN** the response is `404`

### Requirement: Structural request errors are bad requests
A write whose body is missing a required field or is malformed SHALL be answered `400` before any business rule is checked.

#### Scenario: Missing objective
- **WHEN** `POST /mesocycles` is sent without an objective
- **THEN** the response is `400`

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
