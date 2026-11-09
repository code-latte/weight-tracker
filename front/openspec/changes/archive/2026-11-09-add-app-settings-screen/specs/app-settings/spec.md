# Spec Delta

## Purpose

Defines the app's Ajustes screen: the settings the athlete can change on this device (colour mode and language),
their defaults from the system, how a choice applies and is remembered, and what happens when storage fails.

## ADDED Requirements

### Requirement: The Ajustes screen offers theme and language

The Ajustes screen SHALL show, under the section header "Ajustes", a "Tema" setting with the options "Claro" and
"Oscuro", and an "Idioma" setting with the options "Español" and "English". It SHALL offer no other setting and no
"system" option.

#### Scenario: Settings shown

- **WHEN** a signed-in athlete opens Ajustes
- **THEN** the screen shows "Tema" with "Claro" and "Oscuro", and "Idioma" with "Español" and "English"

### Requirement: The current values are selected

Each setting SHALL show selected the value the app is using: the stored choice when there is one, otherwise the value
resolved from the device.

#### Scenario: Nothing stored, device dark and in English

- **WHEN** nothing is stored, the device appearance is dark and the device language is English
- **THEN** "Oscuro" and "English" are selected

#### Scenario: Stored choice wins over the device

- **WHEN** the stored theme is light and the device appearance is dark
- **THEN** "Claro" is selected and the app renders in light mode

### Requirement: Choosing a theme applies it at once and remembers it

Choosing a theme option SHALL switch the whole app to that colour mode immediately, without leaving the screen, and
SHALL store the choice on the device.

#### Scenario: Choose dark

- **WHEN** the app is light and the athlete chooses "Oscuro"
- **THEN** the screen, the tab bar and the status bar switch to dark mode at once
- **AND** after restarting the app it opens in dark mode, whatever the device appearance is

### Requirement: Choosing a language applies it at once and remembers it

Choosing a language option SHALL show every text of the app in that language immediately, without leaving the
screen, and SHALL store the choice on the device.

#### Scenario: Choose English

- **WHEN** the app is in Spanish and the athlete chooses "English"
- **THEN** the screen header reads "Settings" and the tab bar shows "Home", "Workouts", "Progress" and "Profile"
- **AND** after restarting the app it opens in English, whatever the device language is

### Requirement: Without a stored choice the app follows the device

While no theme is stored, the app SHALL follow the device appearance, including changes made while it runs. While no
language is stored, the app SHALL use the first supported device language, else Spanish.

#### Scenario: Device appearance changes

- **WHEN** no theme is stored and the athlete switches the device to dark mode while the app is open
- **THEN** the app switches to dark mode

#### Scenario: Unsupported device language

- **WHEN** no language is stored and the device language is French
- **THEN** the app is in Spanish and "Español" is selected

### Requirement: Preferences belong to the device

Stored preferences SHALL survive logout and SHALL apply to the signed-out screens too. They SHALL NOT be sent to the
API.

#### Scenario: Logout keeps the preferences

- **WHEN** the athlete chose "Oscuro" and "English" and then logs out
- **THEN** the login screen is shown in dark mode and in English

### Requirement: A storage failure is silent

If storing a choice fails, the choice SHALL still apply for the rest of the run and no error SHALL be shown; the next
launch starts from what was stored before. If reading the stored preferences fails, the app SHALL use the device
values and SHALL NOT show an error. A stored value that is not a known option SHALL be ignored.

#### Scenario: Write fails

- **WHEN** the athlete chooses "Oscuro" and storing it fails
- **THEN** the app switches to dark mode and no error message is shown

#### Scenario: Read fails

- **WHEN** reading the stored preferences fails at launch
- **THEN** the app follows the device appearance and language and no error message is shown

### Requirement: Settings texts are translated

All Ajustes texts SHALL be shown in the app language. In English: "Settings", "Theme", "Light", "Dark", "Language".
The language options SHALL be named in their own language in both languages: "Español" and "English".

#### Scenario: English

- **WHEN** the app language is English
- **THEN** the screen shows "Settings", "Theme" with "Light" and "Dark", and "Language" with "Español" and "English"
