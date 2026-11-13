# workout-summary Specification

## Purpose

Lets a caller read the stats of one workout they may see: how many sets met their target, the volume lifted, and each exercise's top set compared with its previous execution.

## Requirements

### Requirement: Callers read the summary of a visible workout
`GET /workouts/{workoutId}/summary` SHALL respond `200 OK` with `workoutId`, the workout `setsInTarget` and `volume`, and `exercises`, one entry per exercise of the workout in workout order, each with `workoutExerciseId`, `exerciseId`, `setsInTarget`, `volume`, `topSet` and `previous`. Any workout status SHALL be accepted.

#### Scenario: Completed workout
- **WHEN** the owner requests the summary of their `COMPLETED` workout with two exercises holding set records
- **THEN** the response is `200 OK` with both exercises in workout order, each with its computed stats

#### Scenario: Workout in progress
- **WHEN** the owner requests the summary of their `STARTED` workout where only the first exercise has set records
- **THEN** the response is `200 OK`, the first exercise carries its stats and the second has `setsInTarget` `0/0`, `volume` `null` and `topSet` `null`

### Requirement: A set is in target when its result meets the matched prescribed set
Prescriptions SHALL be expanded by their count into an ordered list of prescribed sets, and the set record at each position SHALL be matched to the prescribed set at the same position. `evaluated` SHALL count the set records that have a matched prescribed set; `inTarget` SHALL count those whose result meets the target. Set records beyond the prescribed sets and prescribed sets without a record SHALL NOT be evaluated.

#### Scenario: Extra set records are not evaluated
- **WHEN** an exercise prescribes 3 sets and holds 4 set records
- **THEN** its `setsInTarget` has `evaluated` `3`

#### Scenario: Missing set records are not evaluated
- **WHEN** an exercise prescribes 4 sets and holds 2 set records
- **THEN** its `setsInTarget` has `evaluated` `2`

#### Scenario: Workout totals
- **WHEN** one exercise has `inTarget` `2` of `3` and another `3` of `3`
- **THEN** the workout `setsInTarget` is `inTarget` `5` of `evaluated` `6`

### Requirement: Each target kind defines when it is met
A `Reps` target of n SHALL be met by n or more reps. A `RepsRange` target SHALL be met by reps equal to or greater than its minimum, also above its maximum. A `MaxReps` target SHALL always be met. A `Duration` target of s seconds SHALL be met by s or more seconds, and a `Minutes` target of m SHALL be met by m × 60 or more seconds. A result of a different kind than its target SHALL NOT be met.

#### Scenario: Reps target
- **WHEN** a set prescribed at 10 reps records 9, 10 and 11 reps in three evaluations
- **THEN** 9 is not in target and 10 and 11 are

#### Scenario: Reps range target
- **WHEN** a set prescribed at 8-12 reps records 7, 8 and 14 reps in three evaluations
- **THEN** 7 is not in target and 8 and 14 are

#### Scenario: Max reps target
- **WHEN** a set prescribed as max reps records 3 reps
- **THEN** the set is in target

#### Scenario: Duration target
- **WHEN** a set prescribed at 40 seconds records 39 and 40 seconds in two evaluations
- **THEN** 39 is not in target and 40 is

#### Scenario: Minutes target
- **WHEN** a set prescribed at 1.5 minutes records 90 seconds
- **THEN** the set is in target

### Requirement: Volume is the sum of reps times kilograms
The volume of a set SHALL be its reps multiplied by its weight in kilograms. Only sets with reps and a non-null weight SHALL contribute; a weight of `0` SHALL contribute `0`. An exercise volume SHALL be the sum of its contributing sets, or `null` when none contributes. The workout volume SHALL be the sum of the non-null exercise volumes, or `null` when all are `null`.

#### Scenario: Weighted sets
- **WHEN** an exercise holds sets of 10 reps at 40 kg and 8 reps at 50 kg
- **THEN** its `volume` is `800`

#### Scenario: Sets without weight or timed
- **WHEN** an exercise holds only a set of 10 reps without weight and a timed set of 45 seconds at 10 kg
- **THEN** its `volume` is `null`

#### Scenario: Bodyweight set
- **WHEN** an exercise holds only a set of 12 reps at `0` kg
- **THEN** its `volume` is `0`

#### Scenario: Workout volume skips non-applicable exercises
- **WHEN** a workout has one exercise with volume `800` and one with volume `null`
- **THEN** the workout `volume` is `800`

### Requirement: The top set is the heaviest set
`topSet` SHALL be the exercise's set with the highest weight, ties broken by most reps, then longest duration, then lowest position. A `null` weight SHALL rank below `0`. It SHALL hold `reps`, `durationSeconds` and `weight`, each `null` when not recorded, and SHALL be `null` when the exercise has no set records.

#### Scenario: Heaviest wins
- **WHEN** an exercise holds sets of 10 reps at 40 kg and 6 reps at 50 kg
- **THEN** `topSet` is 6 reps at 50 kg

#### Scenario: Tie on weight
- **WHEN** an exercise holds sets of 8 reps at 50 kg and 10 reps at 50 kg
- **THEN** `topSet` is 10 reps at 50 kg

#### Scenario: Null weight ranks below zero
- **WHEN** an exercise holds a set of 15 reps without weight and a set of 5 reps at `0` kg
- **THEN** `topSet` is 5 reps at `0` kg

### Requirement: Previous execution is the owner's last earlier finished session
`previous` SHALL hold `workoutId`, `workoutDate` and `topSet` of the workout owner's latest workout that is `COMPLETED` or `REVIEWED`, holds at least one set record for the exercise, the caller may see, and comes strictly before the summarised workout by workout date, ties broken by the earlier created workout. It SHALL be `null` when there is none.

#### Scenario: Earlier finished session
- **WHEN** the owner has finished workouts with the exercise dated 2026-11-03 and 2026-11-06 and the summarised workout is dated 2026-11-10
- **THEN** `previous` holds the 2026-11-06 workout and its top set

#### Scenario: Later and unfinished sessions are ignored
- **WHEN** the owner has a finished workout with the exercise dated 2026-11-12 and a `STARTED` one dated 2026-11-08, and the summarised workout is dated 2026-11-10
- **THEN** `previous` is `null`

#### Scenario: Another user's sessions are ignored
- **WHEN** only another user has an earlier finished workout with the exercise
- **THEN** `previous` is `null`

#### Scenario: Trainer does not see the athlete's own sessions
- **WHEN** a trainer summarises a workout they prescribed, and the athlete's only earlier finished session of the exercise is a workout the athlete created
- **THEN** `previous` is `null`

### Requirement: Only visible workouts can be summarised
The route SHALL apply the same visibility as reading a workout: an admin sees every workout, a trainer the ones they own or prescribed, a user the ones they own except prescribed drafts not yet published. A workout the caller may not see SHALL respond `403 Forbidden`, and an unknown or malformed `workoutId` SHALL respond `404 Not Found`.

#### Scenario: Another user's workout
- **WHEN** a user requests the summary of a workout of another user
- **THEN** the response is `403 Forbidden`

#### Scenario: Unpublished prescribed draft
- **WHEN** an athlete requests the summary of a draft their trainer prescribed and has not published
- **THEN** the response is `403 Forbidden`

#### Scenario: Trainer reads a prescribed workout
- **WHEN** a trainer requests the summary of a workout they prescribed
- **THEN** the response is `200 OK`

#### Scenario: Unknown workout
- **WHEN** the caller requests the summary of an id that matches no workout
- **THEN** the response is `404 Not Found`

#### Scenario: Malformed workout id
- **WHEN** the caller requests the summary with a `workoutId` that is not a valid id
- **THEN** the response is `404 Not Found`

### Requirement: The summary route requires authentication
The route SHALL respond `401 Unauthorized` without a token or with an invalid one.

#### Scenario: Missing token
- **WHEN** the route is called without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** the route is called with an invalid bearer token
- **THEN** the response is `401 Unauthorized`
