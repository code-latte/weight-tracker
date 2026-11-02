# Spec Delta

## MODIFIED Requirements

### Requirement: Set prescriptions are validated
Every set prescription SHALL have a count of at least 1 and a target type of `Reps`, `Duration`, `MaxReps`, `RepsRange` or `Minutes`. `Reps` and `Duration` SHALL have a target of at least 1; `Duration` is in seconds. `MaxReps` SHALL ignore the target. `RepsRange` SHALL have a target (minimum reps) of at least 1 and a `maxTarget` (maximum reps) strictly greater than the target. Every type other than `RepsRange` SHALL ignore `maxTarget`. `Minutes` SHALL have a `targetMinutes` that is a valid minutes value and SHALL ignore `target` and `maxTarget`. Every type other than `Minutes` SHALL ignore `targetMinutes`. An empty or missing list, or any invalid prescription, SHALL fail with `400 Bad Request` and change nothing.

#### Scenario: Max reps ignores the target
- **WHEN** an exercise is added with sets `2 x MaxReps` and target `0`
- **THEN** the entry has 2 sets to failure and no target value

#### Scenario: Duration in seconds
- **WHEN** an exercise is added with sets `3 x Duration` and target `40`
- **THEN** the entry has 3 sets of 40 seconds

#### Scenario: Reps range
- **WHEN** an exercise is added or updated with sets `3 x RepsRange`, target `8` and maxTarget `12`
- **THEN** the entry has 3 sets of 8 to 12 reps
- **AND** the set target in the response has type `RepsRange`, value `8` and maxValue `12`

#### Scenario: Range maximum not above the minimum
- **WHEN** a `RepsRange` prescription has target `10` and maxTarget `10`, or target `10` and maxTarget `8`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Range without a maximum
- **WHEN** a `RepsRange` prescription has target `8` and no maxTarget
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Range minimum below one
- **WHEN** a `RepsRange` prescription has target `0` and maxTarget `12`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Other types ignore the maximum
- **WHEN** an exercise is added with sets `3 x Reps`, target `10` and maxTarget `12`
- **THEN** the entry has 3 sets of 10 reps
- **AND** the set target in the response has no maxValue

#### Scenario: Minutes target
- **WHEN** an exercise is added or updated with sets `1 x Minutes` and targetMinutes `30`
- **THEN** the entry has 1 set of 30 minutes
- **AND** the set target in the response has type `Minutes`, minutes `30`, and no value or maxValue

#### Scenario: Minutes target with decimals
- **WHEN** an exercise is added with sets `3 x Minutes` and targetMinutes `1.25`
- **THEN** the set target in the response has type `Minutes` and minutes `1.25`

#### Scenario: Minutes target missing or invalid
- **WHEN** a `Minutes` prescription has no targetMinutes, or targetMinutes `0`, `-1`, `1.255` or `1000`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Minutes ignores the integer targets
- **WHEN** an exercise is added with sets `1 x Minutes`, targetMinutes `30`, target `5` and maxTarget `9`
- **THEN** the set target in the response has type `Minutes`, minutes `30`, and no value or maxValue

#### Scenario: Other types ignore targetMinutes
- **WHEN** an exercise is added with sets `3 x Duration`, target `40` and targetMinutes `2`
- **THEN** the entry has 3 sets of 40 seconds
- **AND** the set target in the response has no minutes

#### Scenario: Empty sets
- **WHEN** an exercise is added or updated with an empty sets list
- **THEN** the response is `400 Bad Request`

#### Scenario: Non-positive count or target
- **WHEN** a prescription has count `0`, or a `Reps` or `Duration` target of `0`
- **THEN** the response is `400 Bad Request`

#### Scenario: Unknown target type
- **WHEN** a prescription has target type `Weight`
- **THEN** the response is `400 Bad Request`

### Requirement: Update replaces effort and all sets of an entry
The system SHALL let a caller who may edit the workout replace the effort, the per-side flag, the rest and the whole set prescription list of an exercise in the workout, identified by its catalog exercise id. The entry SHALL keep its entry identifier, position and superseries. The previous set prescriptions SHALL be discarded and the new ones stored, even when they are equal in value. An omitted `perSide` SHALL be stored as `false` and an omitted `restMinutes` as no rest.

#### Scenario: Change effort and sets
- **WHEN** an entry with `3 x 10 Reps` at effort `MID` is updated to `2 x 12 Reps` then `2 x 10 Reps` at effort `HIGH`
- **THEN** the response is `200 OK` with the updated entry
- **AND** reading the workout shows only the new prescriptions, at the same position and with the same entry id

#### Scenario: Effort-only change still replaces the stored sets
- **WHEN** an entry is updated with a new effort and the same set values
- **THEN** the stored set prescriptions are new records equal in value to the old ones
- **AND** none of the previous set records remain

#### Scenario: Change per side and rest
- **WHEN** an entry with perSide `false` and no rest is updated with perSide `true` and restMinutes `1.5`
- **THEN** the response is `200 OK` with perSide `true` and restMinutes `1.5`
- **AND** reading the workout returns the same values

#### Scenario: Omitted per side and rest clear previous values
- **WHEN** an entry with perSide `true` and restMinutes `2` is updated without perSide and without restMinutes
- **THEN** reading the workout returns perSide `false` and restMinutes `null` for that entry

## ADDED Requirements

### Requirement: Minutes values are validated
A minutes value, used by the `Minutes` target and by the rest, SHALL be greater than 0, have at most 2 decimal places, and be no greater than 999.99. Values with more decimals SHALL be rejected, not rounded. The JSON value SHALL be a number with a dot as the decimal separator. An invalid value SHALL fail with `400 Bad Request` and change nothing.

#### Scenario: Two decimals accepted
- **WHEN** an exercise is added with restMinutes `0.75`
- **THEN** the entry's restMinutes is `0.75`

#### Scenario: Upper bound accepted
- **WHEN** an exercise is added with sets `1 x Minutes` and targetMinutes `999.99`
- **THEN** the set target in the response has minutes `999.99`

#### Scenario: Too many decimals rejected
- **WHEN** an exercise is added with restMinutes `1.333`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Out of range rejected
- **WHEN** an exercise is added with restMinutes `0` or `1000`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

### Requirement: An exercise can be marked per side
Add and update SHALL accept an optional `perSide` flag for the whole entry, defaulting to `false`. The flag SHALL NOT change the stored targets: they are per side. It SHALL be allowed with every target type.

#### Scenario: Per-side reps
- **WHEN** an exercise is added with perSide `true` and sets `1 x 8 Reps`
- **THEN** the response is `201 Created` with perSide `true` and a target of 8 reps

#### Scenario: Per-side duration
- **WHEN** an exercise is added with perSide `true` and sets `3 x Duration` with target `30`
- **THEN** the entry has perSide `true` and 3 sets of 30 seconds

#### Scenario: Per side omitted
- **WHEN** an exercise is added without perSide
- **THEN** the entry has perSide `false`

### Requirement: An exercise can prescribe its rest
Add and update SHALL accept an optional `restMinutes` for the whole entry, which SHALL be a valid minutes value. When it is omitted or `null`, the entry SHALL have no rest prescribed.

#### Scenario: Rest with decimals
- **WHEN** an exercise is added with restMinutes `1.5`
- **THEN** the response is `201 Created` with restMinutes `1.5`
- **AND** reading the workout returns restMinutes `1.5` for that entry

#### Scenario: Rest omitted
- **WHEN** an exercise is added without restMinutes
- **THEN** the entry has restMinutes `null`

#### Scenario: Invalid rest leaves the entry unchanged
- **WHEN** an entry is updated with restMinutes `-1`
- **THEN** the response is `400 Bad Request`
- **AND** the entry keeps its previous effort, sets, perSide and rest
