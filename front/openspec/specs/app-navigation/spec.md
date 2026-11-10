# app-navigation Specification

## Purpose

Defines the app's top-level navigation for a signed-in athlete: a bottom tab bar with its tabs, their order, labels
and icons, and which tab is shown first.

## Requirements

### Requirement: Signed-in screens show a bottom tab bar

Every top-level signed-in screen SHALL show a bottom tab bar with four tabs in this order: Inicio, Entrenamientos,
Progreso and Perfil. The app SHALL NOT have a drawer.

#### Scenario: Signed-in athlete sees the tabs

- **WHEN** an athlete with a stored session is on any tab
- **THEN** the tab bar shows "Inicio", "Entrenamientos", "Progreso" and "Perfil", in that order

### Requirement: Each tab has its icon

Each tab SHALL show an icon above its label: a house for Inicio, a dumbbell for Entrenamientos, a bar chart for
Progreso and a user silhouette for Perfil.

#### Scenario: Tab icons

- **WHEN** the tab bar is shown
- **THEN** Inicio shows a house icon, Entrenamientos a dumbbell, Progreso a bar chart and Perfil a user silhouette

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

### Requirement: The selected tab is highlighted

The selected tab SHALL be shown in the theme's primary colour, and the other tabs SHALL NOT.

#### Scenario: Highlight follows the selection

- **WHEN** the athlete taps "Progreso"
- **THEN** "Progreso" is shown in the primary colour and "Inicio" is no longer

### Requirement: The tab bar needs a session

An athlete without a stored session SHALL NOT see the tab bar or any tab screen.

#### Scenario: Launch without a session

- **WHEN** the app starts with no stored tokens
- **THEN** the login screen is shown without the tab bar

#### Scenario: Deep link to a tab without a session

- **WHEN** an athlete with no stored session opens a link to `/workouts`
- **THEN** the login screen is shown and neither the tab bar nor the Entrenamientos screen is rendered

#### Scenario: Logout from Inicio

- **WHEN** the athlete logs out from the home screen
- **THEN** the login screen is shown without the tab bar

### Requirement: Tab labels are translated

Tab labels and the placeholder headings SHALL be shown in the app language: "Inicio", "Entrenamientos", "Progreso"
and "Perfil" in Spanish; "Home", "Workouts", "Progress" and "Profile" in English.

#### Scenario: English

- **WHEN** the app language is English
- **THEN** the tab bar shows "Home", "Workouts", "Progress" and "Profile"

### Requirement: The Perfil tab pushes section screens

The Perfil tab SHALL show the profile screen first and SHALL push section screens (Ajustes in this version) over it.
The tab bar SHALL stay visible on section screens, with Perfil selected. Tapping "Perfil" while on a section screen
SHALL return to the profile screen.

#### Scenario: Open a section

- **WHEN** the athlete opens Ajustes from the profile screen
- **THEN** Ajustes is shown with the tab bar visible and "Perfil" selected

#### Scenario: Tap Perfil on a section screen

- **WHEN** the athlete is on Ajustes and taps "Perfil"
- **THEN** the profile screen is shown

### Requirement: Section screens share a header

Every section screen of the Perfil tab SHALL show a header with a back caret on the left, with the accessible name
"Volver", followed by the section title. Tapping the caret SHALL return to the profile screen. The profile screen
SHALL NOT show this header.

#### Scenario: Ajustes header

- **WHEN** the athlete is on Ajustes
- **THEN** the header shows the back caret and the title "Ajustes"

#### Scenario: Back to the profile

- **WHEN** the athlete taps the back caret on Ajustes
- **THEN** the profile screen is shown

#### Scenario: Header in English

- **WHEN** the app language is English and the athlete is on Ajustes
- **THEN** the header title is "Settings" and the caret's accessible name is "Back"
