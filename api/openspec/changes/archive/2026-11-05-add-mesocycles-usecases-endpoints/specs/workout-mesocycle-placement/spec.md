# Spec Delta

## REMOVED Requirements

### Requirement: Existing workout HTTP behaviour is unchanged
**Reason**: Workout placement is now exposed and accepted through `POST /workouts` and `PUT /workouts/{id}`.
**Migration**: Clients read `mesocycleId` and `mesocycleWeekNumber` from workout responses and send them back on
`PUT /workouts/{id}`; omitting both unlinks the workout.

## ADDED Requirements

### Requirement: Workout responses expose the placement
`POST /workouts`, `GET /workouts/{id}` and `PUT /workouts/{id}` SHALL return `mesocycleId` and `mesocycleWeekNumber`, both `null` for a standalone workout. The workout list item shape SHALL NOT change.

#### Scenario: Reading a linked workout
- **WHEN** an allowed caller reads the seeded workout placed in week 1 of the seeded mesocycle
- **THEN** the response has the mesocycle's id and week number 1

#### Scenario: Reading a standalone workout
- **WHEN** an allowed caller reads a workout with no placement
- **THEN** `mesocycleId` and `mesocycleWeekNumber` are both `null`

### Requirement: The placement is sent as a pair
On `POST /workouts` and `PUT /workouts/{id}`, `mesocycleId` and `mesocycleWeekNumber` SHALL be both sent or both omitted. Sending only one SHALL be refused with `400` and nothing SHALL change. A week number below 1 SHALL be refused with `400`.

#### Scenario: Only the mesocycle id
- **WHEN** a workout is created with a `mesocycleId` and no `mesocycleWeekNumber`
- **THEN** the response is `400` and no workout is created

#### Scenario: Only the week number
- **WHEN** a workout is updated with a `mesocycleWeekNumber` and no `mesocycleId`
- **THEN** the response is `400` and the workout is unchanged

### Requirement: A workout can be created in a mesocycle week
`POST /workouts` with a placement SHALL create the workout already placed in that mesocycle week, when every placement rule holds. Without a placement it SHALL create a standalone workout, as before.

#### Scenario: Trainer creates a planned workout
- **WHEN** the seeded trainer creates a workout for the seeded athlete in week 3 of the seeded mesocycle
- **THEN** the response is `201` with that mesocycle id and week number 3

#### Scenario: Create without placement
- **WHEN** a workout is created without placement
- **THEN** it is created standalone

### Requirement: The update replaces the placement
`PUT /workouts/{id}` SHALL set the placement to the pair sent, moving the workout to another week or mesocycle, and SHALL clear it when both values are omitted.

#### Scenario: Move to another week
- **WHEN** the trainer updates the published workout in week 1 of the seeded mesocycle sending week 3
- **THEN** the workout is in week 3

#### Scenario: Unlink by omitting the pair
- **WHEN** the trainer updates the published workout in week 1 of the seeded mesocycle without a placement
- **THEN** the workout is standalone

### Requirement: The placed mesocycle must exist
A placement naming a mesocycle id that matches no mesocycle SHALL be refused with `404` and nothing SHALL change.

#### Scenario: Unknown mesocycle
- **WHEN** a workout is created with a placement in an unknown mesocycle
- **THEN** the response is `404` and no workout is created

### Requirement: Only the mesocycle's trainer or an admin may place a workout in it
Setting or moving a placement SHALL be allowed only to the mesocycle's trainer or an admin. A regular user, or a trainer of another mesocycle, SHALL be refused with `403` and nothing SHALL change. Clearing a placement SHALL follow the workout editing rules only.

#### Scenario: User places their own workout
- **WHEN** the athlete of a mesocycle creates their own workout with a placement in it
- **THEN** the response is `403` and no workout is created

#### Scenario: Another trainer's mesocycle
- **WHEN** a trainer places a workout in a mesocycle of another trainer
- **THEN** the response is `403`

#### Scenario: Admin places a workout
- **WHEN** an admin creates a workout for the seeded athlete in week 2 of the seeded mesocycle
- **THEN** the workout is created in week 2

### Requirement: The workout must belong to the mesocycle's athlete and trainer
A placement SHALL be refused with `409` when the workout's owner is not the mesocycle's athlete, or when the workout has a trainer who is not the mesocycle's trainer. The check SHALL use the owner and trainer the workout has after the request, including an admin reassignment on `PUT`.

#### Scenario: Another athlete
- **WHEN** an admin creates a workout for a user who is not the seeded mesocycle's athlete, placed in that mesocycle
- **THEN** the response is `409`

#### Scenario: Another trainer on the workout
- **WHEN** an admin creates a workout for the seeded athlete with another trainer, placed in the seeded mesocycle
- **THEN** the response is `409`

#### Scenario: Reassignment breaks the placement
- **WHEN** an admin reassigns a placed workout to another owner while keeping its placement
- **THEN** the response is `409` and the workout is unchanged

### Requirement: The placed week must be within the mesocycle's duration
A placement whose week number is above the mesocycle's duration SHALL be refused with `409`. The week SHALL NOT need to be defined.

#### Scenario: Undefined week inside the duration
- **WHEN** a workout is placed in week 3 of the seeded four-week mesocycle, where week 3 is not defined
- **THEN** the placement is accepted

#### Scenario: Week beyond the duration
- **WHEN** a workout is placed in week 5 of the seeded four-week mesocycle
- **THEN** the response is `409`

### Requirement: Locked workouts keep their placement over HTTP
Changing or clearing the placement of a `STARTED` or `FINISHED` workout through `PUT /workouts/{id}` SHALL be refused with `409`, also for admins. Sending back its current placement unchanged SHALL NOT be refused for that reason.

#### Scenario: Admin moves a started workout
- **WHEN** an admin updates the seeded started workout in week 2 sending week 3
- **THEN** the response is `409` and it stays in week 2

#### Scenario: Admin edits a started workout keeping its placement
- **WHEN** an admin renames the seeded started workout sending its current placement
- **THEN** the response is `200` and it stays in week 2
