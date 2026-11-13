# exercise-history Specification

## Purpose

Lets a caller read how a user's performance of one exercise evolves session by session: max weight, volume and estimated one-rep max of their latest finished sessions.

## Requirements

### Requirement: Callers read a user's exercise history
`GET /users/{userId}/exercises/{exerciseId}/history` SHALL respond `200 OK` with a list of sessions, each with `workoutId`, `workoutDate`, `maxWeight`, `volume` and `estimatedOneRepMax`. It SHALL respond with an empty list when no session counts.

#### Scenario: Sessions exist
- **WHEN** a user with two finished workouts holding set records for the exercise requests their history
- **THEN** the response is `200 OK` with two sessions

#### Scenario: Never done
- **WHEN** a user requests the history of a visible exercise they never recorded
- **THEN** the response is `200 OK` with an empty list

### Requirement: Only finished sessions with records count
A session SHALL be a workout of `{userId}` that is `COMPLETED` or `REVIEWED`, holds at least one set record for the exercise and the caller may see. Workouts in `DRAFT`, `PUBLISHED` or `STARTED` SHALL NOT count.

#### Scenario: Unfinished workout is ignored
- **WHEN** the user has a `STARTED` workout and a `COMPLETED` workout, both with set records for the exercise
- **THEN** only the `COMPLETED` workout is returned

#### Scenario: Finished workout without records is ignored
- **WHEN** the user has a finished workout holding the exercise without set records
- **THEN** that workout is not returned

#### Scenario: Another user's sessions are excluded
- **WHEN** another user has finished workouts with set records for the exercise
- **THEN** none of them is returned

### Requirement: Sessions are ordered newest first and limited
Sessions SHALL be ordered by workout date descending, ties broken by the most recently created workout, and only the first `limit` SHALL be returned. `limit` SHALL default to `10` and SHALL accept integers from `1` to `50`; any other value SHALL respond `400 Bad Request`.

#### Scenario: Limit truncates
- **WHEN** the user has five finished sessions and requests `limit=3`
- **THEN** the three latest sessions are returned, newest first

#### Scenario: Default limit
- **WHEN** the user has twelve finished sessions and requests no `limit`
- **THEN** the ten latest sessions are returned

#### Scenario: Limit out of range
- **WHEN** the caller requests `limit=0` or `limit=51`
- **THEN** the response is `400 Bad Request`

#### Scenario: Limit not a number
- **WHEN** the caller requests `limit=abc`
- **THEN** the response is `400 Bad Request`

### Requirement: Max weight is the heaviest recorded weight
`maxWeight` SHALL be the highest non-null weight among the session's sets of the exercise, whatever their reps or duration, and `null` when none has a weight.

#### Scenario: Heaviest set
- **WHEN** a session holds sets at 40 kg, 50 kg and without weight
- **THEN** `maxWeight` is `50`

#### Scenario: No weight recorded
- **WHEN** a session holds only sets without weight
- **THEN** `maxWeight` is `null`

### Requirement: Session volume is the sum of reps times kilograms
`volume` SHALL be the sum of reps multiplied by weight in kilograms over the session's sets of the exercise that have reps and a non-null weight, and `null` when none does. A weight of `0` SHALL contribute `0`.

#### Scenario: Weighted sets
- **WHEN** a session holds sets of 10 reps at 40 kg and 8 reps at 50 kg
- **THEN** `volume` is `800`

#### Scenario: Timed exercise
- **WHEN** a session holds only timed sets
- **THEN** `volume` is `null`

### Requirement: Estimated one-rep max uses the Epley formula
`estimatedOneRepMax` SHALL be the highest value of `weight × (1 + reps / 30)` among the session's sets of the exercise that have reps and a non-null weight, applied also to a single rep and rounded to two decimals, and `null` when no set qualifies.

#### Scenario: Best set wins
- **WHEN** a session holds sets of 10 reps at 60 kg and 3 reps at 70 kg
- **THEN** `estimatedOneRepMax` is `80`

#### Scenario: Single rep
- **WHEN** a session holds only a set of 1 rep at 60 kg
- **THEN** `estimatedOneRepMax` is `62`

#### Scenario: No qualifying set
- **WHEN** a session holds only sets without weight or timed sets
- **THEN** `estimatedOneRepMax` is `null`

### Requirement: Who may read a user's history
A `USER` SHALL read only their own history; asking for another user SHALL respond `403 Forbidden`. A `TRAINER` SHALL read their own history and, for another user, only the sessions they prescribed. An `ADMIN` SHALL read every session of any user. An unknown `userId` SHALL respond `404 Not Found` to a trainer or an admin.

#### Scenario: User asks for another user
- **WHEN** a user requests the history of another user
- **THEN** the response is `403 Forbidden`

#### Scenario: Trainer reads an athlete
- **WHEN** a trainer requests the history of an athlete who has one finished session the trainer prescribed and one the athlete created
- **THEN** only the prescribed session is returned

#### Scenario: Admin reads any user
- **WHEN** an admin requests the history of that same athlete
- **THEN** both sessions are returned

#### Scenario: Unknown user
- **WHEN** an admin requests the history of an id that matches no user
- **THEN** the response is `404 Not Found`

### Requirement: Unknown or hidden exercises are not found
The route SHALL respond `404 Not Found` when no exercise has the given id or the caller may not see it, with the same visibility rule as reading an exercise. A malformed `userId` or `exerciseId` SHALL respond `404 Not Found`.

#### Scenario: Unknown exercise
- **WHEN** the caller requests the history of an id that matches no exercise
- **THEN** the response is `404 Not Found`

#### Scenario: Custom exercise of another user
- **WHEN** the caller requests the history of a custom exercise they may not see
- **THEN** the response is `404 Not Found`

#### Scenario: Malformed id
- **WHEN** the caller requests the history with an `exerciseId` that is not a valid id
- **THEN** the response is `404 Not Found`

### Requirement: The history route requires authentication
The route SHALL respond `401 Unauthorized` without a token or with an invalid one.

#### Scenario: Missing token
- **WHEN** the route is called without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** the route is called with an invalid bearer token
- **THEN** the response is `401 Unauthorized`
