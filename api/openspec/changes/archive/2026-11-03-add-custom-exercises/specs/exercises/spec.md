# Spec Delta

## MODIFIED Requirements

### Requirement: Exercise reads expose translations, muscle, body part and dataset id
`GET /exercises` and `GET /exercises/{id}` SHALL return each exercise with `id`, `freeExerciseDbId` (or `null`), `videoUrl` (or `null`), `muscle`, `bodyPart`, `type`, `translations` (a list of `{ languageCode, name }`), `ownerId` (or `null` for a system exercise) and `archivedAtUtc` (or `null`). `muscle`, `bodyPart` and `type` SHALL be returned by name. The response SHALL NOT contain a top-level `name`.

#### Scenario: Read a seeded exercise
- **WHEN** an anonymous caller sends `GET /exercises/{id}` for the seeded Sentadilla
- **THEN** the response is `200 OK` with `freeExerciseDbId` `Barbell_Squat`, `muscle` `QUADRICEPS`, `bodyPart` `LEGS`, `type` `STRENGTH` and translations for `es` and `en`
- **AND** `ownerId` and `archivedAtUtc` are `null`
- **AND** the response has no `name` field

#### Scenario: Exercise without translations
- **WHEN** a caller reads an exercise that has no translations
- **THEN** the response is `200 OK` with an empty `translations` list

#### Scenario: List exercises
- **WHEN** an anonymous caller sends `GET /exercises`
- **THEN** every exercise in the response carries its `translations`, `muscle`, `bodyPart`, `type`, `ownerId` and `archivedAtUtc`

#### Scenario: Read a custom exercise
- **WHEN** the owner reads their custom exercise
- **THEN** the response carries `ownerId` equal to the owner's id and `freeExerciseDbId` `null`

## ADDED Requirements

### Requirement: Anonymous callers see only system exercises
A request without an `Authorization` header to `GET /exercises` SHALL return only exercises without an owner. A request without that header to `GET /exercises/{id}` for a custom exercise SHALL respond `404 Not Found`.

#### Scenario: Anonymous list
- **WHEN** an anonymous caller sends `GET /exercises`
- **THEN** every returned exercise has `ownerId` `null`
- **AND** a custom exercise created by the seeded trainer is not returned

#### Scenario: Anonymous read of a custom exercise
- **WHEN** an anonymous caller sends `GET /exercises/{id}` for a custom exercise
- **THEN** the response is `404 Not Found`

### Requirement: Authenticated callers also see their own exercises and those used in their visible workouts
For an authenticated `USER` or `TRAINER`, `GET /exercises` SHALL return every system exercise, every exercise they own, and every custom exercise used in a workout they can see under the workout list scoping (owner or trainer, without prescribed drafts hidden from their owner). Archived exercises SHALL be included. Other custom exercises SHALL be excluded. `GET /exercises/{id}` SHALL follow the same visibility and SHALL respond `404 Not Found` for a custom exercise the caller cannot see.

#### Scenario: Owner sees their exercise
- **WHEN** the seeded user lists exercises with a valid token
- **THEN** the list includes the system exercises and the custom exercise the user created

#### Scenario: Athlete sees their trainer's exercise through a workout
- **WHEN** the seeded user lists exercises and the trainer's custom exercise is used in a published workout prescribed to them
- **THEN** the list includes that exercise

#### Scenario: Exercise used only in a hidden draft
- **WHEN** a trainer's custom exercise is used only in a prescribed `DRAFT` workout of the user
- **THEN** the user's list does not include it
- **AND** `GET /exercises/{id}` for it responds `404 Not Found` to the user

#### Scenario: Another owner's exercise is excluded
- **WHEN** the seeded trainer lists exercises
- **THEN** the list does not include a custom exercise the seeded user created and used in no workout the trainer can see

#### Scenario: Archived own exercise still listed
- **WHEN** an owner lists exercises after archiving one of theirs
- **THEN** the list includes it with a non-null `archivedAtUtc`

### Requirement: Admins see every exercise
For an authenticated `ADMIN`, `GET /exercises` SHALL return every exercise, system and custom, archived included, and `GET /exercises/{id}` SHALL return any exercise.

#### Scenario: Admin list
- **WHEN** an ADMIN lists exercises
- **THEN** the list includes the system exercises and the custom exercises created by the seeded user and trainer

### Requirement: Exercise reads reject a present but invalid token
`GET /exercises` and `GET /exercises/{id}` SHALL respond `401 Unauthorized` when an `Authorization` header is sent with a malformed, wrongly signed or expired token, or with a token whose user no longer exists. They SHALL NOT fall back to the anonymous result.

#### Scenario: Invalid token on the list
- **WHEN** a caller sends `GET /exercises` with a malformed bearer token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token on the read by id
- **WHEN** a caller sends `GET /exercises/{id}` for a system exercise with a malformed bearer token
- **THEN** the response is `401 Unauthorized`

## REMOVED Requirements

### Requirement: Exercises cannot be written through the API
**Reason**: Trainers and athletes may now create, edit and archive their own exercises. The system catalog stays read-only, and the `custom-exercises` capability specifies that.
**Migration**: Use `POST /exercises`, `PUT /exercises/{id}` and `DELETE /exercises/{id}` for custom exercises. System exercises keep changing only through migrations or direct database edits.
