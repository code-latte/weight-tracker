# Spec Delta

## ADDED Requirements

### Requirement: The workout list carries the mesocycle placement

Every item of `GET /workouts` SHALL carry `mesocycleId` and `mesocycleWeekNumber`, matching the workout's placement.
For a workout that is not placed in a mesocycle, both properties SHALL be present with the value `null`, never omitted.
Every other field, the per-role scope, draft hiding and order SHALL be unchanged.

#### Scenario: Placed workout

- **WHEN** an ADMIN lists workouts and one of them is placed in week 2 of a mesocycle
- **THEN** that item's `mesocycleId` is the mesocycle's id and its `mesocycleWeekNumber` is `2`

#### Scenario: Standalone workout

- **WHEN** a caller lists workouts and one of them is not placed in a mesocycle
- **THEN** that item has the property `mesocycleId` with value `null` and the property `mesocycleWeekNumber` with value
  `null`

#### Scenario: Standalone filter keeps the properties

- **WHEN** a caller lists workouts with `standalone=true`
- **THEN** every item has `mesocycleId` and `mesocycleWeekNumber` present and `null`

### Requirement: The workout read by id names its athlete and trainer

A successful `GET /workouts/{id}` SHALL also carry `athlete`, the owner's public profile, and `trainer`, the
prescribing trainer's public profile or `null` when the workout has none. Both SHALL match `userId` and `trainerId`,
SHALL contain exactly `id`, `email`, `name`, `surname` and `role`, and SHALL be returned whatever the state of those
users or of their relationship. Entries SHALL still carry no catalog exercise data. Visibility and the other fields
SHALL be unchanged.

#### Scenario: Prescribed workout read by id

- **WHEN** an ADMIN reads a workout that has a trainer
- **THEN** its `athlete.id` equals its `userId` and its `trainer.id` equals its `trainerId`
- **AND** both have exactly the properties `id`, `email`, `name`, `surname` and `role`

#### Scenario: Self-planned workout read by id

- **WHEN** the owner reads a workout with no trainer
- **THEN** the response has a non-null `athlete` and `trainer` `null`

#### Scenario: Write responses keep their shape

- **WHEN** a caller updates a workout with `PUT /workouts/{id}`
- **THEN** the `200 OK` body has no `athlete` and no `trainer` property

### Requirement: The workout read by id schema is documented

The OpenAPI document SHALL describe the `GET /workouts/{id}` `200` body as `WorkoutByIdResponse`, with a required
`athlete` and a nullable `trainer`, both referencing the user profile schema. `PUT /workouts/{id}` SHALL keep
referencing `WorkoutDetailResponse`.

#### Scenario: By-id schema

- **WHEN** the OpenAPI document is read
- **THEN** the `GET /workouts/{id}` `200` body references `WorkoutByIdResponse`, whose `athlete` and `trainer`
  reference `UserProfileResponse` and whose `trainer` is nullable
- **AND** the `PUT /workouts/{id}` `200` body references `WorkoutDetailResponse`

### Requirement: The workout list item schema documents the placement

The OpenAPI schema `WorkoutListItemResponse` SHALL declare `mesocycleId` as a nullable UUID string and
`mesocycleWeekNumber` as a nullable integer.

#### Scenario: Placement in the item schema

- **WHEN** the OpenAPI document is read
- **THEN** `WorkoutListItemResponse` has a nullable `mesocycleId` of format `uuid` and a nullable integer
  `mesocycleWeekNumber`
