# Spec Delta

## ADDED Requirements

### Requirement: Listed exercises carry their owner and archive date

Each exercise returned by listing the catalog SHALL carry its `ownerId` and `archivedAtUtc` as the API sends them,
unchanged: `ownerId` is `null` for a system exercise and the creator's user id for a custom one, and `archivedAtUtc`
is `null` while the exercise is not archived.

#### Scenario: System exercise

- **WHEN** the API answers with an exercise whose `ownerId` is `null`
- **THEN** the exercise in the successful result has `ownerId` `null`

#### Scenario: Custom exercise

- **WHEN** the API answers with an exercise whose `ownerId` is a user id and whose `archivedAtUtc` is a date
- **THEN** the exercise in the successful result carries that `ownerId` and that `archivedAtUtc`, unchanged

### Requirement: Listing the catalog sends the session when there is one

Listing the catalog SHALL send the stored access token when a session exists, so the API can include the caller's own
exercises, and SHALL still send the request without one when there is no session.

#### Scenario: Signed in

- **WHEN** the catalog is listed with a stored session
- **THEN** the `GET /exercises` request carries that session's access token
