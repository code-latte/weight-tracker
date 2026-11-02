# Spec Delta

## ADDED Requirements

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
