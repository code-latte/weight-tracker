# workout-template-content Specification

## Purpose

Lets the trainer who owns a workout template, or an admin, fill it with exercises, sets and superseries under the same
content rules a workout follows, so a template can hold a complete workout.

## Requirements

### Requirement: Template content routes follow the template access rules
Adding, updating, removing and reordering exercises and creating and breaking superseries on a template SHALL be allowed to the owning trainer and to admins only, as the `workout-templates` access rules state: `401` without a valid token, `403` for a regular user, `404` for an unknown template or another trainer's template.

#### Scenario: Owner adds an exercise
- **WHEN** the owning trainer adds an exercise to their template
- **THEN** the response is `201`

#### Scenario: Other trainer adds an exercise
- **WHEN** trainer `A` adds an exercise to a template owned by trainer `B`
- **THEN** the response is `404` and the template is unchanged

#### Scenario: Regular user reorders exercises
- **WHEN** a regular user reorders the exercises of a template
- **THEN** the response is `403`

### Requirement: Add an exercise to a template
`POST /workout-templates/{id}/exercises` SHALL add an exercise with an effort, an ordered non-empty list of set prescriptions, an optional per-side flag and an optional rest. The entry SHALL be placed after every existing entry, belong to no superseries, and be returned with its own entry identifier.

#### Scenario: Add to an empty template
- **WHEN** the owning trainer adds an exercise to a template with no exercises
- **THEN** the response is `201` and the template has that exercise in position 1

### Requirement: Template exercise input follows the workout rules
Effort, set prescriptions, minutes, per side and rest SHALL be accepted and validated exactly as for workout exercises. An invalid value SHALL be refused with `400` and change nothing.

#### Scenario: Empty sets
- **WHEN** an exercise is added to a template with an empty set list
- **THEN** the response is `400` and the template is unchanged

#### Scenario: Effort by number
- **WHEN** an exercise is added to a template with effort `3`
- **THEN** the response reports effort `MID`

### Requirement: A catalog exercise appears once per template
Adding a catalog exercise that is already in the template SHALL fail with `409` and leave the template unchanged.

#### Scenario: Duplicate exercise
- **WHEN** an exercise already in the template is added again
- **THEN** the response is `409`

### Requirement: The added exercise must exist and be visible
Adding an exercise whose id matches no exercise, or matches a custom exercise the caller cannot see, SHALL fail with `404` and leave the template unchanged.

#### Scenario: Unknown exercise
- **WHEN** an exercise id that matches no exercise is added to a template
- **THEN** the response is `404`

### Requirement: Custom exercises are used only by their owner's templates
A custom exercise SHALL be addable to a template only when its owner is the template's trainer. Otherwise, when the caller can see the exercise, the addition SHALL fail with `409` and leave the template unchanged. System exercises SHALL be addable to any template.

#### Scenario: Trainer adds their own exercise
- **WHEN** the owning trainer adds a custom exercise they own
- **THEN** the response is `201`

#### Scenario: Admin adds another trainer's exercise
- **WHEN** an admin adds trainer `A`'s custom exercise to a template owned by trainer `B`
- **THEN** the response is `409` and the template is unchanged

### Requirement: Archived exercises cannot be added to templates
Adding an archived custom exercise to a template SHALL fail with `409`. Entries that already use an archived exercise SHALL keep it and stay editable.

#### Scenario: Add an archived exercise
- **WHEN** the owning trainer adds their archived exercise
- **THEN** the response is `409`

#### Scenario: Update an entry whose exercise was archived
- **WHEN** the owning trainer updates the sets of an entry whose exercise they archived
- **THEN** the response is `200`

### Requirement: Update replaces effort and all sets of a template entry
`PUT /workout-templates/{id}/exercises/{exerciseId}` SHALL replace the effort, per-side flag, rest and whole set list of the entry identified by its catalog exercise id. The entry SHALL keep its identifier, position and superseries. The exercise id in the body SHALL match the route, otherwise `400`.

#### Scenario: Replace the sets
- **WHEN** the owning trainer sends new sets for an entry
- **THEN** the response is `200` and only the new sets remain

#### Scenario: Body and route disagree
- **WHEN** the exercise id in the body differs from the route
- **THEN** the response is `400` and nothing changes

### Requirement: Update keeps superseries set totals equal in a template
When the updated entry belongs to a superseries and its new total set count differs from the other members', the update SHALL fail with `400` and leave the entry unchanged.

#### Scenario: Unequal totals
- **WHEN** an entry in a superseries is given a different total set count
- **THEN** the response is `400`

### Requirement: Remove an exercise from a template
`DELETE /workout-templates/{id}/exercises/{exerciseId}` SHALL remove the entry and its sets and answer `204`. A superseries left with a single member SHALL ungroup that member.

#### Scenario: Remove a superseries member
- **WHEN** one of the two members of a superseries is removed
- **THEN** the other entry no longer belongs to a superseries

### Requirement: The exercise must be in the template to update or remove it
Updating or removing a catalog exercise that is not in the template SHALL fail with `404`.

#### Scenario: Remove a missing exercise
- **WHEN** an exercise that is not in the template is removed
- **THEN** the response is `404`

### Requirement: Reorder the exercises of a template
`PUT /workout-templates/{id}/exercises/order` SHALL set the order of the entries to the given list of catalog exercise ids. The list SHALL contain every exercise of the template exactly once, otherwise `409`, as for workouts, and nothing changes.

#### Scenario: Reorder
- **WHEN** the owning trainer sends the template's exercises in reverse order
- **THEN** the response is `200` and the template lists them in that order

#### Scenario: Missing an exercise
- **WHEN** the list leaves out one exercise of the template
- **THEN** the response is `409` and the order is unchanged

### Requirement: Group template exercises into a superseries
`POST /workout-templates/{id}/superseries` SHALL group two or more entries, named by entry identifier, into a superseries with an identifier unique within the template. As for workouts: fewer than two entries or unequal set totals SHALL be `400`, an entry not in the template `404`, and an entry already in a superseries `409`.

#### Scenario: Create a superseries
- **WHEN** the owning trainer groups two entries with equal set totals
- **THEN** the response is `201` with the superseries identifier and both entries share it

#### Scenario: Unequal totals
- **WHEN** two entries with different set totals are grouped
- **THEN** the response is `400` and nothing changes

#### Scenario: Entry already grouped
- **WHEN** an entry that already belongs to a superseries is grouped again
- **THEN** the response is `409` and the existing superseries is unchanged

### Requirement: Break a template superseries
`DELETE /workout-templates/{id}/superseries/{superSeriesId}` SHALL ungroup the members of the superseries without removing them and answer `204`. An unknown superseries SHALL be `404`.

#### Scenario: Break a superseries
- **WHEN** the owning trainer breaks a superseries
- **THEN** its entries remain in the template with no superseries

### Requirement: Templates are never locked
Template content SHALL stay editable at any time. A template has no status, so no status SHALL refuse a content change.

#### Scenario: Edit a template already used
- **WHEN** a workout was created from a template and was then started
- **THEN** the owning trainer can still change the template's content
