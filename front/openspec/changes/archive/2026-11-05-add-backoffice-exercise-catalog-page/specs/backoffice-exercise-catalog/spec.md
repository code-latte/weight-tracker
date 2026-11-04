# Spec Delta

## Purpose

The backoffice screen where a trainer or an admin browses the exercise catalog, manages their favorites and, as a
trainer, adds exercises of their own, outside any workout.

## ADDED Requirements

### Requirement: The exercise catalog screen lists the catalog

The screen at `/exercises` SHALL be reachable by a signed-in `ADMIN` or `TRAINER`, SHALL show the heading "Catálogo de
ejercicios" and SHALL read the catalog and the user's favorites once when it opens. It SHALL show the same content as
the workout screen's catalog dialog: the section sidebar ("Todos", "Mis ejercicios", "Favoritos"), the text filter, the
type, body-part and muscle pills, and every catalog exercise as a card with its name and labels. Because no workout is
being built, no exercise SHALL be hidden for being in one. The sections and filters SHALL narrow the list exactly as in
the dialog, without sending any request.

#### Scenario: Catalog listed

- **WHEN** a trainer opens `/exercises` and the catalog holds three exercises
- **THEN** the heading "Catálogo de ejercicios" and the sections "Todos", "Mis ejercicios" and "Favoritos" are shown
- **AND** "Todos" is selected and the three exercises are listed
- **AND** exactly one read of the catalog and one read of the favorites are sent

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

### Requirement: Cards with a video offer Ver vídeo

Each card of the catalog screen whose exercise has a video SHALL show a "Ver vídeo" button with a player icon, below
the card's labels. A card whose exercise has no video SHALL show nothing in its place. Activating the button SHALL open
a dialog titled with the exercise's name that embeds the video when it is a YouTube link and otherwise offers the link
in a new tab. "Cerrar", Escape and a click outside SHALL close it, and closing it SHALL stop the video. The workout
screen's catalog dialog SHALL NOT offer it.

#### Scenario: Button only with a video

- **WHEN** the catalog screen lists an exercise with a video and one without
- **THEN** the first card shows "Ver vídeo" and the second shows no such button

#### Scenario: Video played

- **WHEN** the user clicks "Ver vídeo" on an exercise whose video is a YouTube link
- **THEN** a dialog titled with the exercise's name shows the embedded video

#### Scenario: Other video address

- **WHEN** the video is not a YouTube link
- **THEN** the dialog offers the address as a link that opens in a new tab

#### Scenario: Closed

- **WHEN** the user clicks "Cerrar"
- **THEN** the dialog and its video are gone

#### Scenario: Not in the selector

- **WHEN** the workout screen's catalog dialog lists an exercise that has a video
- **THEN** its card shows no "Ver vídeo" button

### Requirement: Only the list scrolls on the catalog screen

The sections, the text filter and the pills SHALL stay fixed on the catalog screen while the exercise cards scroll
inside their own area, as in the workout screen's catalog dialog. The screen itself SHALL NOT scroll while the viewport
is tall enough for the filters and some cards.

#### Scenario: List scrolled

- **WHEN** the catalog holds more exercises than fit and the user scrolls the list to its end
- **THEN** the section list, the text filter and the pills have not moved
- **AND** the page itself has not scrolled

### Requirement: No exercise on the catalog screen can be chosen

The cards of the catalog screen SHALL NOT show the "Elegir ejercicio" button, for any role. Nothing on the screen
SHALL add an exercise to a workout.

#### Scenario: No choose button

- **WHEN** a trainer or an admin opens `/exercises` with exercises in the catalog
- **THEN** no "Elegir ejercicio" button is shown
- **AND** every card shows its favorite star

### Requirement: Favorites are managed from the catalog screen

Each card of the catalog screen SHALL show the favorite star, and it SHALL mark and unmark the exercise, be disabled
while in progress or while the favorites are not loaded, and show the refusal, exactly as in the workout screen's
catalog dialog. "Favoritos" SHALL list the marked exercises and SHALL be disabled, with its load message shown, when
the favorites fail to load. The catalog SHALL stay usable when the favorites fail.

#### Scenario: Marked

- **WHEN** the user clicks "Añadir a favoritos" on an exercise and the API accepts it
- **THEN** exactly one mark of that exercise is sent
- **AND** its star is filled and labelled "Quitar de favoritos"
- **AND** the exercise is listed under "Favoritos"

#### Scenario: Unmarked under Favoritos

- **WHEN** "Favoritos" is selected and the user unmarks a listed exercise, and the API accepts it
- **THEN** that exercise is no longer listed
- **AND** no new read of the favorites is sent

#### Scenario: Refused

- **WHEN** the user marks an exercise and the API answers `CONFLICT`
- **THEN** the star stays outlined and the message for `CONFLICT` is shown

#### Scenario: Favorites fail to load

- **WHEN** the favorites read fails
- **THEN** "No se pudieron cargar los favoritos" is shown, the stars and "Favoritos" are disabled
- **AND** "Todos" still lists the catalog

### Requirement: Only a trainer can add an exercise

The screen SHALL show an "Añadir ejercicio" button beside the heading for a `TRAINER`, once the catalog has loaded.
An `ADMIN` SHALL NOT see it, because the API does not let an admin create exercises. Activating it SHALL open the
"Nuevo ejercicio" dialog without sending any request.

#### Scenario: Trainer sees the button

- **WHEN** a trainer opens `/exercises`
- **THEN** the "Añadir ejercicio" button is shown

#### Scenario: Admin does not

- **WHEN** an admin opens `/exercises`
- **THEN** no "Añadir ejercicio" button is shown

#### Scenario: Dialog opened

- **WHEN** the trainer clicks "Añadir ejercicio"
- **THEN** the "Nuevo ejercicio" dialog is shown with the empty fields "Nombre", "Músculo", "Tipo" and "Enlace de
  vídeo"
- **AND** no request is sent

### Requirement: The new exercise form is checked before sending

"Crear" SHALL check the form structurally with the limits of the exercise form check and, when any field is invalid,
SHALL show the message of each invalid field beside it and send nothing. The name SHALL be required and between 3 and
150 characters, "Músculo" and "Tipo" SHALL be required, and "Enlace de vídeo" SHALL be optional and, when given, a web
address. The muscle and the type SHALL be chosen from the same names and labels the catalog filters use.

#### Scenario: Empty form

- **WHEN** the user clicks "Crear" with every field empty
- **THEN** a message is shown beside "Nombre", "Músculo" and "Tipo"
- **AND** no request is sent

#### Scenario: Name too short

- **WHEN** the user types "ab" as the name and clicks "Crear"
- **THEN** a message is shown beside "Nombre" and no request is sent

#### Scenario: Invalid link

- **WHEN** the user types "no es un enlace" as the link and clicks "Crear"
- **THEN** a message is shown beside "Enlace de vídeo" and no request is sent

### Requirement: Muscles are grouped by body part

The "Músculo" choices SHALL be grouped by body part. Each group SHALL start with a header written between dashes, such
as "— Brazos —", followed by the group's muscles. Groups and the muscles in each group SHALL be sorted by their name in
the active language. A header SHALL NOT be selectable, and choosing a muscle SHALL send that muscle, not its group.

#### Scenario: Groups shown

- **WHEN** the user opens "Músculo"
- **THEN** the choices read "— Brazos —", "Antebrazos", "Bíceps", "Tríceps", "— Core —", "Abdominales", "Zona lumbar"
  and so on, one header per body part followed by its muscles

#### Scenario: Header clicked

- **WHEN** the user clicks the header "— Espalda —"
- **THEN** no muscle is chosen and "Músculo" stays empty

#### Scenario: Muscle chosen under a group

- **WHEN** the user chooses "Dorsales" under "— Espalda —" and creates the exercise
- **THEN** the creation carries the muscle `LATS`

### Requirement: Crear adds the exercise to the user's own

With a valid form, "Crear" SHALL send exactly one creation with the trimmed name, the chosen muscle and type, the
trimmed link only when one was typed, and the language the screen is shown in. While it is in progress the fields and
both buttons SHALL be disabled. When the API accepts it, the dialog SHALL close and the created exercise SHALL be in the
catalog list and under "Mis ejercicios", without reading the catalog again. The selected section and the filters SHALL
be kept.

#### Scenario: Exercise created

- **WHEN** the user types the name "Remo con banda", chooses the muscle "Espalda media" and the type "Fuerza", and
  clicks "Crear", and the API answers 201 with an exercise owned by the user
- **THEN** exactly one creation is sent with `languageCode` `es`, that name, `MIDDLE_BACK` and `STRENGTH`, and no link
- **AND** the dialog closes
- **AND** "Remo con banda" is listed under "Todos" and under "Mis ejercicios"
- **AND** no new read of the catalog is sent

#### Scenario: Created in the screen's language

- **WHEN** the screen is shown in English and the user creates an exercise
- **THEN** the creation is sent with `languageCode` `en`

#### Scenario: In progress

- **WHEN** the user clicks "Crear" and the API has not answered yet
- **THEN** the fields, "Crear" and "Cancelar" are disabled

#### Scenario: Link sent

- **WHEN** the user types a valid link and clicks "Crear"
- **THEN** the creation carries that link, trimmed

### Requirement: A refused creation is shown and nothing is lost

When the API refuses the creation, the dialog SHALL stay open with every typed value kept and SHALL show the message
for the error code. Closing and opening the dialog again SHALL start from empty fields and no message.

#### Scenario: Validation refused

- **WHEN** the API answers `VALIDATION`
- **THEN** the dialog stays open, the fields keep their values and the message for `VALIDATION` is shown

#### Scenario: No response

- **WHEN** the creation gets no response
- **THEN** the dialog stays open with the typed values and the message for `NETWORK` is shown

#### Scenario: Reopened

- **WHEN** a refused dialog is closed with "Cancelar" and opened again
- **THEN** its fields are empty and no message is shown

### Requirement: The new exercise dialog can be dismissed

"Cancelar", Escape and a click outside the "Nuevo ejercicio" dialog SHALL close it without sending any request, except
while the creation is in progress.

#### Scenario: Cancelled

- **WHEN** the dialog is open with typed values and the user clicks "Cancelar"
- **THEN** the dialog closes and no request is sent
- **AND** the list is unchanged

### Requirement: The exercise catalog screen is translated

Every text of the screen and of the dialog SHALL be shown in the language of the interface, Spanish or English, and the
exercise names SHALL follow the same rule as the rest of the backoffice: the name in the active language, else the
first translation.

#### Scenario: Screen in English

- **WHEN** the language is English and a trainer opens `/exercises`
- **THEN** the heading reads "Exercise catalog", the button "Add exercise" and the dialog "New exercise"
