# Spec Delta

## Purpose

Tells the trainer or admin on the workout screen whose workout it is and, when it belongs to a mesocycle, which plan and
week it sits in, between the page header and the properties card.

## ADDED Requirements

### Requirement: The workout screen shows whose workout it is

Between the page header and the properties card, the workout screen SHALL show "Atleta" with the athlete's full name,
or their email when they have no name, for every loaded workout.

#### Scenario: Athlete shown

- **WHEN** a trainer loads a workout of athlete "Ana García"
- **THEN** "Atleta" and "Ana García" are shown above the properties card

#### Scenario: Athlete without a name

- **WHEN** the workout's athlete has no name and the email "ana@example.com"
- **THEN** "ana@example.com" is shown as the athlete

### Requirement: Only admins see the workout's trainer

For an `ADMIN`, the strip SHALL also show "Entrenador" with the trainer's full name, or their email when they have no
name, when the workout has a trainer. For a `TRAINER`, neither the "Entrenador" label nor the trainer's name SHALL be
shown.

#### Scenario: Admin sees the trainer

- **WHEN** an admin loads a workout of trainer "Luis Pérez"
- **THEN** "Entrenador" and "Luis Pérez" are shown in the strip

#### Scenario: Trainer does not see the trainer

- **WHEN** a trainer loads the same workout
- **THEN** neither "Entrenador" nor "Luis Pérez" is shown

#### Scenario: Workout without a trainer

- **WHEN** an admin loads a workout with no trainer
- **THEN** no "Entrenador" entry is shown

### Requirement: A placed workout shows its mesocycle and week

When the loaded workout is placed in a mesocycle, the strip SHALL read that mesocycle once and SHALL show "Mesociclo"
with "<mesocycle name> · Semana <n>" as a link to `/mesocycle?id=<mesocycleId>`. A standalone workout SHALL show no
mesocycle entry and SHALL NOT read any mesocycle.

#### Scenario: Placed workout

- **WHEN** a trainer loads a workout placed in week 2 of mesocycle `m-1` "Hipertrofia"
- **THEN** exactly one read of mesocycle `m-1` is sent
- **AND** "Hipertrofia · Semana 2" is shown, linking to `/mesocycle?id=m-1`

#### Scenario: Standalone workout

- **WHEN** a trainer loads a workout with no mesocycle
- **THEN** no mesocycle entry is shown and no mesocycle read is sent

### Requirement: The mesocycle entry never blocks the workout

While the mesocycle is read, its entry SHALL show a loading placeholder. When the read fails, the mesocycle entry SHALL
be left out and the rest of the screen SHALL work as usual.

#### Scenario: Mesocycle read fails

- **WHEN** a placed workout loads and its mesocycle read fails with no response
- **THEN** no mesocycle entry is shown
- **AND** the athlete, the properties card and the exercises card are shown

### Requirement: The strip is translated

The strip's labels SHALL be available in Spanish and English and SHALL follow the active language.

#### Scenario: Strip in English

- **WHEN** the active language is English and an admin loads a placed workout
- **THEN** the strip shows "Athlete", "Trainer", "Mesocycle" and "Hipertrofia · Week 2"
