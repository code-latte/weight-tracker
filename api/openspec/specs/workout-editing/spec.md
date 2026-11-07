# workout-editing Specification

## Purpose

Decides who may change an existing workout, so every workout-editing operation applies the same permission rule.

## Requirements

### Requirement: Admins may edit any workout
An authenticated caller with the `ADMIN` role SHALL be allowed to edit any workout, whether or not it has a prescribing trainer.

#### Scenario: Admin edits a prescribed workout
- **WHEN** an admin edits a workout that has a prescribing trainer
- **THEN** the edit is applied

#### Scenario: Admin edits a self-created workout
- **WHEN** an admin edits a workout that has no prescribing trainer
- **THEN** the edit is applied

### Requirement: Only the prescribing trainer may edit a prescribed workout
When a workout has a prescribing trainer, the system SHALL allow that trainer to edit it and SHALL reject every other non-admin caller, including the workout's owner, with `403 Forbidden`.

#### Scenario: Prescribing trainer edits
- **WHEN** the workout's prescribing trainer edits the workout
- **THEN** the edit is applied

#### Scenario: Owner of a prescribed workout is rejected
- **WHEN** the owner of a workout that has a prescribing trainer tries to edit it
- **THEN** the response is `403 Forbidden`
- **AND** the workout is unchanged

#### Scenario: Another trainer is rejected
- **WHEN** a trainer who is neither the prescribing trainer nor the owner tries to edit a prescribed workout
- **THEN** the response is `403 Forbidden`
- **AND** the workout is unchanged

### Requirement: The owner may edit a workout without a prescribing trainer
When a workout has no prescribing trainer, the system SHALL allow its owner to edit it, whatever the owner's role, and SHALL reject every other non-admin caller with `403 Forbidden`.

#### Scenario: Regular user edits their own workout
- **WHEN** a user with the `USER` role edits a workout they own that has no prescribing trainer
- **THEN** the edit is applied

#### Scenario: Trainer edits their own workout
- **WHEN** a trainer edits a workout they own that has no prescribing trainer
- **THEN** the edit is applied

#### Scenario: A trainer who is not the owner is rejected
- **WHEN** a trainer who does not own a workout with no prescribing trainer tries to edit it
- **THEN** the response is `403 Forbidden`
- **AND** the workout is unchanged

### Requirement: Missing workouts are reported before permissions
An edit aimed at a workout that does not exist SHALL fail with `404 Not Found` for every authenticated caller.

#### Scenario: Unknown workout
- **WHEN** an authenticated caller edits a workout id that does not exist
- **THEN** the response is `404 Not Found`

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

### Requirement: Updating a workout's details is an edit
Updating a workout's `name`, `description` or `workoutDate` through `PUT /workouts/{id}` SHALL follow the same edit rule as changing its content: admins always, the prescribing trainer of a prescribed workout, and the owner of a workout without trainer. Every other caller SHALL be rejected with `403 Forbidden` and the workout SHALL be unchanged.

#### Scenario: Prescribing trainer renames a workout
- **WHEN** the prescribing trainer of a workout updates its name
- **THEN** the update is applied

#### Scenario: Owner renames a self-planned workout
- **WHEN** the owner of a workout without trainer updates its name
- **THEN** the update is applied

#### Scenario: Owner of a prescribed workout is rejected
- **WHEN** the owner of a workout that has a prescribing trainer updates its name
- **THEN** the response is `403 Forbidden`
- **AND** the workout is unchanged

#### Scenario: Unrelated caller is rejected
- **WHEN** a user who is neither the owner nor the trainer updates a workout
- **THEN** the response is `403 Forbidden`

#### Scenario: Admin renames any workout
- **WHEN** an admin updates the name of a workout they neither own nor train
- **THEN** the update is applied
