# Spec Delta

## ADDED Requirements

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
