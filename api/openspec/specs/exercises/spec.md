# exercises Specification

## Purpose

Defines the exercises that workouts are built from: their names per language, the muscle they work and the body part derived from it, and their link to the free exercise dataset. The system catalog is changed only through migrations or direct database edits; custom exercises are owned by users, and both are read through `/exercises` according to who is asking.

## Requirements

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

### Requirement: Workout listings embed translations and muscle
`GET /workouts` SHALL return, for every workout entry, the referenced exercise's `translations`, `muscle`, `bodyPart` and `type` in place of the former `name`. Entries SHALL NOT contain `name`. `GET /workouts/{id}` is unchanged and carries no exercise data.

#### Scenario: Listing a workout with exercises
- **WHEN** the owner sends `GET /workouts` and has a workout with the seeded Press Banca
- **THEN** that entry carries the translations of Press Banca, `muscle` `CHEST`, `bodyPart` `CHEST` and `type` `STRENGTH`
- **AND** the entry has no `name` field

### Requirement: Body part is derived from the stored muscle
The `bodyPart` of an exercise SHALL always be the body part mapped from its stored `muscle`. Every muscle SHALL map to exactly one body part. Only the muscle SHALL be stored.

#### Scenario: Muscle mapping
- **WHEN** an exercise has a given muscle
- **THEN** its `bodyPart` is `CORE` for `ABDOMINALS` and `LOWER_BACK`; `LEGS` for `ABDUCTORS`, `ADDUCTORS`, `CALVES`, `GLUTES`, `HAMSTRINGS` and `QUADRICEPS`; `ARMS` for `BICEPS`, `FOREARMS` and `TRICEPS`; `CHEST` for `CHEST`; `BACK` for `LATS`, `MIDDLE_BACK` and `TRAPS`; `NECK` for `NECK`; `SHOULDERS` for `SHOULDERS`

#### Scenario: Body part follows the stored muscle
- **WHEN** a caller reads an exercise whose stored muscle is `HAMSTRINGS`
- **THEN** the response reports `bodyPart` `LEGS`

### Requirement: Every exercise has one mandatory type
Every exercise SHALL have exactly one type out of `STRENGTH`, `CARDIO` or `STRETCHING`, stored with the exercise. An exercise without a type, or with any other value, SHALL NOT exist in the catalog.

#### Scenario: Cardio exercise
- **WHEN** a caller reads the catalog exercise `Running_Treadmill`
- **THEN** its `type` is `CARDIO`

#### Scenario: Stretching exercise
- **WHEN** a caller reads a catalog exercise that the source dataset classifies as stretching
- **THEN** its `type` is `STRETCHING`

#### Scenario: Whole catalog is typed
- **WHEN** the database has been migrated
- **THEN** every catalog exercise has a type, with 739 `STRENGTH`, 14 `CARDIO` and 123 `STRETCHING`

### Requirement: Type is independent of muscle and body part
The type of an exercise SHALL NOT be derived from, nor change, its `muscle` or `bodyPart`. Exercises of any type SHALL keep the muscle and body part they had before types existed.

#### Scenario: Cardio keeps its muscle
- **WHEN** a caller reads `Running_Treadmill`
- **THEN** it reports `muscle` `QUADRICEPS`, `bodyPart` `LEGS` and `type` `CARDIO`

### Requirement: Type comes from the catalog source category
Each catalog exercise's type SHALL follow its source dataset category: `cardio` is `CARDIO`; `stretching` is `STRETCHING`; `strength`, `powerlifting`, `olympic weightlifting`, `strongman` and `plyometrics` are `STRENGTH`.

#### Scenario: Plyometrics is strength
- **WHEN** a caller reads a catalog exercise whose source category is `plyometrics`
- **THEN** its `type` is `STRENGTH`

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
