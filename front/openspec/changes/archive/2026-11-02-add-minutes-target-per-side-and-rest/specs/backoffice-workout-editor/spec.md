## ADDED Requirements

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

## MODIFIED Requirements

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
