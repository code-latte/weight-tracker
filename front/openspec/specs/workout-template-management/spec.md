# workout-template-management Specification

## Purpose

Lets the fronts list, read, create, update and delete workout templates, manage their exercises and superseries, start
a workout from a template and save a workout as a template through `core`, each operation returning the API's answer
or a `DomainError`, with every ownership and placement rule left to the API.

## Requirements

### Requirement: Listing templates forwards the optional trainer

Listing templates SHALL send `GET /workout-templates` with no body, adding `?trainerId=<id>` only when a trainer id is
given, and SHALL return the list the API answers with, unchanged. It SHALL NOT filter, sort or check the caller's role.

#### Scenario: Templates listed

- **WHEN** templates are listed without a trainer id and the API answers 200 with two templates
- **THEN** exactly one `GET /workout-templates` request is sent without a query string
- **AND** the result is successful and holds those two templates in the order the API returned them

#### Scenario: Templates of one trainer

- **WHEN** templates are listed with trainer id `t-1`
- **THEN** exactly one `GET /workout-templates?trainerId=t-1` request is sent

### Requirement: Reading a template returns it with its exercises

Reading a template SHALL send `GET /workout-templates/{id}` with the template id in the path and no body, and SHALL
return the template with its name, description, trainer id and exercises in order, as the API answers with 200.

#### Scenario: Template read

- **WHEN** template `wt-1` is read and the API answers 200
- **THEN** exactly one `GET /workout-templates/wt-1` request is sent
- **AND** the result is successful and holds the template unchanged

#### Scenario: Unknown template

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

### Requirement: Creating a template sends its data unchanged

Creating a template SHALL send `POST /workout-templates` with the given `name`, the optional `description` and the
optional `trainerId`, unchanged and without adding absent keys, and SHALL return the template the API answers with 201.

#### Scenario: Template created by a trainer

- **WHEN** a template is created with name "Pierna A" and no trainer id, and the API answers 201
- **THEN** exactly one `POST /workout-templates` request is sent with body `{ "name": "Pierna A" }`
- **AND** the result is successful and holds the created template

#### Scenario: Template created for a trainer

- **WHEN** a template is created with name "Pierna A", description "Fuerza" and trainer id `t-1`
- **THEN** the body is `{ "name": "Pierna A", "description": "Fuerza", "trainerId": "t-1" }`

### Requirement: Updating a template replaces its name and description

Updating a template SHALL send `PUT /workout-templates/{id}` with the template id in the path and the given body
unchanged, and SHALL return the template the API answers with 200.

#### Scenario: Template updated

- **WHEN** template `wt-1` is updated with name "Pierna B" and the API answers 200
- **THEN** exactly one `PUT /workout-templates/wt-1` request is sent with body `{ "name": "Pierna B" }`
- **AND** the result is successful and holds the returned template

### Requirement: Deleting a template sends its id

Deleting a template SHALL send `DELETE /workout-templates/{id}` with the template id in the path and no body, and SHALL
return a successful empty result when the API answers 204.

#### Scenario: Template deleted

- **WHEN** template `wt-1` is deleted and the API answers 204
- **THEN** exactly one `DELETE /workout-templates/wt-1` request is sent without a body
- **AND** the result is successful

### Requirement: Template exercises are added, updated and removed like workout exercises

Adding, updating and removing an exercise of a template SHALL send, respectively, `POST
/workout-templates/{templateId}/exercises`, `PUT /workout-templates/{templateId}/exercises/{exerciseId}` and `DELETE
/workout-templates/{templateId}/exercises/{exerciseId}`, with the same bodies as the workout exercise operations,
unchanged. Add and update SHALL return the entry the API answers with; remove SHALL return an empty result on 204.

#### Scenario: Exercise added

- **WHEN** catalog exercise `ex-1` is added to template `wt-1` and the API answers 201
- **THEN** exactly one `POST /workout-templates/wt-1/exercises` request is sent with the given body unchanged
- **AND** the result is successful and holds the returned entry

#### Scenario: Exercise updated

- **WHEN** catalog exercise `ex-1` of template `wt-1` is updated and the API answers 200
- **THEN** exactly one `PUT /workout-templates/wt-1/exercises/ex-1` request is sent with the given body unchanged

#### Scenario: Exercise removed

- **WHEN** catalog exercise `ex-1` is removed from template `wt-1` and the API answers 204
- **THEN** exactly one `DELETE /workout-templates/wt-1/exercises/ex-1` request is sent without a body
- **AND** the result is successful

### Requirement: Reordering template exercises sends the catalog ids in order

Reordering SHALL send `PUT /workout-templates/{templateId}/exercises/order` with a body `{ exerciseIds }` holding the
catalog exercise ids in the given order, unchanged, and SHALL return the template the API answers with 200.

#### Scenario: Exercises reordered

- **WHEN** template `wt-1` is reordered to `[ex-2, ex-1]` and the API answers 200
- **THEN** exactly one `PUT /workout-templates/wt-1/exercises/order` request is sent with body
  `{ "exerciseIds": ["ex-2", "ex-1"] }`
- **AND** the result is successful and holds the returned template

### Requirement: Template superseries are created and broken like workout superseries

Creating a superseries SHALL send `POST /workout-templates/{templateId}/superseries` with `{ exerciseIds }` holding the
given entry ids in order, returning the superseries id answered with 201. Breaking SHALL send `DELETE
/workout-templates/{templateId}/superseries/{superSeriesId}` with no body, returning an empty result on 204.

#### Scenario: Superseries created

- **WHEN** a superseries is created in template `wt-1` with entries `[e-1, e-2]` and the API answers 201 with `{ "id": 3 }`
- **THEN** exactly one `POST /workout-templates/wt-1/superseries` request is sent with body `{ "exerciseIds": ["e-1", "e-2"] }`
- **AND** the result is successful and holds the superseries id `3`

#### Scenario: Superseries broken

- **WHEN** superseries `3` of template `wt-1` is broken and the API answers 204
- **THEN** exactly one `DELETE /workout-templates/wt-1/superseries/3` request is sent without a body

### Requirement: Creating a workout from a template sends its placement unchanged

Creating a workout from a template SHALL send `POST /workout-templates/{templateId}/workouts` with the given `userId`
and the optional `workoutDate`, `mesocycleId` and `mesocycleWeekNumber`, unchanged and without adding absent keys or a
trainer id, and SHALL return the workout the API answers with 201.

#### Scenario: Standalone workout from a template

- **WHEN** a workout is created from template `wt-1` for user `u-1` on `2026-11-04` and the API answers 201
- **THEN** exactly one `POST /workout-templates/wt-1/workouts` request is sent with body
  `{ "userId": "u-1", "workoutDate": "2026-11-04" }`
- **AND** the result is successful and holds the created workout

#### Scenario: Week workout from a template

- **WHEN** a workout is created from template `wt-1` for user `u-1` in mesocycle `m-1`, week `2`
- **THEN** the body is `{ "userId": "u-1", "mesocycleId": "m-1", "mesocycleWeekNumber": 2 }`

### Requirement: Saving a workout as a template sends only the workout id

Creating a template from a workout SHALL send `POST /workouts/{workoutId}/templates` with the workout id in the path
and no body, and SHALL return the template the API answers with 201.

#### Scenario: Workout saved as a template

- **WHEN** workout `w-1` is saved as a template and the API answers 201
- **THEN** exactly one `POST /workouts/w-1/templates` request is sent without a body
- **AND** the result is successful and holds the created template

### Requirement: Every template operation returns errors as results

Every template operation SHALL return a `DomainError` result instead of throwing: 400 as `VALIDATION`, 401 as
`UNAUTHORIZED`, 403 as `FORBIDDEN`, 404 as `NOT_FOUND`, 409 as `CONFLICT`, 500 and other unlisted statuses as
`UNEXPECTED`, and a request that gets no response as `NETWORK`.

#### Scenario: Operation refused

- **WHEN** any template operation gets 400, 401, 403, 404 or 409
- **THEN** the result is a failure with code `VALIDATION`, `UNAUTHORIZED`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT`

#### Scenario: Server error or no response

- **WHEN** any template operation gets 500, or no response at all
- **THEN** the result is a failure with code `UNEXPECTED` or `NETWORK`, and nothing is thrown
