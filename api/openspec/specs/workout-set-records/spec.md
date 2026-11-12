# workout-set-records Specification

## Purpose

Lets a workout keep the reps and weight actually performed in each set of its exercises, so athletes can log what they did against what was prescribed.

## Requirements

### Requirement: Set records only on started or completed workouts
Set records SHALL only be added, edited, removed or reordered while the workout is `STARTED` or `COMPLETED`, for every caller including an admin. `COMPLETED` lets the athlete correct results after completing the workout. In any other status the operation fails with a domain error that maps to 409 and nothing changes.

#### Scenario: Started workout
- **WHEN** a set record is added to an exercise of a `STARTED` workout
- **THEN** the set record is added

#### Scenario: Workout in another status
- **WHEN** a set record is added to an exercise of a `DRAFT`, `PUBLISHED` or `REVIEWED` workout
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Correct a set record of a completed workout
- **WHEN** the athlete edits a set record of a `COMPLETED` workout from 8 reps to 10 reps
- **THEN** the set record has 10 reps

#### Scenario: Add, edit, remove or reorder on a completed workout
- **WHEN** a set record of a `COMPLETED` workout is added, edited, removed or reordered
- **THEN** the change is applied

#### Scenario: Admin on a completed workout
- **WHEN** an admin edits a set record of a `COMPLETED` workout
- **THEN** the change is applied

#### Scenario: Edit, remove or reorder on a reviewed workout
- **WHEN** a set record of a `REVIEWED` workout is edited, removed or reordered
- **THEN** the operation fails with a domain error and the set records are unchanged

#### Scenario: Admin on a reviewed workout
- **WHEN** an admin edits a set record of a `REVIEWED` workout
- **THEN** the operation fails with a domain error and the set record is unchanged

### Requirement: Set records only for single-kind exercises
A set record SHALL only be added to an exercise whose prescriptions are all rep-based (fixed reps, reps range or max reps, in any mix) or all time-based (duration or minutes, in any mix). A set record of a rep-based exercise holds reps, and one of a time-based exercise holds a duration. An exercise that mixes both kinds, or a result of the other kind, fails with a domain error that maps to 409 and nothing changes.

#### Scenario: Mixed rep-based prescriptions
- **WHEN** a set record of 10 reps is added to an exercise prescribed as 1 set of 10 reps, 2 sets of 8-12 reps and 1 set of max reps
- **THEN** the set record is added

#### Scenario: Exercise with a duration prescription
- **WHEN** a set record of 40 seconds is added to an exercise prescribed as 3 sets of 40 seconds
- **THEN** the set record is added with 40 seconds

#### Scenario: Mixed time-based prescriptions
- **WHEN** a set record of 60 seconds is added to an exercise prescribed as 2 sets of 40 seconds and 1 set of 1.5 minutes
- **THEN** the set record is added

#### Scenario: Exercise mixing reps and minutes
- **WHEN** a set record is added to an exercise prescribed as 2 sets of 10 reps and 1 set of 1.5 minutes
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Reps on a time-based exercise
- **WHEN** a set record of 10 reps is added to an exercise prescribed as 3 sets of 40 seconds
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Duration on a rep-based exercise
- **WHEN** a set record of 40 seconds is added to an exercise prescribed as 3 sets of 10 reps
- **THEN** the operation fails with a domain error and no set record is added

### Requirement: Set records are not capped by the prescription
An exercise SHALL accept any number of set records, more or fewer than its total prescribed sets, and each set record MAY carry more or fewer reps than prescribed.

#### Scenario: More set records than prescribed sets
- **WHEN** a fourth set record is added to an exercise prescribed as 3 sets of 10 reps
- **THEN** the exercise holds four set records

#### Scenario: Reps outside the prescription
- **WHEN** a set record of 14 reps is added to an exercise prescribed as sets of 8-12 reps
- **THEN** the set record is added with 14 reps

### Requirement: Set record values are validated
The reps of a set record SHALL be at least 1, and a duration SHALL be a whole number of seconds, at least 1. The weight is optional. When it is present, it SHALL be in kilograms, at least 0 (0 means bodyweight), at most the stored maximum and with at most 2 decimals. A missing weight means the weight is not tracked. Invalid values fail with a domain error that maps to 400 and nothing changes.

#### Scenario: Bodyweight set
- **WHEN** a set record of 12 reps at 0 kg is added
- **THEN** the set record is added with 0 kg

#### Scenario: Set without weight
- **WHEN** a set record of 10 reps without a weight is added
- **THEN** the set record is added with no weight, which is different from 0 kg

#### Scenario: Weighted timed set
- **WHEN** a set record of 45 seconds at 10 kg is added to a time-based exercise
- **THEN** the set record is added with 45 seconds and 10 kg

#### Scenario: Weight with two decimals
- **WHEN** a set record at 22.75 kg is added
- **THEN** the set record keeps 22.75 kg

#### Scenario: Zero reps
- **WHEN** a set record of 0 reps is added
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Zero seconds
- **WHEN** a set record of 0 seconds is added
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Negative weight
- **WHEN** a set record at -5 kg is added
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Weight with too many decimals
- **WHEN** a set record at 20.125 kg is added
- **THEN** the operation fails with a domain error and no set record is added

### Requirement: Adding a set record is not a content edit
Adding, editing, removing and reordering set records SHALL NOT count as editing the workout's content. The workout edit rule and its lock on non-draft workouts do not apply to them; the recording rule applies instead.

#### Scenario: Recording on a locked workout
- **WHEN** a set record is added to a `STARTED` workout, which is locked against content edits
- **THEN** the set record is added

#### Scenario: Athlete of a prescribed workout records
- **WHEN** the athlete of a prescribed `STARTED` workout, who may not edit its content, adds a set record
- **THEN** the set record is added

### Requirement: Exercises with set records keep their kind
While an exercise has set records, updating it SHALL refuse prescriptions of the other kind: a rep-based exercise cannot switch to time-based prescriptions, a time-based one cannot switch to rep-based prescriptions, and neither can switch to a mix of both. This applies to every caller, including an admin. The operation fails with a domain error that maps to 409, and the exercise keeps its effort, sets, per-side flag, rest and set records. Changes within the same kind are allowed.

#### Scenario: Switch to a duration target
- **WHEN** a rep-based exercise with set records is updated to 3 sets of 40 seconds
- **THEN** the operation fails with a domain error and the exercise is unchanged

#### Scenario: Switch to a reps target
- **WHEN** a time-based exercise with set records is updated to 3 sets of 10 reps
- **THEN** the operation fails with a domain error and the exercise is unchanged

#### Scenario: Change between rep-based targets
- **WHEN** an exercise with set records is updated from 3 sets of 10 reps to 3 sets of 8-12 reps
- **THEN** the exercise is updated and keeps its set records

#### Scenario: Change between time-based targets
- **WHEN** an exercise with set records is updated from 3 sets of 40 seconds to 3 sets of 1 minute
- **THEN** the exercise is updated and keeps its set records

#### Scenario: Exercise without set records
- **WHEN** an exercise without set records is updated to 3 sets of 40 seconds
- **THEN** the exercise is updated

### Requirement: Set records are stored with the workout
Set records SHALL be persisted with the workout and restored unchanged when it is loaded again, whether they hold reps or a duration and whether or not they have a weight. A workout created from a template starts with no set records. Removing an exercise from a workout also removes its set records.

#### Scenario: Round trip
- **WHEN** a workout is saved and loaded again with set records on two exercises: reps with weight, reps without weight, and a duration with and without weight
- **THEN** each exercise holds the same set records, in the same order, with the same result, weight (or no weight) and registration time

#### Scenario: Recording on a stored workout keeps earlier set records
- **WHEN** a set record is added to a stored workout whose exercise already has set records, and the workout is saved and loaded again
- **THEN** the exercise holds the earlier set records followed by the new one

#### Scenario: Workout from a template
- **WHEN** a workout is created from a template
- **THEN** none of its exercises hold set records

#### Scenario: Removed exercise
- **WHEN** an exercise with set records is removed from a stored workout and the workout is saved
- **THEN** its set records are no longer stored

### Requirement: Add a set record at the end of an exercise
A workout SHALL let a set record be added to one of its exercises, identified by the catalog exercise identifier. A set record holds the result (reps done or seconds held), an optional weight in kilograms and the moment it was registered, in UTC. A new set record is placed after the existing set records of that exercise, whatever its registration time.

#### Scenario: Record a set
- **WHEN** a set record of 8 reps at 62.5 kg is added to an exercise of a started workout
- **THEN** that exercise holds one set record with 8 reps, 62.5 kg and the given registration time
- **AND** the other exercises of the workout hold no set records

#### Scenario: New set records go last
- **WHEN** set records registered at 10:05, 10:01 and 10:03 are added to the same exercise in that order
- **THEN** the exercise holds them ordered 10:05, 10:01, 10:03

#### Scenario: Same registration time keeps the order added
- **WHEN** two set records with the same registration time are added to the same exercise
- **THEN** the exercise holds them in the order they were added

#### Scenario: Exercise not in the workout
- **WHEN** a set record is added for an exercise that is not part of the workout
- **THEN** the operation fails with a domain error and no set record is added

### Requirement: Record a set through the API
`POST /workouts/{workoutId}/exercises/{exerciseId}/set-records` SHALL add a set record from the request body. The body has optional `reps`, `durationSeconds` and `weight` fields. An empty result (neither `reps` nor `durationSeconds`) means the set was done as prescribed. The registration time is set by the server at the moment of the request; the request cannot supply it. The response is 200 with the whole updated workout.

#### Scenario: Athlete records a set
- **WHEN** the owner of a `STARTED` workout posts 8 reps at 62.5 kg for one of its exercises
- **THEN** the response is 200 and that exercise lists one more set record, last, with 8 reps, 62.5 kg and a server registration time

#### Scenario: Athlete records a timed set
- **WHEN** the owner of a `STARTED` workout posts 30 seconds for an exercise prescribed as 3 sets of 45 seconds
- **THEN** the response is 200 and the exercise lists one more set record with 30 seconds and no weight

#### Scenario: Admin records a set
- **WHEN** an admin posts a set record for an exercise of a `STARTED` workout they do not own
- **THEN** the response is 200 and the set record is added

### Requirement: Edit a set record
`PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}` SHALL replace the result and the weight of that set record. The request must give the result explicitly. A missing weight clears it. The identifier, registration time and position are kept. The response is 200 with the whole updated workout.

#### Scenario: Correct reps and weight
- **WHEN** the owner edits a set record of 8 reps at 60 kg to 10 reps at 62.5 kg
- **THEN** the response is 200 and the set record has 10 reps and 62.5 kg, with the same id, registration time and position

#### Scenario: Correct a duration and clear the weight
- **WHEN** the owner edits a set record of 45 seconds at 10 kg to 40 seconds without a weight
- **THEN** the response is 200 and the set record has 40 seconds and no weight, with the same id, registration time and position

#### Scenario: Edit without a result
- **WHEN** a set record is edited with neither `reps` nor `durationSeconds`
- **THEN** the response is 400 and the set record keeps its previous values

#### Scenario: Edit to the other kind
- **WHEN** a set record of a rep-based exercise is edited to 40 seconds
- **THEN** the response is 409 and the set record keeps its previous values

#### Scenario: Invalid values leave the set record unchanged
- **WHEN** a set record is edited to 0 reps, 0 seconds or -5 kg
- **THEN** the response is 400 and the set record keeps its previous values

### Requirement: Remove a set record
`DELETE /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}` SHALL remove that set record. The other set records of the exercise keep their relative order. The response is 200 with the whole updated workout.

#### Scenario: Remove the middle set record
- **WHEN** the owner removes the second of three set records
- **THEN** the response is 200 and the exercise lists the first and third, in that order

### Requirement: Reorder the set records of an exercise
`PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/order` SHALL put the set records of that exercise in the order given by `setRecordIds`. Ids, reps, weight and registration times are kept. The response is 200 with the whole updated workout.

#### Scenario: Reverse the order
- **WHEN** the owner sends the three set record ids of an exercise in reverse order
- **THEN** the response is 200 and the exercise lists them in reverse order with the same values

#### Scenario: Other exercises are untouched
- **WHEN** the set records of one exercise are reordered
- **THEN** the set records of the other exercises keep their order

### Requirement: The new order lists exactly the set records of the exercise
The reorder request SHALL list every set record of the exercise exactly once. A missing, extra, repeated or foreign id fails with a domain error that maps to 409 and the order is unchanged.

#### Scenario: Missing set record
- **WHEN** the order omits one of the exercise's set records
- **THEN** the response is 409 and the order is unchanged

#### Scenario: Repeated set record
- **WHEN** the order lists a set record twice
- **THEN** the response is 409 and the order is unchanged

#### Scenario: Set record of another exercise
- **WHEN** the order includes a set record of another exercise of the workout
- **THEN** the response is 409 and the order is unchanged

### Requirement: Set records are kept in their order
Set records SHALL be stored and read back in the order they hold in the workout, after adding, removing or reordering.

#### Scenario: Reordered set records survive a reload
- **WHEN** the set records of an exercise are reordered and the workout is read again
- **THEN** the set records come back in the new order

### Requirement: Unknown targets are not found
An unknown workout, an exercise not in the workout or an unknown set record of that exercise SHALL fail with 404 on the set record routes, once the caller has passed the recording rule where the workout exists.

#### Scenario: Unknown workout
- **WHEN** a set record is posted to a workout id that does not exist
- **THEN** the response is 404

#### Scenario: Exercise not in the workout
- **WHEN** a set record is posted for an exercise that is not part of the workout
- **THEN** the response is 404 and no set record is added

#### Scenario: Unknown set record
- **WHEN** a set record id that does not belong to the exercise is edited or removed
- **THEN** the response is 404 and the set records are unchanged

### Requirement: Only the athlete or an admin may record sets
Set record routes SHALL be allowed to an admin always, and otherwise only to the workout's owner when the owner's role is not trainer. Any other caller, including every trainer, fails with 403. The permission check runs before the status check, so a rejected caller never learns the workout's status.

#### Scenario: Prescribing trainer is rejected
- **WHEN** the prescribing trainer of a `STARTED` workout posts a set record
- **THEN** the response is 403 and no set record is added

#### Scenario: Trainer who owns a self-planned workout is rejected
- **WHEN** a trainer posts a set record to a `STARTED` workout they own and that has no trainer
- **THEN** the response is 403 and no set record is added

#### Scenario: Another athlete is rejected
- **WHEN** a user who does not own the workout edits one of its set records
- **THEN** the response is 403 and the set record is unchanged

#### Scenario: Rejected caller does not learn the status
- **WHEN** a user who does not own a `DRAFT` workout posts a set record to it
- **THEN** the response is 403, not 409

### Requirement: Set record routes require authentication
Every set record route SHALL reject a request without a token or with an invalid token with 401.

#### Scenario: No token
- **WHEN** any set record route is called without a token
- **THEN** the response is 401

#### Scenario: Invalid token
- **WHEN** any set record route is called with an invalid token
- **THEN** the response is 401

### Requirement: Set record requests are structurally validated
Add and edit requests SHALL NOT send both `reps` and `durationSeconds`. The reorder request SHALL require `setRecordIds`. A request that breaks either rule fails with 400 and nothing changes.

#### Scenario: Missing weight
- **WHEN** a set record is posted with `reps` and without `weight`
- **THEN** the response is 200 and the set record has no weight

#### Scenario: Both reps and duration
- **WHEN** a set record is posted with both `reps` and `durationSeconds`
- **THEN** the response is 400 and no set record is added

#### Scenario: Missing list
- **WHEN** the reorder request has no `setRecordIds`
- **THEN** the response is 400 and the order is unchanged

### Requirement: The workout response lists set records
The workout detail response SHALL list each exercise's set records in order. Each one has:

- its id;
- a `type` of `Reps` or `Duration`;
- `reps` or `durationSeconds`, set according to the type, with the other one null;
- the weight in kilograms, or null when it is not tracked;
- the registration time in UTC.

An exercise without set records lists an empty collection.

#### Scenario: Read a workout with set records
- **WHEN** a workout with two set records on one exercise is read by id
- **THEN** that exercise lists both set records in order with id, type, result, weight and registration time
- **AND** the other exercises list no set records

#### Scenario: Read a timed set without weight
- **WHEN** a workout with a 45-second set record without weight is read by id
- **THEN** the set record has type `Duration`, 45 `durationSeconds`, null `reps` and null `weight`

### Requirement: Every change to the results is timestamped
Adding, editing, removing or reordering a set record SHALL set the workout's `resultsUpdatedAt` to the server time of the request, in UTC. A workout whose set records were never changed SHALL have `resultsUpdatedAt` `null`. Operations that fail SHALL leave it unchanged.

#### Scenario: First set record
- **WHEN** the first set record of a `STARTED` workout is added
- **THEN** the workout's `resultsUpdatedAt` is the time of that request

#### Scenario: Correction moves the timestamp
- **WHEN** a set record of a `COMPLETED` workout is edited, removed or reordered
- **THEN** the workout's `resultsUpdatedAt` is the time of that request

#### Scenario: No results yet
- **WHEN** a workout without set records that were ever changed is read
- **THEN** its `resultsUpdatedAt` is `null`

#### Scenario: Failed change keeps the timestamp
- **WHEN** a set record change on a `REVIEWED` workout is refused
- **THEN** the workout's `resultsUpdatedAt` is unchanged
### Requirement: Check a set as done as prescribed
Adding a set record without a result SHALL record the value of the next prescribed set: the prescriptions expanded in order into one target per set, at the position given by the number of existing set records. Fixed reps and durations are copied, and minutes become seconds rounded to the nearest second. A reps range, max reps or no prescribed set left fails with a domain error that maps to 400 and nothing changes. The weight stays optional.

#### Scenario: Done as prescribed with reps
- **WHEN** a set record without a result and without a weight is added to an exercise prescribed as 3 sets of 10 reps
- **THEN** the exercise holds one set record with 10 reps and no weight

#### Scenario: Done as prescribed with a duration
- **WHEN** a set record without a result is added to an exercise prescribed as 3 sets of 45 seconds
- **THEN** the exercise holds one set record with 45 seconds

#### Scenario: Done as prescribed with minutes
- **WHEN** a set record without a result is added to an exercise prescribed as 2 sets of 1.5 minutes
- **THEN** the exercise holds one set record with 90 seconds

#### Scenario: Next set of a mixed rep prescription
- **WHEN** an exercise prescribed as 2 sets of 10 reps and 1 set of 8 reps already has two set records, and a set record without a result is added
- **THEN** the new set record has 8 reps

#### Scenario: Position follows removals
- **WHEN** an exercise prescribed as 2 sets of 10 reps and 1 set of 8 reps has two set records, one is removed, and a set record without a result is added
- **THEN** the new set record has 10 reps

#### Scenario: Range or max target
- **WHEN** a set record without a result is added to an exercise prescribed as 3 sets of 8-12 reps or 3 sets of max reps
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Beyond the prescription
- **WHEN** an exercise prescribed as 3 sets of 10 reps already has three set records, and a set record without a result is added
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Through the API
- **WHEN** the owner of a `STARTED` workout posts an empty body for an exercise prescribed as 3 sets of 45 seconds
- **THEN** the response is 200 and the exercise lists one more set record with type `Duration`, 45 `durationSeconds` and no weight
