# app-logout Specification

## Purpose

Lets an athlete end their app session from the profile screen, where logout lives alongside the account options.

## Requirements

### Requirement: The profile screen shows a logout button

The app profile screen SHALL show a "Cerrar sesión" button below its other content, and home SHALL NOT show one. An
athlete without a session SHALL NOT see it, since the gate does not render the profile screen for them.

#### Scenario: Signed-in athlete on the profile screen

- **WHEN** an athlete with a stored session is on the profile screen
- **THEN** the profile screen shows the "Cerrar sesión" button

#### Scenario: Signed-in athlete on home

- **WHEN** an athlete with a stored session is on home
- **THEN** home does not show a "Cerrar sesión" button

### Requirement: Logging out from the profile screen shows login

Tapping the logout button SHALL log out. On success the app SHALL show the login screen and SHALL NOT render any
signed-in content. The profile screen SHALL NOT navigate; the access gate follows the session. The button SHALL be
disabled while logout is in flight.

#### Scenario: Successful logout

- **WHEN** the athlete taps "Cerrar sesión" and logout succeeds
- **THEN** the session is read again and is signed out
- **AND** the login screen is shown and no signed-in content is rendered

#### Scenario: Logout in flight

- **WHEN** the athlete taps "Cerrar sesión" and logout has not finished
- **THEN** the button is disabled

#### Scenario: Restart after logout

- **WHEN** the athlete logs out and the app restarts
- **THEN** the login screen is shown

### Requirement: A failed logout stays on the profile screen

If logout fails, the athlete SHALL stay on the profile screen and the screen SHALL show the translated message for the
failure code.

#### Scenario: Storage fails

- **WHEN** logout fails with `UNEXPECTED`
- **THEN** the profile screen stays and shows "Ha ocurrido un error inesperado."
