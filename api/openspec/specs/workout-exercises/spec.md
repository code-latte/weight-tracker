# workout-exercises Specification

## Purpose

Lets an authorized caller put catalog exercises into an existing workout, replace their effort and set prescriptions, and remove them, so a workout can be filled after it is created.

## Requirements

### Requirement: Add an exercise to a workout
The system SHALL let a caller who may edit the workout add a catalog exercise with an effort and an ordered, non-empty list of set prescriptions. The new entry SHALL be placed after every existing entry, belong to no superseries, and be returned with its own entry identifier.

#### Scenario: Prescribing trainer adds an exercise
- **WHEN** the prescribing trainer adds an exercise with effort `HIGH` and sets `3 x 15 Reps` to a prescribed workout
- **THEN** the response is `201 Created` with the entry, its catalog exercise id, effort `HIGH` and the sets as given
- **AND** reading the workout returns the entry last, with no superseries

#### Scenario: Several prescriptions keep their order
- **WHEN** an exercise is added with sets `2 x 12 Reps` then `2 x 10 Reps`
- **THEN** the stored entry has both prescriptions in that order and a total of 4 sets

#### Scenario: Owner of a self-created workout adds an exercise
- **WHEN** the owner of a workout with no prescribing trainer adds an exercise
- **THEN** the response is `201 Created`

#### Scenario: Admin adds an exercise
- **WHEN** an admin adds an exercise to any workout
- **THEN** the response is `201 Created`

### Requirement: A catalog exercise appears once per workout
Adding a catalog exercise that is already in the workout SHALL fail with `409 Conflict` and leave the workout unchanged.

#### Scenario: Duplicate exercise
- **WHEN** an exercise already present in the workout is added again
- **THEN** the response is `409 Conflict`
- **AND** the workout still has a single entry for that exercise

### Requirement: The added exercise must exist in the catalog
Adding an exercise whose identifier matches no exercise, or matches a custom exercise the caller cannot see, SHALL fail with `404 Not Found` and leave the workout unchanged.

#### Scenario: Unknown catalog exercise
- **WHEN** an exercise id that is not in the catalog is added
- **THEN** the response is `404 Not Found`
- **AND** the workout is unchanged

#### Scenario: Custom exercise the caller cannot see
- **WHEN** a trainer adds another user's custom exercise that is in no workout the trainer can see
- **THEN** the response is `404 Not Found` and the workout is unchanged

### Requirement: Effort accepts its name or its number
The request effort SHALL be accepted as a defined effort name (`LOW`, `MIDLOW`, `MID`, `MIDHIGH`, `HIGH`) or its number (1 to 5). Responses SHALL report effort by name. Any other value SHALL fail with `400 Bad Request`.

#### Scenario: Effort as a name
- **WHEN** an exercise is added with effort `"MID"`
- **THEN** the entry's effort is `MID`

#### Scenario: Effort as a number
- **WHEN** an exercise is added with effort `5`
- **THEN** the entry's effort is `HIGH`

#### Scenario: Undefined effort
- **WHEN** an exercise is added with effort `9`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

### Requirement: Set prescriptions are validated
Every set prescription SHALL have a count of at least 1 and a target type of `Reps`, `Duration`, `MaxReps`, `RepsRange` or `Minutes`. `Reps` and `Duration` SHALL have a target of at least 1; `Duration` is in seconds. `MaxReps` SHALL ignore the target. `RepsRange` SHALL have a target (minimum reps) of at least 1 and a `maxTarget` (maximum reps) strictly greater than the target. Every type other than `RepsRange` SHALL ignore `maxTarget`. `Minutes` SHALL have a `targetMinutes` that is a valid minutes value and SHALL ignore `target` and `maxTarget`. Every type other than `Minutes` SHALL ignore `targetMinutes`. An empty or missing list, or any invalid prescription, SHALL fail with `400 Bad Request` and change nothing.

#### Scenario: Max reps ignores the target
- **WHEN** an exercise is added with sets `2 x MaxReps` and target `0`
- **THEN** the entry has 2 sets to failure and no target value

#### Scenario: Duration in seconds
- **WHEN** an exercise is added with sets `3 x Duration` and target `40`
- **THEN** the entry has 3 sets of 40 seconds

#### Scenario: Reps range
- **WHEN** an exercise is added or updated with sets `3 x RepsRange`, target `8` and maxTarget `12`
- **THEN** the entry has 3 sets of 8 to 12 reps
- **AND** the set target in the response has type `RepsRange`, value `8` and maxValue `12`

#### Scenario: Range maximum not above the minimum
- **WHEN** a `RepsRange` prescription has target `10` and maxTarget `10`, or target `10` and maxTarget `8`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Range without a maximum
- **WHEN** a `RepsRange` prescription has target `8` and no maxTarget
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Range minimum below one
- **WHEN** a `RepsRange` prescription has target `0` and maxTarget `12`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Other types ignore the maximum
- **WHEN** an exercise is added with sets `3 x Reps`, target `10` and maxTarget `12`
- **THEN** the entry has 3 sets of 10 reps
- **AND** the set target in the response has no maxValue

#### Scenario: Minutes target
- **WHEN** an exercise is added or updated with sets `1 x Minutes` and targetMinutes `30`
- **THEN** the entry has 1 set of 30 minutes
- **AND** the set target in the response has type `Minutes`, minutes `30`, and no value or maxValue

#### Scenario: Minutes target with decimals
- **WHEN** an exercise is added with sets `3 x Minutes` and targetMinutes `1.25`
- **THEN** the set target in the response has type `Minutes` and minutes `1.25`

#### Scenario: Minutes target missing or invalid
- **WHEN** a `Minutes` prescription has no targetMinutes, or targetMinutes `0`, `-1`, `1.255` or `1000`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Minutes ignores the integer targets
- **WHEN** an exercise is added with sets `1 x Minutes`, targetMinutes `30`, target `5` and maxTarget `9`
- **THEN** the set target in the response has type `Minutes`, minutes `30`, and no value or maxValue

#### Scenario: Other types ignore targetMinutes
- **WHEN** an exercise is added with sets `3 x Duration`, target `40` and targetMinutes `2`
- **THEN** the entry has 3 sets of 40 seconds
- **AND** the set target in the response has no minutes

#### Scenario: Empty sets
- **WHEN** an exercise is added or updated with an empty sets list
- **THEN** the response is `400 Bad Request`

#### Scenario: Non-positive count or target
- **WHEN** a prescription has count `0`, or a `Reps` or `Duration` target of `0`
- **THEN** the response is `400 Bad Request`

#### Scenario: Unknown target type
- **WHEN** a prescription has target type `Weight`
- **THEN** the response is `400 Bad Request`

### Requirement: Update replaces effort and all sets of an entry
The system SHALL let a caller who may edit the workout replace the effort, the per-side flag, the rest and the whole set prescription list of an exercise in the workout, identified by its catalog exercise id. The entry SHALL keep its entry identifier, position and superseries. The previous set prescriptions SHALL be discarded and the new ones stored, even when they are equal in value. An omitted `perSide` SHALL be stored as `false` and an omitted `restMinutes` as no rest.

#### Scenario: Change effort and sets
- **WHEN** an entry with `3 x 10 Reps` at effort `MID` is updated to `2 x 12 Reps` then `2 x 10 Reps` at effort `HIGH`
- **THEN** the response is `200 OK` with the updated entry
- **AND** reading the workout shows only the new prescriptions, at the same position and with the same entry id

#### Scenario: Effort-only change still replaces the stored sets
- **WHEN** an entry is updated with a new effort and the same set values
- **THEN** the stored set prescriptions are new records equal in value to the old ones
- **AND** none of the previous set records remain

#### Scenario: Change per side and rest
- **WHEN** an entry with perSide `false` and no rest is updated with perSide `true` and restMinutes `1.5`
- **THEN** the response is `200 OK` with perSide `true` and restMinutes `1.5`
- **AND** reading the workout returns the same values

#### Scenario: Omitted per side and rest clear previous values
- **WHEN** an entry with perSide `true` and restMinutes `2` is updated without perSide and without restMinutes
- **THEN** reading the workout returns perSide `false` and restMinutes `null` for that entry

### Requirement: Update cannot change the exercise
The catalog exercise id in the update body SHALL match the one in the route. A mismatch SHALL fail with `400 Bad Request` and change nothing. Swapping the exercise needs a removal and an addition.

#### Scenario: Body and route disagree
- **WHEN** an update targets one exercise in the route and names another in the body
- **THEN** the response is `400 Bad Request` and the workout is unchanged

### Requirement: Update keeps superseries set totals equal
When the updated entry belongs to a superseries and its new total set count differs from the other members', the update SHALL fail with `400 Bad Request` and leave the entry unchanged.

#### Scenario: Total changes inside a superseries
- **WHEN** a 3-set member of a superseries is updated to 4 sets
- **THEN** the response is `400 Bad Request`
- **AND** the entry keeps its previous effort and sets and stays in the superseries

#### Scenario: Same total inside a superseries
- **WHEN** a 3-set member of a superseries is updated to `2 x 8 Reps` then `1 x MaxReps`
- **THEN** the update is applied and the entry stays in the superseries

### Requirement: Remove an exercise from a workout
The system SHALL let a caller who may edit the workout remove an exercise from it by catalog exercise id. Its set prescriptions SHALL be removed with it. When the removed entry leaves a superseries with a single member, that member SHALL leave the superseries too.

#### Scenario: Remove an entry and its sets
- **WHEN** an exercise is removed from a workout
- **THEN** the response is `204 No Content`
- **AND** the workout no longer contains it and no set prescription of that entry remains stored

#### Scenario: Remove a member of a two-exercise superseries
- **WHEN** one exercise of a two-member superseries is removed
- **THEN** the remaining exercise belongs to no superseries

### Requirement: Exercise must be in the workout to update or remove it
Updating or removing a catalog exercise that is not in the workout SHALL fail with `404 Not Found`.

#### Scenario: Update an absent exercise
- **WHEN** an update targets a catalog exercise that the workout does not contain
- **THEN** the response is `404 Not Found`

#### Scenario: Remove an absent exercise
- **WHEN** a removal targets a catalog exercise that the workout does not contain
- **THEN** the response is `404 Not Found`

### Requirement: Exercise routes apply the workout edit rule
Adding, updating and removing exercises SHALL follow the workout editing permission rule: an unknown workout is `404 Not Found`, and a caller who may not edit the workout gets `403 Forbidden` with the workout unchanged.

#### Scenario: Owner of a prescribed workout is rejected
- **WHEN** the owner of a workout with a prescribing trainer adds, updates or removes an exercise
- **THEN** the response is `403 Forbidden` and the workout is unchanged

#### Scenario: Trainer who does not own a self-created workout is rejected
- **WHEN** a trainer adds, updates or removes an exercise in a workout with no prescribing trainer that they do not own
- **THEN** the response is `403 Forbidden` and the workout is unchanged

#### Scenario: Unknown workout
- **WHEN** an authenticated caller targets a workout id that does not exist
- **THEN** the response is `404 Not Found`

### Requirement: Exercise routes require authentication
Each of the three exercise routes SHALL reject a request with no token, an invalid token, or a token whose user no longer exists, with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** any exercise route is called without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** any exercise route is called with a malformed or unsigned token
- **THEN** the response is `401 Unauthorized`

#### Scenario: User no longer exists
- **WHEN** any exercise route is called with a valid token whose user was deleted
- **THEN** the response is `401 Unauthorized`

### Requirement: Built workout reads back as added
A workout built through the exercise routes SHALL read back, through the get-by-id route, with its entries in the order they were added and with each entry's effort and sets as last written.

#### Scenario: Create, add, read
- **WHEN** a caller creates a workout, adds two exercises, and reads the workout by id
- **THEN** the response lists both entries in insertion order with their efforts and set prescriptions

### Requirement: Reorder the exercises of a workout
The system SHALL let a caller who may edit the workout set the order of its exercises by sending the catalog exercise ids of the workout in the new order. The response SHALL be `200 OK` with the workout detail in the new order. Each entry SHALL keep its entry identifier, effort, set prescriptions and superseries.

#### Scenario: Prescribing trainer reorders a workout
- **WHEN** the prescribing trainer of a workout with exercises `A, B` sends the order `B, A`
- **THEN** the response is `200 OK` and lists the entries as `B, A`
- **AND** reading the workout by id returns `B, A` with the same entry ids, efforts and sets as before

#### Scenario: Superseries survive a reorder
- **WHEN** a workout whose exercises `A` and `B` form a superseries is reordered
- **THEN** `A` and `B` still belong to the same superseries

#### Scenario: Owner of a self-created workout reorders it
- **WHEN** the owner of a workout with no prescribing trainer sends a new order
- **THEN** the response is `200 OK` and the new order is stored

#### Scenario: Admin reorders a workout
- **WHEN** an admin sends a new order for any workout
- **THEN** the response is `200 OK` and the new order is stored

### Requirement: The new order lists exactly the exercises of the workout
The order SHALL contain every catalog exercise of the workout exactly once and nothing else. A list with fewer or more ids, an id that is not in the workout, or a repeated id SHALL fail with `409 Conflict` and leave the order unchanged. Workout entry ids are not accepted in place of catalog exercise ids.

#### Scenario: Missing exercise
- **WHEN** a workout has exercises `A, B` and the order `B` is sent
- **THEN** the response is `409 Conflict` and the order stays `A, B`

#### Scenario: Extra exercise
- **WHEN** a workout has exercises `A, B` and the order `B, A, C` is sent
- **THEN** the response is `409 Conflict` and the order stays `A, B`

#### Scenario: Exercise not in the workout
- **WHEN** a workout has exercises `A, B` and the order `B, C` is sent
- **THEN** the response is `409 Conflict` and the order stays `A, B`

#### Scenario: Repeated exercise
- **WHEN** a workout has exercises `A, B` and the order `B, B` is sent
- **THEN** the response is `409 Conflict` and the order stays `A, B`

#### Scenario: Entry ids instead of catalog ids
- **WHEN** the order is sent with workout entry ids instead of catalog exercise ids
- **THEN** the response is `409 Conflict` and the order is unchanged

### Requirement: Reorder request is structurally validated
A reorder request without the `exerciseIds` list SHALL fail with `400 Bad Request` and change nothing.

#### Scenario: Missing list
- **WHEN** the reorder route is called with an empty body
- **THEN** the response is `400 Bad Request`

### Requirement: Reorder route applies the workout edit rule
Reordering SHALL follow the workout editing permission rule. An unknown workout SHALL return `404 Not Found`. A caller who may not edit the workout SHALL get `403 Forbidden`, and the order SHALL be unchanged.

#### Scenario: Owner of a prescribed workout is rejected
- **WHEN** the owner of a workout with a prescribing trainer sends a new order
- **THEN** the response is `403 Forbidden` and the order is unchanged

#### Scenario: Trainer who does not own a self-created workout is rejected
- **WHEN** a trainer sends a new order for a workout with no prescribing trainer that they do not own
- **THEN** the response is `403 Forbidden`

#### Scenario: Another trainer on a prescribed workout is rejected
- **WHEN** a trainer other than the prescribing one sends a new order
- **THEN** the response is `403 Forbidden`

#### Scenario: Unknown workout
- **WHEN** an authenticated caller sends an order for a workout id that does not exist
- **THEN** the response is `404 Not Found`

### Requirement: Reorder route requires authentication
The reorder route SHALL reject a request with no token or with an invalid token with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** the reorder route is called without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** the reorder route is called with a malformed or unsigned token
- **THEN** the response is `401 Unauthorized`

### Requirement: Minutes values are validated
A minutes value, used by the `Minutes` target and by the rest, SHALL be greater than 0, have at most 2 decimal places, and be no greater than 999.99. Values with more decimals SHALL be rejected, not rounded. The JSON value SHALL be a number with a dot as the decimal separator. An invalid value SHALL fail with `400 Bad Request` and change nothing.

#### Scenario: Two decimals accepted
- **WHEN** an exercise is added with restMinutes `0.75`
- **THEN** the entry's restMinutes is `0.75`

#### Scenario: Upper bound accepted
- **WHEN** an exercise is added with sets `1 x Minutes` and targetMinutes `999.99`
- **THEN** the set target in the response has minutes `999.99`

#### Scenario: Too many decimals rejected
- **WHEN** an exercise is added with restMinutes `1.333`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

#### Scenario: Out of range rejected
- **WHEN** an exercise is added with restMinutes `0` or `1000`
- **THEN** the response is `400 Bad Request` and the workout is unchanged

### Requirement: An exercise can be marked per side
Add and update SHALL accept an optional `perSide` flag for the whole entry, defaulting to `false`. The flag SHALL NOT change the stored targets: they are per side. It SHALL be allowed with every target type.

#### Scenario: Per-side reps
- **WHEN** an exercise is added with perSide `true` and sets `1 x 8 Reps`
- **THEN** the response is `201 Created` with perSide `true` and a target of 8 reps

#### Scenario: Per-side duration
- **WHEN** an exercise is added with perSide `true` and sets `3 x Duration` with target `30`
- **THEN** the entry has perSide `true` and 3 sets of 30 seconds

#### Scenario: Per side omitted
- **WHEN** an exercise is added without perSide
- **THEN** the entry has perSide `false`

### Requirement: An exercise can prescribe its rest
Add and update SHALL accept an optional `restMinutes` for the whole entry, which SHALL be a valid minutes value. When it is omitted or `null`, the entry SHALL have no rest prescribed.

#### Scenario: Rest with decimals
- **WHEN** an exercise is added with restMinutes `1.5`
- **THEN** the response is `201 Created` with restMinutes `1.5`
- **AND** reading the workout returns restMinutes `1.5` for that entry

#### Scenario: Rest omitted
- **WHEN** an exercise is added without restMinutes
- **THEN** the entry has restMinutes `null`

#### Scenario: Invalid rest leaves the entry unchanged
- **WHEN** an entry is updated with restMinutes `-1`
- **THEN** the response is `400 Bad Request`
- **AND** the entry keeps its previous effort, sets, perSide and rest

### Requirement: Custom exercises are used only by their owner's workouts
A custom exercise SHALL be addable to a workout only when its owner is the workout's editor of record: the prescribing trainer when the workout has one, otherwise the workout's owner. Otherwise, and when the caller can see the exercise, the addition SHALL fail with `409 Conflict` and leave the workout unchanged. System exercises SHALL remain addable to any workout the caller may edit.

#### Scenario: Trainer adds their own exercise to a prescribed workout
- **WHEN** the prescribing trainer adds a custom exercise they own
- **THEN** the response is `201 Created`

#### Scenario: Athlete adds their own exercise to a self-planned workout
- **WHEN** the owner of a workout with no trainer adds a custom exercise they own
- **THEN** the response is `201 Created`

#### Scenario: Admin adds someone else's exercise
- **WHEN** an ADMIN adds trainer A's custom exercise to a workout prescribed by trainer B
- **THEN** the response is `409 Conflict` and the workout is unchanged

#### Scenario: Athlete's exercise in a prescribed workout
- **WHEN** an ADMIN adds an athlete's custom exercise to a workout prescribed to that athlete by a trainer
- **THEN** the response is `409 Conflict`

### Requirement: Archived exercises cannot be added to workouts
Adding an archived custom exercise to a workout SHALL fail with `409 Conflict` and leave the workout unchanged. Entries that already use an archived exercise SHALL keep it, and their effort, sets, per-side flag and rest SHALL stay editable.

#### Scenario: Add an archived exercise
- **WHEN** the owner adds their archived exercise to their workout
- **THEN** the response is `409 Conflict`

#### Scenario: Update an entry with an archived exercise
- **WHEN** the prescribing trainer updates the sets of an entry whose exercise they archived
- **THEN** the response is `200 OK`
