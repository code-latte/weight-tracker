# Spec Delta

## ADDED Requirements

### Requirement: Relationship responses carry the names of both sides
The responses of `POST /trainer-athletes/{trainerId}` and `PUT /trainer-athletes/{trainerId}/end` SHALL include `trainerName` and `trainerSurname` of the trainer and `athleteName` and `athleteSurname` of the athlete, as currently stored.

#### Scenario: Link response
- **WHEN** an `ADMIN` links an athlete to a trainer
- **THEN** the `201` response carries `trainerName`, `trainerSurname`, `athleteName` and `athleteSurname`

#### Scenario: End response
- **WHEN** an allowed caller ends a relationship
- **THEN** the `200` response carries `trainerName`, `trainerSurname`, `athleteName` and `athleteSurname`

## MODIFIED Requirements

### Requirement: Listed athletes expose relationship and profile fields
Each listed item SHALL contain exactly `id` (the relationship), `athleteId`, `email`, `name`, `surname`, `trainerName`, `trainerSurname` and `startedAtUtc`. It SHALL NOT contain the password hash, the date of birth or any token.

#### Scenario: Item shape
- **WHEN** a `TRAINER` or an `ADMIN` lists a trainer's athletes
- **THEN** every item contains exactly `id`, `athleteId`, `email`, `name`, `surname`, `trainerName`, `trainerSurname` and `startedAtUtc`
