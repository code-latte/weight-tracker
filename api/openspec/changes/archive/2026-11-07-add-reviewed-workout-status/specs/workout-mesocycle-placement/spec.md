# Spec Delta

## MODIFIED Requirements

### Requirement: Locked workouts keep their placement over HTTP
Changing or clearing the placement of a `STARTED`, `COMPLETED` or `REVIEWED` workout through `PUT /workouts/{id}` SHALL be refused with `409`, also for admins. Sending back its current placement unchanged SHALL NOT be refused for that reason.

#### Scenario: Admin moves a started workout
- **WHEN** an admin updates the seeded started workout in week 2 sending week 3
- **THEN** the response is `409` and it stays in week 2

#### Scenario: Admin edits a started workout keeping its placement
- **WHEN** an admin renames the seeded started workout sending its current placement
- **THEN** the response is `200` and it stays in week 2

## ADDED Requirements

### Requirement: Executed workouts keep their placement
Linking, moving or unlinking a `STARTED`, `COMPLETED` or `REVIEWED` workout SHALL be refused with a conflict error, whatever the caller, and the workout's placement SHALL stay unchanged. This preserves the planning context of executed workouts. `DRAFT` and `PUBLISHED` workouts are not affected by this rule.

#### Scenario: Move a started workout
- **WHEN** a `STARTED` workout in mesocycle `M`, week 2 is linked to week 3
- **THEN** it is refused with a conflict error and it stays in mesocycle `M`, week 2

#### Scenario: Unlink a completed workout
- **WHEN** a `COMPLETED` workout in mesocycle `M`, week 2 is unlinked
- **THEN** it is refused with a conflict error and it stays in mesocycle `M`, week 2

#### Scenario: Link a completed standalone workout
- **WHEN** a `COMPLETED` standalone workout is linked to mesocycle `M`, week 1
- **THEN** it is refused with a conflict error and it stays standalone

#### Scenario: Move a reviewed workout
- **WHEN** a `REVIEWED` workout in mesocycle `M`, week 2 is linked to week 3
- **THEN** it is refused with a conflict error and it stays in mesocycle `M`, week 2

#### Scenario: Move a published workout
- **WHEN** a `PUBLISHED` workout in mesocycle `M`, week 2 is linked to week 3
- **THEN** it is moved to week 3

## REMOVED Requirements

### Requirement: Started and finished workouts keep their placement
**Reason**: The placement lock now covers `STARTED`, `COMPLETED` and `REVIEWED`, and `FINISHED` is now `COMPLETED`.
**Migration**: Replaced by "Executed workouts keep their placement".
