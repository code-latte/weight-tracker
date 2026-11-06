# Spec Delta

## Purpose

Lets a workout optionally point to one week of a mesocycle, so planned workouts can be placed in, moved between and
removed from training plans, while workouts and mesocycles stay independent and started or finished workouts keep
their planning history.

## ADDED Requirements

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

### Requirement: Existing workout HTTP behaviour is unchanged
No workout endpoint SHALL expose, accept or change the mesocycle id or week number in this change, and existing workout responses SHALL keep their current shape.

#### Scenario: Reading a linked workout
- **WHEN** a caller allowed to see a linked workout reads it through `GET /workouts/{id}`
- **THEN** the response has the same fields as before this change

#### Scenario: Updating a linked workout through the API
- **WHEN** an allowed caller updates a linked workout through `PUT /workouts/{id}`
- **THEN** the workout keeps its mesocycle id and week number
