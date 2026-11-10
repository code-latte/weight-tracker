# Spec Delta

## MODIFIED Requirements

### Requirement: A placed workout shows its mesocycle and week

When the loaded workout is placed in a mesocycle, the strip SHALL show, after the person cards, a card of the same
size with the caption "Mesociclo" and a breadcrumb of the name of the mesocycle the workout carries and "Semana <n>".
The card SHALL NOT link anywhere. The screen SHALL NOT read the mesocycle separately. A standalone workout SHALL show
no mesocycle card.

#### Scenario: Placed workout

- **WHEN** a trainer loads a workout placed in week 2 of mesocycle `m-1` "Hipertrofia"
- **THEN** the mesocycle card shows the breadcrumb "Hipertrofia" / "Semana 2"
- **AND** no mesocycle read is sent
- **AND** the card holds no link

#### Scenario: Standalone workout

- **WHEN** a trainer loads a workout with no mesocycle
- **THEN** no mesocycle card is shown and no mesocycle read is sent

## REMOVED Requirements

### Requirement: The mesocycle entry never blocks the workout

**Reason**: `GET /workouts/{id}` now carries the workout's mesocycle, so there is no separate mesocycle read that can
load or fail on its own.

**Migration**: None. The mesocycle card shows as soon as the workout loads; a failed workout read keeps its existing
error state.
