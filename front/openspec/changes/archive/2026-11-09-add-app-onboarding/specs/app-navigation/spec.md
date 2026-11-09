# Spec Delta

## MODIFIED Requirements

### Requirement: Inicio is the default tab

Inicio SHALL be the selected tab when the session becomes signed in and when the app starts with a stored session,
for an athlete whose profile is known to be complete, and when onboarding ends. Inicio SHALL show the home screen at
`/`.

#### Scenario: After login

- **WHEN** the session becomes signed in on the login screen and the profile is complete
- **THEN** the Inicio tab is selected and the home screen is shown

#### Scenario: Launch with a session

- **WHEN** the app starts with stored tokens and the profile is complete
- **THEN** the Inicio tab is selected and the home screen is shown

#### Scenario: After onboarding

- **WHEN** the athlete completes onboarding
- **THEN** the Inicio tab is selected and the home screen is shown
