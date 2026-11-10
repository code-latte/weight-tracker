# Spec Delta

## ADDED Requirements

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
