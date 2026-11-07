# Spec Delta

## ADDED Requirements

### Requirement: Only draft workouts are editable for non-admins
Every workout-editing operation (updating the name, description or date, adding, updating, removing or reordering exercises, creating or breaking superseries) on a `PUBLISHED`, `STARTED`, `COMPLETED` or `REVIEWED` workout SHALL be rejected with `409 Conflict` for every non-admin caller, including one the edit rule would otherwise allow. The workout SHALL be unchanged. Status changes are not edits and follow the workout status rules instead.

#### Scenario: Trainer edits a published workout
- **WHEN** the prescribing trainer updates an exercise of a `PUBLISHED` workout
- **THEN** the response is `409 Conflict`
- **AND** the workout is unchanged and still `PUBLISHED`

#### Scenario: Trainer edits after unpublishing
- **WHEN** the prescribing trainer moves a `PUBLISHED` workout back to `DRAFT` and then updates one of its exercises
- **THEN** the edit is applied

#### Scenario: Trainer edits a draft workout
- **WHEN** the prescribing trainer updates an exercise of a `DRAFT` workout
- **THEN** the edit is applied

#### Scenario: Trainer edits a started workout
- **WHEN** the prescribing trainer adds an exercise to a `STARTED` workout
- **THEN** the response is `409 Conflict`
- **AND** the workout is unchanged

#### Scenario: Owner edits a completed self-planned workout
- **WHEN** the owner of a `COMPLETED` workout with no prescribing trainer updates one of its exercises
- **THEN** the response is `409 Conflict`

#### Scenario: Trainer renames a started workout
- **WHEN** the prescribing trainer updates the name of a `STARTED` workout
- **THEN** the response is `409 Conflict`
- **AND** the workout is unchanged

#### Scenario: Unauthorized caller on a locked workout
- **WHEN** a caller the edit rule rejects tries to edit a `STARTED` workout
- **THEN** the response is `403 Forbidden`

#### Scenario: Trainer reorders a started workout
- **WHEN** the prescribing trainer sends a new exercise order for a `STARTED` workout
- **THEN** the response is `409 Conflict`
- **AND** the order is unchanged

### Requirement: Admins bypass the edit lock except on reviewed workouts
A caller with the `ADMIN` role SHALL be allowed to edit a workout in any status except `REVIEWED`. Editing a `REVIEWED` workout SHALL be rejected with `409 Conflict` for an admin too, and the workout SHALL be unchanged.

#### Scenario: Admin edits a completed workout
- **WHEN** an admin removes an exercise from a `COMPLETED` workout
- **THEN** the edit is applied

#### Scenario: Admin renames a completed workout
- **WHEN** an admin updates the name of a `COMPLETED` workout
- **THEN** the update is applied

#### Scenario: Admin edits a published workout
- **WHEN** an admin updates an exercise of a `PUBLISHED` workout
- **THEN** the edit is applied
- **AND** the status is still `PUBLISHED`

#### Scenario: Admin edits a reviewed workout
- **WHEN** an admin updates the name of a `REVIEWED` workout
- **THEN** the response is `409 Conflict`
- **AND** the workout is unchanged

## REMOVED Requirements

### Requirement: Started and finished workouts are locked for non-admins
**Reason**: The edit lock now covers every status except `DRAFT`, and `FINISHED` is now `COMPLETED`.
**Migration**: Replaced by "Only draft workouts are editable for non-admins".

### Requirement: Admins bypass the edit lock
**Reason**: Admins no longer edit `REVIEWED` workouts, and `FINISHED` is now `COMPLETED`.
**Migration**: Replaced by "Admins bypass the edit lock except on reviewed workouts".

### Requirement: Draft and published workouts stay editable
**Reason**: A `PUBLISHED` workout is now locked for non-admins, so its trainer must move it back to `DRAFT` before changing the prescription.
**Migration**: Covered by "Only draft workouts are editable for non-admins" and "Admins bypass the edit lock". Clients that edited published workouts must first set them to `DRAFT` through `PUT /workouts/{workoutId}/status`.
