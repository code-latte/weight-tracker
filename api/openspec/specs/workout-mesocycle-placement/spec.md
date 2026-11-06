# workout-mesocycle-placement Specification

## Purpose

Lets a workout optionally point to one week of a mesocycle, so planned workouts can be placed in, moved between and
removed from training plans, while workouts and mesocycles stay independent and started or finished workouts keep
their planning history.

## Requirements

### Requirement: A workout optionally points to one mesocycle week
A workout SHALL carry a mesocycle id and a mesocycle week number, both set or both empty. A workout with both empty SHALL be a valid standalone workout. A workout SHALL know only the week number, not the week's training type or objective.

#### Scenario: New workout is standalone
- **WHEN** a workout is created
- **THEN** its mesocycle id and mesocycle week number are both empty

#### Scenario: Half-set link in stored data
- **WHEN** a workout is restored with a mesocycle id but no week number, or a week number but no mesocycle id
- **THEN** restoring fails with a conflict error

### Requirement: Workouts existing before this change are standalone
Workouts stored before mesocycle placement existed SHALL keep working with mesocycle id and week number both empty.

#### Scenario: Migration on existing data
- **WHEN** the schema migration runs on a database with existing workouts
- **THEN** every existing workout has no mesocycle id and no week number

### Requirement: Planned workouts can be placed in a mesocycle week
A `DRAFT` or `PUBLISHED` workout SHALL be linkable to a mesocycle id and week number, which SHALL set both values. The week number SHALL follow the week number rule (an integer of 1 or more).

#### Scenario: Link a draft workout
- **WHEN** a `DRAFT` standalone workout is linked to mesocycle `M`, week 2
- **THEN** its mesocycle id is `M` and its week number is 2

#### Scenario: Invalid week number
- **WHEN** a workout is linked with week number `0`
- **THEN** it is refused with a validation error and the workout is unchanged

### Requirement: Planned workouts can be moved between weeks and mesocycles
Linking an already linked `DRAFT` or `PUBLISHED` workout SHALL replace both values, moving it to another week of the same mesocycle or to a week of another mesocycle.

#### Scenario: Move to another week
- **WHEN** a `PUBLISHED` workout in mesocycle `M`, week 2 is linked to mesocycle `M`, week 3
- **THEN** its mesocycle id is `M` and its week number is 3

#### Scenario: Move to another mesocycle
- **WHEN** a `DRAFT` workout in mesocycle `M`, week 2 is linked to mesocycle `N`, week 1
- **THEN** its mesocycle id is `N` and its week number is 1

### Requirement: Planned workouts can be taken out of a mesocycle
Unlinking a `DRAFT` or `PUBLISHED` workout SHALL clear both values, leaving a standalone workout.

#### Scenario: Unlink
- **WHEN** a `DRAFT` workout in mesocycle `M`, week 2 is unlinked
- **THEN** its mesocycle id and week number are both empty

### Requirement: Started and finished workouts keep their placement
Linking, moving or unlinking a `STARTED` or `FINISHED` workout SHALL be refused with a conflict error, whatever the caller, and the workout's placement SHALL stay unchanged. This preserves the planning context of executed workouts.

#### Scenario: Move a started workout
- **WHEN** a `STARTED` workout in mesocycle `M`, week 2 is linked to week 3
- **THEN** it is refused with a conflict error and it stays in mesocycle `M`, week 2

#### Scenario: Unlink a finished workout
- **WHEN** a `FINISHED` workout in mesocycle `M`, week 2 is unlinked
- **THEN** it is refused with a conflict error and it stays in mesocycle `M`, week 2

#### Scenario: Link a finished standalone workout
- **WHEN** a `FINISHED` standalone workout is linked to mesocycle `M`, week 1
- **THEN** it is refused with a conflict error and it stays standalone

### Requirement: Workouts and mesocycles stay independent
A workout SHALL reference a mesocycle only by its id and a week number, and a mesocycle SHALL NOT hold its workouts. Loading or saving either SHALL NOT load or change the other.

#### Scenario: Saving a mesocycle leaves workouts alone
- **WHEN** a mesocycle with linked workouts has a week redefined and is saved
- **THEN** the linked workouts keep their mesocycle id and week number unchanged

### Requirement: Placement is stored and restored unchanged
A saved workout SHALL be restored with the same mesocycle id and week number, or with both empty for a standalone workout, both on first save and after later updates.

#### Scenario: Round trip of a linked workout
- **WHEN** a workout linked to mesocycle `M`, week 2 is saved and loaded again
- **THEN** the loaded workout has mesocycle id `M` and week number 2

#### Scenario: Update keeps a new placement
- **WHEN** a stored standalone workout is linked to mesocycle `M`, week 1 and saved again
- **THEN** loading it returns mesocycle id `M` and week number 1

#### Scenario: Update other details keeps placement
- **WHEN** a stored linked workout has its name changed and is saved again
- **THEN** loading it returns the same mesocycle id and week number

### Requirement: Stored placement references an existing mesocycle
Storage SHALL refuse a workout whose mesocycle id matches no mesocycle, and SHALL refuse a workout with only one of the two values set. Whether the week number is within the mesocycle's duration and whether the athlete matches are not checked by storage.

#### Scenario: Unknown mesocycle
- **WHEN** a workout linked to a mesocycle id that matches no mesocycle is saved
- **THEN** persisting fails and the workout is not stored with that link

#### Scenario: Half-set link written directly
- **WHEN** a row with a mesocycle id and no week number is written to storage
- **THEN** storage rejects it

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
