# workout-deletion Specification

## Purpose

Decides who may permanently delete a workout and in which status, so abandoned drafts can be removed without risking published, executed or reviewed workouts.

## Requirements

### Requirement: Authorized callers delete a draft workout
`DELETE /workouts/{workoutId}` SHALL permanently delete a `DRAFT` workout, together with its exercises, set prescriptions and superseries, and respond `204 No Content` with no body.

#### Scenario: Admin deletes a draft
- **WHEN** an `ADMIN` deletes a `DRAFT` workout
- **THEN** the response is `204 No Content`
- **AND** a later `GET /workouts/{workoutId}` returns `404 Not Found`

#### Scenario: Prescribing trainer deletes a draft
- **WHEN** the workout's prescribing trainer deletes a `DRAFT` workout
- **THEN** the response is `204 No Content`
- **AND** the workout no longer appears in `GET /workouts` for the trainer or the athlete

#### Scenario: Owner deletes a draft without trainer
- **WHEN** the owner of a `DRAFT` workout that has no prescribing trainer deletes it
- **THEN** the response is `204 No Content`
- **AND** the workout no longer appears in the owner's `GET /workouts`

#### Scenario: Deleting removes the workout's content
- **WHEN** a `DRAFT` workout with exercises, set prescriptions and a superseries is deleted
- **THEN** none of its exercises, set prescriptions or superseries remain stored

### Requirement: Delete permission follows the edit permission
Only an `ADMIN`, the prescribing trainer of a workout that has one, or the owner of a workout without prescribing trainer SHALL be allowed to delete it. Every other caller, including the owner of a prescribed workout, SHALL be rejected with `403 Forbidden` and the workout SHALL be unchanged.

#### Scenario: Owner of a prescribed workout is rejected
- **WHEN** the owner of a `DRAFT` workout that has a prescribing trainer tries to delete it
- **THEN** the response is `403 Forbidden`
- **AND** the workout still exists

#### Scenario: Unrelated trainer is rejected
- **WHEN** a `TRAINER` who is neither the prescribing trainer nor the owner tries to delete a workout
- **THEN** the response is `403 Forbidden`

#### Scenario: Unrelated user is rejected
- **WHEN** a `USER` who is not the owner tries to delete a workout without trainer
- **THEN** the response is `403 Forbidden`

### Requirement: Only draft workouts can be deleted
Deleting a `PUBLISHED`, `STARTED`, `COMPLETED` or `REVIEWED` workout SHALL be rejected with `409 Conflict` for every permitted caller, `ADMIN` included, and the workout SHALL be unchanged.

#### Scenario: Admin cannot delete a published workout
- **WHEN** an `ADMIN` tries to delete a `PUBLISHED` workout
- **THEN** the response is `409 Conflict`
- **AND** the workout still exists with its status unchanged

#### Scenario: Prescribing trainer cannot delete a non-draft workout
- **WHEN** the prescribing trainer tries to delete a `STARTED`, `COMPLETED` or `REVIEWED` workout
- **THEN** the response is `409 Conflict`

### Requirement: Permission is checked before status
A caller who may not delete a workout SHALL receive `403 Forbidden` whatever the workout's status, so the status is never revealed to them.

#### Scenario: Forbidden caller on a published workout
- **WHEN** a caller without delete permission tries to delete a `PUBLISHED` workout
- **THEN** the response is `403 Forbidden`, not `409 Conflict`

### Requirement: Deleting a missing workout
Deleting a workout that does not exist SHALL fail with `404 Not Found` for every authenticated caller.

#### Scenario: Unknown workout id
- **WHEN** an authenticated caller deletes a workout id that does not exist
- **THEN** the response is `404 Not Found`

### Requirement: Delete requires authentication
`DELETE /workouts/{workoutId}` SHALL reject a request without a valid bearer token with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** the request carries no bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** the request carries an invalid bearer token
- **THEN** the response is `401 Unauthorized`

### Requirement: Concurrent change prevents deletion
If the workout changed after it was read for the delete, the delete SHALL be refused with `409 Conflict` and the workout SHALL remain stored.

#### Scenario: Workout changed between read and delete
- **WHEN** the workout's stored version differs from the version read by the delete
- **THEN** the delete is refused with `409 Conflict`
- **AND** the workout still exists
