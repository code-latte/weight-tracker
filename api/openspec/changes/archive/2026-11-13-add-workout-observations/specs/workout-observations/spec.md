# Spec Delta

## Purpose

States the measurable facts (observations) about a completed workout prescribed by a trainer, compared with its prescription and the athlete's earlier sessions with that trainer, and selects at most 3 of them (highlights) for the trainer's attention.

## ADDED Requirements

### Requirement: Observations are generated when a prescribed workout is completed
Every time a workout with a prescribing trainer reaches `COMPLETED` through `PUT /workouts/{workoutId}/status`, through the lifecycle or an admin override, the system SHALL generate its observations and highlights after the completion is saved. A workout without a prescribing trainer SHALL get none. No other status change and no set record change SHALL generate them.

#### Scenario: Athlete completes a prescribed workout
- **WHEN** the athlete sets a prescribed `STARTED` workout to `COMPLETED`
- **THEN** the response is `200 OK`
- **AND** `GET /workouts/{workoutId}/observations` returns the observations generated for it

#### Scenario: Admin completes a prescribed workout
- **WHEN** an admin sets a prescribed `DRAFT` workout to `COMPLETED`
- **THEN** observations are generated for it

#### Scenario: Workout without trainer
- **WHEN** the athlete completes a workout that has no prescribing trainer
- **THEN** `GET /workouts/{workoutId}/observations` returns empty `observations` and `highlights`

#### Scenario: Other transitions do not generate
- **WHEN** a prescribed workout is published or started
- **THEN** no observations exist for it

#### Scenario: Corrections after completion do not regenerate
- **WHEN** a set record of a `COMPLETED` prescribed workout is updated after its observations were generated
- **THEN** its observations are unchanged

### Requirement: Completing again replaces the observations
When a workout that already has observations reaches `COMPLETED` again, the system SHALL replace all its observations and highlights with the newly generated ones. A workout SHALL never hold observations from two completions.

#### Scenario: Admin reopens and completes again
- **WHEN** an admin moves a completed prescribed workout to `STARTED`, a set record changes, and the workout reaches `COMPLETED` again
- **THEN** the observations reflect the changed set record
- **AND** none of the observations of the first completion remain

### Requirement: A generation failure does not undo the completion
If generating the observations fails, the completion SHALL stay saved, the workout SHALL have no observations from that completion, and the status request SHALL respond `500 Internal Server Error`.

#### Scenario: Generation fails
- **WHEN** an athlete completes a prescribed workout and generating its observations fails
- **THEN** the response is `500 Internal Server Error`
- **AND** the workout's status is `COMPLETED`

### Requirement: History is the athlete's earlier sessions with the same trainer
Rules that compare with history SHALL only use executions of the exercise in the athlete's workouts that are `COMPLETED` or `REVIEWED`, prescribed by the same trainer as the completed workout, and strictly before it by workout date, ties broken by the earlier created workout. The completed workout itself SHALL never be part of its history.

#### Scenario: Self-created sessions are excluded
- **WHEN** the athlete's only earlier finished session of an exercise is a workout without trainer
- **THEN** no observation that compares with history is generated for that exercise

#### Scenario: Sessions with another trainer are excluded
- **WHEN** the athlete's only earlier finished session of an exercise was prescribed by a different trainer
- **THEN** no observation that compares with history is generated for that exercise

#### Scenario: Unfinished and later sessions are excluded
- **WHEN** the same trainer's only other workouts with the exercise are a `STARTED` one dated earlier and a `COMPLETED` one dated later
- **THEN** no observation that compares with history is generated for that exercise

### Requirement: Only comparable executions are compared
An earlier execution SHALL be comparable only when it has the same exercise, the same result kind (reps or duration) and the same per-side setting, and its workout is not in a mesocycle week of type `DELOAD` or `RECOVERY`. Non-comparable executions SHALL be skipped: they neither count towards nor break a streak.

#### Scenario: Deload session skipped
- **WHEN** the previous session of an exercise was in a `DELOAD` week and the one before it was not
- **THEN** comparisons use the one before it as the previous execution

#### Scenario: Per-side change skipped
- **WHEN** the only earlier session of an exercise was prescribed per side and the current one is not
- **THEN** no observation that compares with history is generated for that exercise

### Requirement: A deload or recovery workout only gets compliance observations
When the completed workout is in a mesocycle week of type `DELOAD` or `RECOVERY`, the system SHALL only generate `WORKOUT_INCOMPLETE`, `EXERCISE_SKIPPED`, `REPS_BELOW_TARGET`, `REPS_ABOVE_TARGET` and `ALL_TARGETS_MET`.

#### Scenario: Lighter weight in a deload week
- **WHEN** the athlete lifts 20% less than in the previous comparable execution in a workout placed in a `DELOAD` week
- **THEN** no `WEIGHT_DECREASE` observation is generated

### Requirement: Rules without enough history are skipped
A rule that needs a number of earlier comparable executions SHALL generate no observation when fewer exist. A rule whose measured value is missing on either side (no weighted sets, no rep sets) or whose baseline is 0 SHALL generate no observation.

#### Scenario: First session of an exercise
- **WHEN** the athlete has no earlier comparable execution of an exercise
- **THEN** only compliance observations are generated for it

#### Scenario: No weights recorded
- **WHEN** neither the current nor the previous execution of an exercise has a set with weight
- **THEN** no weight or volume observation is generated for it

### Requirement: Sets are matched to the prescription by position
Recorded sets SHALL be matched to prescribed sets in order: the first recorded set to the first prescribed set, and so on. Recorded sets beyond the prescribed count SHALL NOT count for compliance but SHALL count for weight, reps and volume.

#### Scenario: Extra set recorded
- **WHEN** 3 sets are prescribed and 4 are recorded, the fourth below the target
- **THEN** no `REPS_BELOW_TARGET` is generated for the fourth set
- **AND** the fourth set counts in the exercise's volume

### Requirement: Incomplete workout
The system SHALL generate one `WORKOUT_INCOMPLETE` observation, severity `WARNING`, with no exercise, when the matched recorded sets are fewer than the prescribed sets across the workout. Evidence: `recordedSets`, `prescribedSets`, `completionPercent`.

#### Scenario: Sets missing
- **WHEN** 15 sets are prescribed and 9 are matched
- **THEN** `WORKOUT_INCOMPLETE` is generated with `{ "recordedSets": 9, "prescribedSets": 15, "completionPercent": 60 }`

### Requirement: Skipped exercise
The system SHALL generate `EXERCISE_SKIPPED`, severity `WARNING`, for each prescribed exercise with no recorded sets. Evidence: `prescribedSets`. It SHALL never be highlighted.

#### Scenario: Exercise not done
- **WHEN** an exercise with 3 prescribed sets has no set records
- **THEN** `EXERCISE_SKIPPED` is generated for it with `{ "prescribedSets": 3 }`

### Requirement: Reps outside the prescribed target
For each exercise, the system SHALL generate one `REPS_BELOW_TARGET` (severity `WARNING`) when any matched set has fewer reps than its `Reps` value or `RepsRange` minimum, and one `REPS_ABOVE_TARGET` (severity `INFO`) when any matched set has more reps than its `RepsRange` maximum. Evidence: `setPositions`, `reps`, and `targetMins` or `targetMaxes`, one entry per affected set.

#### Scenario: Two sets below the range
- **WHEN** an exercise prescribed `8-12` reps for 4 sets records 10, 9, 6 and 5 reps
- **THEN** one `REPS_BELOW_TARGET` is generated with `{ "setPositions": [3, 4], "reps": [6, 5], "targetMins": [8, 8] }`

#### Scenario: Above the range
- **WHEN** an exercise prescribed `8-12` reps records 14 reps in its first set
- **THEN** `REPS_ABOVE_TARGET` is generated with `{ "setPositions": [1], "reps": [14], "targetMaxes": [12] }`

#### Scenario: Max reps and fixed reps have no upper bound
- **WHEN** a set prescribed as `MaxReps` or as `10` reps records 20 reps
- **THEN** no `REPS_ABOVE_TARGET` is generated for it

### Requirement: All targets met
The system SHALL generate one `ALL_TARGETS_MET` observation, severity `SUCCESS`, with no exercise, when every prescribed set of the workout has a matched recorded set that meets its target and none is above a range maximum. Evidence: `prescribedSets`.

#### Scenario: Perfect workout
- **WHEN** every prescribed set is recorded within its target
- **THEN** `ALL_TARGETS_MET` is generated

#### Scenario: One set above the range
- **WHEN** every prescribed set is recorded and met, but one is above its range maximum
- **THEN** no `ALL_TARGETS_MET` is generated

### Requirement: Weight change against the previous execution
For each exercise, the system SHALL compare the highest weight of its sets with the highest weight of the previous comparable execution, and generate `WEIGHT_INCREASE` or `WEIGHT_DECREASE`, severity `INFO`, when it differs. Evidence: `previousMaxKg`, `currentMaxKg`, `changePercent`, plus `baselineMaxKg` for a decrease when the execution before the previous exists.

#### Scenario: Heavier than last time
- **WHEN** the previous comparable execution topped at 100 kg and the current one at 105 kg
- **THEN** `WEIGHT_INCREASE` is generated with `{ "previousMaxKg": 100, "currentMaxKg": 105, "changePercent": 5 }`

#### Scenario: Same weight
- **WHEN** both executions topped at 100 kg
- **THEN** neither `WEIGHT_INCREASE` nor `WEIGHT_DECREASE` is generated

### Requirement: Weight personal record
The system SHALL generate `WEIGHT_PERSONAL_RECORD`, severity `SUCCESS`, when the highest weight of the exercise is above the highest weight of every earlier comparable execution. Evidence: `previousMaxKg` (the earlier record), `currentMaxKg`, `changePercent`. A personal record SHALL NOT suppress `WEIGHT_INCREASE`.

#### Scenario: New record
- **WHEN** the earlier record is 120 kg and the current top weight is 130 kg
- **THEN** `WEIGHT_PERSONAL_RECORD` is generated with `{ "previousMaxKg": 120, "currentMaxKg": 130, "changePercent": 8.33 }`

### Requirement: Repetition personal record
The system SHALL generate `REPS_PERSONAL_RECORD`, severity `SUCCESS`, when, at a weight also used in an earlier comparable execution, the athlete did more reps in one set than ever at exactly that weight. When several weights qualify, the heaviest SHALL be reported. Evidence: `weightKg`, `previousBestReps`, `currentReps`.

#### Scenario: More reps at the same weight
- **WHEN** the best earlier set at 80 kg had 8 reps and the current workout has a set of 10 reps at 80 kg
- **THEN** `REPS_PERSONAL_RECORD` is generated with `{ "weightKg": 80, "previousBestReps": 8, "currentReps": 10 }`

#### Scenario: Weight never used before
- **WHEN** the current workout uses 85 kg and no earlier comparable execution used exactly 85 kg
- **THEN** no `REPS_PERSONAL_RECORD` is generated for 85 kg

### Requirement: Volume change against the previous execution
When the previous comparable execution had the same number of prescribed sets, the system SHALL compare the exercise's volume (sum of weight times reps over sets that have both) and generate `VOLUME_INCREASE` or `VOLUME_DECREASE`, severity `INFO`, when it differs. Evidence: `previousVolumeKg`, `currentVolumeKg`, `changePercent`, plus `baselineVolumeKg` for a decrease when available.

#### Scenario: More volume
- **WHEN** both executions prescribed 3 sets, the previous volume was 2400 kg and the current is 2640 kg
- **THEN** `VOLUME_INCREASE` is generated with `changePercent` 10

#### Scenario: Planned set count change
- **WHEN** the previous execution prescribed 4 sets and the current one prescribes 3
- **THEN** no volume observation is generated

### Requirement: Total reps change against the previous execution
When the previous comparable execution had the same number of prescribed sets and the same number of recorded rep sets, the system SHALL compare the total reps and generate `REPS_INCREASE` or `REPS_DECREASE`, severity `INFO`, when they differ. Evidence: `previousTotalReps`, `currentTotalReps`, `sets`, `changePercent`, plus `baselineTotalReps` for a decrease when available.

#### Scenario: More reps in the same sets
- **WHEN** both executions recorded 3 sets and total reps went from 30 to 33
- **THEN** `REPS_INCREASE` is generated with `{ "previousTotalReps": 30, "currentTotalReps": 33, "sets": 3, "changePercent": 10 }`

#### Scenario: Different number of recorded sets
- **WHEN** the previous execution recorded 3 sets and the current one 2
- **THEN** no reps change observation is generated

### Requirement: Repeated target failure
The system SHALL generate `REPEATED_TARGET_MISS`, severity `WARNING`, when the current execution and the previous comparable executions, `RepeatedMissCount` in total (3 by default), each had at least one matched set below its rep target. Evidence: `consecutiveMisses`, the full length of the streak.

#### Scenario: Third miss in a row
- **WHEN** the two previous comparable executions and the current one each have a set below its rep target
- **THEN** `REPEATED_TARGET_MISS` is generated with `{ "consecutiveMisses": 3 }`

#### Scenario: Streak broken
- **WHEN** the previous comparable execution met every rep target
- **THEN** no `REPEATED_TARGET_MISS` is generated

### Requirement: No weight progression
The system SHALL generate `WEIGHT_PLATEAU`, severity `INFO`, when the current execution and the previous comparable ones, `PlateauCount` in total (4 by default), all topped at or below the highest weight of the execution just before them. Evidence: `baselineMaxKg`, `windowMaxWeightsKg`, `executions`.

#### Scenario: Stuck at 100 kg
- **WHEN** the baseline execution topped at 100 kg and the next four, the current one last, topped at 100, 97.5, 100 and 100 kg
- **THEN** `WEIGHT_PLATEAU` is generated with `{ "baselineMaxKg": 100, "windowMaxWeightsKg": [100, 97.5, 100, 100], "executions": 4 }`

### Requirement: Weight trends
The system SHALL generate `WEIGHT_UPTREND` (severity `SUCCESS`) or `WEIGHT_DOWNTREND` (severity `INFO`) when the highest weight increased, or decreased, in each of the last `TrendCount` consecutive comparable executions (3 by default), the current one last. Evidence: `previousWeightsKg`, `currentWeightKg`, and `consecutiveIncreases` or `consecutiveDecreases`.

#### Scenario: Downtrend
- **WHEN** the three previous comparable executions topped at 110, 105 and 100 kg and the current at 95 kg
- **THEN** `WEIGHT_DOWNTREND` is generated with `{ "previousWeightsKg": [110, 105, 100], "currentWeightKg": 95, "consecutiveDecreases": 3 }`

#### Scenario: Flat step breaks the trend
- **WHEN** the previous comparable executions topped at 100, 105 and 105 kg and the current at 110 kg
- **THEN** no `WEIGHT_UPTREND` is generated

### Requirement: Highlight eligibility
An observation SHALL be eligible for highlighting, with a priority, only when it is `WORKOUT_INCOMPLETE` below `SevereNonCompliancePercent` (CRITICAL); `REPEATED_TARGET_MISS` or `WEIGHT_DOWNTREND` (HIGH); a decrease whose previous execution was also at least `SignificantDeclinePercent` below the same baseline (HIGH); a weight record or an increase above its threshold (MEDIUM); `ALL_TARGETS_MET` ending a repeated miss (LOW).

#### Scenario: Severe non-compliance
- **WHEN** 60% of the prescribed sets are recorded and `SevereNonCompliancePercent` is 70
- **THEN** `WORKOUT_INCOMPLETE` has priority `CRITICAL`

#### Scenario: Mild non-compliance
- **WHEN** 80% of the prescribed sets are recorded
- **THEN** `WORKOUT_INCOMPLETE` has no priority

#### Scenario: Confirmed decline
- **WHEN** the baseline execution topped at 100 kg, the previous one at 84 kg and the current one at 82 kg, with `SignificantDeclinePercent` 15
- **THEN** `WEIGHT_DECREASE` has priority `HIGH`

#### Scenario: Single bad session
- **WHEN** the baseline execution topped at 100 kg, the previous one at 100 kg and the current one at 80 kg
- **THEN** `WEIGHT_DECREASE` has no priority

#### Scenario: Significant record
- **WHEN** a weight record is 8.33% above the earlier record and `SignificantRecordPercent` is 5
- **THEN** `WEIGHT_PERSONAL_RECORD` has priority `MEDIUM`

#### Scenario: Exceptional progression
- **WHEN** volume increased 12% and `ExceptionalProgressionPercent` is 10
- **THEN** `VOLUME_INCREASE` has priority `MEDIUM`

#### Scenario: Perfect compliance after repeated misses
- **WHEN** `ALL_TARGETS_MET` is generated and an exercise of the workout had its rep target missed in the previous `RepeatedMissCount` comparable executions
- **THEN** `ALL_TARGETS_MET` has priority `LOW`

#### Scenario: Routine perfect compliance
- **WHEN** `ALL_TARGETS_MET` is generated and no exercise ends a repeated miss
- **THEN** `ALL_TARGETS_MET` has no priority

### Requirement: Highlight selection
Highlights SHALL be chosen among eligible observations ordered by priority (CRITICAL, HIGH, MEDIUM, LOW), then by magnitude (percent deviation) descending, then by exercise position, workout-level first. At most one highlight SHALL refer to the same exercise. At most 3 SHALL be selected, ranked from 1, and no ineligible observation SHALL ever be highlighted.

#### Scenario: More than three eligible
- **WHEN** one `CRITICAL`, two `HIGH` and two `MEDIUM` observations on different exercises are eligible
- **THEN** the `CRITICAL` is rank 1, the two `HIGH` are ranks 2 and 3, and no `MEDIUM` is highlighted

#### Scenario: Same priority ordered by magnitude
- **WHEN** two `MEDIUM` increases of 12% and 25% are eligible on different exercises
- **THEN** the 25% increase ranks above the 12% one

#### Scenario: One highlight per exercise
- **WHEN** an exercise has an eligible `WEIGHT_PERSONAL_RECORD` of 8% and an eligible `VOLUME_INCREASE` of 15%
- **THEN** only the `VOLUME_INCREASE` of that exercise is highlighted

#### Scenario: Nothing eligible
- **WHEN** no observation is eligible
- **THEN** `highlights` is empty, even though `observations` is not

### Requirement: Thresholds come from configuration
`RepeatedMissCount`, `PlateauCount`, `TrendCount`, `SevereNonCompliancePercent`, `SignificantDeclinePercent`, `SignificantRecordPercent` and `ExceptionalProgressionPercent` SHALL be read from the `WorkoutObservations` configuration section. The API SHALL fail to start when any is missing or out of range.

#### Scenario: Invalid threshold
- **WHEN** `WorkoutObservations:TrendCount` is configured as 0
- **THEN** the API does not start

### Requirement: Callers read the observations of a visible workout
`GET /workouts/{workoutId}/observations` SHALL respond `200 OK` with `observations` and `highlights`. Each observation SHALL have `id`, `code`, `severity`, `exerciseId` (null for the workout), `priority` (null when ineligible) and `data`. Each highlight SHALL have `observationId`, `rank` and `priority`, ordered by rank.

#### Scenario: Completed prescribed workout
- **WHEN** the prescribing trainer reads the observations of a completed workout with one eligible observation
- **THEN** `observations` holds every generated observation, workout-level first, then by exercise position
- **AND** `highlights` holds one entry whose `observationId` is in `observations`, with `rank` 1

#### Scenario: Workout without observations
- **WHEN** a caller reads the observations of a visible workout that is not completed
- **THEN** the response is `200 OK` with empty `observations` and `highlights`

### Requirement: Only visible workouts expose their observations
The route SHALL apply the same visibility as reading a workout: an admin sees every workout, a trainer the ones they own or prescribed, a user the ones they own except prescribed drafts not yet published. A workout the caller may not see SHALL respond `403 Forbidden`, and an unknown or malformed `workoutId` SHALL respond `404 Not Found`.

#### Scenario: Athlete reads their own prescribed workout
- **WHEN** the athlete reads the observations of their completed prescribed workout
- **THEN** the response is `200 OK`

#### Scenario: Unrelated trainer
- **WHEN** a trainer who neither owns nor prescribed the workout reads its observations
- **THEN** the response is `403 Forbidden`

#### Scenario: Unknown workout
- **WHEN** a caller reads the observations of a workout that does not exist
- **THEN** the response is `404 Not Found`

#### Scenario: Malformed id
- **WHEN** a caller reads the observations of `workoutId` `not-a-guid`
- **THEN** the response is `404 Not Found`

### Requirement: The observations route requires authentication
`GET /workouts/{workoutId}/observations` SHALL respond `401 Unauthorized` without a token, with an invalid token, or when the token's user no longer exists.

#### Scenario: Missing token
- **WHEN** the route is called without a token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** the route is called with an invalid token
- **THEN** the response is `401 Unauthorized`
