# Spec Delta

## Purpose

Lets a workout keep the reps and weight actually performed in each set of its exercises, so athletes can log what they did against what was prescribed.

## ADDED Requirements

### Requirement: Add a set record to an exercise of a workout
A workout SHALL let a set record be added to one of its exercises, identified by the catalog exercise identifier. A set record holds the reps done, the weight lifted in kilograms and the moment it was registered, in UTC. Set records are ordered by registration time, and set records with the same registration time keep the order they were added in.

#### Scenario: Record a set
- **WHEN** a set record of 8 reps at 62.5 kg is added to an exercise of a started workout
- **THEN** that exercise holds one set record with 8 reps, 62.5 kg and the given registration time
- **AND** the other exercises of the workout hold no set records

#### Scenario: Several set records are ordered by registration time
- **WHEN** set records registered at 10:05, 10:01 and 10:03 are added to the same exercise in that order
- **THEN** the exercise holds them ordered 10:01, 10:03, 10:05

#### Scenario: Same registration time keeps the order added
- **WHEN** two set records with the same registration time are added to the same exercise
- **THEN** the exercise holds them in the order they were added

#### Scenario: Exercise not in the workout
- **WHEN** a set record is added for an exercise that is not part of the workout
- **THEN** the operation fails with a domain error and no set record is added

### Requirement: Set records only on started workouts
A set record SHALL only be added while the workout is `STARTED`. In any other status the operation fails with a domain error that maps to 409 and nothing changes.

#### Scenario: Started workout
- **WHEN** a set record is added to an exercise of a `STARTED` workout
- **THEN** the set record is added

#### Scenario: Workout in another status
- **WHEN** a set record is added to an exercise of a `DRAFT`, `PUBLISHED` or `FINISHED` workout
- **THEN** the operation fails with a domain error and no set record is added

### Requirement: Set records only for rep-based exercises
A set record SHALL only be added to an exercise whose prescriptions are all rep-based: fixed reps, reps range or max reps, in any mix. If any prescription is a duration or minutes target, the operation fails with a domain error that maps to 409 and nothing changes.

#### Scenario: Mixed rep-based prescriptions
- **WHEN** a set record is added to an exercise prescribed as 1 set of 10 reps, 2 sets of 8-12 reps and 1 set of max reps
- **THEN** the set record is added

#### Scenario: Exercise with a duration prescription
- **WHEN** a set record is added to an exercise prescribed as 3 sets of 40 seconds
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Exercise mixing reps and minutes
- **WHEN** a set record is added to an exercise prescribed as 2 sets of 10 reps and 1 set of 1.5 minutes
- **THEN** the operation fails with a domain error and no set record is added

### Requirement: Set records are not capped by the prescription
An exercise SHALL accept any number of set records, more or fewer than its total prescribed sets, and each set record MAY carry more or fewer reps than prescribed.

#### Scenario: More set records than prescribed sets
- **WHEN** a fourth set record is added to an exercise prescribed as 3 sets of 10 reps
- **THEN** the exercise holds four set records

#### Scenario: Reps outside the prescription
- **WHEN** a set record of 14 reps is added to an exercise prescribed as sets of 8-12 reps
- **THEN** the set record is added with 14 reps

### Requirement: Set record values are validated
The reps of a set record SHALL be at least 1. The weight SHALL be in kilograms, at least 0 (0 means bodyweight), at most the stored maximum and with at most 2 decimals. Invalid values fail with a domain error that maps to 400 and nothing changes.

#### Scenario: Bodyweight set
- **WHEN** a set record of 12 reps at 0 kg is added
- **THEN** the set record is added with 0 kg

#### Scenario: Weight with two decimals
- **WHEN** a set record at 22.75 kg is added
- **THEN** the set record keeps 22.75 kg

#### Scenario: Zero reps
- **WHEN** a set record of 0 reps is added
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Negative weight
- **WHEN** a set record at -5 kg is added
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Weight with too many decimals
- **WHEN** a set record at 20.125 kg is added
- **THEN** the operation fails with a domain error and no set record is added

### Requirement: Adding a set record is not a content edit
Adding a set record SHALL NOT count as editing the workout's content. The workout edit rule and its lock on `STARTED` and `FINISHED` workouts do not apply to it.

#### Scenario: Recording on a locked workout
- **WHEN** a set record is added to a `STARTED` workout, which is locked against content edits
- **THEN** the set record is added

### Requirement: Exercises with set records stay rep-based
While an exercise has set records, updating it SHALL refuse any duration or minutes prescription, for every caller including an admin. The operation fails with a domain error that maps to 409 and the exercise keeps its effort, sets, per-side flag, rest and set records. Changes among rep-based targets are allowed.

#### Scenario: Switch to a duration target
- **WHEN** an exercise with set records is updated to 3 sets of 40 seconds
- **THEN** the operation fails with a domain error and the exercise is unchanged

#### Scenario: Change between rep-based targets
- **WHEN** an exercise with set records is updated from 3 sets of 10 reps to 3 sets of 8-12 reps
- **THEN** the exercise is updated and keeps its set records

#### Scenario: Exercise without set records
- **WHEN** an exercise without set records is updated to 3 sets of 40 seconds
- **THEN** the exercise is updated

### Requirement: Set records are stored with the workout
Set records SHALL be persisted with the workout and restored unchanged when it is loaded again. A workout created from a template starts with no set records. Removing an exercise from a workout also removes its set records.

#### Scenario: Round trip
- **WHEN** a workout with set records on two exercises is saved and loaded again
- **THEN** each exercise holds the same set records, in the same order, with the same reps, weight and registration time

#### Scenario: Recording on a stored workout keeps earlier set records
- **WHEN** a set record is added to a stored workout whose exercise already has set records, and the workout is saved and loaded again
- **THEN** the exercise holds the earlier set records followed by the new one

#### Scenario: Workout from a template
- **WHEN** a workout is created from a template
- **THEN** none of its exercises hold set records

#### Scenario: Removed exercise
- **WHEN** an exercise with set records is removed from a stored workout and the workout is saved
- **THEN** its set records are no longer stored
