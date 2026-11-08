# app-access-gate Specification

## Purpose

Defines which app screens a stored session unlocks, what an athlete without one sees instead, and what shows while
the session is restored. This is a navigation guard, not authorization: the API decides access to data.

## Requirements

### Requirement: Signed-in screens require a stored session

The home screen (`/`) and every later signed-in screen SHALL be reachable only while a session is stored. With no
stored session, the app SHALL show the login screen and SHALL NOT render any signed-in content.

#### Scenario: Launch without a session

- **WHEN** the app starts with no stored tokens
- **THEN** the login screen is shown
- **AND** the home heading is never rendered

#### Scenario: Deep link to home without a session

- **WHEN** an athlete with no stored session opens a link to `/`
- **THEN** the login screen is shown and no home content is rendered

### Requirement: Signed-out screens are hidden from a signed-in athlete

While a session is stored, the login and signup screens SHALL NOT be reachable, and the app SHALL show the home
screen instead.

#### Scenario: Link to signup with a session

- **WHEN** an athlete with stored access and refresh tokens opens a link to `/signup`
- **THEN** the home screen is shown

#### Scenario: Link to login with a session

- **WHEN** an athlete with stored access and refresh tokens opens a link to `/login`
- **THEN** the home screen is shown

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

### Requirement: The gate follows the session without navigation

When the session changes from signed out to signed in, the app SHALL show the home screen. When the session changes
from signed in to signed out, including when a refresh token is rejected, the app SHALL show the login screen and
SHALL NOT render any signed-in content. Screens SHALL NOT navigate to grant or deny access.

#### Scenario: Session appears after signup

- **WHEN** the session becomes signed in while the signup screen is shown
- **THEN** the home screen is shown and the signup screen is removed from the history

#### Scenario: Session appears after login

- **WHEN** the session becomes signed in while the login screen is shown
- **THEN** the home screen is shown and the login screen is removed from the history

#### Scenario: Session ends after a rejected refresh

- **WHEN** the session becomes signed out because the refresh token was rejected while a signed-in screen is shown
- **THEN** the login screen is shown and no signed-in content is rendered
- **AND** no screen called a navigation function to get there

### Requirement: Session presence is enough

The gate SHALL only check that both tokens are stored. It SHALL NOT check whether they are expired or valid, and
SHALL NOT call the API.

#### Scenario: Stale tokens

- **WHEN** the stored tokens are expired
- **THEN** the home screen is still shown

### Requirement: A session read failure is treated as signed out

If reading the session fails, the app SHALL behave as if no session were stored.

#### Scenario: Storage fails

- **WHEN** reading the session fails with `UNEXPECTED`
- **THEN** the login screen is shown and no home content is rendered

### Requirement: Unknown paths show a not-found screen

A path that matches no screen SHALL show a not-found screen with a translated message and a link to `/`.

#### Scenario: Unknown deep link

- **WHEN** an athlete opens a link to a path the app does not have
- **THEN** the screen shows "Esta pantalla no existe." and the link "Ir al inicio"
