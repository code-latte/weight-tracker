# app-workout-exercise Specification

## Purpose

The athlete's exercise screen in the app: one exercise of a workout with its prescribed sets, where the athlete
records what was done set by set, switches to another exercise of the same workout and sees the rest time.

## Requirements

### Requirement: The exercise screen shows one exercise of a workout

The app SHALL offer a signed-in screen at `/workouts/[id]/exercises/[exerciseId]`, where `exerciseId` is the workout
exercise id. It SHALL read the workout by `id` when it opens and show the exercise with that id. While reading it
SHALL show a loading indicator under the header; when the read fails it SHALL show the error for its code with a
retry action.

#### Scenario: Opened from the workout

- **WHEN** the athlete opens `/workouts/wk1/exercises/we1` and the API returns workout `wk1`
- **THEN** exactly one read of workout `wk1` is sent
- **AND** the screen shows the exercise `we1`

#### Scenario: Loading

- **WHEN** the workout has not been read yet
- **THEN** the loading indicator is shown and no set row is shown

#### Scenario: Failure and retry

- **WHEN** the read fails with `NETWORK` and the athlete taps retry
- **THEN** the error for `NETWORK` is shown and the workout is read again

### Requirement: The header names the workout and the exercise

The header SHALL show a back button labelled "Volver" that returns to the previous screen, the workout name, and the
exercise name in the active language (else its first translation, else "Ejercicio").

#### Scenario: Header content

- **WHEN** the workout "Pierna A" has the exercise named "Sentadilla" in Spanish and "Squat" in English and the active
  language is Spanish
- **THEN** the header shows "Pierna A" and "Sentadilla"

#### Scenario: Back

- **WHEN** the athlete taps "Volver"
- **THEN** the app goes back to the workout screen

### Requirement: The header shows the English name when another language is active

Below the exercise name the header SHALL show the exercise's English name, greyed, when the active language is not
English and the English name exists and differs from the shown name. Otherwise it SHALL show no subtitle.

#### Scenario: Spanish active

- **WHEN** the active language is Spanish and the exercise is "Sentadilla" / "Squat"
- **THEN** the header shows "Squat" below "Sentadilla"

#### Scenario: English active

- **WHEN** the active language is English and the exercise is "Sentadilla" / "Squat"
- **THEN** the header shows "Squat" once and no subtitle

#### Scenario: No English name

- **WHEN** the active language is Spanish and the exercise only has a Spanish name
- **THEN** the header shows no subtitle

### Requirement: The header shows the exercise's body part and muscle

Below the subtitle (or below the name when there is none), the header SHALL show a small filled orange chip with the
exercise's body part and, beside it, a small outlined orange chip with its muscle, both in the active language. When
the catalog has a single muscle for that body part, the header SHALL show the body part chip alone.

#### Scenario: Body part with several muscles

- **WHEN** the exercise works the quadriceps and the catalog has several muscles for legs
- **THEN** the header shows "Piernas" and "Cuádriceps"

#### Scenario: Body part with one muscle

- **WHEN** the exercise's body part has a single muscle in the catalog
- **THEN** the header shows the body part chip only

### Requirement: The header links to the exercise's video

When the exercise has a `videoUrl`, the header SHALL show a "Ver vídeo" link that opens that URL. Without one it
SHALL show no link.

#### Scenario: Exercise with video

- **WHEN** the exercise has the video `https://v.example/1` and the athlete taps "Ver vídeo"
- **THEN** that URL is opened

#### Scenario: Exercise without video

- **WHEN** the exercise has no video
- **THEN** no "Ver vídeo" link is shown

### Requirement: The navigator lists the workout's exercises

Below the header the screen SHALL show a horizontally scrollable navigator labelled "Ejercicios" with one item per
exercise of the workout, in the order the API returns them. Each item SHALL show the exercise's progress icon (done,
in progress, not started, as on the workout screen) and its name in the active language. The current exercise's item
SHALL be marked as selected.

#### Scenario: Items in order

- **WHEN** the workout has the exercises "Press inclinado" (done), "Jalón al pecho" (in progress) and "Remo"
  (not started), in that order, and the screen shows "Jalón al pecho"
- **THEN** the navigator shows three items in that order, labelled "Completado", "En curso" and "Sin empezar"
- **AND** the "Jalón al pecho" item is selected and the others are not

### Requirement: The navigator switches the exercise

Tapping another item SHALL show that exercise in the same screen, updating the route's `exerciseId` in place with
no screen transition, so that going back still returns to the workout screen.

#### Scenario: Switch

- **WHEN** the screen shows "Press inclinado" and the athlete taps "Remo"
- **THEN** the route's `exerciseId` becomes Remo's id without a new screen, and the header and sets show "Remo"

### Requirement: The sets list expands the prescriptions

The screen SHALL list one row per prescribed set, expanding each prescription `count × target` in order, numbered
from 1. An exercise with no prescriptions SHALL show no rows.

#### Scenario: Two prescriptions

- **WHEN** the exercise is prescribed `3 × 10 reps` then `1 × 8 reps`
- **THEN** four rows are shown, numbered 1 to 4, the first three with target "10 reps" and the last with "8 reps"

### Requirement: Each row shows its number and target

Each row SHALL show its set number standing alone beside the card, and inside the card its target as text by
`target.type`: `Reps` -> "{value} reps"; `RepsRange` -> "{value}–{maxValue} reps"; `MaxReps` -> "máx. reps";
`Duration` -> "{value} s"; `Minutes` -> "{minutes} min".

#### Scenario: Targets

- **WHEN** the exercise is prescribed one set of each kind: 10 reps, 8–12 reps, max reps, 45 seconds and 2 minutes
- **THEN** the rows show the numbers 1 to 5 and read "10 reps", "8–12 reps", "máx. reps", "45 s" and "2 min", in that
  order

### Requirement: Each row offers the inputs its target asks for

Each row SHALL offer a "Peso (kg)" input for `Reps`; "Reps" and "Peso (kg)" inputs for `RepsRange` and `MaxReps`; and
no input for `Duration` and `Minutes`. A row whose set is already recorded SHALL prefill its inputs from the record.

#### Scenario: Inputs by target

- **WHEN** the exercise is prescribed 10 reps, 8–12 reps and 45 seconds
- **THEN** the first row has a weight input only, the second has reps and weight inputs, and the third has no input

#### Scenario: Recorded row

- **WHEN** the second prescribed set has the record `{ type: "Reps", reps: 11, weight: 40 }`
- **THEN** its inputs show 11 and 40

### Requirement: Each row shows a save button or a saved mark

Each row SHALL end with one action: a "Guardar" button while the set has no record or the inputs differ from its
record, and a "Guardada" mark while they match it.

#### Scenario: Not recorded

- **WHEN** a row's set has no record
- **THEN** the row shows "Guardar" and no "Guardada"

#### Scenario: Recorded and untouched

- **WHEN** a row's set is recorded and its inputs still hold the record's values
- **THEN** the row shows "Guardada" and no "Guardar"

#### Scenario: Recorded value changed

- **WHEN** a recorded row's weight is changed from 60 to 62.5
- **THEN** the row shows "Guardar" and no "Guardada"
- **AND** typing 60 again brings "Guardada" back

### Requirement: Saving a set records it as prescribed with the typed values

Tapping "Guardar" on row `i` SHALL record the set when the exercise has fewer than `i` records, and correct record
`i` (by position) otherwise. The body SHALL carry `reps` for rep-based targets (the prescribed value for `Reps`, the
typed value otherwise) or `durationSeconds` for timed ones (`value` for `Duration`, `minutes × 60` rounded for
`Minutes`), and `weight` only when typed. After a success the workout SHALL be read again.

#### Scenario: Plain reps set with weight

- **WHEN** row 1 targets 10 reps, has no record, the athlete types weight 60 and taps "Guardar"
- **THEN** a record is added for the exercise with body `{ "reps": 10, "weight": 60 }`
- **AND** the workout is read again

#### Scenario: Range set

- **WHEN** row 2 targets 8–12 reps, has no record, the athlete types 11 reps and taps "Guardar" without a weight
- **THEN** a record is added with body `{ "reps": 11 }`

#### Scenario: Timed set

- **WHEN** row 3 targets 2 minutes, has no record, and the athlete taps "Guardar"
- **THEN** a record is added with body `{ "durationSeconds": 120 }`

#### Scenario: Existing record corrected

- **WHEN** row 1 already has record `sr-1`, the athlete changes the weight to 62.5 and taps "Guardar"
- **THEN** record `sr-1` is corrected with body `{ "reps": 10, "weight": 62.5 }` and no record is added

### Requirement: Rows check their inputs before sending

A `RepsRange` or `MaxReps` row SHALL require a whole number of reps of at least 1 and show "Indica las
repeticiones." otherwise. Any weight typed SHALL be a number of at least 0 with at most two decimals, else the row
shows "Peso no válido.". A row with a violation SHALL send nothing.

#### Scenario: Reps missing

- **WHEN** a range row has an empty reps input and the athlete taps "Guardar"
- **THEN** "Indica las repeticiones." is shown and nothing is sent

#### Scenario: Weight invalid

- **WHEN** the athlete types weight "abc" and taps "Guardar"
- **THEN** "Peso no válido." is shown and nothing is sent

### Requirement: A failed save stays on the row

When recording or correcting fails, the row SHALL show the error for its code (`CONFLICT` reads "No se puede
registrar esta serie.") and keep the typed values. The workout SHALL NOT be read again. The row's button SHALL be
disabled while its save is in flight.

#### Scenario: Conflict

- **WHEN** the save fails with `CONFLICT`
- **THEN** the row shows "No se puede registrar esta serie." and keeps its values

### Requirement: Rows are read-only while the workout does not accept records

When the workout's status is not `STARTED` or `COMPLETED`, every row SHALL show its inputs disabled, with the
recorded values when any, and no "Guardar" button. Recorded rows keep their "Guardada" mark; the others show no action.

#### Scenario: Published workout

- **WHEN** the workout is `PUBLISHED`
- **THEN** no row shows "Guardar" and every input is disabled

#### Scenario: Reviewed workout with records

- **WHEN** the workout is `REVIEWED` and the first set is recorded
- **THEN** the first row reads "Guardada" with its values, disabled, and shows no "Guardar"

#### Scenario: Started workout

- **WHEN** the workout is `STARTED`
- **THEN** every row shows "Guardar" and enabled inputs

### Requirement: The rest card shows the exercise's rest time

A full-width band kept against the tab bar SHALL read "Descanso: M:SS" when the exercise has `restMinutes`, with
the minutes converted to minutes and seconds (`1.5` -> `1:30`, `2` -> `2:00`, rounded to the second), and "Sin
descanso" when it does not.

#### Scenario: With rest

- **WHEN** the exercise has `restMinutes` 1.5
- **THEN** the band reads "Descanso: 1:30"

#### Scenario: Without rest

- **WHEN** the exercise has no `restMinutes`
- **THEN** the card reads "Sin descanso"

### Requirement: The screen follows the active language

In English the screen SHALL read "Back" for the header back, "Exercises" for the navigator label, "max reps",
"Reps", "Weight (kg)", "Save" and "Saved" in the rows, and "Rest: M:SS" / "No rest" in the rest band.

#### Scenario: English copy

- **WHEN** the active language is English and the exercise is prescribed max reps with 1 minute of rest
- **THEN** the row reads "max reps" and "Save" and the band reads "Rest: 1:00"
