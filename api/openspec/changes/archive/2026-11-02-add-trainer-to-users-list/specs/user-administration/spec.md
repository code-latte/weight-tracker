# Spec Delta

## MODIFIED Requirements

### Requirement: Listed users expose only public profile fields
Each listed user SHALL contain exactly `id`, `email`, `name`, `surname`, `role`, `isActive`, `deactivatedAtUtc` and `trainer`. `deactivatedAtUtc` SHALL be `null` for an active user. It SHALL NOT contain the password, the password hash, the date of birth, the creation date, or any token.

#### Scenario: Item shape
- **WHEN** an `ADMIN` calls `GET /users`
- **THEN** every item contains exactly `id`, `email`, `name`, `surname`, `role`, `isActive`, `deactivatedAtUtc` and `trainer`

#### Scenario: Deactivated user in the list
- **WHEN** an `ADMIN` deactivates a user and then calls `GET /users`
- **THEN** that user is still listed, with `isActive` `false` and a non-null `deactivatedAtUtc`

## ADDED Requirements

### Requirement: Listed athletes carry their current trainer
In `GET /users`, `trainer` SHALL be the trainer of the user's single `ACTIVE` trainer–athlete relationship when the user's role is `USER`, and `null` otherwise. `ENDED` relationships SHALL be ignored. The trainer SHALL be returned whatever its own activation state.

#### Scenario: Athlete with an active trainer
- **WHEN** a `USER` has an `ACTIVE` relationship with a trainer and an `ADMIN` calls `GET /users`
- **THEN** that user's `trainer` is that trainer

#### Scenario: Athlete whose relationship ended
- **WHEN** a `USER`'s only relationship has been ended and an `ADMIN` calls `GET /users`
- **THEN** that user's `trainer` is `null`

#### Scenario: Athlete never linked
- **WHEN** a `USER` has no trainer–athlete relationship and an `ADMIN` calls `GET /users`
- **THEN** that user's `trainer` is `null`

#### Scenario: Trainers and administrators
- **WHEN** an `ADMIN` calls `GET /users`
- **THEN** every user whose role is `TRAINER` or `ADMIN` has `trainer` `null`

#### Scenario: Deactivated trainer
- **WHEN** a `USER` has an `ACTIVE` relationship with a trainer who has been deactivated and an `ADMIN` calls `GET /users`
- **THEN** that user's `trainer` is still that trainer

### Requirement: A listed trainer exposes only its public profile
A non-null `trainer` in `GET /users` SHALL contain exactly `id`, `email`, `name`, `surname` and `role`. It SHALL NOT contain activation data, the password, the password hash, any token, or a nested trainer.

#### Scenario: Trainer shape
- **WHEN** an `ADMIN` calls `GET /users` and a listed `USER` has an active trainer
- **THEN** that user's `trainer` contains exactly `id`, `email`, `name`, `surname` and `role`

### Requirement: Activation responses do not carry a trainer
`PUT /users/{id}/activate` and `PUT /users/{id}/deactivate` SHALL keep returning the user summary without a `trainer` property.

#### Scenario: Activation response shape
- **WHEN** an `ADMIN` activates or deactivates a user
- **THEN** the response contains exactly `id`, `email`, `name`, `surname`, `role`, `isActive` and `deactivatedAtUtc`
