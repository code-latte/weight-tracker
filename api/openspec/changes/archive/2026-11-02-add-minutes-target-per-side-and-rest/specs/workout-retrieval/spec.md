# Spec Delta

## MODIFIED Requirements

### Requirement: The response contains the whole workout without catalog exercise data
A successful read SHALL respond `200 OK` with `id`, `name`, `description` (or `null`), `workoutDate`, `userId`, `trainerId` (or `null`), `status` and the `exercises` list. Each entry SHALL carry its own `id`, the catalog `exerciseId`, `effort`, `perSide`, `restMinutes` (or `null`), its `sets` and its `superSeriesId` (or `null`). Entries SHALL NOT carry catalog fields such as `name` or `bodyPart`.

#### Scenario: Workout with exercises and sets
- **WHEN** the owner reads a workout that has exercises with sets
- **THEN** the response is `200 OK` with its `name`, `description`, `status`, every entry, its sets and its superseries membership
- **AND** no entry contains `name` or `bodyPart`

#### Scenario: Empty workout
- **WHEN** the owner reads a workout with no exercises
- **THEN** the response is `200 OK` with an empty `exercises` list

#### Scenario: Entries carry per side and rest
- **WHEN** the owner reads a workout with one entry that has perSide `true` and restMinutes `1.25`, and another with neither
- **THEN** the first entry has `perSide` `true` and `restMinutes` `1.25`
- **AND** the second entry has `perSide` `false` and `restMinutes` `null`

#### Scenario: Minutes target in the response
- **WHEN** the owner reads a workout with a `1 x Minutes` set of 30 minutes
- **THEN** that set's target has `type` `Minutes` and `minutes` `30`, and no `value` or `maxValue`

## ADDED Requirements

### Requirement: The workout list carries per side and rest
Each entry in the workout list response SHALL carry `perSide` and `restMinutes` (or `null`), and each `Minutes` set target SHALL carry `minutes`, in the same way as the read-by-id response.

#### Scenario: List entry with per side and rest
- **WHEN** a caller lists workouts that include an entry with perSide `true`, restMinutes `2` and a `1 x Minutes` set of 30 minutes
- **THEN** that entry has `perSide` `true` and `restMinutes` `2`, and its set target has `type` `Minutes` and `minutes` `30`
