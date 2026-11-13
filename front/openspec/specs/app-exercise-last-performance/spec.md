# app-exercise-last-performance Specification

## Purpose

Lets the athlete check, from the exercise screen of the app, what they did the last time they finished a workout with
that exercise (the date, the effort and each set's reps or seconds and weight) before recording today's sets.

## Requirements

### Requirement: The last performance is read when the exercise is shown

When an exercise is shown, the app SHALL read the signed-in user's last performance of it by its catalog exercise id, not
by the workout exercise id of the route. It SHALL read again for each exercise the athlete switches to, and it SHALL NOT
read again when the dialog opens or closes.

#### Scenario: Reading the catalog exercise

- **WHEN** the athlete opens a workout exercise whose catalog exercise id is `ex-1`
- **THEN** the last performance of `ex-1` is requested once, without any tap

#### Scenario: Switching exercise

- **WHEN** the athlete chooses another exercise in the navigator
- **THEN** the last performance of that exercise is requested

### Requirement: A skeleton holds the place while the last performance is read

While the last performance of the shown exercise is being read, the screen SHALL show a skeleton in place of the
"Marcas anteriores" link and the list of sets, with one placeholder row per prescribed set, and keep the header, the
navigator and the rest band, which SHALL stay against the bottom of the screen. The link and the list SHALL appear together once the read settles, successful or not.

#### Scenario: Reading

- **WHEN** an exercise is shown and its last performance has not arrived
- **THEN** the screen shows the skeleton and neither the sets nor the link, and the rest band stays against the bottom

#### Scenario: Switching exercise

- **WHEN** the athlete chooses another exercise whose last performance has not arrived
- **THEN** the skeleton is shown again until it does

#### Scenario: Read settles

- **WHEN** the last performance arrives, or its read fails
- **THEN** the skeleton gives way to the list of sets and, when there is a weight to show, the link

### Requirement: Previous sets link above the sets

When the last session of the shown exercise has at least one set with a weight, the exercise screen SHALL show a
"Marcas anteriores" link with a history icon between the exercise navigator and the list of sets. The header SHALL keep
only the "Ver vídeo" link. The link SHALL be shown in every workout status.

#### Scenario: Exercise with video

- **WHEN** the athlete opens an exercise that has a `videoUrl` and a last session with a weight
- **THEN** the header shows "Ver vídeo", and "Marcas anteriores" with its icon appears below the navigator and above the sets

#### Scenario: Exercise without video

- **WHEN** the athlete opens an exercise with no `videoUrl` and a last session with a weight
- **THEN** the header shows no "Ver vídeo" link, and "Marcas anteriores" appears below the navigator and above the sets

#### Scenario: Only one set has a weight

- **WHEN** the last session has three sets and only one of them has a weight
- **THEN** the link is shown

### Requirement: The link is hidden when there is no weight to show

The "Marcas anteriores" link SHALL NOT be shown while the last performance is being read, when the read fails, when the
athlete never did the exercise, or when no set of the last session has a weight. A weight of `0` counts as a weight; a
weight that was not tracked (`null`) does not.

#### Scenario: No set has a weight

- **WHEN** every set of the last session has no weight
- **THEN** no "Marcas anteriores" link is shown

#### Scenario: Never done

- **WHEN** the athlete never finished a workout with the exercise
- **THEN** no "Marcas anteriores" link is shown

#### Scenario: Read fails

- **WHEN** reading the last performance fails
- **THEN** no "Marcas anteriores" link is shown and the screen shows no error for it

#### Scenario: Still reading

- **WHEN** the last performance has not arrived yet, or the workout is still being read
- **THEN** no "Marcas anteriores" link is shown

### Requirement: The link opens the last session dialog

Tapping "Marcas anteriores" SHALL open a dialog titled "Última sesión" over the exercise screen with what was already
read for the shown exercise.

#### Scenario: Opening shows what was read

- **WHEN** the athlete taps "Marcas anteriores"
- **THEN** the dialog "Última sesión" opens with the date, cards and sets of the last session, and no new request is made

### Requirement: The dialog shows the session date

Below the title the dialog SHALL show a small calendar icon and the workout date as day, short month and year in the
active language, followed by a divider.

#### Scenario: Date in Spanish

- **WHEN** the last performance has `workoutDate` `2026-11-10` and the active language is `es`
- **THEN** the dialog shows a calendar icon and "10 nov 2026", with a divider below them

### Requirement: The dialog summarises the session in two cards

Below the divider the dialog SHALL show a row of two cards. The first SHALL have the small title "Series realizadas" and,
big, the number of sets the API returned. The second SHALL have the small title "Esfuerzo" and, big, the effort label.
With no effort the second card SHALL be left out.

#### Scenario: Sets done

- **WHEN** the last performance has three sets
- **THEN** the first card reads "Series realizadas" and a big "3"

#### Scenario: No effort

- **WHEN** the last performance has no effort
- **THEN** only the "Series realizadas" card is shown

### Requirement: The effort is shown in the exercise type's scale

The effort label SHALL be picked by the exercise's `type`: `STRENGTH` uses the RIR scale, `CARDIO` the RPE scale,
`STRETCHING` the descriptive scale. Any other type SHALL use the generic scale, and an unknown effort code SHALL be
shown as it comes.

#### Scenario: Strength exercise

- **WHEN** the exercise type is `STRENGTH` and the effort is `HIGH`
- **THEN** the second card reads "Esfuerzo" and a big "RIR 0"

#### Scenario: Cardio exercise

- **WHEN** the exercise type is `CARDIO` and the effort is `MID`
- **THEN** the second card reads "Esfuerzo" and a big "RPE 5-6"

#### Scenario: Stretching exercise

- **WHEN** the exercise type is `STRETCHING` and the effort is `LOW`
- **THEN** the second card reads "Esfuerzo" and a big "Muy suave"

#### Scenario: Unknown type

- **WHEN** the exercise type is not one of the three known types and the effort is `MIDLOW`
- **THEN** the second card reads "Esfuerzo" and a big "Medio-bajo"

### Requirement: The dialog lists the recorded sets in a table

Below the cards the dialog SHALL show the title "Series registradas" and one card laid out as a table, with a separator
between its rows. It SHALL have one row per set in the order the API returns them: the set's number from 1 on the left,
its amount centred and its weight on the right. The card SHALL have a plain, outlined background. The set number SHALL be
big and in the accent colour, like the numbers of the register list, and the weight SHALL be big in the normal text
colour.

#### Scenario: Reps with weight

- **WHEN** a set has `reps: 10`, `weight: 20`
- **THEN** its row reads "1" on the left, "10 reps" centred and "20 kg" on the right

### Requirement: A set's amount and weight read as recorded

The amount SHALL read "{reps} reps" when `reps` is set, or "{seconds} s" when `durationSeconds` is set. The weight SHALL
read "{weight} kg" when `weight` is set, including "0 kg" for `0`, or "sin peso" when `weight` is `null`. No row SHALL
ever read "null".

#### Scenario: Reps without weight

- **WHEN** a set has `reps: 10`, `weight: null`
- **THEN** its line reads "10 reps" and "sin peso"

#### Scenario: Timed set

- **WHEN** a set has `durationSeconds: 45`, `weight: null`
- **THEN** its line reads "45 s" and "sin peso"

#### Scenario: Zero weight

- **WHEN** a set has `reps: 8`, `weight: 0`
- **THEN** its line reads "8 reps" and "0 kg"

### Requirement: The highest weight is starred

The row of the set with the highest weight SHALL show a star in the primary colour before its weight and, under the
weight, the small text "Mayor carga" in the primary colour. When the highest weight is repeated, only the last of those
sets SHALL be marked. Sets without a weight SHALL not count, and nothing SHALL be marked when no set has a weight above 0.

#### Scenario: One highest weight

- **WHEN** the sets weigh 20, 30 and 25 kg
- **THEN** only the second row shows the star before its weight and "Mayor carga" under it

#### Scenario: Repeated highest weight

- **WHEN** the sets weigh 30, 20 and 30 kg
- **THEN** only the third row shows the star and "Mayor carga"

#### Scenario: Sets without weight

- **WHEN** only the second of three sets has a weight
- **THEN** that row shows the star and "Mayor carga"

#### Scenario: Nothing to star

- **WHEN** no set has a weight, or the highest weight is 0
- **THEN** no row shows a star or "Mayor carga"

### Requirement: Closing the dialog

The dialog SHALL offer a "Cerrar" button and SHALL close on it or on dismissal, leaving the exercise screen as it was,
with the link still shown. Switching to another exercise with the navigator SHALL close it.

#### Scenario: Close

- **WHEN** the athlete taps "Cerrar"
- **THEN** the dialog closes and the exercise screen keeps its typed values and rest countdown

#### Scenario: Exercise switch closes it

- **WHEN** the dialog is open and the shown exercise changes to another one
- **THEN** the dialog is closed

### Requirement: English copy

In English the link SHALL read "Previous sets", the title "Last session", the date as "Nov 10, 2026", the card titles
"Sets done" and "Effort" (with, e.g., "Very light" for `STRETCHING` `LOW` and "Medium-low" for the generic `MIDLOW`),
the table title "Recorded sets", "Highest weight" under the highest weight, "{n} reps", "{n} s", "{n} kg", "no weight" and the button "Close".

#### Scenario: Dialog in English

- **WHEN** the active language is `en` and the athlete opens the dialog on a set with `reps: 10`, `weight: null`
- **THEN** the dialog reads "Last session", "Nov 10, 2026", "Sets done", "Effort" and "Recorded sets", and the set row reads "1", "10 reps", "no weight"
