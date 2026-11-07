# Spec Delta

## ADDED Requirements

### Requirement: Recording a set sends its reps and weight

Recording a set SHALL send `POST /workouts/{workoutId}/exercises/{exerciseId}/set-records` with the workout id and the catalog exercise id (not the workout entry id) in the path, and a body `{ reps, weight }` unchanged. It SHALL return a successful result holding the workout detail the API answers with 200. It SHALL NOT check the caller's role or the workout's status.

#### Scenario: Set recorded

- **WHEN** a set with reps 8 and weight 60.5 is recorded for exercise `ex-1` of workout `w-1` and the API answers 200 with the workout detail
- **THEN** exactly one `POST /workouts/w-1/exercises/ex-1/set-records` request is sent with body `{ "reps": 8, "weight": 60.5 }`
- **AND** the result is successful and holds the returned workout detail, including the exercise's set records as the API answered them

#### Scenario: Recording rejected

- **WHEN** the API answers 400, 403, 404 or 409 (invalid values, caller not the athlete or an admin, unknown workout or exercise, workout not started or set not allowed for the exercise's target)
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively, with the API's detail kept in the message

### Requirement: Correcting a set record replaces its reps and weight

Correcting a set record SHALL send `PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}` with the workout id, the catalog exercise id and the set record id in the path, and a body `{ reps, weight }` unchanged. It SHALL return a successful result holding the workout detail the API answers with 200.

#### Scenario: Set record corrected

- **WHEN** set record `sr-1` of exercise `ex-1` in workout `w-1` is corrected to reps 10 and weight 55 and the API answers 200 with the workout detail
- **THEN** exactly one `PUT /workouts/w-1/exercises/ex-1/set-records/sr-1` request is sent with body `{ "reps": 10, "weight": 55 }`
- **AND** the result is successful and holds the returned workout detail

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

## MODIFIED Requirements

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

### Requirement: Reading a workout by id returns it with its exercises

Reading a workout SHALL send `GET /workouts/{id}` with the given workout id in the path and no body, and SHALL return a
successful result holding the workout, its exercises with their set records, its placement, its review fields
(`reviewedAt`, `reviewedBy`, `resultsUpdatedAt`) and its athlete and trainer profiles as the API answers them.

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

#### Scenario: Unknown workout

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Workout of someone else

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`
