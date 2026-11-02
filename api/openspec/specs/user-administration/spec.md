# user-administration Specification

## Purpose

Lets an administrator manage other people's accounts. It starts with creating a user of any role on someone
else's behalf, without opening a session for that user.

## Requirements

### Requirement: Only administrators can create users
The system SHALL expose `POST /users` to authenticated callers whose stored role is `ADMIN`. A caller without a valid access token SHALL get `401 Unauthorized`, a `USER` or `TRAINER` caller SHALL get `403 Forbidden`, and in both cases no user SHALL be created.

#### Scenario: Anonymous caller
- **WHEN** `POST /users` is called without an access token, or with a malformed one
- **THEN** the response is `401 Unauthorized` and no user is created

#### Scenario: Athlete caller
- **WHEN** a `USER` calls `POST /users` with a valid body
- **THEN** the response is `403 Forbidden` and no user is created

#### Scenario: Trainer caller
- **WHEN** a `TRAINER` calls `POST /users` with a valid body
- **THEN** the response is `403 Forbidden` and no user is created

### Requirement: An administrator creates a user of any role
`POST /users` SHALL create a user with the given `email`, `password` and `role`, where `role` is any of `USER`, `TRAINER` or `ADMIN`. The created user SHALL be able to log in with that email and password.

#### Scenario: Create a user of each role
- **WHEN** an `ADMIN` calls `POST /users` with a new email, a strong password and role `USER`, `TRAINER` or `ADMIN`
- **THEN** the response is `201 Created`, the user is stored with that role, and `POST /auth/login` with that email and password succeeds

### Requirement: Name, surname and date of birth are optional
`name`, `surname` and `dateBirth` MAY be omitted. When given, they SHALL be stored; `name` and `surname` are trimmed. When omitted, `name` and `surname` SHALL be stored empty and `dateBirth` SHALL be stored as `0001-01-01`, the same unset values signup uses.

#### Scenario: Only required fields
- **WHEN** an `ADMIN` calls `POST /users` with only `email`, `password` and `role`
- **THEN** the response is `201 Created` with empty `name` and `surname` and `dateBirth` `0001-01-01`

#### Scenario: All fields
- **WHEN** an `ADMIN` calls `POST /users` with `email`, `password`, `role`, `name`, `surname` and `dateBirth`
- **THEN** the response is `201 Created` and echoes the given values, with `name` and `surname` trimmed

### Requirement: The creation result carries no authentication data
A successful `POST /users` SHALL respond with the created user's `id`, `email`, `name`, `surname`, `dateBirth` and `role` only. It SHALL NOT contain an access token, a refresh token, a password or a password hash, and SHALL NOT start a session for the created user.

#### Scenario: Response shape
- **WHEN** an `ADMIN` creates a user successfully
- **THEN** the body contains exactly `id`, `email`, `name`, `surname`, `dateBirth` and `role`, and no token or password field

#### Scenario: No session for the created user
- **WHEN** an `ADMIN` creates a user successfully
- **THEN** no refresh token exists for the created user

### Requirement: Invalid creation input is rejected without changes
`POST /users` SHALL respond `400 Bad Request` and create no user when `email`, `password` or `role` is missing, the email is malformed, the password is not strong enough, the role is not a known value, or a given `name` or `surname` is empty, whitespace only, or longer than 100 characters.

#### Scenario: Malformed email
- **WHEN** an `ADMIN` calls `POST /users` with an email that is not a valid address
- **THEN** the response is `400 Bad Request` and no user is created

#### Scenario: Weak password
- **WHEN** an `ADMIN` calls `POST /users` with a password that does not meet the strength rules
- **THEN** the response is `400 Bad Request` and no user is created

#### Scenario: Missing required field
- **WHEN** an `ADMIN` calls `POST /users` without `email`, `password` or `role`
- **THEN** the response is `400 Bad Request` and no user is created

#### Scenario: Unknown role
- **WHEN** an `ADMIN` calls `POST /users` with a `role` that is not `USER`, `TRAINER` or `ADMIN`
- **THEN** the response is `400 Bad Request` and no user is created

#### Scenario: Invalid name
- **WHEN** an `ADMIN` calls `POST /users` with a `name` or `surname` that is whitespace only or longer than 100 characters
- **THEN** the response is `400 Bad Request` and no user is created

### Requirement: Email must be unique
`POST /users` SHALL respond `409 Conflict` and create no user when a user with the same email already exists.

#### Scenario: Email already registered
- **WHEN** an `ADMIN` calls `POST /users` with the email of an existing user
- **THEN** the response is `409 Conflict` and the existing user is unchanged

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

### Requirement: An administrator lists every user
For an `ADMIN` caller, `GET /users` SHALL return `200 OK` with every stored user, whatever their role, including the caller, ordered by email ascending.

#### Scenario: Users of every role
- **WHEN** an `ADMIN` calls `GET /users`
- **THEN** the response is `200 OK` and lists every stored user, including users with role `USER`, `TRAINER` and `ADMIN` and the caller

#### Scenario: A newly created user is listed
- **WHEN** an `ADMIN` creates a user and then calls `GET /users`
- **THEN** the created user is in the list

#### Scenario: Order
- **WHEN** an `ADMIN` calls `GET /users`
- **THEN** the users are ordered by email ascending

### Requirement: Listed users expose only public profile fields
Each listed user SHALL contain exactly `id`, `email`, `name`, `surname`, `role`, `isActive`, `deactivatedAtUtc` and `trainer`. `deactivatedAtUtc` SHALL be `null` for an active user. It SHALL NOT contain the password, the password hash, the date of birth, the creation date, or any token.

#### Scenario: Item shape
- **WHEN** an `ADMIN` calls `GET /users`
- **THEN** every item contains exactly `id`, `email`, `name`, `surname`, `role`, `isActive`, `deactivatedAtUtc` and `trainer`

#### Scenario: Deactivated user in the list
- **WHEN** an `ADMIN` deactivates a user and then calls `GET /users`
- **THEN** that user is still listed, with `isActive` `false` and a non-null `deactivatedAtUtc`

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

### Requirement: Users start active
Every user SHALL start active with no deactivation date, whether created by signup, by an administrator, or already stored before this change.

#### Scenario: Created user is active
- **WHEN** an `ADMIN` creates a user and then calls `GET /users`
- **THEN** that user has `isActive` `true` and `deactivatedAtUtc` `null`

#### Scenario: Seeded users are active
- **WHEN** an `ADMIN` calls `GET /users` on a freshly migrated and seeded database
- **THEN** every user has `isActive` `true` and `deactivatedAtUtc` `null`

### Requirement: Only administrators can activate or deactivate users
The system SHALL expose `PUT /users/{id}/activate` and `PUT /users/{id}/deactivate` to `ADMIN` callers only. A caller without a valid access token SHALL get `401 Unauthorized`, and a `USER` or `TRAINER` caller SHALL get `403 Forbidden` with no change.

#### Scenario: Anonymous caller
- **WHEN** either route is called without an access token, or with a malformed one
- **THEN** the response is `401 Unauthorized`

#### Scenario: Athlete caller
- **WHEN** a `USER` calls either route for an existing user
- **THEN** the response is `403 Forbidden` and the target user is unchanged

#### Scenario: Trainer caller
- **WHEN** a `TRAINER` calls either route for an existing user
- **THEN** the response is `403 Forbidden` and the target user is unchanged

### Requirement: An administrator deactivates an active user
`PUT /users/{id}/deactivate` SHALL mark an active user as deactivated, set `deactivatedAtUtc` to the current UTC time, and return `200 OK` with the updated user item.

#### Scenario: Deactivate an active user
- **WHEN** an `ADMIN` deactivates an active `USER`
- **THEN** the response is `200 OK` with `isActive` `false` and a non-null `deactivatedAtUtc`
- **AND** `GET /users` shows the same state for that user

#### Scenario: Deactivate a trainer
- **WHEN** an `ADMIN` deactivates an active `TRAINER`
- **THEN** the response is `200 OK` with `isActive` `false`

### Requirement: An already deactivated user cannot be deactivated again
Deactivating a user who is already deactivated SHALL be rejected with `409 Conflict` and SHALL NOT change the stored deactivation date.

#### Scenario: Deactivate twice
- **WHEN** an `ADMIN` deactivates a user who is already deactivated
- **THEN** the response is `409 Conflict`
- **AND** `deactivatedAtUtc` keeps its original value

### Requirement: An administrator cannot deactivate themselves
`PUT /users/{id}/deactivate` with the caller's own id SHALL be rejected with `403 Forbidden` and change nothing.

#### Scenario: Admin targets themselves
- **WHEN** an `ADMIN` calls `PUT /users/{id}/deactivate` with their own id
- **THEN** the response is `403 Forbidden`
- **AND** the admin stays active and can still log in

### Requirement: An administrator activates a deactivated user
`PUT /users/{id}/activate` SHALL mark a deactivated user as active, clear `deactivatedAtUtc`, and return `200 OK` with the updated user item.

#### Scenario: Reactivate a user
- **WHEN** an `ADMIN` activates a deactivated user
- **THEN** the response is `200 OK` with `isActive` `true` and `deactivatedAtUtc` `null`

#### Scenario: Reactivated user can log in
- **WHEN** a user is deactivated, then activated, and then logs in with valid credentials
- **THEN** the login succeeds

### Requirement: An already active user cannot be activated
Activating a user who is already active SHALL be rejected with `409 Conflict` and change nothing.

#### Scenario: Activate an active user
- **WHEN** an `ADMIN` activates a user who is active
- **THEN** the response is `409 Conflict`

### Requirement: Activation targets must exist
Both routes SHALL answer `404 Not Found` when no user has the given id, and when the id is not a valid identifier, since such a path matches no route.

#### Scenario: Unknown user
- **WHEN** an `ADMIN` calls either route with an id that matches no user
- **THEN** the response is `404 Not Found`

#### Scenario: Malformed id
- **WHEN** an `ADMIN` calls either route with an id that is not a valid identifier
- **THEN** the response is `404 Not Found`

### Requirement: Activation responses do not carry a trainer
`PUT /users/{id}/activate` and `PUT /users/{id}/deactivate` SHALL keep returning the user summary without a `trainer` property.

#### Scenario: Activation response shape
- **WHEN** an `ADMIN` activates or deactivates a user
- **THEN** the response contains exactly `id`, `email`, `name`, `surname`, `role`, `isActive` and `deactivatedAtUtc`

### Requirement: Deactivated users cannot log in
`POST /auth/login` with valid credentials of a deactivated user SHALL respond `403 Forbidden` with a problem body whose `code` is `USER_DEACTIVATED`, and SHALL NOT issue any token. Invalid credentials SHALL keep answering `401 Unauthorized` without revealing the account state.

#### Scenario: Valid credentials of a deactivated user
- **WHEN** a deactivated user logs in with the correct email and password
- **THEN** the response is `403 Forbidden` with `code` `USER_DEACTIVATED`
- **AND** the body contains no access token or refresh token

#### Scenario: Wrong password of a deactivated user
- **WHEN** someone logs in with a deactivated user's email and a wrong password
- **THEN** the response is `401 Unauthorized` with no `code` `USER_DEACTIVATED`

### Requirement: Deactivated users cannot renew sessions
`POST /auth/refresh` with an otherwise valid refresh token of a deactivated user SHALL respond `403 Forbidden` with `code` `USER_DEACTIVATED` and SHALL NOT issue new tokens.

#### Scenario: Refresh after deactivation
- **WHEN** a user logs in, an `ADMIN` deactivates them, and they call `POST /auth/refresh` with their refresh token
- **THEN** the response is `403 Forbidden` with `code` `USER_DEACTIVATED`
- **AND** the body contains no tokens
