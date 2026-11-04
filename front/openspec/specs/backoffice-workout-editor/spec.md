# backoffice-workout-editor Specification

## Purpose

Gives trainers and admins one backoffice screen per workout, showing the exercise catalog, the workout with its
exercises, and the configuration of the exercise they select, as the base of the workout editor.

## Requirements

### Requirement: The workout screen is reached by the workout id

The backoffice SHALL offer a signed-in screen at `/workout?id=<workout id>` that reads that workout and the exercise
catalog once on opening. A visitor without a session SHALL end on `/login`.

#### Scenario: Trainer opens a workout

- **WHEN** a trainer with a stored session opens `/workout?id=w-1`
- **THEN** exactly one read of workout `w-1` and one read of the exercise catalog are sent
- **AND** the screen is shown beside the navigation drawer

#### Scenario: Visitor without a session

- **WHEN** a visitor with no stored session opens `/workout?id=w-1`
- **THEN** the visitor ends on `/login` and no workout is read

### Requirement: The screen has a page header

Above the cards, the screen SHALL show the page heading "Editar entrenamiento" and a "Volver" link to
`/assigned-plans`. The "Guardar" button that saves the workout's data SHALL sit inside the properties card, after its
fields.

#### Scenario: Header shown

- **WHEN** workout "Pierna" is loaded
- **THEN** the page heading reads "Editar entrenamiento"
- **AND** a "Volver" link leads to `/assigned-plans`
- **AND** the properties card shows "Guardar" after Descripción

### Requirement: Guardar checks the workout's data and sends nothing when invalid

On "Guardar", the screen SHALL check the card's fields structurally and, if any fails, SHALL show the message under
that field and SHALL NOT send any request. Nombre is required and at most 256 characters after trimming; Descripción
is optional and at most 1024 characters; Semana always holds a week, so it is never reported.

#### Scenario: Blank name

- **WHEN** the user clears Nombre and clicks "Guardar"
- **THEN** Nombre shows "Este campo es obligatorio." and no request is sent

#### Scenario: Description too long

- **WHEN** the description has 1025 characters and the user clicks "Guardar"
- **THEN** Descripción shows "No puede superar los 1024 caracteres." and no request is sent

### Requirement: Guardar replaces the workout's editable data

When the fields are valid, "Guardar" SHALL send exactly one update of the workout with its id, the trimmed name, the
Wednesday of the selected week as its date, the trimmed description or none when blank, and the athlete and trainer
exactly as they were read. While it is in progress, "Guardar" SHALL be disabled.

#### Scenario: Workout saved

- **WHEN** workout `w-1` dated 2026-11-02 is loaded, the user changes the name to " Pierna B " and clicks "Guardar"
- **THEN** exactly one update of workout `w-1` is sent with name "Pierna B", date "2026-11-04" (the Wednesday of the
  loaded week), description "Día 1", and the loaded athlete and trainer

#### Scenario: Week changed

- **WHEN** workout `w-1` dated 2026-11-02 is loaded, the user clicks "Semana anterior" and clicks "Guardar"
- **THEN** the update carries the date "2026-10-28"

#### Scenario: Description cleared

- **WHEN** the user empties Descripción and clicks "Guardar"
- **THEN** the update carries no description

#### Scenario: Save in progress

- **WHEN** the update has been sent and has not answered yet
- **THEN** "Guardar" is disabled

### Requirement: The save outcome is shown

When the update succeeds, the screen SHALL show "Entrenamiento guardado.". Editing a field again SHALL hide the
confirmation. When it fails, the screen SHALL show the message for the error code and keep the typed values.

#### Scenario: Saved

- **WHEN** the API accepts the update with the name "Pierna B"
- **THEN** "Entrenamiento guardado." is shown and the Nombre field holds "Pierna B"

#### Scenario: Rejected

- **WHEN** the API answers 403
- **THEN** "No tienes permiso para realizar esta acción." is shown
- **AND** the fields keep the typed values

### Requirement: Catalog filters combine and are local

Typing SHALL split the text into words and keep only the exercises whose names contain every word, in any order and
anywhere in the name, ignoring case, accents and extra spaces; a word may be only part of a longer one. With English
selected, the names looked in SHALL be only the exercise's English name; with any other language, they SHALL be the
name in that language and the English name. An exercise with no English name SHALL be looked up by the name it is
shown with. A text of only spaces SHALL keep every exercise.
Selecting a pill SHALL keep only exercises of that type, body part or muscle; selecting the selected pill again SHALL
clear it. The selected section SHALL narrow the catalog first, and the text and pills SHALL apply on top of it. All
the filters SHALL apply together, and filtering SHALL NOT send any request. No match SHALL show "Ningún ejercicio
coincide con el filtro.". The section and the filters SHALL be kept when the catalog dialog is closed and opened again
on the same screen.

#### Scenario: Filter by text

- **WHEN** the user types "sentadi"
- **THEN** only exercises whose name contains "sentadi" (such as "Sentadilla") are shown

#### Scenario: Every word must appear

- **WHEN** the user types "Curl bíceps" and an exercise is named "Curl de bíceps"
- **THEN** that exercise is shown

#### Scenario: Words in any order

- **WHEN** the user types "polea tríceps" and an exercise is named "Extensión de tríceps en polea"
- **THEN** that exercise is shown, and exercises that lack one of the words are not

#### Scenario: Spanish also looks in English

- **WHEN** Spanish is selected and the user types "Squat"
- **THEN** the exercise shown as "Sentadilla" is listed

#### Scenario: English looks only in English

- **WHEN** English is selected and the user types "Sentadilla"
- **THEN** no exercise is listed for that text
- **AND** typing "Squat" lists the squat

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

### Requirement: Each exercise shows its name, body part and muscle

Each exercise, in the catalog and in the workout, SHALL show its name in the active language and, below it, labels with
its body part and muscle translated. An unknown code SHALL be shown as it comes. A workout exercise SHALL take these
from the catalog exercise with the same id; with no match it SHALL show its exercise id as the name and no labels.

#### Scenario: Labels in Spanish

- **WHEN** an exercise of body part `LEGS` and muscle `QUADRICEPS` is shown in Spanish, and the catalog has legs
  exercises for another muscle too
- **THEN** its labels read "Piernas" and "Cuádriceps"

#### Scenario: Name in the active language

- **WHEN** an exercise has the names "Sentadilla" (es) and "Squat" (en) and the language is English
- **THEN** it is shown as "Squat"

#### Scenario: Workout exercise missing from the catalog

- **WHEN** a workout exercise has an exercise id the catalog does not contain
- **THEN** it is shown with that id as its name and without labels

### Requirement: Body-part labels are coloured by body part

The body-part label of an exercise SHALL use a colour of its own for each body part, the same everywhere on the
screen, filled with that colour; an unknown body part SHALL use the neutral colour. The muscle label SHALL use the
colour of the exercise's body part as its outline only. The body-part and muscle filter pills SHALL use the same
colours, outlined while unselected and filled while selected.

#### Scenario: Chest and legs differ

- **WHEN** a chest exercise and a legs exercise are shown
- **THEN** their body-part labels are filled with different colours
- **AND** each muscle label shown is outlined in its body part's colour

#### Scenario: Pills follow the labels

- **WHEN** the pill "Piernas" is selected in the catalog
- **THEN** it is filled with the legs colour, the other body-part pills are outlined in their colours, and the muscle
  pills are outlined in the legs colour

### Requirement: The catalog hides exercises already in the workout

The catalog SHALL NOT list an exercise that is already saved in the workout, so the same exercise cannot be added
twice.

#### Scenario: Saved exercise hidden

- **WHEN** the workout already holds "Sentadilla"
- **THEN** the catalog does not list "Sentadilla"

#### Scenario: Added exercise leaves the catalog

- **WHEN** the API accepts the addition of "Prensa de piernas" and the user opens the catalog again
- **THEN** the catalog no longer lists "Prensa de piernas"

### Requirement: A removal in progress or refused is shown

While a removal is in progress, its "Quitar ejercicio" button SHALL be disabled. A failed removal SHALL show the
message for the error code in the exercises card and keep the exercise listed.

#### Scenario: Removal rejected

- **WHEN** the API answers 409 to the removal
- **THEN** "La operación entra en conflicto con el estado actual." is shown and the exercise stays listed

### Requirement: Dropping a saved exercise reorders the list and saves the order

Dragging a saved exercise by its handle and dropping it at another position SHALL reorder the list on screen at once,
renumbering it. It SHALL then send exactly one reorder of the workout with the catalog ids of every saved exercise in
the new order. On success the list SHALL keep the new order.

#### Scenario: Exercise moved down

- **WHEN** the workout lists "Sentadilla" then "Press banca" and the user drops "Sentadilla" after "Press banca"
- **THEN** "Press banca" is listed as 1 and "Sentadilla" as 2 before the API answers
- **AND** exactly one reorder is sent with the ids of "Press banca" and "Sentadilla" in that order

#### Scenario: Reorder accepted

- **WHEN** the API accepts the reorder
- **THEN** the list keeps the new order and no error is shown

### Requirement: Dropping an exercise at its own position sends nothing

Dropping an exercise where it was, or cancelling the drag, SHALL leave the order unchanged and SHALL NOT send a request.

#### Scenario: Dropped in place

- **WHEN** the user picks up "Sentadilla" and drops it at its own position
- **THEN** the order is unchanged and no reorder is sent

#### Scenario: Drag cancelled

- **WHEN** the user picks up "Sentadilla", moves it and presses Escape
- **THEN** the order is unchanged and no reorder is sent

### Requirement: A refused reorder restores the previous order

When the reorder fails, the list SHALL go back to the order it had before the drop, and the message for the error code
SHALL be shown in the exercises card.

#### Scenario: Reorder rejected

- **WHEN** the API answers 409 to the reorder
- **THEN** the exercises are listed in the order they had before the drop
- **AND** "La operación entra en conflicto con el estado actual." is shown

### Requirement: Dragging is disabled while a reorder is in progress

While a reorder request is in progress, every drag handle SHALL be disabled, so a second reorder cannot start before
the first one is answered.

#### Scenario: Reorder in progress

- **WHEN** a reorder has been sent and the API has not answered yet
- **THEN** every "Reordenar ejercicio" handle is disabled

### Requirement: Exercises can be reordered from the keyboard

A focused drag handle SHALL pick up its exercise with Space or Enter. The arrow keys SHALL move it across the rows of
cards, Space or Enter SHALL drop it, and Escape SHALL cancel. Screen readers SHALL get the pick-up, move, drop and
cancel announcements in the active language.

#### Scenario: Keyboard reorder

- **WHEN** the user focuses the handle of exercise 1, presses Space, ArrowRight and Space
- **THEN** that exercise is listed as 2 and a reorder is sent

### Requirement: The sets of an exercise are listed with a delete action

The configuration dialog SHALL list the exercise's sets in order, each as a summary by target type, such as "2 × 15
repeticiones", "1 × 10–12 repeticiones", "3 × 40 s", "1 × máximo" or "3 × 1:30 min", with a "Quitar serie" trash button
on its right. A minutes target SHALL be shown as `M` or `M:SS`, never as a decimal. Removing a set SHALL take it out of
the list only; nothing is sent until "Guardar".

#### Scenario: Several sets listed

- **WHEN** an exercise has the sets 2 × 15, 1 × 10–12 and 1 × max reps
- **THEN** the list shows "2 × 15 repeticiones", "1 × 10–12 repeticiones" and "1 × máximo", in that order

#### Scenario: Stored minutes sets listed

- **WHEN** an exercise is stored with the sets 3 × `minutes` 1.5 and 1 × `minutes` 20
- **THEN** the list shows "3 × 1:30 min" and "1 × 20 min"

#### Scenario: Set removed

- **WHEN** the user clicks "Quitar serie" on the second set
- **THEN** that set leaves the list and no request is sent

### Requirement: The set form adds a set to the list

Below the list, a form SHALL ask for Series, Tipo, and the target fields the type needs:

- Valor for repetitions and seconds;
- Valor and Máximo for a repetition range;
- none for max reps;
- Valor for minutes, typed as `M` or `M:SS` with the hint "m:ss".

Tipo SHALL offer "Minutos" after the other four types. "Añadir serie" SHALL check the form structurally and show the
message under each invalid field. Otherwise it SHALL append the set to the end of the list and empty the form.

#### Scenario: Set added

- **WHEN** the user enters Series 2, Tipo "Repeticiones", Valor 15 and clicks "Añadir serie"
- **THEN** "2 × 15 repeticiones" is appended to the list and the form is empty again
- **AND** no request is sent

#### Scenario: Minutes set added

- **WHEN** the user enters Series 3, Tipo "Minutos", Valor `1:30` and clicks "Añadir serie"
- **THEN** "3 × 1:30 min" is appended to the list and no request is sent

#### Scenario: Max reps needs no value

- **WHEN** the user picks Tipo "Máximas repeticiones"
- **THEN** the form shows no Valor nor Máximo field

#### Scenario: Invalid set

- **WHEN** the user clicks "Añadir serie" with Series empty or below 1, or a needed Valor or Máximo empty or below 1,
  for a type other than minutes
- **THEN** each of those fields shows "Introduce un número entero mayor que 0." and the list is unchanged

#### Scenario: Invalid minutes

- **WHEN** the user clicks "Añadir serie" with Tipo "Minutos" and Valor `1.5`, `1:60`, `0` or empty
- **THEN** Valor shows "Usa minutos (2) o minutos:segundos (1:30)." and the list is unchanged

### Requirement: The exercise video is embedded when it is a YouTube link

When the exercise's video is a YouTube watch link, the configuration dialog SHALL embed it as a player from
`https://www.youtube-nocookie.com/embed/<video id>`, titled with the exercise name. Any other link SHALL be shown as a
"Ver vídeo" link opening in a new tab. An exercise without video SHALL show no video section.

#### Scenario: YouTube video

- **WHEN** the video is `https://www.youtube.com/watch?v=h_yZV27H684`
- **THEN** a player from `https://www.youtube-nocookie.com/embed/h_yZV27H684` is shown

#### Scenario: Other link

- **WHEN** the video is `https://example.com/squat.mp4`
- **THEN** a "Ver vídeo" link to that address opening in a new tab is shown and no player

#### Scenario: No video

- **WHEN** the exercise has no video
- **THEN** no video section is shown

### Requirement: The screen shows loading, not found and failure states

While the workout or the catalog loads, the screen SHALL show a loading placeholder. A missing `id`, or a workout the
API does not find, SHALL show "No hemos encontrado este entrenamiento." with a "Volver a planes asignados" link to
`/assigned-plans`. Any other failure SHALL show the error for its code with a retry that reads again what failed.

#### Scenario: Missing id

- **WHEN** the visitor opens `/workout` without `id`
- **THEN** "No hemos encontrado este entrenamiento." and the link back are shown
- **AND** no workout read is sent

#### Scenario: Unknown workout

- **WHEN** the API answers 404 for the workout
- **THEN** "No hemos encontrado este entrenamiento." and the link back are shown

#### Scenario: Catalog fails and the user retries

- **WHEN** the catalog read fails with no response and the user clicks "Reintentar"
- **THEN** the catalog is read again and shown when it succeeds

### Requirement: The workout screen is translated

Every text of the screen and its dialogs, including muscle, body-part and effort labels, SHALL be available in Spanish
and English and SHALL follow the active language.

#### Scenario: Screen in English

- **WHEN** the active language is English
- **THEN** the screen shows "Workout properties", "Add exercise", "Save" and, in the catalog dialog, "Search exercise"
  and the label "Quadriceps"

### Requirement: The screen shows two stacked cards at full width

The workout screen SHALL use the full width of the page beside the navigation drawer. It SHALL show two cards, one
above the other and each taking the full width, on every screen size: "Propiedades del entrenamiento" on top and
"Ejercicios" below. The page SHALL NOT scroll horizontally.

#### Scenario: Cards in order

- **WHEN** the workout and the catalog are loaded
- **THEN** the card "Propiedades del entrenamiento" is shown above the card "Ejercicios", both at full width
- **AND** neither the catalog nor the exercise configuration is shown on the page

### Requirement: Añadir ejercicio opens the catalog dialog

The "Ejercicios" card SHALL show an "Añadir ejercicio" button in its header. Activating it SHALL open the "Catálogo de
ejercicios" dialog, at least 90% of the viewport width on wide screens, so the section sidebar fits beside the filters
and the list, without sending any request.

#### Scenario: Catalog opened

- **WHEN** the user clicks "Añadir ejercicio"
- **THEN** the "Catálogo de ejercicios" dialog is shown
- **AND** no request is sent

### Requirement: The catalog dialog can be dismissed

The catalog dialog SHALL close with its "Cerrar" button, with Escape and with a click outside it. Closing it SHALL add
nothing to the workout and send no request.

#### Scenario: Catalog dismissed with Escape

- **WHEN** the catalog dialog is open and the user presses Escape
- **THEN** the dialog closes, the workout's list is unchanged and no request is sent

#### Scenario: Catalog dismissed with Cerrar

- **WHEN** the catalog dialog is open and the user clicks "Cerrar"
- **THEN** the dialog closes and no request is sent

### Requirement: Choosing a catalog exercise opens its configuration

Each exercise in the catalog dialog SHALL show an "Elegir ejercicio" plus icon button on its right. Activating it SHALL
close the catalog dialog and open the configuration dialog for that exercise, with no effort, no sets and an empty set
form. It SHALL NOT send any request and SHALL NOT add the exercise to the list.

#### Scenario: Exercise chosen

- **WHEN** the user clicks "Elegir ejercicio" on "Prensa de piernas"
- **THEN** the catalog dialog closes and the configuration dialog shows "Prensa de piernas"
- **AND** its effort is empty, its list of sets is empty and the set form is empty
- **AND** the workout's list is unchanged and no request is sent

### Requirement: The configuration dialog can be dismissed

The configuration dialog SHALL close with its "Cancelar" button, with Escape and with a click outside it, except while
its save is in progress. Closing it SHALL discard the edits and send no request. Closing it while adding SHALL leave the
exercise out of the workout and still listed in the catalog.

#### Scenario: Add dismissed

- **WHEN** the configuration dialog is open for "Prensa de piernas" being added, the user adds a set and clicks
  "Cancelar"
- **THEN** the dialog closes, "Prensa de piernas" is not in the workout's list and no request is sent
- **AND** reopening the catalog lists "Prensa de piernas"

#### Scenario: Edit dismissed

- **WHEN** the user opens "Sentadilla" with the pencil, changes its effort and presses Escape
- **THEN** the dialog closes and no request is sent
- **AND** opening "Sentadilla" again shows its stored effort

#### Scenario: Save in progress

- **WHEN** the save has been sent and has not answered yet
- **THEN** "Cancelar" is disabled and Escape does not close the dialog

### Requirement: Each listed exercise shows its sets and effort

Under its labels, each exercise of the workout SHALL show:

- its sets on one line, without a label, each as its count and target only ("2 × 15", "1 × 10–12", "3 × 40 s",
  "1 × máx", "3 × 1:30 min"), separated by commas, and ending with "· por lado" when the exercise is per side;
- its effort on the next line;
- its rest on the line after, as "Descanso: 1:30 min".

A line SHALL NOT be shown while its property is empty.

#### Scenario: Sets and effort shown

- **WHEN** an exercise has effort `HIGH`, the sets 2 × 15, 1 × 10–12 and 1 × max reps, and no rest
- **THEN** its item shows "2 × 15, 1 × 10–12, 1 × máx" on one line
- **AND** "Esfuerzo: Alto" on the next line
- **AND** no rest line

#### Scenario: Minutes and rest shown

- **WHEN** an exercise has effort `MID`, the set 1 × `minutes` 20 and `restMinutes` 1.5
- **THEN** its item shows "1 × 20 min", then "Esfuerzo: Medio", then "Descanso: 1:30 min"

#### Scenario: No exercises

- **WHEN** the workout has no exercises
- **THEN** "Este entrenamiento aún no tiene ejercicios." is shown

### Requirement: The exercises card has an add button and grows with its list

The workout's exercises SHALL be preceded by the "Ejercicios" card title with the "Añadir ejercicio" button beside it,
and their list SHALL have no maximum height and no scroll of its own: the card SHALL grow with its rows and the page
SHALL scroll instead.

#### Scenario: Add button in the exercises card

- **WHEN** the exercises card is shown
- **THEN** it shows the "Ejercicios" title and an "Añadir ejercicio" button

#### Scenario: Long workout grows the card

- **WHEN** the workout's exercises are shown
- **THEN** their list has no maximum height and does not scroll on its own

### Requirement: Quitar ejercicio removes a saved exercise from the workout

"Quitar ejercicio" SHALL send exactly one removal of that catalog exercise from the workout, and on success SHALL take
it out of the list and list it again in the catalog.

#### Scenario: Saved exercise removed

- **WHEN** the user clicks "Quitar ejercicio" on "Sentadilla"
- **THEN** exactly one removal of "Sentadilla" from the workout is sent
- **AND** "Sentadilla" leaves the workout's list and is listed again in the catalog

### Requirement: Guardar in the configuration dialog saves the effort and sets

"Guardar" in the configuration dialog SHALL require an effort ("Elige un esfuerzo.") and at least one set ("Añade al
menos una serie."). A rest that is filled SHALL be in minutes format ("Usa minutos (2) o minutos:segundos (1:30)."). It
SHALL send nothing while any of these fails.

Otherwise it SHALL send exactly one request:

- an addition to the workout, for an exercise chosen from the catalog;
- a replacement of the exercise's prescription, for one opened with the pencil.

The request SHALL hold the catalog exercise id, the effort, the listed sets, `perSide`, and the rest as minutes with at
most two decimals. Rest SHALL be omitted when empty. A minutes set SHALL be sent with its value as minutes with at most
two decimals. While the request is in progress, "Guardar" SHALL be disabled.

#### Scenario: Chosen exercise added

- **WHEN** "Prensa de piernas", chosen from the catalog, has effort "Alto" and the sets 2 × 15 and 1 × max reps and the
  user clicks "Guardar"
- **THEN** exactly one addition of that exercise to the workout is sent with effort `HIGH` and those two sets
- **AND** `perSide` false and no rest

#### Scenario: Minutes, rest and per side sent as decimals

- **WHEN** a chosen exercise has effort "Medio", the set 3 × `1:30` minutes, "Descanso entre series" `2` and "Por lado"
  on, and the user clicks "Guardar"
- **THEN** exactly one addition is sent with the set `targetType` `Minutes` and `targetMinutes` 1.5, `restMinutes` 2
  and `perSide` true

#### Scenario: Saved exercise updated

- **WHEN** the user removes a set of "Sentadilla", opened with the pencil, and clicks "Guardar"
- **THEN** exactly one replacement of the prescription of "Sentadilla" in the workout is sent with the remaining sets,
  and its stored rest and per side unchanged

#### Scenario: Missing effort or sets

- **WHEN** a chosen exercise has no effort and no sets and the user clicks "Guardar"
- **THEN** "Elige un esfuerzo." and "Añade al menos una serie." are shown and no request is sent

#### Scenario: Invalid rest

- **WHEN** "Descanso entre series" is `1:75` and the user clicks "Guardar"
- **THEN** the field shows "Usa minutos (2) o minutos:segundos (1:30)." and no request is sent

### Requirement: The exercise save closes the dialog or shows the error

When the save succeeds, the configuration dialog SHALL close. An added exercise SHALL be appended to the end of the
workout's list. An edited one SHALL show the sets, effort, rest and per side as the API answered them. When the save
fails, the dialog SHALL stay open, show the message for the error code and keep the effort, sets, rest and per side as
edited.

#### Scenario: Chosen exercise added to the list

- **WHEN** the API accepts the addition of "Prensa de piernas" in a workout with two exercises
- **THEN** the dialog closes and "Prensa de piernas" is listed third with the sets the API answered

#### Scenario: Edited exercise updated in the list

- **WHEN** the API accepts the replacement of the sets of "Sentadilla" and answers with `restMinutes` 1.5
- **THEN** the dialog closes and the "Sentadilla" item shows the sets the API answered and "Descanso: 1:30 min"

#### Scenario: Exercise rejected

- **WHEN** the API answers 409 to the addition
- **THEN** the dialog stays open, "La operación entra en conflicto con el estado actual." is shown and the listed sets
  are kept
- **AND** the exercise is not in the workout's list

#### Scenario: Time rejected by the API

- **WHEN** the API answers 400 to the addition
- **THEN** the dialog stays open with the message for `VALIDATION`, and the rest and per side are kept as edited

### Requirement: The properties card shows the workout's data

The "Propiedades del entrenamiento" card SHALL hold the workout's Nombre and Descripción in editable fields and its
week in the week field Semana, filled with the loaded values, and no exercises. Editing them SHALL change nothing until
"Guardar" is pressed.

#### Scenario: Card filled

- **WHEN** workout "Pierna" dated 2026-11-02 with description "Día 1" is loaded, in 2026
- **THEN** the card's fields hold "Pierna" and "Día 1", and the week field shows "Semana del 2 nov – 8 nov"
- **AND** the card lists no exercises

### Requirement: The exercises card lists the workout's exercises

The "Ejercicios" card SHALL list the workout's saved exercises in their current order (the order the API returns
them until the user reorders them), numbered from 1. Each SHALL have a "Reordenar ejercicio" drag handle on its left,
a "Quitar ejercicio" trash icon button and an "Editar ejercicio" pencil icon button. With no exercises it SHALL show
"Este entrenamiento aún no tiene ejercicios.".

#### Scenario: Exercises listed

- **WHEN** the workout has two exercises
- **THEN** both are listed in that order, numbered 1 and 2, each in its own bordered card with "Reordenar ejercicio",
  "Quitar ejercicio" and "Editar ejercicio"

### Requirement: Listed exercises are cards in rows of up to three

Each exercise of the workout SHALL be shown in its own bordered card. The cards SHALL flow left to right in rows of at
most three, wrapping onto the next row, with fewer per row on narrower screens. Inside a card, the first row SHALL hold
the drag handle, the position, the name and the action icons; the second the body-part and muscle labels; the third
the sets.

#### Scenario: Rows inside a card

- **WHEN** "Sentadilla", listed first, with the sets 3 × 10, is shown and the catalog has more than one legs muscle
- **THEN** its card's first row holds "Reordenar ejercicio", "1", "Sentadilla", "Quitar ejercicio" and "Editar
  ejercicio"
- **AND** the labels "Piernas" and "Cuádriceps" come below that row, and "3 × 10" below the labels

#### Scenario: Exercises in a row

- **WHEN** the workout has two exercises on a wide screen
- **THEN** their cards sit side by side in the same row, the first on the left

#### Scenario: At most three per row

- **WHEN** the workout has four exercises on a wide screen
- **THEN** the first three share a row and the fourth starts the next one

### Requirement: The pencil opens the exercise's configuration

Clicking "Editar ejercicio" SHALL open the configuration dialog for that workout exercise, the same dialog used to add
one, filled with its stored effort and sets. It SHALL NOT send any request.

#### Scenario: Edit an exercise

- **WHEN** the user clicks "Editar ejercicio" on the second exercise
- **THEN** the configuration dialog shows that exercise with its stored effort and sets
- **AND** no request is sent

### Requirement: The configuration dialog shows the exercise

The configuration dialog SHALL be titled "Configuración del ejercicio" and SHALL show the exercise's video when the catalog
gives one, then its name, body part and muscle; then its effort, the list of its sets and a form to define a new set
with an "Añadir serie" button; and SHALL end with "Cancelar" and "Guardar". Each opening SHALL start from the stored
values, discarding any edit left unsaved.

#### Scenario: Exercise shown

- **WHEN** a squat with effort `HIGH` and one set of 3 × 10 repetitions is opened with the pencil
- **THEN** the dialog shows "Sentadilla", "Piernas" and "Cuádriceps"
- **AND** the effort field shows "Alto" and the list of sets shows "3 × 10 repeticiones"

#### Scenario: Another exercise opened

- **WHEN** the user changes the effort of one exercise, cancels, and opens another exercise
- **THEN** the dialog shows the other exercise's stored effort

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

### Requirement: Each row of filter pills has a label

In the catalog, the type pills SHALL be preceded by the label "Tipo", the body-part pills by the label "Partes del
cuerpo", and the muscle pills by the label "Músculos". Each label SHALL name its group of pills, and the muscle label
SHALL be shown and hidden together with its row.

#### Scenario: Labels shown

- **WHEN** the user selects the pill "Piernas" in the catalog
- **THEN** the label "Partes del cuerpo" precedes the body-part pills and "Músculos" precedes the muscle pills

#### Scenario: Type label first

- **WHEN** the catalog dialog opens
- **THEN** the label "Tipo" precedes the type pills, and comes before "Partes del cuerpo"

### Requirement: Muscle pills refine a selected body part

While a body part is selected in the catalog, a second row of pills SHALL show one pill per distinct muscle of the
catalog exercises of that body part, sorted by name. Selecting a muscle SHALL keep only the exercises of that body
part and muscle; selecting it again SHALL clear it. Changing or clearing the body part SHALL clear the muscle. With no
body part selected, or when the listed exercises of that body part work a single muscle, the row SHALL NOT be shown.

#### Scenario: No body part, no muscle row

- **WHEN** the catalog dialog opens with no body part selected
- **THEN** no muscle pills and no "Músculos" label are shown

#### Scenario: Muscles of the body part

- **WHEN** the user selects the pill "Brazos" and the catalog's arms exercises work biceps and triceps
- **THEN** a second row shows the pills "Bíceps" and "Tríceps"

#### Scenario: Filter by muscle

- **WHEN** the user selects "Brazos" and then "Bíceps"
- **THEN** only arms exercises that work the biceps are listed

#### Scenario: Single muscle, no muscle row

- **WHEN** the user selects the pill "Pecho" and every listed chest exercise works the chest
- **THEN** no muscle pills and no "Músculos" label are shown

#### Scenario: Muscle cleared with its body part

- **WHEN** a muscle is selected and the user selects another body part
- **THEN** no muscle is selected and the new body part's muscles are shown

### Requirement: The configuration dialog shows the exercise beside its configuration

On wide screens the configuration dialog SHALL show the exercise (video, name and labels) in a left column of about
30% of its width, and the configuration (effort, sets and set form) in a right column of about 70%. On narrow screens
the columns SHALL stack, the exercise first.

#### Scenario: Exercise beside its configuration

- **WHEN** the dialog is open on a wide screen
- **THEN** the exercise's name is in the left column and the effort field in the right one, side by side

### Requirement: A single muscle is left out of the exercise lists

In the catalog and the workout list, an exercise's muscle label SHALL be left out when every catalog exercise of the
same body part works that one muscle. The configuration dialog SHALL always show it.

#### Scenario: Single-muscle body part

- **WHEN** every chest exercise of the catalog works the chest
- **THEN** a chest exercise in the catalog and in the workout list shows only the label "Pecho"
- **AND** its configuration dialog shows both "Pecho" labels

### Requirement: Type pills filter the catalog by exercise type

The catalog dialog SHALL show, before the body-part row, a row of pills with one pill per exercise type, in the order
"Fuerza", "Cardio" and "Estiramiento". The row SHALL always be shown, and selecting a type SHALL NOT show or hide any
other row. Each pill SHALL use its type's colour, outlined while unselected and filled while selected.

#### Scenario: Type row first

- **WHEN** the catalog dialog opens
- **THEN** the type pills "Fuerza", "Cardio" and "Estiramiento" are shown, outlined in blue, red and green, before the
  body-part pills

#### Scenario: Type selected

- **WHEN** the user selects the pill "Cardio"
- **THEN** only cardio exercises are listed
- **AND** "Cardio" is filled in red, and the body-part row is shown as before

#### Scenario: Type cleared

- **WHEN** the user selects the already selected pill "Cardio"
- **THEN** no type is selected and the type no longer filters the list

### Requirement: The configuration dialog asks for rest and per side

Below the effort, the configuration dialog SHALL show an optional "Descanso entre series" field, with the hint
"Minutos o m:ss, p. ej. 1:30". The set form SHALL show a "Por lado" switch, off by default, after its target fields:
beside Valor when the type needs Valor only. The switch applies to every set of the exercise. Each opening SHALL start from the exercise's
stored values. Stored rest SHALL be shown as `M` or `M:SS`, and no stored rest SHALL leave the field empty. Changing
either one SHALL send nothing until "Guardar".

#### Scenario: Exercise chosen from the catalog

- **WHEN** an exercise chosen from the catalog opens its configuration
- **THEN** "Descanso entre series" is empty and "Por lado" is off

#### Scenario: Stored rest and per side

- **WHEN** an exercise stored with `restMinutes` 1.5 and `perSide` true opens its configuration
- **THEN** "Descanso entre series" shows `1:30` and "Por lado" is on

#### Scenario: Whole minutes of rest

- **WHEN** an exercise stored with `restMinutes` 2 opens its configuration
- **THEN** "Descanso entre series" shows `2`

### Requirement: Per side is shown in the set summaries

While "Por lado" is on, each set of the dialog's list SHALL read with "por lado" after its target, such as "2 × 8
repeticiones por lado". In the workout list, an exercise stored per side SHALL show "· por lado" at the end of its sets
line.

#### Scenario: Per side in the dialog

- **WHEN** an exercise has the set 2 × 8 repetitions and the user turns "Por lado" on
- **THEN** the set reads "2 × 8 repeticiones por lado"

#### Scenario: Per side in the workout list

- **WHEN** a listed exercise is stored per side with the sets 2 × 8 and 1 × max reps
- **THEN** its sets line reads "2 × 8, 1 × máx · por lado"

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

### Requirement: Catalog cards show the English name under the name

When the language is not English, each catalog card SHALL show the exercise's English name on a line under the name,
in a softer grey than the name. It SHALL show nothing there when English is selected, when the exercise has no English
name, or when the English name is the same as the name shown. The workout's own exercise list SHALL NOT show it.

#### Scenario: English name under the Spanish one

- **WHEN** Spanish is selected and the catalog dialog lists the squat, named "Sentadilla" and "Squat"
- **THEN** its card shows "Sentadilla" and, under it and softer, "Squat"

#### Scenario: Nothing to add

- **WHEN** an exercise has only a Spanish name, or the same name in both languages
- **THEN** its card shows its name once

#### Scenario: English selected

- **WHEN** English is selected
- **THEN** each card shows the English name once, as its name
