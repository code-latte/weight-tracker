# Spec Delta

## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Defining a week refuses a number already defined
Defining a week SHALL only add a week whose number is not yet defined. Defining a number that is already defined SHALL be refused with a conflict error and SHALL leave that week's training type and objective unchanged.

#### Scenario: Define an existing week
- **WHEN** week 2 is `ACCUMULATION` and week 2 is defined again as `DELOAD`
- **THEN** it is refused with a conflict error and week 2 is still `ACCUMULATION`
