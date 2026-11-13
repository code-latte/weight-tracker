# workout-management Specification

## Purpose

Lets both fronts list, read, create and update workouts and manage their exercises through `core`, each operation
returning the API's answer or a `DomainError` as a result, with every role and placement rule left to the API.

## Requirements

### Requirement: Creating a workout sends its data without exercises

Creating a workout SHALL send `POST /workouts` with the given `name`, the optional `workoutDate` and `description`, the optional `userId` and `trainerId` and the optional `mesocycleId` and `mesocycleWeekNumber`, unchanged, and SHALL return a successful result holding the workout the API answers with. It SHALL NOT send any exercises, SHALL NOT fill in a date the caller omitted and SHALL NOT check the caller's role.

#### Scenario: Workout created with every field

- **WHEN** a workout is created with name "Pierna", a date, description "Día 1", a user id and a trainer id and the API accepts it
- **THEN** exactly one `POST /workouts` request is sent with that body, unchanged
- **AND** the result is successful and its value is the workout the API answered with

#### Scenario: Optional fields omitted

- **WHEN** a workout is created with only a name and a date
- **THEN** the request body holds only those two fields

#### Scenario: Workout created in a mesocycle week

- **WHEN** a workout is created with a `mesocycleId` and a `mesocycleWeekNumber` and the API accepts it
- **THEN** the request body holds both values unchanged
- **AND** the result's workout holds the `mesocycleId` and `mesocycleWeekNumber` the API answered with

#### Scenario: Placed workout created without a date

- **WHEN** a workout is created with a name, a user id, a `mesocycleId` and a `mesocycleWeekNumber` and no date, and the API accepts it
- **THEN** the request body holds no `workoutDate`
- **AND** the result's workout holds the date the API answered with

#### Scenario: Rejected by the API

- **WHEN** the API refuses the creation (missing name, missing date without a placement, missing user or trainer for the caller's role, unknown user or trainer)
- **THEN** the result is a failure holding the matching `DomainError` code (`VALIDATION`, `FORBIDDEN` or `NOT_FOUND`) with the API's detail kept in the message

#### Scenario: Placement rejected by the API

- **WHEN** the API refuses the placement with 400 (half-set pair), 403 (caller may not place it), 404 (unknown mesocycle) or 409 (athlete or trainer mismatch, week beyond the duration)
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively

### Requirement: Updating a workout replaces its editable data

Updating a workout SHALL send `PUT /workouts/{id}` with the given workout id in the path and the given body unchanged, including the optional `mesocycleId` and `mesocycleWeekNumber`, and SHALL return a successful result holding the updated workout. It SHALL NOT decide which fields a role may change and SHALL NOT fill in a placement the caller omitted.

#### Scenario: Workout updated

- **WHEN** a workout is updated with a new name, description and date and the API accepts it
- **THEN** exactly one `PUT /workouts/{id}` request is sent with that id in the path and the body unchanged
- **AND** the result is successful and its value is the workout the API answered with

#### Scenario: Workout moved to a mesocycle week

- **WHEN** a workout is updated with a `mesocycleId` and a `mesocycleWeekNumber` and the API accepts it
- **THEN** the request body holds both values unchanged

#### Scenario: Placement omitted

- **WHEN** a workout is updated without `mesocycleId` and `mesocycleWeekNumber`
- **THEN** the request body holds neither field
- **AND** the result's workout holds the placement the API answered with

#### Scenario: Placement locked

- **WHEN** the placement of a started or completed workout is changed and the API answers 409
- **THEN** the result is a failure with code `CONFLICT`

#### Scenario: Reassignment by a non-admin

- **WHEN** a non-admin sends a different `userId` or `trainerId` and the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

#### Scenario: Unknown workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

### Requirement: Adding an exercise to a workout sends its prescription

Adding an exercise SHALL send `POST /workouts/{workoutId}/exercises`, with the workout id in the path. The body SHALL
hold, unchanged:

- `exerciseId`, `effort` and the list of set prescriptions;
- `perSide`, and `restMinutes` when given.

A set prescription of type `Minutes` SHALL carry `targetMinutes`. The result SHALL be successful and hold what the API
answers with, including `perSide`, `restMinutes` and each minutes target.

#### Scenario: Exercise added

- **WHEN** an exercise is added with an exercise id, an effort and two set prescriptions and the API accepts it
- **THEN** exactly one `POST /workouts/{workoutId}/exercises` request is sent with that body, unchanged
- **AND** the result is successful and its value is the API's answer

#### Scenario: Exercise added with minutes, rest and per side

- **WHEN** an exercise is added with `perSide` true, `restMinutes` 1.5 and a set prescription of type `Minutes` with
  `targetMinutes` 2, and the API accepts it
- **THEN** exactly one `POST /workouts/{workoutId}/exercises` request is sent with that body, unchanged
- **AND** the result's value holds `perSide`, `restMinutes` and the minutes target as the API answered them

#### Scenario: Exercise already in the workout

- **WHEN** the API answers 409
- **THEN** the result is a failure with code `CONFLICT`

#### Scenario: Invalid prescription

- **WHEN** the API answers 400
- **THEN** the result is a failure with code `VALIDATION` and the API's detail kept

### Requirement: Workout operations return failures as results

Every workout operation SHALL return a `DomainError` result instead of throwing: 401 as `UNAUTHORIZED`, 500 and other unlisted statuses as `UNEXPECTED`, and a request that gets no response as `NETWORK`.

#### Scenario: Missing token

- **WHEN** the API answers 401
- **THEN** the result is a failure with code `UNAUTHORIZED`

#### Scenario: No response

- **WHEN** the request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`

### Requirement: Listing workouts returns what the API answers

Listing workouts SHALL send `GET /workouts` with no body and SHALL return a successful result holding the list the API
answers with, unchanged. It SHALL NOT filter, sort or check the caller's role.

#### Scenario: Workouts listed

- **WHEN** workouts are listed and the API answers with two workouts
- **THEN** exactly one `GET /workouts` request is sent without a body
- **AND** the result is successful and its value is that list, in the same order

#### Scenario: No workouts

- **WHEN** the API answers with an empty list
- **THEN** the result is successful and its value is an empty list

#### Scenario: Role not allowed

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

### Requirement: Reading a workout by id returns it with its exercises

Reading a workout SHALL send `GET /workouts/{id}` with the given workout id in the path and no body, and SHALL return a
successful result holding the workout, its exercises with their set records, its placement, its session dates
(`startedAt`, `completedAt`), its review fields (`reviewedAt`, `reviewedBy`, `resultsUpdatedAt`) and its athlete and
trainer profiles as the API answers them.

#### Scenario: Workout read

- **WHEN** a workout is read by id and the API answers with it
- **THEN** exactly one `GET /workouts/{id}` request is sent with that id in the path
- **AND** the result is successful and its value is the API's answer

#### Scenario: Workout read with its people

- **WHEN** the API answers with a workout whose athlete is "Ana García" and whose trainer is "Luis Pérez"
- **THEN** the result's workout holds both profiles unchanged

#### Scenario: Workout read with its results

- **WHEN** the API answers with a reviewed workout whose exercise holds two set records
- **THEN** the result's workout holds both set records in the order the API answered, and its `reviewedAt`, `reviewedBy` and `resultsUpdatedAt` unchanged

#### Scenario: Workout read with its session dates

- **WHEN** the API answers with a completed workout whose `startedAt` and `completedAt` are set
- **THEN** the result's workout holds both dates unchanged

#### Scenario: Unknown workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Workout of someone else

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

### Requirement: Updating an exercise of a workout replaces its prescription

Updating an exercise of a workout SHALL send `PUT /workouts/{workoutId}/exercises/{exerciseId}`, with the workout id
and the catalog exercise id in the path. The body SHALL hold, unchanged:

- that same `exerciseId`, the `effort` and the list of set prescriptions;
- `perSide`, and `restMinutes` when given.

The result SHALL be successful and hold what the API answers with.

#### Scenario: Exercise updated

- **WHEN** the exercise of a workout is updated with an effort and two set prescriptions and the API accepts it
- **THEN** exactly one `PUT /workouts/{workoutId}/exercises/{exerciseId}` request is sent with the catalog exercise id in
  the path and the body unchanged
- **AND** the result is successful and its value is the API's answer

#### Scenario: Exercise updated with rest and per side

- **WHEN** the exercise of a workout is updated with `perSide` true and `restMinutes` 0.75 and the API accepts it
- **THEN** exactly one `PUT /workouts/{workoutId}/exercises/{exerciseId}` request is sent with that body, unchanged

#### Scenario: Exercise not in the workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Invalid prescription

- **WHEN** the API answers 400
- **THEN** the result is a failure with code `VALIDATION` and the API's detail kept

### Requirement: Removing an exercise from a workout sends its catalog id

Removing an exercise SHALL send `DELETE /workouts/{workoutId}/exercises/{exerciseId}`, with the workout id and the
catalog exercise id in the path and no body, and SHALL return a successful empty result when the API answers 204.

#### Scenario: Exercise removed

- **WHEN** an exercise is removed from a workout and the API answers 204
- **THEN** exactly one `DELETE /workouts/{workoutId}/exercises/{exerciseId}` request is sent without a body
- **AND** the result is successful

#### Scenario: Removal rejected

- **WHEN** the API answers 404 or 409
- **THEN** the result is a failure with code `NOT_FOUND` or `CONFLICT`

### Requirement: Reordering a workout's exercises sends the full order

Reordering SHALL send `PUT /workouts/{workoutId}/exercises/order` with the workout id in the path and a body
`{ exerciseIds }` holding the catalog exercise ids in the given order, unchanged, and SHALL return the workout detail
the API answers with 200.

#### Scenario: Exercises reordered

- **WHEN** a workout's exercises are reordered to `[B, A]` and the API answers 200 with the workout detail
- **THEN** exactly one `PUT /workouts/{workoutId}/exercises/order` request is sent with body `{ "exerciseIds": ["B", "A"] }`
- **AND** the result is successful and holds the returned workout detail

#### Scenario: Reorder rejected

- **WHEN** the API answers 400, 403, 404 or 409
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT`

#### Scenario: Reorder fails without a response

- **WHEN** the reorder request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`

### Requirement: Creating a superseries sends the workout entry ids

Creating a superseries SHALL send `POST /workouts/{workoutId}/superseries` with the workout id in the path and a body
`{ exerciseIds }` holding the given workout entry ids (not catalog ids), in the given order, unchanged. It SHALL return
the superseries id the API answers with 201.

#### Scenario: Superseries created

- **WHEN** a superseries is created in workout `w-1` with entries `[e-1, e-2]` and the API answers 201 with `{ "id": 3 }`
- **THEN** exactly one `POST /workouts/w-1/superseries` request is sent with body `{ "exerciseIds": ["e-1", "e-2"] }`
- **AND** the result is successful and holds the superseries id `3`

#### Scenario: Creation rejected

- **WHEN** the API answers 400, 403, 404 or 409
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT`

#### Scenario: Creation fails without a response

- **WHEN** the create request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`

### Requirement: Breaking a superseries sends its id

Breaking a superseries SHALL send `DELETE /workouts/{workoutId}/superseries/{superSeriesId}` with the workout id and
the superseries id in the path and no body, and SHALL return a successful empty result when the API answers 204.

#### Scenario: Superseries broken

- **WHEN** superseries `3` of workout `w-1` is broken and the API answers 204
- **THEN** exactly one `DELETE /workouts/w-1/superseries/3` request is sent without a body
- **AND** the result is successful

#### Scenario: Break rejected

- **WHEN** the API answers 400, 403, 404 or 409
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT`

#### Scenario: Break fails without a response

- **WHEN** the break request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`

### Requirement: Recording a set sends its reps and weight

Recording a set SHALL send `POST /workouts/{workoutId}/exercises/{exerciseId}/set-records` (the workout id and the catalog exercise id in the path) with the given body unchanged: `reps` or `durationSeconds`, never both, and an optional `weight`. It SHALL return the workout detail the API answers with 200, its set records as answered, nulls included. It SHALL NOT check the caller's role, the workout's status or the exercise's kind.

#### Scenario: Set recorded

- **WHEN** a set with reps 8 and weight 60.5 is recorded for exercise `ex-1` of workout `w-1` and the API answers 200 with the workout detail
- **THEN** exactly one `POST /workouts/w-1/exercises/ex-1/set-records` request is sent with body `{ "reps": 8, "weight": 60.5 }`
- **AND** the result is successful and holds the returned workout detail, including the exercise's set records as the API answered them

#### Scenario: Timed set recorded without a weight

- **WHEN** a set with 45 seconds and no weight is recorded and the API answers 200 with the workout detail
- **THEN** exactly one request is sent with body `{ "durationSeconds": 45 }`
- **AND** the result holds the returned workout detail, whose new record has `type` "Duration", `durationSeconds` 45, `reps` null and `weight` null, unchanged

#### Scenario: Recording rejected

- **WHEN** the API answers 400, 403, 404 or 409 (invalid values or both `reps` and `durationSeconds` sent, caller not the athlete or an admin, unknown workout or exercise, workout not started or set not of the exercise's kind)
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively, with the API's detail kept in the message

### Requirement: Correcting a set record replaces its reps and weight

Correcting a set record SHALL send `PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}` with the workout id, the catalog exercise id and the set record id in the path, and the given body unchanged: `reps` or `durationSeconds` and an optional `weight`. It SHALL return a successful result holding the workout detail the API answers with 200.

#### Scenario: Set record corrected

- **WHEN** set record `sr-1` of exercise `ex-1` in workout `w-1` is corrected to reps 10 and weight 55 and the API answers 200 with the workout detail
- **THEN** exactly one `PUT /workouts/w-1/exercises/ex-1/set-records/sr-1` request is sent with body `{ "reps": 10, "weight": 55 }`
- **AND** the result is successful and holds the returned workout detail

#### Scenario: Timed set record corrected

- **WHEN** set record `sr-1` is corrected to 60 seconds with no weight and the API answers 200
- **THEN** exactly one `PUT` request is sent with body `{ "durationSeconds": 60 }`
- **AND** the result holds the returned workout detail

#### Scenario: Correction rejected

- **WHEN** the API answers 400, 403, 404 or 409
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively

### Requirement: Removing a set record sends its id

Removing a set record SHALL send `DELETE /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}` with the workout id, the catalog exercise id and the set record id in the path and no body. It SHALL return a successful result holding the workout detail the API answers with 200.

#### Scenario: Set record removed

- **WHEN** set record `sr-1` of exercise `ex-1` in workout `w-1` is removed and the API answers 200 with the workout detail
- **THEN** exactly one `DELETE /workouts/w-1/exercises/ex-1/set-records/sr-1` request is sent without a body
- **AND** the result is successful and holds the returned workout detail

#### Scenario: Removal rejected

- **WHEN** the API answers 403, 404 or 409
- **THEN** the result is a failure with code `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively

### Requirement: Reordering set records sends the full order

Reordering set records SHALL send `PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/order` with the workout id and the catalog exercise id in the path and a body `{ setRecordIds }` holding the set record ids in the given order, unchanged. It SHALL return a successful result holding the workout detail the API answers with 200. Whether the list matches the exercise's set records is decided by the API.

#### Scenario: Set records reordered

- **WHEN** the set records of exercise `ex-1` in workout `w-1` are reordered to `[sr-2, sr-1]` and the API answers 200 with the workout detail
- **THEN** exactly one `PUT /workouts/w-1/exercises/ex-1/set-records/order` request is sent with body `{ "setRecordIds": ["sr-2", "sr-1"] }`
- **AND** the result is successful and holds the returned workout detail

#### Scenario: Order mismatch

- **WHEN** the API answers 409 because the list does not hold every set record of the exercise exactly once
- **THEN** the result is a failure with code `CONFLICT`

#### Scenario: Reorder fails without a response

- **WHEN** the reorder request is rejected without a response
- **THEN** the result is a failure with code `NETWORK`

### Requirement: Reviewing a workout sends the results timestamp last read

Reviewing a workout SHALL send `PUT /workouts/{workoutId}/review` with the workout id in the path and a body `{ resultsUpdatedAt }` holding the given value unchanged, including `null`. It SHALL return a successful result holding the workout detail the API answers with 200. It SHALL NOT compare timestamps or check the caller's role.

#### Scenario: Workout reviewed

- **WHEN** workout `w-1` is reviewed with the `resultsUpdatedAt` last read and the API answers 200 with the workout detail
- **THEN** exactly one `PUT /workouts/w-1/review` request is sent with that `resultsUpdatedAt` in the body, unchanged
- **AND** the result is successful and holds the returned workout detail, including its `reviewedAt` and `reviewedBy`

#### Scenario: Workout reviewed with no results timestamp

- **WHEN** workout `w-1` is reviewed with `resultsUpdatedAt` `null`
- **THEN** the request body is `{ "resultsUpdatedAt": null }`

#### Scenario: Results changed or review not allowed

- **WHEN** the API answers 409 (results changed after the timestamp sent, or the workout is not completed)
- **THEN** the result is a failure with code `CONFLICT` and the API's detail kept in the message

#### Scenario: Not the prescribing trainer

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

### Requirement: Changing a workout's status sends the target status

Changing a workout's status SHALL send `PUT /workouts/{workoutId}/status` with the workout id in the path and a body `{ status }` holding the given status unchanged. It SHALL return a successful result holding the workout detail the API answers with 200. It SHALL NOT decide which transitions a role may make.

#### Scenario: Workout started

- **WHEN** workout `w-1` is moved to `STARTED` and the API answers 200 with the workout detail
- **THEN** exactly one `PUT /workouts/w-1/status` request is sent with body `{ "status": "STARTED" }`
- **AND** the result is successful and holds the returned workout detail

#### Scenario: Transition rejected

- **WHEN** the API answers 400, 403, 404 or 409 (invalid status, role not allowed, unknown workout, transition not allowed)
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively

### Requirement: Deleting a workout sends its id

Deleting a workout SHALL send `DELETE /workouts/{workoutId}` with the workout id in the path and no body, and SHALL
return a successful empty result when the API answers 204. It SHALL NOT check the workout's status or the caller's
role: which workouts may be deleted is the API's answer.

#### Scenario: Workout deleted

- **WHEN** workout `w-1` is deleted and the API answers 204
- **THEN** exactly one `DELETE /workouts/w-1` request is sent without a body
- **AND** the result is successful

#### Scenario: Deletion rejected

- **WHEN** the API answers 403, 404 or 409 (caller may not change the workout, unknown workout, workout no longer a
  draft or changed since it was read)
- **THEN** the result is a failure with code `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively
