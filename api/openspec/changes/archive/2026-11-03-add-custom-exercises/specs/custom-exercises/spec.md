# Spec Delta

## Purpose

Lets trainers and athletes create, edit and archive exercises they own, alongside the read-only system catalog, so workouts can use exercises the catalog does not have.

## ADDED Requirements

### Requirement: Trainers and athletes create their own exercises
`POST /exercises` SHALL let an authenticated `TRAINER` or `USER` create an exercise owned by them, from `languageCode`, `name`, `muscle`, `type` and an optional `videoUrl`. The response SHALL be `201 Created` with the exercise, its new `id`, `ownerId` equal to the caller, exactly one translation in `languageCode`, `freeExerciseDbId` `null` and `archivedAtUtc` `null`.

#### Scenario: Trainer creates an exercise
- **WHEN** the seeded trainer sends `POST /exercises` with `languageCode` `es`, `name` `Remo con banda`, `muscle` `MIDDLE_BACK` and `type` `STRENGTH`
- **THEN** the response is `201 Created` with `ownerId` equal to the trainer, one translation `{ es, Remo con banda }` and `bodyPart` `BACK`
- **AND** the trainer's `GET /exercises` includes it

#### Scenario: Athlete creates an exercise
- **WHEN** the seeded user sends a valid `POST /exercises`
- **THEN** the response is `201 Created` with `ownerId` equal to the user

#### Scenario: Only the sent language is created
- **WHEN** an exercise is created with `languageCode` `en`
- **THEN** its `translations` list contains only the `en` entry

### Requirement: Admins cannot create exercises
`POST /exercises` by an `ADMIN` SHALL respond `403 Forbidden` and create nothing.

#### Scenario: Admin creates
- **WHEN** an ADMIN sends a valid `POST /exercises`
- **THEN** the response is `403 Forbidden`
- **AND** the admin's `GET /exercises` returns the same exercises as before

### Requirement: Exercise input is validated
Create and update SHALL respond `400 Bad Request`, and change nothing, when `languageCode`, `name`, `muscle` or `type` is missing or invalid, or `videoUrl` is not a valid URL. The same name rules and language code rules as the system catalog SHALL apply.

#### Scenario: Missing name
- **WHEN** an exercise is created without `name`
- **THEN** the response is `400 Bad Request`

#### Scenario: Unknown muscle
- **WHEN** an exercise is created with `muscle` `WINGS`
- **THEN** the response is `400 Bad Request`

#### Scenario: Invalid language code
- **WHEN** an exercise is created with `languageCode` `spanish`
- **THEN** the response is `400 Bad Request`

### Requirement: Exercise names are not unique
Creating or updating an exercise SHALL NOT be rejected because another exercise, whoever owns it, already has the same name in the same language.

#### Scenario: Same name as a system exercise
- **WHEN** a trainer creates an exercise named exactly like a system exercise's `es` name
- **THEN** the response is `201 Created`

### Requirement: Owners and admins edit custom exercises
`PUT /exercises/{id}` SHALL let the owner or an `ADMIN` replace the `muscle`, `type` and `videoUrl` of a custom exercise. It SHALL also set the name in `languageCode`: it replaces that translation if it exists and adds it otherwise. Translations in other languages SHALL be kept. The response SHALL be `200 OK` with the updated exercise, and the `id` and `ownerId` SHALL NOT change.

#### Scenario: Owner renames in the same language
- **WHEN** the owner of an exercise with translation `{ es, Remo con banda }` sends `PUT` with `languageCode` `es` and `name` `Remo con goma`
- **THEN** the response is `200 OK` and the only `es` translation is `Remo con goma`

#### Scenario: Owner adds another language
- **WHEN** the owner of an exercise with only an `es` translation sends `PUT` with `languageCode` `en` and `name` `Band row`
- **THEN** the exercise has both the `es` and `en` translations

#### Scenario: Admin edits a custom exercise
- **WHEN** an ADMIN updates a custom exercise created by the seeded trainer
- **THEN** the response is `200 OK` and its `ownerId` is unchanged

### Requirement: Owners and admins archive custom exercises
`DELETE /exercises/{id}` SHALL let the owner or an `ADMIN` archive a custom exercise by setting its `archivedAtUtc`. It SHALL respond `204 No Content`. The exercise SHALL NOT be removed: it SHALL stay readable by the callers who could see it, and workouts that use it SHALL keep reading it.

#### Scenario: Owner archives
- **WHEN** the owner archives their exercise
- **THEN** the response is `204 No Content`
- **AND** the owner's `GET /exercises/{id}` returns it with a non-null `archivedAtUtc`

#### Scenario: Workout keeps an archived exercise
- **WHEN** a trainer archives a custom exercise used in a prescribed workout
- **THEN** `GET /workouts` still returns that entry with the exercise's translations

### Requirement: Archived exercises cannot change
Updating or archiving an archived exercise SHALL respond `409 Conflict` and change nothing.

#### Scenario: Update an archived exercise
- **WHEN** the owner sends `PUT` for an archived exercise
- **THEN** the response is `409 Conflict`

#### Scenario: Archive twice
- **WHEN** the owner archives an already archived exercise
- **THEN** the response is `409 Conflict`

### Requirement: Only the owner or an admin changes a custom exercise
Updating or archiving a custom exercise SHALL respond `403 Forbidden` for a caller who is neither its owner nor an `ADMIN` but can see it, and `404 Not Found` for a caller who cannot see it. A missing exercise id SHALL respond `404 Not Found`.

#### Scenario: Athlete edits their trainer's exercise
- **WHEN** the seeded user, who sees the trainer's exercise through a workout, sends `PUT` for it
- **THEN** the response is `403 Forbidden` and the exercise is unchanged

#### Scenario: Invisible exercise
- **WHEN** a trainer sends `DELETE` for a user's custom exercise that is in no workout the trainer can see
- **THEN** the response is `404 Not Found`

#### Scenario: Unknown id
- **WHEN** a caller sends `PUT /exercises/{id}` for an id that matches no exercise
- **THEN** the response is `404 Not Found`

### Requirement: System exercises stay read-only
Updating or archiving an exercise without an owner SHALL respond `403 Forbidden` for every caller, `ADMIN` included, and change nothing.

#### Scenario: Admin edits a system exercise
- **WHEN** an ADMIN sends `PUT /exercises/{id}` for the seeded Sentadilla
- **THEN** the response is `403 Forbidden`
- **AND** `GET /exercises/{id}` returns the seeded Sentadilla unchanged

#### Scenario: Admin archives a system exercise
- **WHEN** an ADMIN sends `DELETE /exercises/{id}` for the seeded Curl de Bíceps
- **THEN** the response is `403 Forbidden` and it is not archived

### Requirement: Exercise write routes require authentication
`POST /exercises`, `PUT /exercises/{id}` and `DELETE /exercises/{id}` SHALL each respond `401 Unauthorized` without a token, with an invalid token, and with a token whose user no longer exists.

#### Scenario: No token
- **WHEN** any exercise write route is called without a bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** any exercise write route is called with a malformed bearer token
- **THEN** the response is `401 Unauthorized`
