# mesocycle-management Specification

## Purpose

Lets both fronts read, list, create and update mesocycles and define and redefine their weeks through `core`, each
operation returning the API's answer or a `DomainError` as a result, with every role rule left to the API.

## Requirements

### Requirement: Listing mesocycles returns what the API answers

Listing mesocycles SHALL send `GET /mesocycles` with no body and SHALL return a successful result holding the
mesocycles the API answers with, in the order the API gives. It SHALL NOT filter them or check the caller's role.

#### Scenario: Mesocycles listed

- **WHEN** the mesocycles are listed and the API answers 200 with two mesocycles
- **THEN** exactly one `GET /mesocycles` request is sent
- **AND** the result is successful and its value is those two mesocycles, in the same order

#### Scenario: No mesocycles

- **WHEN** the API answers 200 with an empty list
- **THEN** the result is successful and its value is an empty list

#### Scenario: Role not allowed

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

### Requirement: Reading a mesocycle by id returns it with its weeks

Reading a mesocycle SHALL send `GET /mesocycles/{id}` with the given id in the path and SHALL return a successful
result holding the mesocycle the API answers with, including its weeks and the trainer and athlete profiles.

#### Scenario: Mesocycle read

- **WHEN** a mesocycle is read by id and the API answers 200
- **THEN** exactly one `GET /mesocycles/{id}` request is sent with that id in the path
- **AND** the result is successful and its value is the mesocycle the API answered with, weeks included

#### Scenario: Unknown or invisible mesocycle

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Mesocycle of someone else

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

### Requirement: Listing an athlete's mesocycles sends the athlete id

Listing an athlete's mesocycles SHALL send `GET /users/{userId}/mesocycles` with the given athlete id in the path and
SHALL return a successful result holding the mesocycles the API answers with, in the order the API gives.

#### Scenario: Athlete's mesocycles listed

- **WHEN** an athlete's mesocycles are listed and the API answers 200 with one mesocycle
- **THEN** exactly one `GET /users/{userId}/mesocycles` request is sent with that id in the path
- **AND** the result is successful and its value is that mesocycle

#### Scenario: Caller is an athlete

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

#### Scenario: Unknown athlete

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

### Requirement: Creating a mesocycle sends its data unchanged

Creating a mesocycle SHALL send `POST /mesocycles` with the given `athleteId`, `name`, `objective`, `startDate`,
`durationInWeeks` and optional `trainerId`, unchanged, and SHALL return a successful result holding the mesocycle the
API answers with. It SHALL NOT check the caller's role or add any field.

#### Scenario: Mesocycle created by a trainer

- **WHEN** a mesocycle is created without a `trainerId` and the API answers 201
- **THEN** exactly one `POST /mesocycles` request is sent with that body, unchanged and without `trainerId`
- **AND** the result is successful and its value is the mesocycle the API answered with

#### Scenario: Mesocycle created with a trainer

- **WHEN** a mesocycle is created with a `trainerId` and the API answers 201
- **THEN** the request body holds that `trainerId` unchanged

#### Scenario: Rejected by the API

- **WHEN** the API refuses the creation with 400, 403, 404 or 409
- **THEN** the result is a failure with code `VALIDATION`, `FORBIDDEN`, `NOT_FOUND` or `CONFLICT` respectively, with the
  API's detail kept in the message

### Requirement: Updating a mesocycle replaces its editable data

Updating a mesocycle SHALL send `PUT /mesocycles/{id}` with the given id in the path and a body holding only `name`,
`objective`, `startDate` and `durationInWeeks`, unchanged, and SHALL return a successful result holding the updated
mesocycle. The id SHALL NOT be sent in the body.

#### Scenario: Mesocycle updated

- **WHEN** a mesocycle is updated and the API answers 200
- **THEN** exactly one `PUT /mesocycles/{id}` request is sent with that id in the path and the four fields in the body
- **AND** the result is successful and its value is the mesocycle the API answered with

#### Scenario: Duration shorter than a defined week

- **WHEN** the API answers 409
- **THEN** the result is a failure with code `CONFLICT`

#### Scenario: Unknown mesocycle

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

### Requirement: Defining a week sends it to its mesocycle

Defining a week SHALL send `POST /mesocycles/{id}/weeks` with the mesocycle id in the path and a body holding
`weekNumber`, `trainingWeekType` and `objective`, unchanged, and SHALL return a successful result holding the mesocycle
the API answers with.

#### Scenario: Week defined

- **WHEN** week 3 is defined in a mesocycle and the API answers 201
- **THEN** exactly one `POST /mesocycles/{id}/weeks` request is sent with that id in the path and the week in the body
- **AND** the result is successful and its value is the mesocycle the API answered with

#### Scenario: Week already defined or beyond the duration

- **WHEN** the API answers 409
- **THEN** the result is a failure with code `CONFLICT`

### Requirement: Redefining a week addresses it by its number

Redefining a week SHALL send `PUT /mesocycles/{id}/weeks/{weekNumber}` with the mesocycle id and week number in the
path and a body holding only `trainingWeekType` and `objective`, unchanged, and SHALL return a successful result holding
the mesocycle the API answers with. The week number SHALL NOT be sent in the body.

#### Scenario: Week redefined

- **WHEN** week 2 of a mesocycle is redefined and the API answers 200
- **THEN** exactly one `PUT /mesocycles/{id}/weeks/2` request is sent with only the type and objective in the body
- **AND** the result is successful and its value is the mesocycle the API answered with

#### Scenario: Week not defined

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

### Requirement: Mesocycle operations return failures as results

Every mesocycle operation SHALL return a failure result instead of throwing when the API rejects the request or does not
answer, using the existing `DomainError` codes.

#### Scenario: Missing token

- **WHEN** the API answers 401
- **THEN** the result is a failure with code `UNAUTHORIZED`

#### Scenario: Unexpected status

- **WHEN** the API answers 500
- **THEN** the result is a failure with code `UNEXPECTED`

#### Scenario: No response

- **WHEN** the request gets no response
- **THEN** the result is a failure with code `NETWORK`

### Requirement: Listing a mesocycle's workouts sends the mesocycle id

Listing a mesocycle's workouts SHALL send `GET /mesocycles/{id}/workouts` with the given mesocycle id in the path and no
body, and SHALL return a successful result holding the workouts the API answers with, in the order the API gives, each
with its week number. It SHALL NOT filter, group or sort them, nor check the caller's role.

#### Scenario: Workouts listed

- **WHEN** the workouts of mesocycle `m-1` are listed and the API answers 200 with two workouts in weeks 1 and 2
- **THEN** exactly one `GET /mesocycles/m-1/workouts` request is sent without a body
- **AND** the result is successful and its value is those two workouts, in the same order, with their week numbers

#### Scenario: No workouts

- **WHEN** the API answers 200 with an empty list
- **THEN** the result is successful and its value is an empty list

#### Scenario: Unknown or invisible mesocycle

- **WHEN** the API answers 404
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Mesocycle of someone else

- **WHEN** the API answers 403
- **THEN** the result is a failure with code `FORBIDDEN`

#### Scenario: No response

- **WHEN** the request gets no response
- **THEN** the result is a failure with code `NETWORK`
