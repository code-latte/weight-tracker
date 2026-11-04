# exercise-favorites Specification

## Purpose

Lets every user keep a personal list of favorite exercises, with the date each was added, so clients can filter the exercise list by them.

## Requirements

### Requirement: Users mark exercises as favorites
`PUT /exercise-favorites/{exerciseId}` SHALL let an authenticated user of any role mark an exercise as their favorite. The response SHALL be `200 OK` with `exerciseId` and `createdAtUtc`, the moment the favorite was stored.

#### Scenario: Athlete marks a system exercise
- **WHEN** the seeded user sends `PUT /exercise-favorites/{id}` for the seeded Sentadilla
- **THEN** the response is `200 OK` with that `exerciseId` and a `createdAtUtc`
- **AND** the user's `GET /exercise-favorites` includes it

#### Scenario: Trainer marks their own custom exercise
- **WHEN** a trainer marks a custom exercise they created
- **THEN** the response is `200 OK`

#### Scenario: Admin marks any exercise
- **WHEN** an ADMIN marks a custom exercise created by another user
- **THEN** the response is `200 OK`

### Requirement: Marking a favorite is idempotent
Marking an exercise that is already a favorite of the caller SHALL respond `200 OK` with the stored favorite, keep its original `createdAtUtc`, and store no second favorite.

#### Scenario: Mark twice
- **WHEN** a user marks the same exercise twice
- **THEN** both responses are `200 OK` with the same `createdAtUtc`
- **AND** `GET /exercise-favorites` lists that exercise once

### Requirement: Only visible, unarchived exercises can be marked
Marking an exercise that does not exist, or a custom exercise the caller cannot see under the exercise visibility rule, SHALL respond `404 Not Found`. Marking an archived exercise SHALL respond `409 Conflict`. Neither SHALL store a favorite.

#### Scenario: Unknown exercise
- **WHEN** a user marks an id that matches no exercise
- **THEN** the response is `404 Not Found`

#### Scenario: Invisible custom exercise
- **WHEN** a trainer marks a custom exercise created by a user that is in no workout the trainer can see
- **THEN** the response is `404 Not Found`

#### Scenario: Athlete marks their trainer's exercise through a workout
- **WHEN** the seeded user marks a custom exercise of the seeded trainer used in a published workout prescribed to them
- **THEN** the response is `200 OK`

#### Scenario: Archived exercise
- **WHEN** an owner marks their archived custom exercise
- **THEN** the response is `409 Conflict` and the exercise is not in their favorites

### Requirement: Users remove their favorites
`DELETE /exercise-favorites/{exerciseId}` SHALL remove the exercise from the caller's favorites and respond `204 No Content`. When it is not a favorite, the response SHALL still be `204 No Content`. An exercise that does not exist, or that the caller cannot see, SHALL respond `404 Not Found`. A favorite whose exercise is archived SHALL still be removable.

#### Scenario: Remove a favorite
- **WHEN** a user removes an exercise from their favorites
- **THEN** the response is `204 No Content`
- **AND** `GET /exercise-favorites` no longer lists it

#### Scenario: Remove twice
- **WHEN** a user removes an exercise that is not a favorite
- **THEN** the response is `204 No Content`

#### Scenario: Remove an unknown exercise
- **WHEN** a user removes an id that matches no exercise
- **THEN** the response is `404 Not Found`

#### Scenario: Remove after archiving
- **WHEN** an owner removes a favorite whose exercise they archived after marking it
- **THEN** the response is `204 No Content`

### Requirement: Users list only their own favorites
`GET /exercise-favorites` SHALL respond `200 OK` with every favorite of the caller, newest `createdAtUtc` first, each as `{ exerciseId, createdAtUtc }`, without exercise data. It SHALL NOT include another user's favorites, for any role, `ADMIN` included. A favorite SHALL stay listed when its exercise is archived or stops being visible to the caller.

#### Scenario: Newest first
- **WHEN** a user marks exercise A and then exercise B
- **THEN** `GET /exercise-favorites` lists B before A

#### Scenario: Other users' favorites are excluded
- **WHEN** the seeded trainer and the seeded user each mark a different exercise, and the user lists favorites
- **THEN** only the user's exercise is listed

#### Scenario: Admin sees only their own
- **WHEN** a user marks an exercise and an ADMIN lists favorites
- **THEN** the admin's list does not contain it

#### Scenario: No favorites
- **WHEN** a user who marked nothing lists favorites
- **THEN** the response is `200 OK` with an empty list

#### Scenario: Archived favorite stays listed
- **WHEN** an owner archives an exercise they marked
- **THEN** `GET /exercise-favorites` still lists it

### Requirement: Favorite routes require authentication
`PUT /exercise-favorites/{exerciseId}`, `DELETE /exercise-favorites/{exerciseId}` and `GET /exercise-favorites` SHALL each respond `401 Unauthorized` without a token, with an invalid token, and with a token whose user no longer exists.

#### Scenario: No token
- **WHEN** any favorite route is called without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** any favorite route is called with a malformed bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: User no longer exists
- **WHEN** any favorite route is called with a valid token whose user was deleted
- **THEN** the response is `401 Unauthorized`
