# Spec Delta

## MODIFIED Requirements

### Requirement: Tapping a tab shows its screen

Tapping a tab SHALL select it and show its screen: home for Inicio (`/`), the workouts week view for Entrenamientos
(`/workouts`, see `app-workouts-list`), and a screen headed "Progreso" (`/progress`) or "Perfil" (`/profile`) for the
other tabs.

#### Scenario: Open Entrenamientos

- **WHEN** the athlete taps "Entrenamientos"
- **THEN** the Entrenamientos tab is selected and the screen shows the week-progress card of the current week

#### Scenario: Open Progreso

- **WHEN** the athlete taps "Progreso"
- **THEN** the Progreso tab is selected and the screen shows the heading "Progreso"

#### Scenario: Open Perfil

- **WHEN** the athlete taps "Perfil"
- **THEN** the Perfil tab is selected and the screen shows the heading "Perfil"

#### Scenario: Back to Inicio

- **WHEN** the athlete is on another tab and taps "Inicio"
- **THEN** the Inicio tab is selected and the home screen is shown
