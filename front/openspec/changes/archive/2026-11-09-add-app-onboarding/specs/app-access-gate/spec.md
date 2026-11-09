# Spec Delta

## ADDED Requirements

### Requirement: Onboarding requires a session

Onboarding screens SHALL be reachable only while a session is stored, exactly like the other signed-in screens. With
no stored session, the app SHALL show the login screen and SHALL NOT render any onboarding content.

#### Scenario: Deep link to onboarding without a session

- **WHEN** an athlete with no stored session opens a link to `/name`
- **THEN** the login screen is shown and no onboarding content is rendered

#### Scenario: Session ends during onboarding

- **WHEN** the session becomes signed out while an onboarding screen is shown
- **THEN** the login screen is shown and no onboarding content is rendered

## MODIFIED Requirements

### Requirement: Signed-out screens are hidden from a signed-in athlete

While a session is stored, the login and signup screens SHALL NOT be reachable, and the app SHALL show the home
screen instead, or the onboarding when the athlete is in onboarding.

#### Scenario: Link to signup with a session

- **WHEN** an athlete with stored access and refresh tokens and a complete profile opens a link to `/signup`
- **THEN** the home screen is shown

#### Scenario: Link to login with a session

- **WHEN** an athlete with stored access and refresh tokens and a complete profile opens a link to `/login`
- **THEN** the home screen is shown

#### Scenario: Link to login while in onboarding

- **WHEN** an athlete with stored tokens and an empty surname opens a link to `/login`
- **THEN** the name step is shown

### Requirement: Nothing shows until the session is restored

While the stored session is being read at launch, while the app fonts are still loading, or, with a stored session,
while the athlete's profile is being read, the app SHALL keep its splash screen up and SHALL NOT render any screen. The
splash hides once the session state is known, the fonts are loaded or have failed to load, and, with a stored
session, the profile read has ended (read or failed).

#### Scenario: Session not read yet

- **WHEN** the app has started and the stored session has not been read yet
- **THEN** the splash screen is still shown and neither the home nor the login screen is rendered

#### Scenario: Session read, fonts still loading

- **WHEN** the stored session has been read but the fonts are still loading
- **THEN** the splash screen is still shown and neither the home nor the login screen is rendered

#### Scenario: Session read, fonts settled

- **WHEN** the stored session has been read and the fonts are loaded or have failed to load
- **THEN** the splash screen hides and the gate shows the home or the login screen

#### Scenario: Profile still being read

- **WHEN** a stored session has been read and the fonts are loaded but the profile has not been read yet
- **THEN** the splash screen is still shown and neither the home nor any onboarding screen is rendered

### Requirement: The gate follows the session without navigation

When the session changes from signed out to signed in, the app SHALL show the home screen, or the onboarding when the
athlete is in onboarding. When the session changes from signed in to signed out, including when a refresh token is
rejected, the app SHALL show the login screen and SHALL NOT render any signed-in content. Screens SHALL NOT navigate
to grant or deny access.

#### Scenario: Session appears after signup

- **WHEN** the session becomes signed in while the signup screen is shown and the new profile has no name
- **THEN** the name step is shown and the signup screen is removed from the history

#### Scenario: Session appears after login

- **WHEN** the session becomes signed in while the login screen is shown and the profile is complete
- **THEN** the home screen is shown and the login screen is removed from the history

#### Scenario: Session ends after a rejected refresh

- **WHEN** the session becomes signed out because the refresh token was rejected while a signed-in screen is shown
- **THEN** the login screen is shown and no signed-in content is rendered
- **AND** no screen called a navigation function to get there

### Requirement: Session presence is enough

The session check SHALL only check that both tokens are stored. It SHALL NOT check whether they are expired or valid.
Once signed in, the gate SHALL read the athlete's profile from the API to decide between home and onboarding; an
expired access token SHALL be renewed for that read as for any other request.

#### Scenario: Stale tokens

- **WHEN** the stored access token is expired, the refresh succeeds and the profile is complete
- **THEN** the home screen is shown

#### Scenario: Rejected refresh on the profile read

- **WHEN** the stored tokens are expired and the refresh token is rejected while reading the profile
- **THEN** the login screen is shown
