# Spec Delta

## MODIFIED Requirements

### Requirement: The added exercise must exist in the catalog
Adding an exercise whose identifier matches no exercise, or matches a custom exercise the caller cannot see, SHALL fail with `404 Not Found` and leave the workout unchanged.

#### Scenario: Unknown catalog exercise
- **WHEN** an exercise id that is not in the catalog is added
- **THEN** the response is `404 Not Found`
- **AND** the workout is unchanged

#### Scenario: Custom exercise the caller cannot see
- **WHEN** a trainer adds another user's custom exercise that is in no workout the trainer can see
- **THEN** the response is `404 Not Found` and the workout is unchanged

## ADDED Requirements

### Requirement: Custom exercises are used only by their owner's workouts
A custom exercise SHALL be addable to a workout only when its owner is the workout's editor of record: the prescribing trainer when the workout has one, otherwise the workout's owner. Otherwise, and when the caller can see the exercise, the addition SHALL fail with `409 Conflict` and leave the workout unchanged. System exercises SHALL remain addable to any workout the caller may edit.

#### Scenario: Trainer adds their own exercise to a prescribed workout
- **WHEN** the prescribing trainer adds a custom exercise they own
- **THEN** the response is `201 Created`

#### Scenario: Athlete adds their own exercise to a self-planned workout
- **WHEN** the owner of a workout with no trainer adds a custom exercise they own
- **THEN** the response is `201 Created`

#### Scenario: Admin adds someone else's exercise
- **WHEN** an ADMIN adds trainer A's custom exercise to a workout prescribed by trainer B
- **THEN** the response is `409 Conflict` and the workout is unchanged

#### Scenario: Athlete's exercise in a prescribed workout
- **WHEN** an ADMIN adds an athlete's custom exercise to a workout prescribed to that athlete by a trainer
- **THEN** the response is `409 Conflict`

### Requirement: Archived exercises cannot be added to workouts
Adding an archived custom exercise to a workout SHALL fail with `409 Conflict` and leave the workout unchanged. Entries that already use an archived exercise SHALL keep it, and their effort, sets, per-side flag and rest SHALL stay editable.

#### Scenario: Add an archived exercise
- **WHEN** the owner adds their archived exercise to their workout
- **THEN** the response is `409 Conflict`

#### Scenario: Update an entry with an archived exercise
- **WHEN** the prescribing trainer updates the sets of an entry whose exercise they archived
- **THEN** the response is `200 OK`
