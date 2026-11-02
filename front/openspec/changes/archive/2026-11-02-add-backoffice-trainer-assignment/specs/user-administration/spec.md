# Spec Delta

## MODIFIED Requirements

### Requirement: Listing users returns what the API answers

Listing users SHALL send `GET /users` with no body and SHALL return a successful result holding the users the API
answers with, unchanged and in the same order: id, email, name, surname, role, whether the user is active, when the
user was deactivated (empty for an active user), and the user's current trainer (empty when the API answers none). It
SHALL NOT filter by role, by activation state or by trainer.

#### Scenario: Users listed

- **WHEN** users are listed and the API answers with two users
- **THEN** exactly one `GET /users` request is sent with no body
- **AND** the result is successful and holds those two users in the API's order

#### Scenario: Empty list

- **WHEN** users are listed and the API answers with an empty list
- **THEN** the result is successful and holds an empty list

#### Scenario: Activation state kept

- **WHEN** users are listed and the API answers with one active user and one user deactivated on "2026-10-20T09:30:00Z"
- **THEN** the result holds the first user as active with no deactivation date
- **AND** the second as inactive with the deactivation date "2026-10-20T09:30:00Z", unchanged

#### Scenario: Current trainer kept

- **WHEN** users are listed and the API answers with one athlete whose trainer is "Ana García" and one user with no
  trainer
- **THEN** the result holds the first user with that trainer's id, email, name, surname and role, unchanged
- **AND** the second with no trainer
