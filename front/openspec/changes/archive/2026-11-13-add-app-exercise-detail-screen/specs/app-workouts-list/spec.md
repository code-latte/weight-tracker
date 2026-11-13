# Spec Delta

## ADDED Requirements

### Requirement: The list reads again when it regains focus

When the athlete comes back to the list from a workout's screen, the list SHALL read the workouts again without a
spinner, so a status or progress changed there shows without a pull to refresh. The first read when the list opens
SHALL NOT be repeated, and a failed read SHALL keep what is shown.

#### Scenario: Status changed in the workout

- **WHEN** the athlete finishes a started workout on its screen and goes back to the list
- **THEN** the workouts are read again and the workout's icon shows "Completado" instead of "En curso"
- **AND** no refresh spinner is shown

#### Scenario: Opening the list

- **WHEN** the list opens
- **THEN** the workouts are read once
