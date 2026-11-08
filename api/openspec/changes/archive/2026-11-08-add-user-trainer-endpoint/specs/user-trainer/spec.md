# Spec Delta

## Purpose

Lets a user look up their own current trainer, and an administrator look up the current trainer of any user, from the user's active trainer-athlete relationship.

## ADDED Requirements

### Requirement: Read a user's current trainer
The system SHALL expose `GET /users/{userId}/trainer`. When the user has an active trainer-athlete relationship, it SHALL respond `200 OK` with the trainer's public profile: `id`, `email`, `name`, `surname` and `role`. The trainer SHALL be returned whatever their activation state.

#### Scenario: Athlete reads their trainer
- **WHEN** the seeded user, who has an active relationship with the seeded trainer, reads `/users/{their id}/trainer`
- **THEN** the response is `200 OK` with the seeded trainer's `id`, `email`, `name`, `surname` and role `TRAINER`

#### Scenario: Only the public profile is exposed
- **WHEN** a caller allowed to read a user's trainer reads it
- **THEN** the body carries exactly `id`, `email`, `name`, `surname` and `role`, and no activation, relationship or authentication data

#### Scenario: Deactivated trainer
- **WHEN** the user's trainer has been deactivated and the user reads their trainer
- **THEN** the response is `200 OK` with that trainer's profile

### Requirement: A user without an active trainer gets no content
When the user has no active trainer-athlete relationship, the route SHALL respond `204 No Content` with an empty body. This includes:
- a user with no relationship;
- a user whose relationships have all ended;
- a user whose role is `TRAINER` or `ADMIN`, who cannot be an athlete.

#### Scenario: User whose relationship ended
- **WHEN** the active relationship of the seeded user is ended and the user reads their trainer
- **THEN** the response is `204 No Content` with an empty body

#### Scenario: User who never had a trainer
- **WHEN** a user with no relationship reads their trainer
- **THEN** the response is `204 No Content` with an empty body

#### Scenario: Admin asks about a trainer
- **WHEN** an admin reads the trainer of the seeded trainer
- **THEN** the response is `204 No Content` with an empty body

### Requirement: Only the user themselves or an admin may read a user's trainer
A caller with role `USER` SHALL be allowed to read only their own trainer. A caller with role `ADMIN` SHALL be allowed to read the trainer of any user. Every other caller, including every `TRAINER` even for their own id, SHALL be rejected with `403 Forbidden`.

#### Scenario: Admin reads an athlete's trainer
- **WHEN** an admin reads the trainer of the seeded user
- **THEN** the response is `200 OK` with the seeded trainer's profile

#### Scenario: User asks about another user
- **WHEN** the seeded user reads the trainer of another existing user
- **THEN** the response is `403 Forbidden`

#### Scenario: Trainer asks about their athlete
- **WHEN** the seeded trainer reads the trainer of the seeded user
- **THEN** the response is `403 Forbidden`

#### Scenario: Trainer asks about themselves
- **WHEN** the seeded trainer reads `/users/{their id}/trainer`
- **THEN** the response is `403 Forbidden`

### Requirement: The route does not reveal which users exist
The permission check SHALL run before the user is looked up. A `USER` asking about any id other than their own SHALL get `403 Forbidden` whether that id exists or not. Only an admin SHALL get `404 Not Found` for a well-formed id that matches no user.

#### Scenario: User asks about an unknown id
- **WHEN** the seeded user reads the trainer of a GUID that matches no user
- **THEN** the response is `403 Forbidden`

#### Scenario: Admin asks about an unknown id
- **WHEN** an admin reads the trainer of a GUID that matches no user
- **THEN** the response is `404 Not Found`

### Requirement: The user trainer route requires authentication
The route SHALL reject a request without a token or with an invalid token with `401 Unauthorized`.

#### Scenario: No token
- **WHEN** a caller sends `GET /users/{userId}/trainer` without an access token
- **THEN** the response is `401 Unauthorized`

#### Scenario: Invalid token
- **WHEN** a caller sends `GET /users/{userId}/trainer` with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized`
