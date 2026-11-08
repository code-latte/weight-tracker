# Spec Delta

## ADDED Requirements

### Requirement: Reading a user's trainer returns the trainer or none

Reading a user's current trainer SHALL send the user's id in the request route and return a successful result holding
the trainer's profile as the API answers it. When the API answers that the user has no active trainer, the result
SHALL succeed with no trainer instead of failing. It SHALL NOT check the caller's role.

#### Scenario: User with a trainer

- **WHEN** the trainer of user `u-1` is read and the API answers `200` with the trainer's profile
- **THEN** the request goes to the trainer route of `u-1`
- **AND** the result succeeds holding that profile unchanged

#### Scenario: User without a trainer

- **WHEN** the trainer of a user is read and the API answers `204`
- **THEN** the result succeeds holding no trainer

### Requirement: Trainer read failures are returned as domain error codes

Reading a user's trainer SHALL NOT throw. A failed request SHALL return a failed result whose code comes from the
shared HTTP error mapping.

#### Scenario: Not signed in or asking about someone else

- **WHEN** the API answers `401` or `403`
- **THEN** the result fails with code `UNAUTHORIZED` or `FORBIDDEN` respectively

#### Scenario: Unknown user

- **WHEN** the API answers `404`
- **THEN** the result fails with code `NOT_FOUND`

#### Scenario: Server error or no connection

- **WHEN** the API answers `500`, or the request cannot reach the API
- **THEN** the result fails with code `UNEXPECTED` or `NETWORK` respectively
