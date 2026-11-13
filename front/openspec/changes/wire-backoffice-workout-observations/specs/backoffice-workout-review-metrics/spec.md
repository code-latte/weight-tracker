# Spec Delta

## ADDED Requirements

### Requirement: Observations are read on the tab's first opening

The review SHALL read the workout's observations the first time "Resumen y métricas" is selected, together with the
summary. It SHALL NOT read them again while the review stays open. While the read is in flight, the observations card
SHALL show a loading state.

#### Scenario: First opening

- **WHEN** the review opens and the user selects "Resumen y métricas"
- **THEN** the observations are requested once for that workout

#### Scenario: Coming back to the tab

- **WHEN** the user selects "Ejercicios" and then "Resumen y métricas" again
- **THEN** no new observations request is sent

#### Scenario: Tab never opened

- **WHEN** the review opens and the user stays on "Ejercicios"
- **THEN** no observations request is sent

### Requirement: A failed observations read shows an inline load error with retry

When the observations read fails, the observations card SHALL show a load error with a retry action. The other blocks
of the tab SHALL stay. Retrying SHALL read the observations again.

#### Scenario: Observations fail, then succeed

- **WHEN** the observations read fails with `NETWORK` and the summary succeeds, and the user retries and the retry
  succeeds
- **THEN** the other blocks show their content, the card shows the load error first, and the highlights after the retry

### Requirement: The card shows the highlights

The card "Observaciones de la sesión", subtitled "Puntos clave y recomendaciones", SHALL list the highlights in `rank`
order, each shown as the observation whose `id` is its `observationId`. A highlight with no matching observation SHALL
be skipped. The card SHALL NOT show a "Vista previa" mark.

#### Scenario: Highlights in rank order

- **WHEN** the API returns five observations and highlights ranked 1 to 3 pointing at the fourth, the first and the
  second
- **THEN** the card shows those three observations in that order, and not the other two

#### Scenario: Dangling highlight

- **WHEN** a highlight's `observationId` matches no observation
- **THEN** that highlight is not shown and the others are

### Requirement: Each observation shows its severity, title and text

Each observation SHALL show an icon and colour from its `severity` (`SUCCESS` success with a trend icon, `INFO` info
with an info icon, `WARNING` warning with a warning icon), a title translated from its `code`, and a text built from
its `data`. An exercise-level text SHALL start with the exercise name. A code the front does not know SHALL show the
title "Observación" and no text.

#### Scenario: Weight record

- **WHEN** an observation is `WEIGHT_PERSONAL_RECORD`, `SUCCESS`, on "Sentadilla hack", with `previousMaxKg` 120,
  `currentMaxKg` 130 and `changePercent` 8.33, in Spanish
- **THEN** it shows the success colour, the title "Récord de peso" and the text "Sentadilla hack: 130 kg, antes 120 kg
  (+8,3 %)."

#### Scenario: Incomplete workout

- **WHEN** an observation is `WORKOUT_INCOMPLETE`, `WARNING`, with `exerciseId: null`, `recordedSets` 9,
  `prescribedSets` 15 and `completionPercent` 60
- **THEN** it shows the warning colour, the title "Entrenamiento incompleto" and the text "9 de 15 series registradas
  (60 %)."

#### Scenario: Sets below target

- **WHEN** an observation is `REPS_BELOW_TARGET` with `setPositions` [3, 4], `reps` [6, 5] and `targetMins` [8, 8]
- **THEN** its text lists "serie 3: 6/8 · serie 4: 5/8" after the exercise name

#### Scenario: Optional baseline present

- **WHEN** a `WEIGHT_DECREASE` observation carries `baselineMaxKg` 100
- **THEN** its text ends with "Referencia previa: 100 kg."

#### Scenario: Unknown code

- **WHEN** an observation has a code the front has no copy for
- **THEN** it shows the title "Observación", no text, and its severity colour

### Requirement: Exercise names come from the reviewed workout

The exercise name of an observation SHALL be the name, in the current language, of the first exercise of the reviewed
workout with the same catalog exercise id as the observation's `exerciseId`. When none matches, the name SHALL be
"Ejercicio".

#### Scenario: No matching exercise

- **WHEN** an observation's `exerciseId` matches no exercise of the workout
- **THEN** its text starts with "Ejercicio"

### Requirement: The card offers the full list

When `observations` is not empty, the card SHALL show a "Ver todas las observaciones (N)" button, where N is the number
of observations. It SHALL be shown even when there are no highlights. Selecting it SHALL open the observations dialog.

#### Scenario: Button with count

- **WHEN** the API returns twelve observations and three highlights
- **THEN** the card shows "Ver todas las observaciones (12)"

#### Scenario: Opening the dialog

- **WHEN** the user selects the button
- **THEN** the observations dialog opens

### Requirement: The dialog lists every observation by subject

The dialog, titled "Observaciones de la sesión", SHALL list every observation in the order the API returns it, grouped
by subject: an "Entrenamiento" group for observations with no `exerciseId`, then one group per exercise headed by its
name, in order of first appearance. Highlighted observations SHALL carry a "Destacada" mark. The dialog SHALL offer no
action other than closing.

#### Scenario: Grouped list

- **WHEN** the API returns one workout-level observation and observations for two exercises, one of them highlighted
- **THEN** the dialog shows the "Entrenamiento" group first, then each exercise's group with its name, and the
  highlighted observation carries "Destacada"

#### Scenario: Closing

- **WHEN** the user closes the dialog with its close button, Escape or a click outside
- **THEN** the dialog closes and the tab stays as it was, with no new request

### Requirement: The card has empty states

When both lists are empty, the card SHALL show "No hay observaciones para esta sesión" and no button. When there are
observations but no highlights, the card SHALL show "Nada destacable en esta sesión" and the button.

#### Scenario: Nothing generated

- **WHEN** the API returns empty `observations` and `highlights`
- **THEN** the card shows "No hay observaciones para esta sesión" and no "Ver todas las observaciones" button

#### Scenario: Nothing highlighted

- **WHEN** the API returns four observations and no highlights
- **THEN** the card shows "Nada destacable en esta sesión" and "Ver todas las observaciones (4)"

## REMOVED Requirements

### Requirement: Session observations are a marked preview

**Reason**: The API now returns the real observations and highlights (`add-workout-observations`).

**Migration**: Replaced by "The card shows the highlights", "The card offers the full list", "The dialog lists every
observation by subject" and "The card has empty states".
