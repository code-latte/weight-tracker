# Spec Delta

## MODIFIED Requirements

### Requirement: Adding a set record is not a content edit
Adding, editing, removing and reordering set records SHALL NOT count as editing the workout's content. The workout edit rule and its lock on non-draft workouts do not apply to them; the recording rule applies instead.

#### Scenario: Recording on a locked workout
- **WHEN** a set record is added to a `STARTED` workout, which is locked against content edits
- **THEN** the set record is added

#### Scenario: Athlete of a prescribed workout records
- **WHEN** the athlete of a prescribed `STARTED` workout, who may not edit its content, adds a set record
- **THEN** the set record is added

## ADDED Requirements

### Requirement: Set records only on started or completed workouts
Set records SHALL only be added, edited, removed or reordered while the workout is `STARTED` or `COMPLETED`, for every caller including an admin. `COMPLETED` lets the athlete correct results after completing the workout. In any other status the operation fails with a domain error that maps to 409 and nothing changes.

#### Scenario: Started workout
- **WHEN** a set record is added to an exercise of a `STARTED` workout
- **THEN** the set record is added

#### Scenario: Workout in another status
- **WHEN** a set record is added to an exercise of a `DRAFT`, `PUBLISHED` or `REVIEWED` workout
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Correct a set record of a completed workout
- **WHEN** the athlete edits a set record of a `COMPLETED` workout from 8 reps to 10 reps
- **THEN** the set record has 10 reps

#### Scenario: Add, edit, remove or reorder on a completed workout
- **WHEN** a set record of a `COMPLETED` workout is added, edited, removed or reordered
- **THEN** the change is applied

#### Scenario: Admin on a completed workout
- **WHEN** an admin edits a set record of a `COMPLETED` workout
- **THEN** the change is applied

#### Scenario: Edit, remove or reorder on a reviewed workout
- **WHEN** a set record of a `REVIEWED` workout is edited, removed or reordered
- **THEN** the operation fails with a domain error and the set records are unchanged

#### Scenario: Admin on a reviewed workout
- **WHEN** an admin edits a set record of a `REVIEWED` workout
- **THEN** the operation fails with a domain error and the set record is unchanged

### Requirement: Every change to the results is timestamped
Adding, editing, removing or reordering a set record SHALL set the workout's `resultsUpdatedAt` to the server time of the request, in UTC. A workout whose set records were never changed SHALL have `resultsUpdatedAt` `null`. Operations that fail SHALL leave it unchanged.

#### Scenario: First set record
- **WHEN** the first set record of a `STARTED` workout is added
- **THEN** the workout's `resultsUpdatedAt` is the time of that request

#### Scenario: Correction moves the timestamp
- **WHEN** a set record of a `COMPLETED` workout is edited, removed or reordered
- **THEN** the workout's `resultsUpdatedAt` is the time of that request

#### Scenario: No results yet
- **WHEN** a workout without set records that were ever changed is read
- **THEN** its `resultsUpdatedAt` is `null`

#### Scenario: Failed change keeps the timestamp
- **WHEN** a set record change on a `REVIEWED` workout is refused
- **THEN** the workout's `resultsUpdatedAt` is unchanged

## REMOVED Requirements

### Requirement: Set records only on started workouts
**Reason**: Set records may now also change while the workout is `COMPLETED`, so the athlete can correct results; `REVIEWED` locks them.
**Migration**: Replaced by "Set records only on started or completed workouts".
