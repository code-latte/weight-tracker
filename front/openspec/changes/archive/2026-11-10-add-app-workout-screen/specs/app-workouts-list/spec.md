# Spec Delta

## MODIFIED Requirements

### Requirement: Each workout of the week is a card

Between the week-progress card and the week navigator, the screen SHALL list the viewed week's workouts, one card
each, ordered by `workoutDate` ascending (same date keeps the API's order). Only the list SHALL scroll; the
week-progress card and the week navigator SHALL stay in place. Tapping a card SHALL open that workout's screen.
Coming back from it SHALL show the list on the same week the athlete left.

#### Scenario: Order

- **WHEN** the week has workouts dated 13, 9 and 11 November
- **THEN** they are listed 9, 11, 13

#### Scenario: Tapping a card opens the workout

- **WHEN** the athlete taps the card of a workout
- **THEN** that workout's screen is shown

#### Scenario: Coming back keeps the week

- **GIVEN** the athlete moved to next week and opened one of its workouts
- **WHEN** the athlete goes back
- **THEN** the list shows next week
