# Spec Delta

## ADDED Requirements

### Requirement: Each week of a mesocycle has a date
Every week number from 1 to the duration SHALL have a date, whether or not the week is defined. Week 1 SHALL be the Wednesday of the Monday-to-Sunday week that contains the start date, and week *n* SHALL be that Wednesday plus `7 × (n − 1)` days. Asking for the date of a week beyond the duration SHALL be refused with a conflict and SHALL NOT return a date.

#### Scenario: Start on a Monday
- **WHEN** a mesocycle starting on Monday `2026-11-02` is asked for the date of week 1
- **THEN** the date is `2026-11-04`

#### Scenario: Start on a Wednesday
- **WHEN** a mesocycle starting on Wednesday `2026-11-04` is asked for the date of week 1
- **THEN** the date is `2026-11-04`

#### Scenario: Start on a Sunday
- **WHEN** a mesocycle starting on Sunday `2026-11-08` is asked for the date of week 1
- **THEN** the date is `2026-11-04`, before the start date

#### Scenario: Later week
- **WHEN** a mesocycle starting on Monday `2026-11-02` is asked for the date of week 3
- **THEN** the date is `2026-11-18`

#### Scenario: Undefined week inside the duration
- **WHEN** a four-week mesocycle with only week 1 defined is asked for the date of week 4
- **THEN** a date is returned

#### Scenario: Week beyond the duration
- **WHEN** a four-week mesocycle is asked for the date of week 5
- **THEN** it is refused with a conflict

#### Scenario: Rescheduling moves the week dates
- **WHEN** a mesocycle starting on `2026-11-02` is rescheduled to `2026-11-09`
- **THEN** the date of week 1 is `2026-11-11`

## MODIFIED Requirements

### Requirement: Defined weeks can be removed
Removing a defined week SHALL leave the other weeks untouched. Removing a week number that is not defined SHALL be refused with a not-found error. The mesocycle does not know its workouts, so the rule that a week with workouts cannot be removed is enforced where workouts are known, not by the mesocycle itself.

#### Scenario: Remove a week
- **WHEN** week 3 is removed from a mesocycle with weeks 1, 2 and 3
- **THEN** the mesocycle has weeks 1 and 2

#### Scenario: Remove an undefined week
- **WHEN** week 4 is removed from a mesocycle where week 4 is not defined
- **THEN** it is refused with a not-found error
