# app-workout-detail Specification

## Purpose

The athlete's workout screen in the app: a summary panel with the workout's name, mesocycle week and exercise
progress, and the list of its exercises with their progress, type and video.

## Requirements

### Requirement: The workout screen shows one workout

The app SHALL show one workout per screen, the one whose id the screen was opened with, as the API returns it to the
athlete. The screen SHALL read the workout every time it is entered, with a single request: the workout carries
its exercises' names, type and video and its mesocycle, so no exercise catalog or mesocycle read SHALL be sent.

#### Scenario: Opened from the list

- **WHEN** the athlete taps the card of the workout "Tren superior A" in the Entrenamientos list
- **THEN** the workout screen of "Tren superior A" is shown
- **AND** only the workout is read

### Requirement: The jumbo panel heads the screen

A panel fixed at the top of the screen SHALL show, on the primary colour and running under the status bar, a back
button and the workout name on its first row. Only the exercise list below it SHALL scroll.

#### Scenario: Name in the panel

- **WHEN** the workout is named "Tren superior A"
- **THEN** the panel's first row shows a back button and "Tren superior A"

### Requirement: The back button returns to the list

The back button SHALL be labelled "Volver" and SHALL return to the Entrenamientos list. It SHALL be available in every
state of the screen, including loading and failure.

#### Scenario: Back to the list

- **GIVEN** the athlete opened the workout screen from the list
- **WHEN** the athlete taps the back button
- **THEN** the Entrenamientos list is shown

#### Scenario: Back on failure

- **WHEN** reading the workout failed
- **THEN** the back button is still shown

### Requirement: The panel shows the mesocycle week

When the workout belongs to a mesocycle, the panel SHALL show under the name the name of the mesocycle the workout
carries and the week number
as `<mesocycle> · Semana <n>`. A standalone workout SHALL show no subtitle and leave no space for it.

#### Scenario: Mesocycle workout

- **WHEN** the workout is in week 3 of the mesocycle "Hipertrofia otoño"
- **THEN** the panel shows "Hipertrofia otoño · Semana 3"

#### Scenario: Standalone workout

- **WHEN** the workout belongs to no mesocycle
- **THEN** the panel shows no subtitle

#### Scenario: English

- **WHEN** the active language is English and the workout is in week 3 of "Hipertrofia otoño"
- **THEN** the panel shows "Hipertrofia otoño · Week 3"

### Requirement: The panel shows the exercise progress

The panel SHALL show `done / total ejercicios` and, below it, a progress bar filled at `done / total`. Total is the
number of the workout's exercises; done is how many of them are done. With no exercises the panel SHALL show `0 / 0
ejercicios` and an empty bar.

#### Scenario: Half done

- **WHEN** the workout has 6 exercises and 3 are done
- **THEN** the panel shows "3 / 6 ejercicios" and a bar at 50%

#### Scenario: No exercises

- **WHEN** the workout has no exercises
- **THEN** the panel shows "0 / 0 ejercicios" and an empty bar

### Requirement: An exercise's progress comes from its set records

Each workout exercise SHALL be done when it has at least one set record and at least as many set records as
prescribed sets; in progress when it has set records but fewer than its sets; and not started when it has none. Its
prescribed sets are the sum of the counts of its set prescriptions.

#### Scenario: Done

- **WHEN** an exercise has 4 sets and 4 set records
- **THEN** it is done

#### Scenario: Sets from several prescriptions

- **WHEN** an exercise is prescribed 3 sets of 10 reps and 1 set of 8 to 12 reps, and has 3 set records
- **THEN** it is in progress, because it has 4 sets

#### Scenario: More records than sets

- **WHEN** an exercise has 3 sets and 4 set records
- **THEN** it is done

#### Scenario: In progress

- **WHEN** an exercise has 4 sets and 2 set records
- **THEN** it is in progress

#### Scenario: Not started

- **WHEN** an exercise has 4 sets and no set records, or no sets and no set records
- **THEN** it is not started

### Requirement: The exercise list

Below the panel, the screen SHALL list the workout's exercises, one row each, in the order the API returns them.

#### Scenario: API order

- **WHEN** the API returns the exercises "Press inclinado", "Jalón al pecho" and "Remo en máquina" in that order
- **THEN** the rows are listed in that order

### Requirement: Each row shows the exercise's progress

On its left, each row SHALL show a progress icon: a filled circle with a tick in the success (green) colour when done,
a circle with a play sign in the primary colour when in progress, and an empty outlined circle when not started. The
icons SHALL be labelled "Completado", "En curso" and "Sin empezar".

#### Scenario: Progress icons

- **WHEN** the workout has one done, one in-progress and one not-started exercise
- **THEN** their icons are labelled "Completado", "En curso" and "Sin empezar"

### Requirement: Each row shows the exercise's type

After the progress icon, each row SHALL show a type icon: a dumbbell for `STRENGTH`, a heart for `CARDIO` and a
meditation pose for `STRETCHING`, labelled "Fuerza", "Cardio" and "Estiramiento". A row whose exercise has another
type SHALL show no type icon.

#### Scenario: Type icons

- **WHEN** the workout has a strength, a cardio and a stretching exercise
- **THEN** their type icons are labelled "Fuerza", "Cardio" and "Estiramiento"

#### Scenario: Unknown type

- **WHEN** a workout exercise has a type other than strength, cardio or stretching
- **THEN** its row shows no type icon

### Requirement: Each row shows the exercise's name

In its middle, the widest column, each row SHALL show the exercise name in the active language, else its first
translation. An exercise with no translation SHALL read "Ejercicio".

#### Scenario: Spanish name

- **WHEN** the active language is Spanish and the exercise is named "Sentadilla" in Spanish and "Squat" in English
- **THEN** its row shows "Sentadilla"

#### Scenario: No translation

- **WHEN** the exercise has no names
- **THEN** its row shows "Ejercicio"

#### Scenario: Fallback to the first translation

- **WHEN** the active language is Spanish and the exercise only has the English name "Squat"
- **THEN** its row shows "Squat"

### Requirement: Each row links to the exercise's video

Under the exercise name, a row SHALL show a "Ver vídeo" link when the exercise has a video URL, and nothing otherwise.
Tapping the link SHALL open the URL outside the app.

#### Scenario: Exercise with video

- **WHEN** the exercise has a video URL
- **THEN** its row shows "Ver vídeo", and tapping it opens that URL

#### Scenario: Exercise without video

- **WHEN** the exercise has no video URL
- **THEN** its row shows no video link

### Requirement: Each row shows a details caret

On its right, each row SHALL show a right caret labelled "Ver detalle". Tapping the row or the caret SHALL do nothing
in this version.

#### Scenario: Caret

- **WHEN** the screen lists an exercise
- **THEN** its row shows a caret labelled "Ver detalle"

### Requirement: Loading and failure

While the workout is being read, the screen SHALL show a loading state. If the read fails, the screen SHALL show the
error state with a retry action that reads it again.

#### Scenario: Loading

- **WHEN** the workout has not arrived yet
- **THEN** the screen shows the loading state

#### Scenario: Failure and retry

- **GIVEN** reading the workout fails
- **WHEN** the athlete taps retry and the read succeeds
- **THEN** the screen shows the workout
