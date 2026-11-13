# Spec Delta

## MODIFIED Requirements

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
