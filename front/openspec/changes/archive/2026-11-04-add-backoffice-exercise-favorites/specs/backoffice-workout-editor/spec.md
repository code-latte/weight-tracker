# Spec Delta

## ADDED Requirements

### Requirement: The catalog dialog has a section sidebar

The catalog dialog SHALL show, on the left of the filters and the exercise list, a list of three sections, each an
icon and a name, in this order: "Todos", "Mis ejercicios" and "Favoritos". Exactly one section SHALL be selected, and
"Todos" SHALL be selected the first time the dialog opens on the screen.

#### Scenario: Sections shown

- **WHEN** the catalog dialog opens for the first time on the screen
- **THEN** the sections "Todos", "Mis ejercicios" and "Favoritos" are shown in that order, each with its icon
- **AND** "Todos" is selected and every catalog exercise not in the workout is listed

#### Scenario: Section changed

- **WHEN** the user selects "Favoritos"
- **THEN** "Favoritos" is selected and "Todos" no longer is

### Requirement: Mis ejercicios lists the exercises the user owns

The "Mis ejercicios" section SHALL list only the catalog exercises whose owner is the signed-in user. System exercises
and custom exercises owned by anyone else SHALL NOT be listed there; they stay under "Todos".

#### Scenario: Own exercises only

- **WHEN** the catalog holds a system exercise, an exercise owned by the signed-in user and an exercise owned by
  another user, and the user selects "Mis ejercicios"
- **THEN** only the exercise owned by the signed-in user is listed

#### Scenario: No own exercises

- **WHEN** the user owns no catalog exercise and selects "Mis ejercicios"
- **THEN** "Ningún ejercicio coincide con el filtro." is shown

### Requirement: Favoritos lists the user's favorite exercises

The "Favoritos" section SHALL list only the catalog exercises that are among the user's favorites. A favorite whose
exercise is not in the catalog SHALL be ignored. Marking or unmarking an exercise SHALL update the section at once,
without reading the favorites again.

#### Scenario: Favorites only

- **WHEN** two catalog exercises are favorites and the user selects "Favoritos"
- **THEN** only those two exercises are listed

#### Scenario: Favorite missing from the catalog

- **WHEN** a favorite's exercise is not in the catalog and the user selects "Favoritos"
- **THEN** it is not listed and no error is shown

#### Scenario: Unmarked while in Favoritos

- **WHEN** "Favoritos" is selected and the user unmarks a listed exercise, and the API accepts it
- **THEN** that exercise is no longer listed
- **AND** no new read of the favorites is sent

### Requirement: Favorites are read when the screen opens

The workout screen SHALL read the user's favorites once on opening, beside the workout and the catalog. While they
load, the stars SHALL be disabled. If the read fails, the catalog SHALL stay usable, the stars and the "Favoritos"
section SHALL be disabled, and the dialog SHALL show "No se pudieron cargar los favoritos".

#### Scenario: Favorites read once

- **WHEN** a trainer opens `/workout?id=w-1`
- **THEN** exactly one read of the favorites is sent

#### Scenario: Favorites fail to load

- **WHEN** the favorites read fails and the user opens the catalog dialog
- **THEN** "No se pudieron cargar los favoritos" is shown in the dialog
- **AND** the stars and the "Favoritos" section are disabled
- **AND** "Todos" lists the catalog and "Elegir ejercicio" still opens the configuration

### Requirement: The star marks and unmarks a catalog exercise as a favorite

Each catalog card SHALL show a star button beside "Elegir ejercicio": an outlined star labelled "Añadir a favoritos"
while the exercise is not a favorite, a filled star labelled "Quitar de favoritos" while it is. Activating it SHALL
send exactly one mark or unmark of that exercise, and the star SHALL change only when the API accepts it.

#### Scenario: Marked

- **WHEN** the user clicks "Añadir a favoritos" on an exercise and the API accepts it
- **THEN** exactly one mark of that exercise is sent
- **AND** its star is filled and labelled "Quitar de favoritos"

#### Scenario: Unmarked

- **WHEN** the user clicks "Quitar de favoritos" on a favorite and the API accepts it
- **THEN** exactly one unmark of that exercise is sent
- **AND** its star is outlined and labelled "Añadir a favoritos"

#### Scenario: Only in the catalog

- **WHEN** the workout lists its saved exercises
- **THEN** no star is shown on them, nor in the configuration dialog

### Requirement: A star in progress or refused is shown

While an exercise's mark or unmark is in progress, its star SHALL be disabled; other stars SHALL stay usable. When
the API refuses it, the star SHALL keep its previous state and the dialog SHALL show the message for the error code.

#### Scenario: In progress

- **WHEN** the user clicks an exercise's star and the API has not answered yet
- **THEN** that star is disabled

#### Scenario: Archived exercise refused

- **WHEN** the user marks an exercise and the API answers `CONFLICT`
- **THEN** the star stays outlined
- **AND** the message for `CONFLICT` is shown in the catalog dialog

#### Scenario: No response

- **WHEN** the user unmarks a favorite and the request gets no response
- **THEN** the star stays filled
- **AND** the message for `NETWORK` is shown in the catalog dialog

## MODIFIED Requirements

### Requirement: Catalog filters combine and are local

Typing SHALL keep only exercises whose name in the active language contains the text, ignoring case and accents.
Selecting a pill SHALL keep only exercises of that type, body part or muscle; selecting the selected pill again SHALL
clear it. The selected section SHALL narrow the catalog first, and the text and pills SHALL apply on top of it. All
the filters SHALL apply together, and filtering SHALL NOT send any request. No match SHALL show "Ningún ejercicio
coincide con el filtro.". The section and the filters SHALL be kept when the catalog dialog is closed and opened again
on the same screen.

#### Scenario: Filter by text

- **WHEN** the user types "sentadi"
- **THEN** only exercises whose name contains "sentadi" (such as "Sentadilla") are shown

#### Scenario: Filter by body part and text together

- **WHEN** the user selects the pill "Piernas" and types "prensa"
- **THEN** only legs exercises whose name contains "prensa" are shown

#### Scenario: Filter by type and body part together

- **WHEN** the user selects the pills "Fuerza" and "Piernas"
- **THEN** only strength exercises of the legs are shown

#### Scenario: Filter by section and body part together

- **WHEN** the user selects "Favoritos" and the pill "Piernas"
- **THEN** only favorite legs exercises are shown
- **AND** no request is sent

#### Scenario: Clear the body part

- **WHEN** the user selects the already selected pill "Piernas"
- **THEN** no pill is selected and the body part no longer filters the list

#### Scenario: Nothing matches

- **WHEN** no exercise matches the filters
- **THEN** "Ningún ejercicio coincide con el filtro." is shown

#### Scenario: Filters kept on reopening

- **WHEN** the user types "prensa", closes the catalog dialog and clicks "Añadir ejercicio" again
- **THEN** the filter field holds "prensa" and only matching exercises are shown

#### Scenario: Type kept on reopening

- **WHEN** the user selects "Cardio", closes the catalog dialog and opens it again
- **THEN** "Cardio" is still selected and only cardio exercises are shown

#### Scenario: Section kept on reopening

- **WHEN** the user selects "Mis ejercicios", closes the catalog dialog and opens it again
- **THEN** "Mis ejercicios" is still selected and only the user's own exercises are shown

### Requirement: Añadir ejercicio opens the catalog dialog

The "Ejercicios" card SHALL show an "Añadir ejercicio" button in its header. Activating it SHALL open the "Catálogo de
ejercicios" dialog, at least 90% of the viewport width on wide screens, so the section sidebar fits beside the filters
and the list, without sending any request.

#### Scenario: Catalog opened

- **WHEN** the user clicks "Añadir ejercicio"
- **THEN** the "Catálogo de ejercicios" dialog is shown
- **AND** no request is sent

### Requirement: Catalog exercises are cards in rows of up to three

In the catalog dialog, each exercise SHALL be shown in its own bordered card, with its name, its favorite star and the
"Elegir ejercicio" button on the first row and its body-part and muscle labels below. The cards SHALL flow left to
right in rows of at most three, wrapping onto the next row, with fewer per row on narrower screens. The dialog SHALL
keep the same height whatever the filters leave, and the list SHALL scroll inside it.

#### Scenario: Catalog as cards

- **WHEN** the catalog dialog lists three exercises on a wide screen
- **THEN** each one is in its own bordered card, and the three cards share one row from left to right

#### Scenario: Same height whatever the filters leave

- **WHEN** the catalog dialog is open and a filter leaves no exercise to list
- **THEN** the dialog keeps the height it had with the full list

#### Scenario: Star beside the choose button

- **WHEN** the catalog dialog lists an exercise
- **THEN** its card's first row shows its name, then its star, then "Elegir ejercicio"
