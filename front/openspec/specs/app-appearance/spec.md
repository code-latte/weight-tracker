# app-appearance Specification

## Purpose

Defines how the app looks on every screen: the brand colours shared with the backoffice, the typography and its
fonts, and the colour mode the app uses.

## Requirements

### Requirement: The app uses the brand colours

Every screen SHALL use the brand palette: teal `#0F766E` as the primary colour, orange `#F97316` as the secondary
colour, background `#F8FAFC`, surfaces `#FFFFFF`, text `#0F172A`, secondary text `#475569` and error `#DC2626`.

#### Scenario: Login screen colours

- **WHEN** a signed-out athlete opens the app
- **THEN** the login screen background is `#F8FAFC` and its main button is filled with `#0F766E`

#### Scenario: Selected tab colour

- **WHEN** a signed-in athlete is on the Inicio tab
- **THEN** the Inicio tab is shown in `#0F766E` on a `#FFFFFF` tab bar and the other tabs in `#475569`

### Requirement: Raised surfaces keep the surface colour

Raised surfaces (cards, menus, dialogs, elevated buttons) SHALL use the plain surface colour at every elevation level,
with no tint from the primary colour.

#### Scenario: No purple tint

- **WHEN** any screen shows a raised surface
- **THEN** it is `#FFFFFF` and no purple or primary-tinted surface appears

### Requirement: Headings use Plus Jakarta Sans and body text uses Inter

Display, headline and large title text SHALL use Plus Jakarta Sans (bold or extra bold). All other text (titles,
body, labels, buttons, inputs) SHALL use Inter.

#### Scenario: Home heading and body

- **WHEN** a signed-in athlete is on home
- **THEN** the "Trainera" heading is in Plus Jakarta Sans Bold and "Has iniciado sesión." in Inter Regular

#### Scenario: Button label

- **WHEN** the login screen is shown
- **THEN** the button label is in Inter Bold

### Requirement: The typography scale matches the backoffice

Text styles SHALL use the sizes and line heights of the backoffice scale: display 40/48, 32/40 and 28/36; headline
28/36, 24/32 and 20/28; large title 20/28; titles 16/24 and 14/20; body 16/24, 14/22 and 12/16; labels 15/22, 12/16
and 11/16 (font size / line height, in points).

#### Scenario: Medium headline

- **WHEN** a screen shows a medium headline, such as the Entrenamientos heading
- **THEN** it is 24 points with a 32-point line height

### Requirement: No font jump at launch

At launch the app SHALL NOT render any screen before the fonts are loaded or have failed to load, so no text is
first drawn in the system font and then redrawn in the brand fonts.

#### Scenario: Fonts still loading

- **WHEN** the session is known but the fonts are still loading
- **THEN** the splash screen is still shown and no screen is rendered

### Requirement: A font failure does not block the app

If the fonts fail to load, the app SHALL continue with the system font and SHALL NOT show an error.

#### Scenario: Fonts fail

- **WHEN** loading the fonts fails at launch
- **THEN** the splash screen hides, the screen the session leads to is shown in the system font, and no error
  message appears

### Requirement: The app uses light mode

The app SHALL always render in light mode, whatever the device colour scheme is.

#### Scenario: Device in dark mode

- **WHEN** the device colour scheme is dark
- **THEN** the app still shows the `#F8FAFC` background and `#0F172A` text
