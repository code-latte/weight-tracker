# Spec Delta

## Purpose

Defines the athlete's profile screen in the app: who they are signed in as, who their trainer is, the account menu,
and how the screen behaves while loading or after a failure.

## ADDED Requirements

### Requirement: The profile screen has a title row with a settings button

The Perfil tab SHALL show the title "Perfil" and, on the same row at the right, a settings button with a cog icon and
the accessible name "Ajustes". Tapping it SHALL do nothing yet.

#### Scenario: Title row

- **WHEN** a signed-in athlete opens the Perfil tab
- **THEN** the screen shows the heading "Perfil" and the "Ajustes" button on the same row

#### Scenario: Settings not wired

- **WHEN** the athlete taps "Ajustes"
- **THEN** the screen stays as it is and no request is made

### Requirement: The profile screen shows the athlete's identity

Below the title, the screen SHALL show an avatar with the athlete's initials (first letter of the name and of the
surname, upper-cased), the full name (name and surname), and the email as a subtitle under the name, as the API
returns them for the signed-in athlete.

#### Scenario: Identity row

- **WHEN** the signed-in athlete is Ana López with email `ana@example.com`
- **THEN** the screen shows the avatar "AL", the name "Ana López" and, under it, "ana@example.com"

### Requirement: The trainer card shows only when the athlete has a trainer

When the athlete has a current trainer, the screen SHALL show a card below the identity row with the trainer's
initials avatar, the title "Mi entrenador" and the trainer's full name as subtitle. When the athlete has no trainer,
the screen SHALL NOT show the card or anything in its place. Tapping the card SHALL do nothing yet.

#### Scenario: Athlete with a trainer

- **WHEN** the athlete's trainer is Carlos Ruiz
- **THEN** the screen shows a card with the avatar "CR", "Mi entrenador" and "Carlos Ruiz"

#### Scenario: Athlete without a trainer

- **WHEN** the athlete has no current trainer
- **THEN** the screen shows no "Mi entrenador" card and the menu card follows the identity row

### Requirement: The profile screen shows the account menu

Below the trainer card (or the identity row when there is none), the screen SHALL show a card with these items in this
order: "Mis Datos" ("Medidas, pesos, edad..."), "Mis Objetivos" ("Grasa, Músculo, Rendimiento..."), "Limitaciones"
("Lesiones, molestias, observaciones..."), "Notificaciones" and "Acerca de Trainera", each with its icon. Tapping an
item SHALL do nothing yet.

#### Scenario: Menu items

- **WHEN** the profile screen is shown
- **THEN** the menu card shows the five items in order, with subtitles only on the first three

#### Scenario: Menu not wired

- **WHEN** the athlete taps "Mis Datos"
- **THEN** the screen stays as it is and no request is made

### Requirement: The profile screen shows a loading state

While the athlete's profile or trainer is being read, the screen SHALL show a loading indicator and SHALL NOT show the
identity row, the trainer card or the menu.

#### Scenario: Profile still loading

- **WHEN** the athlete opens the Perfil tab and the profile has not been read yet
- **THEN** the screen shows a loading indicator and no identity row

### Requirement: A failed load shows an error with retry

If reading the profile or the trainer fails, the screen SHALL show the translated message for the failure code and a
"Reintentar" button, and SHALL NOT show a partial profile. Tapping "Reintentar" SHALL read both again.

#### Scenario: No connection

- **WHEN** reading the profile fails with `NETWORK`
- **THEN** the screen shows "No hay conexión con el servidor." and "Reintentar", and no identity row

#### Scenario: Trainer read fails

- **WHEN** the profile is read but reading the trainer fails with `UNEXPECTED`
- **THEN** the screen shows "Ha ocurrido un error inesperado." and "Reintentar", and no identity row

#### Scenario: Retry succeeds

- **WHEN** the athlete taps "Reintentar" and both reads succeed
- **THEN** the screen shows the identity row, the menu and, if there is one, the trainer card

### Requirement: Profile texts are translated

All profile texts SHALL be shown in the app language. In English: "Profile", "Settings", "My trainer", "My data",
"My goals", "Limitations", "Notifications", "About Trainera", "Log out" and "Retry".

#### Scenario: English

- **WHEN** the app language is English
- **THEN** the screen title is "Profile" and the menu shows "My data", "My goals", "Limitations", "Notifications" and
  "About Trainera"
