# Spec Delta

## MODIFIED Requirements

### Requirement: Set records only on started workouts
Set records SHALL only be added, edited, removed or reordered while the workout is `STARTED`, for every caller including an admin. In any other status the operation fails with a domain error that maps to 409 and nothing changes.

#### Scenario: Started workout
- **WHEN** a set record is added to an exercise of a `STARTED` workout
- **THEN** the set record is added

#### Scenario: Workout in another status
- **WHEN** a set record is added to an exercise of a `DRAFT`, `PUBLISHED` or `FINISHED` workout
- **THEN** the operation fails with a domain error and no set record is added

#### Scenario: Edit, remove or reorder on a finished workout
- **WHEN** a set record of a `FINISHED` workout is edited, removed or reordered
- **THEN** the operation fails with a domain error and the set records are unchanged

#### Scenario: Admin on a finished workout
- **WHEN** an admin edits a set record of a `FINISHED` workout
- **THEN** the operation fails with a domain error and the set record is unchanged

### Requirement: Adding a set record is not a content edit
Adding, editing, removing and reordering set records SHALL NOT count as editing the workout's content. The workout edit rule and its lock on `STARTED` and `FINISHED` workouts do not apply to them; the recording rule applies instead.

#### Scenario: Recording on a locked workout
- **WHEN** a set record is added to a `STARTED` workout, which is locked against content edits
- **THEN** the set record is added

#### Scenario: Athlete of a prescribed workout records
- **WHEN** the athlete of a prescribed `STARTED` workout, who may not edit its content, adds a set record
- **THEN** the set record is added

## ADDED Requirements

### Requirement: Add a set record at the end of an exercise
A workout SHALL let a set record be added to one of its exercises, identified by the catalog exercise identifier. A set record holds the reps done, the weight lifted in kilograms and the moment it was registered, in UTC. A new set record is placed after the existing set records of that exercise, whatever its registration time.

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
`POST /workouts/{workoutId}/exercises/{exerciseId}/set-records` SHALL add a set record with the reps and weight of the request body. The registration time is set by the server at the moment of the request; the request cannot supply it. The response is 200 with the whole updated workout.

#### Scenario: Athlete records a set
- **WHEN** the owner of a `STARTED` workout posts 8 reps at 62.5 kg for one of its exercises
- **THEN** the response is 200 and that exercise lists one more set record, last, with 8 reps, 62.5 kg and a server registration time

#### Scenario: Admin records a set
- **WHEN** an admin posts a set record for an exercise of a `STARTED` workout they do not own
- **THEN** the response is 200 and the set record is added

### Requirement: Edit a set record
`PUT /workouts/{workoutId}/exercises/{exerciseId}/set-records/{setRecordId}` SHALL replace the reps and weight of that set record. Its identifier, registration time and position are kept. The response is 200 with the whole updated workout.

#### Scenario: Correct reps and weight
- **WHEN** the owner edits a set record of 8 reps at 60 kg to 10 reps at 62.5 kg
- **THEN** the response is 200 and the set record has 10 reps and 62.5 kg, with the same id, registration time and position

#### Scenario: Invalid values leave the set record unchanged
- **WHEN** a set record is edited to 0 reps or to -5 kg
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
Add and edit requests SHALL require `reps` and `weight`, and the reorder request SHALL require `setRecordIds`. A missing field fails with 400 and nothing changes.

#### Scenario: Missing weight
- **WHEN** a set record is posted without `weight`
- **THEN** the response is 400 and no set record is added

#### Scenario: Missing list
- **WHEN** the reorder request has no `setRecordIds`
- **THEN** the response is 400 and the order is unchanged

### Requirement: The workout response lists set records
The workout detail response SHALL list, for each exercise, its set records in order, each with its id, reps, weight in kilograms and registration time in UTC. An exercise without set records lists an empty collection.

#### Scenario: Read a workout with set records
- **WHEN** a workout with two set records on one exercise is read by id
- **THEN** that exercise lists both set records in order with id, reps, weight and registration time
- **AND** the other exercises list no set records

## REMOVED Requirements

### Requirement: Add a set record to an exercise of a workout
**Reason**: Set records are no longer ordered by registration time; they keep an explicit order that reorder can change.
**Migration**: Replaced by "Add a set record at the end of an exercise". Stored set records keep their stored `Position`, so existing data reads back in the same order.
