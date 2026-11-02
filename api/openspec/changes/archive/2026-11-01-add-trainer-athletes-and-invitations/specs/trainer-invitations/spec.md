# Spec Delta

## Purpose

Lets a trainer invite an email address to become their athlete, and lets the invited user accept or reject that
invitation, or the trainer revoke it, while the invitation is pending and not expired.

## ADDED Requirements

### Requirement: Invitation routes require authentication
`POST /athlete-invitation`, `PUT /athlete-invitation/{invitationId}/revoke`, `PUT /athlete-invitation/{invitationId}/accept` and `PUT /athlete-invitation/{invitationId}/reject` SHALL respond `401 Unauthorized` without an access token or with an invalid one, and SHALL change no invitation.

#### Scenario: No token
- **WHEN** any invitation route is called without an access token
- **THEN** the response is `401 Unauthorized` and no invitation is created or changed

#### Scenario: Invalid token
- **WHEN** any invitation route is called with a malformed or wrongly signed access token
- **THEN** the response is `401 Unauthorized` and no invitation is created or changed

### Requirement: Only trainers can invite
`POST /athlete-invitation` SHALL be available only to callers whose role is `TRAINER`. `USER` and `ADMIN` callers SHALL get `403 Forbidden` and no invitation SHALL be created.

#### Scenario: Athlete caller
- **WHEN** a `USER` calls `POST /athlete-invitation` with a valid email
- **THEN** the response is `403 Forbidden` and no invitation is created

#### Scenario: Admin caller
- **WHEN** an `ADMIN` calls `POST /athlete-invitation` with a valid email
- **THEN** the response is `403 Forbidden` and no invitation is created

### Requirement: A trainer invites a registered athlete or an unregistered email
For a `TRAINER` caller, `POST /athlete-invitation` with body `{ "email": "..." }` SHALL create a `PENDING` invitation from the caller to that email and respond `201 Created`. When the email belongs to a registered `USER`, the invitation SHALL carry that user's id; otherwise its user id SHALL be `null`.

#### Scenario: Registered athlete
- **WHEN** a `TRAINER` invites the email of a registered, active `USER` with no active trainer
- **THEN** the response is `201 Created` with status `PENDING`, the invited email, and `userId` equal to that user's id

#### Scenario: Unregistered email
- **WHEN** a `TRAINER` invites an email that no user has
- **THEN** the response is `201 Created` with status `PENDING` and `userId` `null`

### Requirement: The invitation token is returned only once
The `201` response of `POST /athlete-invitation` SHALL include the raw invitation `token`, `id`, `inviteEmail`, `userId`, `status`, `createdAtUtc` and `expiresAtUtc`. Only a hash of the token SHALL be stored. No other response SHALL contain the token or its hash.

#### Scenario: Token in the creation response
- **WHEN** a `TRAINER` creates an invitation
- **THEN** the response contains a non-empty `token`, and `expiresAtUtc` is later than `createdAtUtc`

#### Scenario: No token afterwards
- **WHEN** the invitation is later revoked, accepted or rejected
- **THEN** that response contains neither the token nor its hash

### Requirement: Invitation expiry is configurable
An invitation SHALL expire at its creation time plus the number of days configured in `TrainerInvitations:ExpirationDays`.

#### Scenario: Expiry from configuration
- **WHEN** a `TRAINER` creates an invitation
- **THEN** `expiresAtUtc` equals `createdAtUtc` plus the configured number of days

### Requirement: Only athlete emails can be invited
`POST /athlete-invitation` SHALL respond `409 Conflict` and create no invitation when the email belongs to a registered `TRAINER` or `ADMIN`, or to a deactivated `USER`.

#### Scenario: Email of a trainer
- **WHEN** a `TRAINER` invites the email of a registered `TRAINER`, including their own
- **THEN** the response is `409 Conflict` and no invitation is created

#### Scenario: Email of an admin
- **WHEN** a `TRAINER` invites the email of a registered `ADMIN`
- **THEN** the response is `409 Conflict` and no invitation is created

#### Scenario: Deactivated athlete
- **WHEN** a `TRAINER` invites the email of a deactivated `USER`
- **THEN** the response is `409 Conflict` and no invitation is created

### Requirement: An athlete with an active trainer cannot be invited
`POST /athlete-invitation` SHALL respond `409 Conflict` and create no invitation when the email belongs to a `USER` who already has an active trainer–athlete relationship with any trainer.

#### Scenario: Athlete already linked
- **WHEN** a `TRAINER` invites the email of a `USER` with an active relationship
- **THEN** the response is `409 Conflict` and no invitation is created

### Requirement: No duplicate pending invitations
`POST /athlete-invitation` SHALL respond `409 Conflict` when the same trainer already has a `PENDING`, non-expired invitation to the same email. An expired, revoked, accepted or rejected invitation SHALL NOT block a new one.

#### Scenario: Pending invitation exists
- **WHEN** a `TRAINER` invites an email they already invited and that invitation is still pending and not expired
- **THEN** the response is `409 Conflict` and no new invitation is created

#### Scenario: Previous invitation revoked
- **WHEN** a `TRAINER` revokes an invitation and invites the same email again
- **THEN** the response is `201 Created`

#### Scenario: Another trainer's pending invitation
- **WHEN** a `TRAINER` invites an email that a different trainer has a pending invitation for
- **THEN** the response is `201 Created`

### Requirement: Invalid invitation input is rejected
`POST /athlete-invitation` SHALL respond `400 Bad Request` and create no invitation when the body or `email` is missing or the email is malformed.

#### Scenario: Malformed email
- **WHEN** a `TRAINER` calls `POST /athlete-invitation` with `email` `not-an-email`
- **THEN** the response is `400 Bad Request` and no invitation is created

#### Scenario: Missing email
- **WHEN** a `TRAINER` calls `POST /athlete-invitation` with an empty body
- **THEN** the response is `400 Bad Request` and no invitation is created

### Requirement: Only the inviting trainer can revoke
`PUT /athlete-invitation/{invitationId}/revoke` SHALL respond `403 Forbidden` and change nothing when the caller is not the trainer who sent the invitation, whatever their role. The permission check SHALL run before any status or expiry check.

#### Scenario: Another trainer
- **WHEN** a `TRAINER` who did not send the invitation revokes it
- **THEN** the response is `403 Forbidden` and the invitation is unchanged

#### Scenario: Admin or athlete
- **WHEN** an `ADMIN` or a `USER` revokes an invitation
- **THEN** the response is `403 Forbidden` and the invitation is unchanged

### Requirement: The inviting trainer revokes a pending invitation
For the inviting trainer, `PUT /athlete-invitation/{invitationId}/revoke` SHALL move a `PENDING`, non-expired invitation to `REVOKED` and respond `200 OK` with the invitation.

#### Scenario: Revoke
- **WHEN** the inviting trainer revokes a pending invitation
- **THEN** the response is `200 OK` with status `REVOKED`

### Requirement: Only the invited user can accept or reject
`PUT /athlete-invitation/{invitationId}/accept` and `/reject` SHALL respond `403 Forbidden` and change nothing unless the caller is a `USER` whose id is the invitation's user id. An invitation with a `null` user id SHALL be refused for every caller. This check SHALL run before the token, status and expiry checks.

#### Scenario: Another athlete
- **WHEN** a `USER` who is not the invited user accepts or rejects an invitation
- **THEN** the response is `403 Forbidden` and the invitation is unchanged

#### Scenario: Trainer or admin
- **WHEN** a `TRAINER` or an `ADMIN` accepts or rejects an invitation
- **THEN** the response is `403 Forbidden` and the invitation is unchanged

#### Scenario: Unregistered invitation
- **WHEN** any `USER` accepts or rejects an invitation whose `userId` is `null`
- **THEN** the response is `403 Forbidden` and the invitation is unchanged

### Requirement: Accept and reject require the invitation token
`PUT /athlete-invitation/{invitationId}/accept` and `/reject` SHALL take body `{ "token": "..." }`. A missing token SHALL get `400 Bad Request`. A token that does not match the invitation SHALL get `403 Forbidden`, and the invitation SHALL be unchanged.

#### Scenario: Wrong token
- **WHEN** the invited user accepts or rejects with a token that does not match
- **THEN** the response is `403 Forbidden` and the invitation is unchanged

#### Scenario: Missing token
- **WHEN** the invited user accepts or rejects with an empty body
- **THEN** the response is `400 Bad Request` and the invitation is unchanged

### Requirement: The invited user accepts a pending invitation
For the invited user with the right token, `PUT /athlete-invitation/{invitationId}/accept` SHALL move a `PENDING`, non-expired invitation to `ACCEPTED`, record the acceptance time and respond `200 OK`. It SHALL NOT create a trainer–athlete relationship.

#### Scenario: Accept
- **WHEN** the invited user accepts a pending invitation with its token
- **THEN** the response is `200 OK` with status `ACCEPTED` and a non-null `acceptedAtUtc`

#### Scenario: No relationship is created
- **WHEN** the invited user accepts an invitation and the trainer then calls `GET /trainer-athletes`
- **THEN** the athlete is not listed

### Requirement: The invited user rejects a pending invitation
For the invited user with the right token, `PUT /athlete-invitation/{invitationId}/reject` SHALL move a `PENDING`, non-expired invitation to `REJECTED` and respond `200 OK`. `acceptedAtUtc` SHALL stay `null`.

#### Scenario: Reject
- **WHEN** the invited user rejects a pending invitation with its token
- **THEN** the response is `200 OK` with status `REJECTED` and `acceptedAtUtc` `null`

### Requirement: Only pending, non-expired invitations change
Revoke, accept and reject SHALL respond `409 Conflict` and change nothing when the invitation is not `PENDING` or its expiry time has passed.

#### Scenario: Already accepted
- **WHEN** the invited user accepts or rejects an invitation that is already `ACCEPTED`
- **THEN** the response is `409 Conflict` and the invitation stays `ACCEPTED`

#### Scenario: Revoke after reject
- **WHEN** the inviting trainer revokes an invitation that is `REJECTED`
- **THEN** the response is `409 Conflict` and the invitation stays `REJECTED`

#### Scenario: Expired
- **WHEN** the inviting trainer or the invited user acts on a `PENDING` invitation whose expiry time has passed
- **THEN** the response is `409 Conflict` and the invitation stays `PENDING`

### Requirement: Unknown invitations are not found
Revoke, accept and reject SHALL respond `404 Not Found` when no invitation has the given id.

#### Scenario: Unknown id
- **WHEN** a `TRAINER` revokes, or a `USER` accepts or rejects, an invitation id that does not exist
- **THEN** the response is `404 Not Found`

### Requirement: Listing invitations is limited to administrators and trainers
`GET /athlete-invitation` SHALL respond `401 Unauthorized` without an access token or with an invalid one, and `403 Forbidden` with no invitation data to a `USER` caller.

#### Scenario: No token
- **WHEN** `GET /athlete-invitation` is called without an access token, or with a malformed one
- **THEN** the response is `401 Unauthorized`

#### Scenario: Athlete caller
- **WHEN** a `USER` calls `GET /athlete-invitation`
- **THEN** the response is `403 Forbidden` and contains no invitation data

### Requirement: An administrator lists every invitation
For an `ADMIN`, `GET /athlete-invitation` SHALL return `200 OK` with every invitation of every trainer and in every status, newest first.

#### Scenario: Invitations of several trainers
- **WHEN** two trainers have sent invitations and an `ADMIN` calls `GET /athlete-invitation`
- **THEN** the response lists the invitations of both trainers, whatever their status, ordered by `createdAtUtc` descending

### Requirement: A trainer lists only their own invitations
For a `TRAINER`, `GET /athlete-invitation` SHALL return `200 OK` with only the invitations the caller sent, in every status, newest first. Invitations sent by other trainers SHALL NOT be listed.

#### Scenario: Own invitations only
- **WHEN** two trainers have sent invitations and one of them calls `GET /athlete-invitation`
- **THEN** the response lists that trainer's invitations and none of the other trainer's

### Requirement: Listed invitations never expose their secret
Each listed invitation SHALL contain exactly `id`, `trainerId`, `inviteEmail`, `userId`, `status`, `createdAtUtc`, `expiresAtUtc` and `acceptedAtUtc`, and SHALL NOT contain the token or its hash.

#### Scenario: Item shape
- **WHEN** an `ADMIN` or a `TRAINER` lists invitations
- **THEN** every item contains exactly those fields and the response contains no token and no token hash
