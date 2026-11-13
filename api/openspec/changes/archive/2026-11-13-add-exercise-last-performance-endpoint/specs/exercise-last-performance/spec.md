# Spec Delta

## Purpose

Lets an authenticated user read the sets they recorded the last time they finished a workout with a given exercise, so the client can show their previous performance while they train.

## ADDED Requirements

### Requirement: Callers read their last performance of an exercise
`GET /users/me/exercises/{exerciseId}/last-performance` SHALL respond `200 OK` with `workoutDate`, the date of the caller's last finished workout that holds set records for the exercise, `effort`, the effort prescribed for the exercise in that workout, and `sets`, every set record of that exercise in that workout ordered by position. Any authenticated role SHALL be allowed.

#### Scenario: Exercise done in a finished workout
- **WHEN** a user whose `COMPLETED` workout dated 2026-11-10 prescribes the exercise at effort `MID` and holds set records of 10, 8 and 6 reps for it requests its last performance
- **THEN** the response is `200 OK` with `workoutDate` `2026-11-10`, `effort` `MID` and `sets` holding those three sets in that order

#### Scenario: Reviewed workouts count
- **WHEN** the caller's last workout holding set records for the exercise is `REVIEWED`
- **THEN** the response is `200 OK` with the date and sets of that workout

#### Scenario: Trainers and admins read their own performance
- **WHEN** a `TRAINER` or an `ADMIN` with a finished workout holding set records for the exercise requests its last performance
- **THEN** the response is `200 OK` with the date and sets of their own workout

### Requirement: The effort is the one prescribed in the reference workout
`effort` SHALL be the effort the reference workout prescribes for the exercise, with the same names as the workout responses (`LOW`, `MIDLOW`, `MID`, `MIDHIGH`, `HIGH`). It SHALL NOT be taken from any other workout.

#### Scenario: Effort differs between sessions
- **WHEN** the caller's finished workout dated 2026-11-03 prescribes the exercise at `LOW` and the one dated 2026-11-10 at `HIGH`, both with set records for it
- **THEN** the response has `effort` `HIGH`

### Requirement: The last finished workout is the reference
The reference workout SHALL be the caller's workout with the latest workout date among those that are `COMPLETED` or `REVIEWED` and hold at least one set record for the exercise. Ties on the date SHALL be broken by the most recently created workout. Sets from other workouts SHALL NOT be included.

#### Scenario: Several finished workouts
- **WHEN** the caller has finished workouts dated 2026-11-03 and 2026-11-10, both with set records for the exercise
- **THEN** the response holds the date and sets of the 2026-11-10 workout only

#### Scenario: Unfinished workout is ignored
- **WHEN** the caller has a `STARTED` workout dated 2026-11-12 with set records for the exercise and a `COMPLETED` one dated 2026-11-10
- **THEN** the response holds the date and sets of the 2026-11-10 workout

#### Scenario: Finished workout without records for the exercise is ignored
- **WHEN** the caller's latest finished workout holds the exercise without any set record, and an earlier finished workout holds set records for it
- **THEN** the response holds the date and sets of the earlier workout

### Requirement: Only the caller's workouts are read
The response SHALL be built only from workouts that belong to the caller, whoever prescribed them. Set records of other users SHALL NOT be returned or affect which workout is the reference.

#### Scenario: Another user did the exercise more recently
- **WHEN** another user has a later finished workout with set records for the exercise
- **THEN** the caller's response holds only the caller's own last workout

#### Scenario: Prescribed workouts belong to the athlete
- **WHEN** a trainer prescribed a finished workout to an athlete and the trainer requests their own last performance of that exercise
- **THEN** the athlete's set records are not returned to the trainer

### Requirement: Each set is returned with its recorded values
Each set SHALL hold `reps`, `durationSeconds` and `weight`, each `null` when it was not recorded. A set without weight SHALL return `weight` `null`, and a weight of `0` SHALL return `0`. A timed set SHALL return `durationSeconds` with `reps` `null`.

#### Scenario: Set done without weight
- **WHEN** the reference workout holds a set of 10 reps recorded without weight
- **THEN** that set is returned with `reps` `10`, `durationSeconds` `null` and `weight` `null`

#### Scenario: Bodyweight set with weight zero
- **WHEN** the reference workout holds a set of 12 reps recorded with weight `0`
- **THEN** that set is returned with `weight` `0`, not `null`

#### Scenario: Timed set
- **WHEN** the reference workout holds a set of 45 seconds recorded with weight `10`
- **THEN** that set is returned with `reps` `null`, `durationSeconds` `45` and `weight` `10`

### Requirement: Never done returns no content
The route SHALL respond `204 No Content` with no body when the exercise exists and is visible to the caller but no finished workout of the caller holds set records for it.

#### Scenario: Exercise never done
- **WHEN** a user requests the last performance of a visible exercise they never recorded
- **THEN** the response is `204 No Content` with no body

#### Scenario: Exercise only done in an unfinished workout
- **WHEN** the caller's only set records for the exercise are in a `STARTED` workout
- **THEN** the response is `204 No Content`

### Requirement: Unknown or hidden exercises are not found
The route SHALL respond `404 Not Found` when no exercise has the given id or the caller may not see it, with the same visibility rule as reading an exercise. A malformed id SHALL NOT match the route and SHALL respond `404 Not Found`.

#### Scenario: Unknown exercise
- **WHEN** the caller requests the last performance of an id that matches no exercise
- **THEN** the response is `404 Not Found`

#### Scenario: Custom exercise of another user
- **WHEN** the caller requests the last performance of a custom exercise they may not see
- **THEN** the response is `404 Not Found`

#### Scenario: Malformed exercise id
- **WHEN** the caller requests the last performance with an `exerciseId` that is not a valid id
- **THEN** the response is `404 Not Found`

### Requirement: The route requires authentication
The route SHALL respond `401 Unauthorized` without a token or with an invalid one, and SHALL only ever read the caller identified by the token.

#### Scenario: Missing token
- **WHEN** the route is called without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** the route is called with an invalid bearer token
- **THEN** the response is `401 Unauthorized`
