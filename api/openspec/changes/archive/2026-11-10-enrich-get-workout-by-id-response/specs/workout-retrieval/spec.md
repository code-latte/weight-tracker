# Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: The response contains the whole workout without catalog exercise data`
- TO: `### Requirement: The response contains the whole workout with catalog exercise data`

## MODIFIED Requirements

### Requirement: The response contains the whole workout with catalog exercise data
A successful read SHALL respond `200 OK` with `id`, `name`, `description` (or `null`), `workoutDate`, `userId`, `trainerId` (or `null`), `status` and the `exercises` list. Each entry SHALL carry its own `id`, the catalog `exerciseId`, `effort`, `perSide`, `restMinutes` (or `null`), its `sets` and its `superSeriesId` (or `null`), plus the referenced exercise's `translations`, `videoUrl` (or `null`), `muscle`, `bodyPart` and `type`. Entries SHALL NOT carry `name`.

#### Scenario: Workout with exercises and sets
- **WHEN** the owner reads a workout that has exercises with sets
- **THEN** the response is `200 OK` with its `name`, `description`, `status`, every entry, its sets and its superseries membership
- **AND** no entry contains `name`

#### Scenario: Entries carry the catalog exercise data
- **WHEN** an allowed caller reads a workout with an entry for the seeded Press Banca
- **THEN** that entry carries the translations of Press Banca, `muscle` `CHEST`, `bodyPart` `CHEST` and `type` `STRENGTH`
- **AND** it carries Press Banca's `videoUrl`, `null` when it has none
- **AND** its `exerciseId` is still the id of Press Banca

#### Scenario: Custom and archived exercises are still described
- **WHEN** an allowed caller reads a workout with an entry whose exercise is a custom exercise, archived or not
- **THEN** that entry carries the exercise's `translations`, `muscle`, `bodyPart` and `type`

#### Scenario: Empty workout
- **WHEN** the owner reads a workout with no exercises
- **THEN** the response is `200 OK` with an empty `exercises` list

#### Scenario: Entries carry per side and rest
- **WHEN** the owner reads a workout with one entry that has perSide `true` and restMinutes `1.25`, and another with neither
- **THEN** the first entry has `perSide` `true` and `restMinutes` `1.25`
- **AND** the second entry has `perSide` `false` and `restMinutes` `null`

#### Scenario: Minutes target in the response
- **WHEN** the owner reads a workout with a `1 x Minutes` set of 30 minutes
- **THEN** that set's target has `type` `Minutes` and `minutes` `30`, and no `value` or `maxValue`

### Requirement: The workout read by id names its athlete and trainer

A successful `GET /workouts/{id}` SHALL also carry `athlete`, the owner's public profile, and `trainer`, the
prescribing trainer's public profile or `null` when the workout has none. Both SHALL match `userId` and `trainerId`,
SHALL contain exactly `id`, `email`, `name`, `surname` and `role`, and SHALL be returned whatever the state of those
users or of their relationship. Visibility and the other fields SHALL be unchanged.

#### Scenario: Prescribed workout read by id

- **WHEN** an ADMIN reads a workout that has a trainer
- **THEN** its `athlete.id` equals its `userId` and its `trainer.id` equals its `trainerId`
- **AND** both have exactly the properties `id`, `email`, `name`, `surname` and `role`

#### Scenario: Self-planned workout read by id

- **WHEN** the owner reads a workout with no trainer
- **THEN** the response has a non-null `athlete` and `trainer` `null`

#### Scenario: Write responses keep their shape

- **WHEN** a caller updates a workout with `PUT /workouts/{id}`
- **THEN** the `200 OK` body has no `athlete`, no `trainer` and no `mesocycle` property
- **AND** its entries carry no `translations`, `muscle`, `bodyPart` or `type`

### Requirement: The workout read by id schema is documented

The OpenAPI document SHALL describe the `GET /workouts/{id}` `200` body as `WorkoutByIdResponse`, with a required
`athlete`, a nullable `trainer`, both referencing the user profile schema, and a nullable `mesocycle` whose `weeks`
reference the mesocycle week schema. Its entries SHALL declare `translations`, `muscle`, `bodyPart` and `type`.
`PUT /workouts/{id}` SHALL keep referencing `WorkoutDetailResponse`.

#### Scenario: By-id schema

- **WHEN** the OpenAPI document is read
- **THEN** the `GET /workouts/{id}` `200` body references `WorkoutByIdResponse`, whose `athlete` and `trainer`
  reference `UserProfileResponse` and whose `trainer` is nullable
- **AND** the `PUT /workouts/{id}` `200` body references `WorkoutDetailResponse`

#### Scenario: By-id schema embeds exercises and mesocycle

- **WHEN** the OpenAPI document is read
- **THEN** `WorkoutByIdResponse` has a nullable `mesocycle` whose `weeks` items reference `MesocycleWeekResponse`
- **AND** the items of its `exercises` declare `translations`, `muscle`, `bodyPart` and `type`

## ADDED Requirements

### Requirement: The workout read by id embeds its mesocycle
A successful `GET /workouts/{id}` SHALL carry `mesocycle`: for a placed workout, the mesocycle its `mesocycleId` names with `id`, `name`, `objective`, `startDate`, `durationInWeeks` and every defined week as `weekNumber`, `trainingWeekType` and `objective`, ordered by week number; `null` for a standalone workout. `mesocycleId` and `mesocycleWeekNumber` SHALL still be returned.

#### Scenario: Placed workout carries its mesocycle
- **WHEN** an allowed caller reads the seeded workout placed in week 1 of the seeded mesocycle
- **THEN** `mesocycle.id` equals `mesocycleId` and `mesocycle` carries the seeded mesocycle's `name`, `objective`, `startDate` and `durationInWeeks`
- **AND** `mesocycle.weeks` lists every defined week of the seeded mesocycle in ascending `weekNumber`
- **AND** `mesocycleWeekNumber` is still `1`

#### Scenario: Standalone workout has no mesocycle
- **WHEN** an allowed caller reads a workout with no placement
- **THEN** `mesocycle` is present and `null`

#### Scenario: Mesocycle carries no people
- **WHEN** an allowed caller reads a placed workout
- **THEN** `mesocycle` has no `trainer`, `athlete`, `trainerId` or `athleteId` property

#### Scenario: Every caller who sees the workout sees its mesocycle
- **WHEN** the admin, the prescribing trainer or the athlete reads the seeded placed workout
- **THEN** each response is `200 OK` with the same non-null `mesocycle`

### Requirement: A refused read discloses no exercise or mesocycle data
When `GET /workouts/{id}` is refused with `403 Forbidden` or `404 Not Found`, the body SHALL contain no exercise data and no mesocycle data of that workout.

#### Scenario: Forbidden placed workout
- **WHEN** a caller who may not see the seeded placed workout reads it
- **THEN** the response is `403 Forbidden`
- **AND** the body has no `exercises`, `mesocycle`, `translations` or `weeks` property
