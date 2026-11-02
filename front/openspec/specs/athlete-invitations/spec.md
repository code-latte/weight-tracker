# athlete-invitations Specification

## Purpose
Lets both fronts list, send, revoke, accept and reject athlete invitations through the API, with the invite form's
structural rules shared in one place and every failure returned as a `DomainError` code.

## Requirements

### Requirement: Listing invitations returns what the API answers

Listing invitations SHALL send `GET /athlete-invitations` with no body and SHALL return a successful result holding
the invitations the API answers with, unchanged and in the same order. Each one holds its id, the trainer's id, name
and surname, the invited email, the invited user's id, name and surname (empty for an unregistered email), its
status, and when it was created, expires and was accepted. It SHALL NOT filter by role or status.

#### Scenario: Invitations listed

- **WHEN** invitations are listed and the API answers with two invitations
- **THEN** exactly one `GET /athlete-invitations` request is sent with no body
- **AND** the result is successful and holds those two invitations in the API's order

#### Scenario: Invitation to an unregistered email

- **WHEN** the API answers with an invitation whose invited user id, name and surname are null
- **THEN** the result holds that invitation with those three fields null, unchanged

### Requirement: Inviting sends the email unchanged

Inviting an athlete SHALL send `POST /athlete-invitations` with a body holding only the given email, unchanged, and
SHALL return the created invitation the API answers with, including its token. It SHALL NOT check the caller's role.

#### Scenario: Athlete invited

- **WHEN** "ana@example.com" is invited and the API accepts it
- **THEN** exactly one `POST /athlete-invitations` request is sent with the body `{ "email": "ana@example.com" }`
- **AND** the result is successful and its value is the created invitation the API answered with

### Requirement: Revoking sends the invitation's id

Revoking an invitation SHALL send `PUT /athlete-invitations/{invitationId}/revoke` with the given id and no body, and
SHALL return the invitation the API answers with.

#### Scenario: Invitation revoked

- **WHEN** invitation "5e6f…" is revoked and the API accepts it
- **THEN** exactly one `PUT /athlete-invitations/5e6f…/revoke` request is sent with no body
- **AND** the result is successful and its value is the invitation the API answered with

### Requirement: Accepting and rejecting send the invitation's token

Accepting or rejecting an invitation SHALL send `PUT /athlete-invitations/{invitationId}/accept` or `…/reject` with
the given id and a body holding only the given token, and SHALL return the invitation the API answers with.

#### Scenario: Invitation accepted

- **WHEN** invitation "5e6f…" is accepted with the token "raw-token" and the API accepts it
- **THEN** exactly one `PUT /athlete-invitations/5e6f…/accept` request is sent with the body `{ "token": "raw-token" }`
- **AND** the result is successful and its value is the invitation the API answered with

#### Scenario: Invitation rejected

- **WHEN** invitation "5e6f…" is rejected with the token "raw-token" and the API accepts it
- **THEN** exactly one `PUT /athlete-invitations/5e6f…/reject` request is sent with the body `{ "token": "raw-token" }`

### Requirement: Invitation failures are returned as domain error codes

No invitation operation SHALL throw. A failed request SHALL return a failed result whose code comes from the shared
HTTP error mapping. A status the mapping does not list SHALL become `UNEXPECTED`.

#### Scenario: Email cannot be invited, or invitation not pending

- **WHEN** the API answers inviting, revoking, accepting or rejecting with `409`
- **THEN** the result is a failure with code `CONFLICT` and the API's detail kept in the message

#### Scenario: Unknown invitation

- **WHEN** the API answers revoking, accepting or rejecting with `404`
- **THEN** the result is a failure with code `NOT_FOUND`

#### Scenario: Rejected body

- **WHEN** the API answers inviting, accepting or rejecting with `400`
- **THEN** the result is a failure with code `VALIDATION`

#### Scenario: Caller not allowed

- **WHEN** the API answers any operation with `401` or `403`
- **THEN** the result is a failure with code `UNAUTHORIZED` or `FORBIDDEN` respectively

#### Scenario: Server error or no connection

- **WHEN** the API answers with `500`, or the request cannot reach the API
- **THEN** the result is a failure with code `UNEXPECTED` or `NETWORK` respectively

### Requirement: The invite form has shared structural rules

Both fronts SHALL check the invite form with one shared, pure rule set before sending it: the email is required and
shaped like an email, after trimming. It SHALL NOT check whether the email can be invited. The create user form SHALL
keep its current email rule, now taken from the same shared check.

#### Scenario: Email missing

- **WHEN** the invite form is checked with an empty or blank email
- **THEN** email is reported as `required`

#### Scenario: Malformed email

- **WHEN** the invite form is checked with "ana.example.com"
- **THEN** email is reported as `email`

#### Scenario: Valid email with spaces around it

- **WHEN** the invite form is checked with " ana@example.com "
- **THEN** no violation is reported
