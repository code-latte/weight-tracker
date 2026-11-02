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
