# Spec Delta

## MODIFIED Requirements

### Requirement: The workout screen shows whose workout it is

Between the page header and the properties card, the workout screen SHALL show the athlete of every loaded workout as a
person card: a small card, sized to its content, with an avatar holding the person's initials, the athlete's full name,
or their email when they have no name, and the role caption "Atleta". The initials SHALL be the first letters of the
name and surname, or the first letter of the email when the person has no name.

#### Scenario: Athlete shown

- **WHEN** a trainer loads a workout of athlete "Ana García"
- **THEN** a person card above the properties card shows the initials "AG", "Ana García" and "Atleta"

#### Scenario: Athlete without a name

- **WHEN** the workout's athlete has no name and the email "ana@example.com"
- **THEN** the athlete's person card shows the initial "A", "ana@example.com" and "Atleta"

### Requirement: Only admins see the workout's trainer

For an `ADMIN`, the strip SHALL also show the workout's trainer as a person card after the athlete's, with their
initials, their full name or email, and the role caption "Entrenador", when the workout has a trainer. For a
`TRAINER`, no trainer person card, no "Entrenador" caption and no trainer name SHALL be shown.

#### Scenario: Admin sees the trainer

- **WHEN** an admin loads a workout of trainer "Luis Pérez"
- **THEN** a person card with "LP", "Luis Pérez" and "Entrenador" is shown after the athlete's

#### Scenario: Trainer does not see the trainer

- **WHEN** a trainer loads the same workout
- **THEN** neither "Entrenador" nor "Luis Pérez" is shown

#### Scenario: Workout without a trainer

- **WHEN** an admin loads a workout with no trainer
- **THEN** no trainer person card is shown

### Requirement: A placed workout shows its mesocycle and week

When the loaded workout is placed in a mesocycle, the strip SHALL read that mesocycle once and SHALL show, after the
person cards, a card of the same size with the caption "Mesociclo" and a breadcrumb of the mesocycle's name and
"Semana <n>". The card SHALL NOT link anywhere. A standalone workout SHALL show no mesocycle card and SHALL NOT read
any mesocycle.

#### Scenario: Placed workout

- **WHEN** a trainer loads a workout placed in week 2 of mesocycle `m-1` "Hipertrofia"
- **THEN** exactly one read of mesocycle `m-1` is sent
- **AND** the mesocycle card shows the breadcrumb "Hipertrofia" / "Semana 2"
- **AND** the card holds no link

#### Scenario: Standalone workout

- **WHEN** a trainer loads a workout with no mesocycle
- **THEN** no mesocycle card is shown and no mesocycle read is sent

### Requirement: The mesocycle entry never blocks the workout

While the mesocycle is read, its card SHALL show a loading placeholder. When the read fails, the mesocycle card SHALL
be left out and the rest of the screen SHALL work as usual.

#### Scenario: Mesocycle read fails

- **WHEN** a placed workout loads and its mesocycle read fails with no response
- **THEN** no mesocycle card is shown
- **AND** the athlete's person card, the properties card and the exercises card are shown

### Requirement: The strip is translated

The strip's role captions, the "Mesociclo" caption and the week label SHALL be available in Spanish and English and
SHALL follow the active language.

#### Scenario: Strip in English

- **WHEN** the active language is English and an admin loads a placed workout
- **THEN** the strip shows "Athlete", "Trainer", "Mesocycle" and the breadcrumb "Hipertrofia" / "Week 2"
