# mesocycles Specification

## Purpose

Defines a mesocycle: a training plan from a trainer to an athlete with a name, an objective, a start date, a
duration in weeks and the weeks defined inside it, and the rules that keep it consistent when it is built, changed
and stored.

## Requirements

### Requirement: A mesocycle goes from a trainer to a different athlete
A mesocycle SHALL reference exactly one trainer and one athlete by user id. The trainer and the athlete SHALL be different users; otherwise the mesocycle SHALL be refused with a conflict error and nothing SHALL change. This rule SHALL be enforced without looking up the users themselves.

#### Scenario: Trainer and athlete differ
- **WHEN** a mesocycle is built with trainer `T` and athlete `A`, and `T` differs from `A`
- **THEN** the mesocycle exists with trainer `T` and athlete `A`

#### Scenario: Trainer is the athlete
- **WHEN** a mesocycle is built with the same user as trainer and athlete
- **THEN** it is refused with a conflict error

### Requirement: A mesocycle has a required name
The name SHALL be trimmed and SHALL NOT be blank or longer than 256 characters after trimming. An invalid name SHALL be refused with a validation error, on creation and on rename, and a refused rename SHALL leave the previous name.

#### Scenario: Name is trimmed
- **WHEN** a mesocycle is built with name `"  Hypertrophy block  "`
- **THEN** its name is `"Hypertrophy block"`

#### Scenario: Blank name
- **WHEN** a mesocycle is built or renamed with a blank name
- **THEN** it is refused with a validation error

#### Scenario: Name too long
- **WHEN** a mesocycle is built or renamed with a 257-character name
- **THEN** it is refused with a validation error

### Requirement: A mesocycle has a required objective
The mesocycle objective SHALL be trimmed and SHALL NOT be blank or longer than 4000 characters after trimming. An invalid objective SHALL be refused with a validation error, on creation and on change, and a refused change SHALL leave the previous objective.

#### Scenario: Objective accepted at the limit
- **WHEN** a mesocycle is built with a 4000-character objective
- **THEN** the mesocycle keeps that objective

#### Scenario: Blank objective
- **WHEN** a mesocycle is built with a blank objective
- **THEN** it is refused with a validation error

#### Scenario: Objective too long
- **WHEN** a mesocycle objective is changed to a 4001-character text
- **THEN** the change is refused with a validation error and the previous objective is kept

### Requirement: A mesocycle starts on a calendar date
The start date SHALL be a calendar date with no time of day, and any day of the week SHALL be accepted. Rescheduling SHALL replace the start date and leave weeks, duration and the rest untouched.

#### Scenario: Time of day is dropped
- **WHEN** a mesocycle is built with start `2026-11-04 15:30`
- **THEN** its start date is `2026-11-04`

#### Scenario: Rescheduling
- **WHEN** a mesocycle starting `2026-11-02` is rescheduled to `2026-11-09`
- **THEN** its start date is `2026-11-09` and its weeks are unchanged

### Requirement: A mesocycle lasts between 1 and 52 weeks
The duration in weeks SHALL be an integer from 1 to 52 inclusive. A duration outside that range SHALL be refused with a validation error, on creation and on change.

#### Scenario: Bounds accepted
- **WHEN** a mesocycle is built with a duration of 1 or of 52 weeks
- **THEN** the mesocycle has that duration

#### Scenario: Out of range
- **WHEN** a mesocycle is built or changed to a duration of 0 or of 53 weeks
- **THEN** it is refused with a validation error

### Requirement: Week numbers are positive integers
A week number SHALL be an integer of 1 or more. A week number below 1 SHALL be refused with a validation error wherever a week number is given.

#### Scenario: Week number zero
- **WHEN** a week is defined with week number `0`
- **THEN** it is refused with a validation error

### Requirement: Weeks are defined progressively within the duration
A mesocycle SHALL allow defining any week whose number is from 1 to its duration, in any order, and SHALL allow gaps. A week number above the duration SHALL be refused with a conflict error. The mesocycle SHALL never create weeks on its own.

#### Scenario: New mesocycle has no weeks
- **WHEN** a mesocycle is built
- **THEN** it has no weeks defined

#### Scenario: Sparse weeks
- **WHEN** weeks 1 and 3 are defined on a 4-week mesocycle
- **THEN** the mesocycle has exactly weeks 1 and 3, and week 2 is not defined

#### Scenario: Week beyond the duration
- **WHEN** week 5 is defined on a 4-week mesocycle
- **THEN** it is refused with a conflict error and the weeks are unchanged

### Requirement: Each week has a training type and an objective
Every defined week SHALL have one training type out of `ADAPTATION`, `ACCUMULATION`, `INTENSIFICATION`, `REALIZATION`, `DELOAD` or `RECOVERY`, and an objective following the same rules as the mesocycle objective (required, trimmed, at most 4000 characters).

#### Scenario: Week defined
- **WHEN** week 2 is defined as `INTENSIFICATION` with objective `"Heavy triples"`
- **THEN** week 2 has type `INTENSIFICATION` and objective `"Heavy triples"`

#### Scenario: Week without objective
- **WHEN** a week is defined with a blank objective
- **THEN** it is refused with a validation error and the weeks are unchanged

#### Scenario: Undefined training type
- **WHEN** a week is defined or restored with a training type value that is not one of the six
- **THEN** it is refused with a validation error

### Requirement: Redefining a week keeps its number
Redefining a defined week SHALL replace that week's training type and objective and SHALL keep its number. Redefining a week number that is not defined SHALL be refused with a not-found error and SHALL NOT create the week. A week number SHALL be unique within its mesocycle.

#### Scenario: Redefine an existing week
- **WHEN** week 2 is `ACCUMULATION` and is redefined as `DELOAD`
- **THEN** the mesocycle still has one week 2, now `DELOAD`

#### Scenario: Redefine an undefined week
- **WHEN** week 3 is redefined on a mesocycle where week 3 is not defined
- **THEN** it is refused with a not-found error and week 3 stays undefined

#### Scenario: Duplicate week numbers in stored data
- **WHEN** a mesocycle is restored from storage with two weeks numbered 2
- **THEN** restoring fails with a conflict error

### Requirement: Defined weeks can be removed
Removing a defined week SHALL leave the other weeks untouched. Removing a week number that is not defined SHALL be refused with a not-found error. The mesocycle does not know its workouts, so the rule that a week with workouts cannot be removed is enforced where workouts are known, not by the mesocycle itself.

#### Scenario: Remove a week
- **WHEN** week 3 is removed from a mesocycle with weeks 1, 2 and 3
- **THEN** the mesocycle has weeks 1 and 2

#### Scenario: Remove an undefined week
- **WHEN** week 4 is removed from a mesocycle where week 4 is not defined
- **THEN** it is refused with a not-found error

### Requirement: The duration cannot drop below a defined week
Changing the duration SHALL be refused with a conflict error when the new duration is lower than the highest defined week number. Defined weeks SHALL never be removed as a side effect of a duration change.

#### Scenario: Shorten while weeks fit
- **WHEN** a 6-week mesocycle with weeks 1 to 4 defined is changed to 4 weeks
- **THEN** its duration is 4 and weeks 1 to 4 are kept

#### Scenario: Shorten below a defined week
- **WHEN** a 6-week mesocycle with week 5 defined is changed to 4 weeks
- **THEN** it is refused with a conflict error, the duration stays 6 and week 5 is kept

#### Scenario: Stored week beyond the duration
- **WHEN** a mesocycle is restored from storage with duration 4 and a week 5
- **THEN** restoring fails with a conflict error

### Requirement: Weeks are read in week order and cannot be changed from outside
The weeks of a mesocycle SHALL be read ordered by week number ascending. Changing the list that was read SHALL NOT change the mesocycle.

#### Scenario: Ordered weeks
- **WHEN** weeks 3, 1 and 2 are defined in that order
- **THEN** reading the weeks returns 1, 2, 3

### Requirement: Mesocycles are stored and restored unchanged
A saved mesocycle SHALL be restored with the same id, trainer, athlete, name, objective, start date, duration and weeks (number, type, objective), whether it has no weeks, some weeks or all its weeks. Saving changes to an existing mesocycle SHALL update it in place, without creating a second one.

#### Scenario: Round trip with sparse weeks
- **WHEN** a 4-week mesocycle with weeks 1 and 3 is saved and loaded again
- **THEN** the loaded mesocycle equals the saved one, with weeks 1 and 3

#### Scenario: Update after redefining and removing weeks
- **WHEN** a stored mesocycle has week 1 redefined and week 3 removed, and is saved again
- **THEN** loading it returns the redefined week 1, no week 3, and the same mesocycle id

#### Scenario: Unknown id
- **WHEN** a mesocycle is loaded by an id that matches none
- **THEN** no mesocycle is returned

### Requirement: Mesocycles reference existing users
Stored mesocycles SHALL reference existing users as trainer and athlete. Storing a mesocycle whose trainer or athlete does not exist SHALL fail.

#### Scenario: Unknown athlete
- **WHEN** a mesocycle whose athlete id matches no user is saved
- **THEN** persisting fails and no mesocycle is stored

### Requirement: Defining a week refuses a number already defined
Defining a week SHALL only add a week whose number is not yet defined. Defining a number that is already defined SHALL be refused with a conflict error and SHALL leave that week's training type and objective unchanged.

#### Scenario: Define an existing week
- **WHEN** week 2 is `ACCUMULATION` and week 2 is defined again as `DELOAD`
- **THEN** it is refused with a conflict error and week 2 is still `ACCUMULATION`

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
