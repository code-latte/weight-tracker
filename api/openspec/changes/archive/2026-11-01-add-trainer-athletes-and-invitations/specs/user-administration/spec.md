# Spec Delta

## RENAMED Requirements

- FROM: `### Requirement: Only administrators and trainers can list users`
- TO: `### Requirement: Only administrators can list users`

## MODIFIED Requirements

### Requirement: Only administrators can list users
The system SHALL expose `GET /users` to authenticated callers whose stored role is `ADMIN`. A caller without a valid access token SHALL get `401 Unauthorized`, and a `USER` or `TRAINER` caller SHALL get `403 Forbidden` with no user data.

#### Scenario: Anonymous caller
- **WHEN** `GET /users` is called without an access token, or with a malformed one
- **THEN** the response is `401 Unauthorized`

#### Scenario: Athlete caller
- **WHEN** a `USER` calls `GET /users`
- **THEN** the response is `403 Forbidden` and contains no user data

#### Scenario: Trainer caller
- **WHEN** a `TRAINER` calls `GET /users`
- **THEN** the response is `403 Forbidden` and contains no user data

## REMOVED Requirements

### Requirement: Trainer listing is not implemented yet
**Reason**: Trainers now list their athletes through `GET /trainer-athletes` (capability `trainer-athletes`), so `GET /users` no longer needs a trainer branch.
**Migration**: A trainer calls `GET /trainer-athletes` instead of `GET /users`, which now answers `403 Forbidden` to trainers.
