# Spec Delta

## ADDED Requirements

### Requirement: Invitation responses carry the names of both sides
Every invitation in a response of `/athlete-invitations` SHALL include `trainerName` and `trainerSurname` of the inviting trainer, and `userName` and `userSurname` of the invited user. `userName` and `userSurname` SHALL be `null` when the invitation has no `userId`. Names SHALL be the users' current stored names.

#### Scenario: Registered invitee
- **WHEN** a `TRAINER` invites the email of a registered `USER`
- **THEN** the `201` response carries the trainer's `trainerName` and `trainerSurname` and the invitee's `userName` and `userSurname`

#### Scenario: Unregistered invitee
- **WHEN** a `TRAINER` invites an email that no user has
- **THEN** the response carries the trainer's names and `userName` and `userSurname` are `null`

#### Scenario: Answered or listed invitation
- **WHEN** an invitation is revoked, accepted, rejected or listed
- **THEN** each invitation in the response carries the trainer's and the invited user's names

## MODIFIED Requirements

### Requirement: Listed invitations never expose their secret
Each listed invitation SHALL contain exactly `id`, `trainerId`, `trainerName`, `trainerSurname`, `inviteEmail`, `userId`, `userName`, `userSurname`, `status`, `createdAtUtc`, `expiresAtUtc` and `acceptedAtUtc`, and SHALL NOT contain the token or its hash.

#### Scenario: Item shape
- **WHEN** an `ADMIN` or a `TRAINER` lists invitations
- **THEN** every item contains exactly those fields and the response contains no token and no token hash
