# workout-retrieval Specification

## Purpose

Lets authenticated callers read a single workout by id, with the visibility each role is entitled to, without catalog exercise data.

## Requirements

### Requirement: Authenticated callers may read a workout by id
The system SHALL expose `GET /workouts/{id}` to authenticated callers. A request without a valid access token SHALL be rejected with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** a caller sends `GET /workouts/{id}` without an access token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** a caller sends `GET /workouts/{id}` with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`

### Requirement: The response contains the whole workout without catalog exercise data
A successful read SHALL respond `200 OK` with `id`, `name`, `description` (or `null`), `workoutDate`, `userId`, `trainerId` (or `null`), `status` and the `exercises` list. Each entry SHALL carry its own `id`, the catalog `exerciseId`, `effort`, `perSide`, `restMinutes` (or `null`), its `sets` and its `superSeriesId` (or `null`). Entries SHALL NOT carry catalog fields such as `name` or `bodyPart`.

#### Scenario: Workout with exercises and sets
- **WHEN** the owner reads a workout that has exercises with sets
- **THEN** the response is `200 OK` with its `name`, `description`, `status`, every entry, its sets and its superseries membership
- **AND** no entry contains `name` or `bodyPart`

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

### Requirement: Visibility depends on the caller's role
An ADMIN SHALL read any workout. A TRAINER SHALL read workouts they own and workouts where they are the trainer. A USER SHALL read only workouts they own. A caller SHALL NOT read a `DRAFT` workout they own when it has a prescribing trainer. An existing workout outside the caller's scope SHALL be rejected with `403 Forbidden` and its content SHALL NOT be disclosed.

#### Scenario: Admin reads any workout
- **WHEN** an ADMIN reads a workout owned by another user with another trainer
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads a prescribed workout
- **WHEN** a TRAINER reads a workout where they are the trainer
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads a prescribed draft
- **WHEN** a TRAINER reads a `DRAFT` workout where they are the trainer
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads their own workout
- **WHEN** a TRAINER reads a workout they own
- **THEN** the response is `200 OK`

#### Scenario: Trainer reads an unrelated workout
- **WHEN** a TRAINER reads a workout they neither own nor train
- **THEN** the response is `403 Forbidden`

#### Scenario: User reads their own workout
- **WHEN** a USER reads a workout they own
- **THEN** the response is `200 OK`

#### Scenario: User reads their own self-planned draft
- **WHEN** a USER reads a `DRAFT` workout they own that has no trainer
- **THEN** the response is `200 OK`

#### Scenario: User reads a prescribed draft
- **WHEN** a USER reads a `DRAFT` workout they own that has a prescribing trainer
- **THEN** the response is `403 Forbidden`

#### Scenario: User reads a published prescribed workout
- **WHEN** a USER reads a `PUBLISHED`, `STARTED` or `FINISHED` workout they own that has a prescribing trainer
- **THEN** the response is `200 OK`

#### Scenario: User reads another user's workout
- **WHEN** a USER reads a workout owned by someone else, even one where their trainer is set
- **THEN** the response is `403 Forbidden`

### Requirement: Unknown workouts are not found
A well-formed id that matches no workout SHALL be rejected with `404 Not Found`. An id that is not a valid GUID SHALL NOT match the route.

#### Scenario: Missing workout
- **WHEN** an authenticated caller reads a GUID that matches no workout
- **THEN** the response is `404 Not Found`

#### Scenario: Malformed id
- **WHEN** an authenticated caller sends `GET /workouts/not-a-guid`
- **THEN** the response is `404 Not Found`

### Requirement: The workout list hides prescribed drafts from their owner
`GET /workouts` SHALL apply the same draft rule as the read by id: a caller's list SHALL NOT include a `DRAFT` workout they own when it has a prescribing trainer. Every other workout in the caller's role scope SHALL still be listed, with its `status`.

#### Scenario: User list excludes prescribed drafts
- **WHEN** a USER who owns a prescribed `DRAFT` workout and a prescribed `PUBLISHED` workout lists workouts
- **THEN** the list includes the `PUBLISHED` workout
- **AND** the list does not include the `DRAFT` workout

#### Scenario: User list includes self-planned drafts
- **WHEN** a USER who owns a `DRAFT` workout with no trainer lists workouts
- **THEN** the list includes that workout

#### Scenario: Trainer list includes prescribed drafts
- **WHEN** a TRAINER who prescribed a `DRAFT` workout lists workouts
- **THEN** the list includes that workout

#### Scenario: Admin list includes every draft
- **WHEN** an ADMIN lists workouts
- **THEN** the list includes every workout, `DRAFT` ones included

### Requirement: The workout list carries name and description
Every workout returned by `GET /workouts` SHALL include its `name` and its `description` (or `null`).

#### Scenario: Listed workout shows its name
- **WHEN** a caller lists workouts and one of them has a name and a description
- **THEN** that entry carries the same `name` and `description`

#### Scenario: Listed workout without description
- **WHEN** a caller lists workouts and one of them has no description
- **THEN** that entry carries `description` `null`

### Requirement: The workout list carries per side and rest
Each entry in the workout list response SHALL carry `perSide` and `restMinutes` (or `null`), and each `Minutes` set target SHALL carry `minutes`, in the same way as the read-by-id response.

#### Scenario: List entry with per side and rest
- **WHEN** a caller lists workouts that include an entry with perSide `true`, restMinutes `2` and a `1 x Minutes` set of 30 minutes
- **THEN** that entry has `perSide` `true` and `restMinutes` `2`, and its set target has `type` `Minutes` and `minutes` `30`

### Requirement: The workout list names each workout's athlete and trainer
Every item of `GET /workouts` SHALL carry `athlete`, the owner's public profile, and `trainer`, the prescribing trainer's public profile or `null` when the workout has no trainer. Both SHALL match the item's `userId` and `trainerId`, which stay unchanged. Per-role scoping, draft hiding and order SHALL be unchanged.

#### Scenario: Prescribed workout names both people
- **WHEN** an ADMIN lists workouts and one of them has a trainer
- **THEN** that item's `athlete.id` equals its `userId` and its `trainer.id` equals its `trainerId`
- **AND** both carry the user's `email`, `name`, `surname` and `role`

#### Scenario: Self-planned workout has no trainer
- **WHEN** a caller lists workouts and one of them has no trainer
- **THEN** that item has `trainer` `null` and a non-null `athlete`

#### Scenario: User sees themselves and their trainer
- **WHEN** a USER lists their published prescribed workout
- **THEN** the item's `athlete` is the caller and its `trainer` is the prescribing trainer

#### Scenario: Scoping is unchanged
- **WHEN** a USER or TRAINER lists workouts
- **THEN** the list contains exactly the workouts it contained before this change, in the same order

### Requirement: Listed people are resolved regardless of their state
The athlete and trainer of a listed workout SHALL be returned whether or not they are active, and whether or not a trainer–athlete relationship between them is still active.

#### Scenario: Former athlete is still named
- **WHEN** a TRAINER lists workouts after ending the relationship with an athlete they prescribed a workout to
- **THEN** that workout's item still carries the athlete

#### Scenario: Deactivated athlete is still named
- **WHEN** an ADMIN lists workouts after deactivating the owner of one of them
- **THEN** that workout's item still carries the athlete

### Requirement: Listed people expose only their public profile
The `athlete` and `trainer` objects SHALL contain exactly `id`, `email`, `name`, `surname` and `role`. They SHALL NOT contain the password hash, refresh tokens, activation data or trainer relationships.

#### Scenario: No sensitive field
- **WHEN** a caller lists workouts
- **THEN** every `athlete` and non-null `trainer` has exactly the properties `id`, `email`, `name`, `surname` and `role`

### Requirement: Creating a workout keeps its response shape
`POST /workouts` SHALL keep returning its existing workout response, without `athlete` or `trainer`.

#### Scenario: Created workout response
- **WHEN** a caller creates a workout
- **THEN** the `201 Created` body has no `athlete` and no `trainer` property

### Requirement: The workout list item schema is documented
The OpenAPI document SHALL describe the `GET /workouts` `200` item as its own schema, with a required `athlete` and a nullable `trainer`, both referencing the user profile schema. `POST /workouts` SHALL keep referencing the existing workout response schema.

#### Scenario: Item schema
- **WHEN** the OpenAPI document is read
- **THEN** the `GET /workouts` item references `WorkoutListItemResponse`, whose `athlete` and `trainer` reference `UserProfileResponse` and whose `trainer` is nullable
- **AND** the `POST /workouts` `201` body references `WorkoutResponse`

### Requirement: The workout list can be limited to standalone workouts

`GET /workouts` SHALL accept an optional `standalone` boolean query parameter. With `standalone=true` the list SHALL
include only the workouts in the caller's role scope that are not placed in a mesocycle. Without the parameter, or with
`standalone=false`, the list SHALL be the same as before. The parameter SHALL NOT widen what the caller may see.

#### Scenario: Admin lists standalone workouts

- **WHEN** an ADMIN lists workouts with `standalone=true` and two workouts are placed in a mesocycle
- **THEN** the list includes every other workout
- **AND** the list does not include the two placed workouts

#### Scenario: Trainer's standalone list stays within their scope

- **WHEN** a TRAINER lists workouts with and without `standalone=true`
- **THEN** every workout in the standalone list is also in the full list
- **AND** no workout placed in a mesocycle is in the standalone list

#### Scenario: Placed workouts are still listed without the parameter

- **WHEN** a caller lists workouts without `standalone`
- **THEN** workouts placed in a mesocycle are included as before

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
