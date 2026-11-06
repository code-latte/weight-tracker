# Spec Delta

## ADDED Requirements

### Requirement: Reading a mesocycle's workouts requires an authenticated caller

`GET /mesocycles/{id}/workouts` SHALL require a valid bearer token. A request without a token or with an invalid token
SHALL be answered `401`.

#### Scenario: No token

- **WHEN** `GET /mesocycles/{id}/workouts` is called without a token
- **THEN** the response is `401`

#### Scenario: Invalid token

- **WHEN** `GET /mesocycles/{id}/workouts` is called with an invalid token
- **THEN** the response is `401`

### Requirement: A mesocycle's workouts are listed across all its weeks

`GET /mesocycles/{id}/workouts` SHALL return the workouts placed in mesocycle `{id}`, whatever their week number, in
the same item shape as `GET /workouts` (including `athlete`, `trainer`, `mesocycleId` and `mesocycleWeekNumber`).
Workouts that are standalone or placed in another mesocycle SHALL NOT appear.

#### Scenario: Workouts from several weeks

- **WHEN** an allowed caller reads the workouts of a mesocycle that has workouts placed in two different weeks
- **THEN** the list contains the workouts of both weeks, each with `mesocycleId` equal to `{id}`

#### Scenario: Other workouts are excluded

- **WHEN** an ADMIN reads the workouts of a mesocycle while standalone workouts and workouts of another mesocycle exist
- **THEN** the list contains none of them

#### Scenario: Mesocycle without workouts

- **WHEN** an allowed caller reads the workouts of a mesocycle that has no placed workout
- **THEN** the response is `200` with an empty list

### Requirement: A mesocycle's workouts are visible under both mesocycle and workout visibility

`GET /mesocycles/{id}/workouts` SHALL be refused with `403` to a caller who may not read mesocycle `{id}` through
`GET /mesocycles/{id}`. For a caller who may read it, the list SHALL contain only the workouts that caller may see
through `GET /workouts`. The route SHALL NOT widen either scope.

#### Scenario: Athlete reads their mesocycle's workouts

- **WHEN** the athlete of a mesocycle reads its workouts
- **THEN** the response is `200` with its published workouts

#### Scenario: Athlete does not see prescribed drafts

- **WHEN** the athlete of a mesocycle reads its workouts and one of them is a `DRAFT` prescribed by their trainer
- **THEN** the draft is not in the list

#### Scenario: Trainer reads their mesocycle's workouts

- **WHEN** the trainer of a mesocycle reads its workouts
- **THEN** the response is `200` with the placed workouts they prescribe, including drafts

#### Scenario: Admin reads any mesocycle's workouts

- **WHEN** an admin reads the workouts of a mesocycle they are not part of
- **THEN** the response is `200` with every workout placed in it

#### Scenario: Another trainer

- **WHEN** a trainer who is not the mesocycle's trainer reads its workouts
- **THEN** the response is `403`

#### Scenario: Another user

- **WHEN** a regular user who is not the mesocycle's athlete reads its workouts
- **THEN** the response is `403`

### Requirement: Reading the workouts of an unknown mesocycle is not found

`GET /mesocycles/{id}/workouts` for an id that matches no mesocycle SHALL be answered `404`, whatever the caller's role.

#### Scenario: Unknown id

- **WHEN** an authenticated caller reads the workouts of a mesocycle id that does not exist
- **THEN** the response is `404`

### Requirement: A mesocycle's workouts are ordered by week and date

The list SHALL be ordered by `mesocycleWeekNumber` ascending, then by `workoutDate` ascending.

#### Scenario: Two weeks

- **WHEN** a mesocycle has a workout in week 2 and an earlier-dated workout in week 1 and a later-dated one in week 1
- **THEN** the list is the earlier week-1 workout, the later week-1 workout, then the week-2 workout
