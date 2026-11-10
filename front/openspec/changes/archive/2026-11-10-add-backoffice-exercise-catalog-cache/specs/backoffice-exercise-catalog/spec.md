# Spec Delta

## MODIFIED Requirements

### Requirement: The exercise catalog screen lists the catalog

The screen at `/exercises` SHALL be reachable by a signed-in `ADMIN` or `TRAINER`, SHALL show the heading "Catálogo de
ejercicios", SHALL use the session's exercise catalog and SHALL read the user's favorites once when it opens. It SHALL show the same content as
the workout screen's catalog dialog: the section sidebar ("Todos", "Mis ejercicios", "Favoritos"), the text filter, the
type, body-part and muscle pills, and every catalog exercise as a card with its name and labels. Because no workout is
being built, no exercise SHALL be hidden for being in one. The sections and filters SHALL narrow the list exactly as in
the dialog, without sending any request.

#### Scenario: Catalog listed

- **WHEN** a trainer opens `/exercises` and the catalog holds three exercises
- **THEN** the heading "Catálogo de ejercicios" and the sections "Todos", "Mis ejercicios" and "Favoritos" are shown
- **AND** "Todos" is selected and the three exercises are listed
- **AND** exactly one read of the favorites and no read of the catalog are sent, the catalog having been read after
  sign-in

#### Scenario: Sections and filters apply

- **WHEN** the user selects "Mis ejercicios" and types a name
- **THEN** only the exercises the user owns whose name contains the text are listed
- **AND** no request is sent

#### Scenario: No match

- **WHEN** no exercise matches the section and the filters
- **THEN** "Ningún ejercicio coincide con el filtro." is shown

#### Scenario: Catalog fails to load

- **WHEN** the catalog read fails
- **THEN** the screen shows the message for the error code with a way to retry
- **AND** no exercise and no "Añadir ejercicio" button is shown

#### Scenario: Admin sees the same catalog

- **WHEN** an admin opens `/exercises`
- **THEN** the same sections, filters and cards are shown as to a trainer

### Requirement: Crear adds the exercise to the user's own

With a valid form, "Crear" SHALL send exactly one creation with the trimmed name, the chosen muscle and type, the
trimmed link only when one was typed, and the language the screen is shown in. While it is in progress the fields and
both buttons SHALL be disabled. When the API accepts it, the dialog SHALL close and the created exercise SHALL be in the
catalog list and under "Mis ejercicios" at once, and the session's catalog SHALL then be read again in the background. The selected section and the filters SHALL
be kept.

#### Scenario: Exercise created

- **WHEN** the user types the name "Remo con banda", chooses the muscle "Espalda media" and the type "Fuerza", and
  clicks "Crear", and the API answers 201 with an exercise owned by the user
- **THEN** exactly one creation is sent with `languageCode` `es`, that name, `MIDDLE_BACK` and `STRENGTH`, and no link
- **AND** the dialog closes
- **AND** "Remo con banda" is listed under "Todos" and under "Mis ejercicios"
- **AND** exactly one new read of the catalog is sent, and the list stays visible while it runs

#### Scenario: Created in the screen's language

- **WHEN** the screen is shown in English and the user creates an exercise
- **THEN** the creation is sent with `languageCode` `en`

#### Scenario: In progress

- **WHEN** the user clicks "Crear" and the API has not answered yet
- **THEN** the fields, "Crear" and "Cancelar" are disabled

#### Scenario: Link sent

- **WHEN** the user types a valid link and clicks "Crear"
- **THEN** the creation carries that link, trimmed

## ADDED Requirements

### Requirement: Each section shows how many exercises it holds

The section sidebar of the exercise catalog, on the catalog screen and in the catalog dialog of the workout and template
editors, SHALL show each section's name followed by the number of exercises it holds, as "Todos (4)", "Mis ejercicios
(1)", "Favoritos (2)". The count SHALL cover the section before the text and the pills narrow it, so it does not change
while the user filters. "Favoritos" SHALL show no count until the favorites are read, nor when they fail to load.

#### Scenario: Counts per section

- **WHEN** the catalog holds four exercises, one owned by the user, and two of them are favorites
- **THEN** the sections read "Todos (4)", "Mis ejercicios (1)" and "Favoritos (2)"

#### Scenario: Filters do not change the counts

- **WHEN** the user types a name that matches one exercise
- **THEN** the sections still read "Todos (4)", "Mis ejercicios (1)" and "Favoritos (2)"

#### Scenario: Favorites not available

- **WHEN** the favorites fail to load
- **THEN** the sections read "Todos (4)", "Mis ejercicios (1)" and "Favoritos"

#### Scenario: English

- **WHEN** the screen is shown in English
- **THEN** the sections read "All (n)", "My exercises (n)" and "Favorites (n)"
