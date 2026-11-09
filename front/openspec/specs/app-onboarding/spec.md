# app-onboarding Specification

## Purpose

Defines the app's onboarding: when a signed-in athlete must complete it, how its steps are walked, the name step,
how the collected values are saved, and what the athlete sees when the profile cannot be read.

## Requirements

### Requirement: Incomplete profiles are in onboarding

A signed-in athlete SHALL be in onboarding when their profile has an empty name or an empty surname, after trimming.
An athlete whose name and surname are both set SHALL NOT be in onboarding.

#### Scenario: Empty surname

- **WHEN** the signed-in athlete's profile has name "Ana" and an empty surname
- **THEN** the athlete is in onboarding and the name step is shown

#### Scenario: Whitespace-only name

- **WHEN** the profile has name " " and surname "López"
- **THEN** the athlete is in onboarding

#### Scenario: Complete profile

- **WHEN** the profile has name "Ana" and surname "López"
- **THEN** the athlete is not in onboarding and the home screen is shown

### Requirement: An unreadable profile keeps the athlete in onboarding

When reading the signed-in athlete's profile fails, the athlete SHALL be in onboarding and SHALL NOT reach any tab.
Onboarding SHALL show the translated message for the failure code and a "Reintentar" button instead of the step
fields. "Reintentar" SHALL read the profile again.

#### Scenario: No connection

- **WHEN** reading the profile fails with `NETWORK`
- **THEN** onboarding shows "No hay conexión con el servidor." and "Reintentar", no step fields and no tab bar

#### Scenario: Retry with a complete profile

- **WHEN** the athlete taps "Reintentar" and the profile is read with name and surname set
- **THEN** the home screen is shown

#### Scenario: Retry with an incomplete profile

- **WHEN** the athlete taps "Reintentar" and the profile is read with an empty surname
- **THEN** the name step is shown, pre-filled with the name

#### Scenario: Retry fails again

- **WHEN** the athlete taps "Reintentar" and the read fails again
- **THEN** the error and "Reintentar" stay

### Requirement: Onboarding blocks the rest of the app

While the athlete is in onboarding, the app SHALL show only onboarding screens: no tab bar, no tab screen and no other
signed-in screen, including through a deep link. When onboarding ends, the app SHALL show the home screen without any
screen calling a navigation function.

#### Scenario: Deep link while in onboarding

- **WHEN** an athlete in onboarding opens a link to `/workouts`
- **THEN** the first onboarding step is shown and the Entrenamientos screen is not rendered

#### Scenario: Deep link to onboarding with a complete profile

- **WHEN** an athlete with a complete profile opens a link to `/name`
- **THEN** the home screen is shown

### Requirement: Each step shows its progress and actions

Every step screen SHALL show "Paso <current> de <total>" with a progress bar, the step title and its fields, and at
the bottom "Siguiente" on every step but the last and "Finalizar" on the last step only.

#### Scenario: Single step

- **WHEN** the onboarding has only the name step
- **THEN** the screen shows "Paso 1 de 1", the title "Dinos cómo te llamas" and only "Finalizar"

#### Scenario: First of two steps

- **WHEN** the onboarding has two steps and the first is shown
- **THEN** the screen shows "Paso 1 de 2" and "Siguiente", and no "Finalizar"

### Requirement: Steps are walked forward and back keeping the values

"Siguiente" SHALL validate only the current step's fields and, when they are valid, show the next step. Going back
SHALL show the previous step with the values the athlete had typed. The first step SHALL offer no way back.

#### Scenario: Invalid step blocks Siguiente

- **WHEN** the athlete taps "Siguiente" with an invalid field
- **THEN** the step does not change and the field shows its error

#### Scenario: Back keeps the values

- **WHEN** the athlete moves to the second step and goes back
- **THEN** the first step shows the values typed before

#### Scenario: No back on the first step

- **WHEN** the first step is shown
- **THEN** no back control is shown and the system back gesture does not leave it

### Requirement: The name step asks for name and surname

The name step, titled "Dinos cómo te llamas", SHALL ask for "Nombre" and "Apellidos", pre-filled with the profile's
current values. Each field SHALL be required and SHALL have 3 to 100 characters after trimming, with the error under
the field.

#### Scenario: Field too short

- **WHEN** the athlete enters "Al" as name and taps "Finalizar"
- **THEN** "Nombre" shows "Debe tener al menos 3 caracteres." and no update is sent

#### Scenario: Field empty

- **WHEN** the athlete leaves "Apellidos" empty and taps "Finalizar"
- **THEN** "Apellidos" shows "Este campo es obligatorio." and no update is sent

#### Scenario: Field too long

- **WHEN** the athlete enters 101 characters as surname and taps "Finalizar"
- **THEN** "Apellidos" shows "No puede superar los 100 caracteres." and no update is sent

#### Scenario: Pre-filled name

- **WHEN** the profile has name "Ana" and an empty surname
- **THEN** "Nombre" shows "Ana" and "Apellidos" is empty

### Requirement: Finalizar saves every step and ends onboarding

"Finalizar" on a valid last step SHALL send the trimmed values collected by every step in one profile update, with
"Finalizar" disabled while it is pending. On success onboarding SHALL end and the home screen SHALL show. On failure
the step SHALL stay, keep the values and show the translated message for the failure code.

#### Scenario: Onboarding completed

- **WHEN** the athlete enters " Ana " and "López" and taps "Finalizar" and the update succeeds
- **THEN** the update is sent with name "Ana" and surname "López"
- **AND** the home screen is shown with the tab bar

#### Scenario: Update fails

- **WHEN** the update fails with `NETWORK`
- **THEN** the name step stays with the values typed and shows "No hay conexión con el servidor."

### Requirement: The athlete can log out from onboarding

Every onboarding screen, including the unreadable-profile state, SHALL offer "Cerrar sesión". It SHALL end the session
and the app SHALL show the login screen.

#### Scenario: Log out from the name step

- **WHEN** the athlete taps "Cerrar sesión" on the name step
- **THEN** the session ends and the login screen is shown

### Requirement: Onboarding texts are translated

All onboarding texts SHALL be shown in the app language. In English: "Step 1 of 1", "Tell us your name", "Name",
"Surname", "Next", "Finish", "Retry" and "Log out".

#### Scenario: English

- **WHEN** the app language is English and the name step is shown
- **THEN** it shows "Step 1 of 1", "Tell us your name", "Name", "Surname" and "Finish"
