# Spec Delta

## Purpose

Lets athletes, trainers and admins read mesocycles over HTTP: one mesocycle with its weeks, the caller's own
mesocycles, and the mesocycles of one athlete, each scoped by the caller's role and ownership.

## ADDED Requirements

### Requirement: Mesocycle reads require an authenticated caller
`GET /mesocycles/{id}`, `GET /mesocycles` and `GET /users/{userId}/mesocycles` SHALL require a valid bearer token. A request without a token or with an invalid token SHALL be answered `401`.

#### Scenario: No token
- **WHEN** any of the three routes is called without a token
- **THEN** the response is `401`

#### Scenario: Invalid token
- **WHEN** any of the three routes is called with an invalid token
- **THEN** the response is `401`

### Requirement: A mesocycle response carries its weeks and people
A mesocycle SHALL be returned with its id, trainer id, athlete id, name, objective, start date as `yyyy-MM-dd`, duration in weeks, its defined weeks ordered by week number, and the public profiles of its trainer and athlete. Each week SHALL carry its week number, its training type as an upper-case string and its objective.

#### Scenario: Seeded mesocycle
- **WHEN** an allowed caller reads the seeded four-week mesocycle with weeks 1, 2 and 4 defined
- **THEN** the response has duration 4, weeks 1, 2 and 4 in that order with types `ADAPTATION`, `ACCUMULATION` and `DELOAD`, and the trainer and athlete profiles

#### Scenario: Mesocycle without weeks
- **WHEN** an allowed caller reads a mesocycle with no week defined
- **THEN** the response has an empty week list

#### Scenario: Week row ids are not exposed
- **WHEN** an allowed caller reads a mesocycle
- **THEN** no week in the response carries an id other than its week number

### Requirement: One mesocycle is visible to its athlete, its trainer and admins
`GET /mesocycles/{id}` SHALL return the mesocycle to its athlete, to its trainer and to any admin. Any other caller SHALL be refused with `403`.

#### Scenario: Athlete reads their mesocycle
- **WHEN** the athlete of a mesocycle reads it
- **THEN** the response is `200` with the mesocycle

#### Scenario: Trainer reads their mesocycle
- **WHEN** the trainer of a mesocycle reads it
- **THEN** the response is `200` with the mesocycle

#### Scenario: Admin reads any mesocycle
- **WHEN** an admin reads a mesocycle they are not part of
- **THEN** the response is `200` with the mesocycle

#### Scenario: Another trainer
- **WHEN** a trainer who is not the mesocycle's trainer reads it
- **THEN** the response is `403`

#### Scenario: Another user
- **WHEN** a regular user who is not the mesocycle's athlete reads it
- **THEN** the response is `403`

### Requirement: Unknown mesocycles are not found
`GET /mesocycles/{id}` for an id that matches no mesocycle SHALL be answered `404`, whatever the caller's role.

#### Scenario: Unknown id
- **WHEN** an authenticated caller reads a mesocycle id that does not exist
- **THEN** the response is `404`

### Requirement: The mesocycle list is scoped by role
`GET /mesocycles` SHALL return the mesocycles where a regular user is the athlete, the mesocycles where a trainer is the trainer, and every mesocycle for an admin. Mesocycles outside that scope SHALL NOT appear.

#### Scenario: User sees only their mesocycles
- **WHEN** a regular user lists mesocycles and another athlete has a mesocycle
- **THEN** the list contains the user's mesocycles and not the other athlete's

#### Scenario: Trainer sees only their mesocycles
- **WHEN** a trainer lists mesocycles and another trainer has a mesocycle
- **THEN** the list contains the trainer's mesocycles and not the other trainer's

#### Scenario: Admin sees every mesocycle
- **WHEN** an admin lists mesocycles
- **THEN** the list contains every mesocycle of every trainer and athlete

#### Scenario: No mesocycles
- **WHEN** a caller with no mesocycle in scope lists mesocycles
- **THEN** the response is `200` with an empty list

### Requirement: Mesocycle lists are ordered by start date
Every mesocycle list SHALL be ordered by start date, latest first.

#### Scenario: Two mesocycles
- **WHEN** a caller lists two mesocycles starting on 2026-09-01 and 2026-11-01
- **THEN** the one starting on 2026-11-01 comes first

### Requirement: An athlete's mesocycles are listed for trainers and admins
`GET /users/{userId}/mesocycles` SHALL return to a trainer the mesocycles where they are the trainer and `userId` is the athlete, and to an admin every mesocycle where `userId` is the athlete. A regular user SHALL be refused with `403`, also for their own id.

#### Scenario: Trainer lists an athlete's mesocycles
- **WHEN** a trainer lists the mesocycles of an athlete who also has a mesocycle from another trainer
- **THEN** the list contains only the mesocycles from the calling trainer

#### Scenario: Admin lists an athlete's mesocycles
- **WHEN** an admin lists the mesocycles of an athlete with mesocycles from two trainers
- **THEN** the list contains both, and no mesocycle of another athlete

#### Scenario: Trainer with no mesocycle for that athlete
- **WHEN** a trainer lists the mesocycles of an existing user they have no mesocycle for
- **THEN** the response is `200` with an empty list

#### Scenario: Regular user
- **WHEN** a regular user lists the mesocycles of any user, including themselves
- **THEN** the response is `403`

### Requirement: Listing the mesocycles of an unknown user is not found
`GET /users/{userId}/mesocycles` for a `userId` that matches no user SHALL be answered `404` to trainers and admins.

#### Scenario: Unknown user
- **WHEN** a trainer lists the mesocycles of a user id that does not exist
- **THEN** the response is `404`

### Requirement: Trainers keep reading their mesocycles after a relationship ends
Read access of a trainer SHALL depend only on being the mesocycle's trainer, not on an active trainer–athlete relationship.

#### Scenario: Ended relationship
- **WHEN** the relationship between a trainer and an athlete ends and the trainer reads a mesocycle they created for that athlete
- **THEN** the response is `200` with the mesocycle
