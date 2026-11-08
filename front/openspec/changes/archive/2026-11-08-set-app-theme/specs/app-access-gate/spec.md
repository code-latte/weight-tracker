# Spec Delta

## MODIFIED Requirements

### Requirement: Nothing shows until the session is restored

While the stored session is being read at launch, or while the app fonts are still loading, the app SHALL keep its
splash screen up and SHALL NOT render any screen. The splash hides once the session state is known and the fonts
are loaded or have failed to load.

#### Scenario: Session not read yet

- **WHEN** the app has started and the stored session has not been read yet
- **THEN** the splash screen is still shown and neither the home nor the login screen is rendered

#### Scenario: Session read, fonts still loading

- **WHEN** the stored session has been read but the fonts are still loading
- **THEN** the splash screen is still shown and neither the home nor the login screen is rendered

#### Scenario: Session read, fonts settled

- **WHEN** the stored session has been read and the fonts are loaded or have failed to load
- **THEN** the splash screen hides and the gate shows the home or the login screen
