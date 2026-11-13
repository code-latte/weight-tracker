# Spec Delta

## Purpose

Fills the backoffice review's "Resumen y métricas" tab so trainers and admins can read how a session went: its
results, each weighted exercise against its previous execution and over recent sessions, the sets per muscle, and the
session observations.

## ADDED Requirements

### Requirement: The summary is read on the tab's first opening

The review SHALL read the workout's summary the first time "Resumen y métricas" is selected. It SHALL NOT read it
again while the review stays open, including when the user leaves the tab and comes back. While the read is in flight,
the tab SHALL show a loading state.

#### Scenario: First opening

- **WHEN** the review opens and the user selects "Resumen y métricas"
- **THEN** the summary is requested once for that workout

#### Scenario: Coming back to the tab

- **WHEN** the user selects "Ejercicios" and then "Resumen y métricas" again
- **THEN** no new summary request is sent

#### Scenario: Tab never opened

- **WHEN** the review opens and the user stays on "Ejercicios"
- **THEN** no summary request is sent

### Requirement: A failed summary shows a load error with retry

When the summary read fails, the tab SHALL show a load error with a retry action. The rest of the review SHALL stay
as it is. Retrying SHALL read the summary again.

#### Scenario: Summary fails, then succeeds

- **WHEN** the summary read fails with `NETWORK` and the user retries, and the retry succeeds
- **THEN** the load error is shown first, and the tab content replaces it after the retry

### Requirement: Weighted exercises

An exercise SHALL count as weighted when its summary `volume` is not `null`. Only weighted exercises SHALL appear in
the comparison table and in the chart exercise selector.

#### Scenario: A stretch is not listed

- **WHEN** the summary returns a stretching exercise with `volume: null` and two exercises with a volume
- **THEN** the comparison table has two rows and the selector offers those two exercises only

### Requirement: Session results show five cards

The tab SHALL show a card "Resultados de la sesión", subtitled "Cumplimiento, volumen e intensidad del entrenamiento",
with five stat cards. None of them SHALL show a change against another workout. The cards are:

- "Series completadas";
- "Ejercicios completados";
- "Series en rango objetivo";
- "Volumen de fuerza";
- "Duración".

#### Scenario: Completed session

- **WHEN** the workout has 29 of 29 counted records, 10 of 10 completed exercises, a summary with `setsInTarget` 27 of
  29 and `volume` 16913.4, and lasted 1 h 12 min
- **THEN** the cards show "29 / 29" with "100%", "10 / 10" with "100%", "27 / 29" with "93%", "16.913,4 kg" and
  "1 h 12 min"

### Requirement: Set and exercise counts follow the review's counting rule

"Series completadas" SHALL show "<counted records> / <planned sets>" over all exercises. "Ejercicios completados" SHALL
show "<completed> / <total>". Both SHALL use the review's existing counting rule and show their rounded percentage
below, or no percentage when the total is 0.

#### Scenario: Partial session

- **WHEN** the workout has 12 counted records of 29 planned sets and 4 of 10 exercises completed
- **THEN** the cards show "12 / 29" with "41%" and "4 / 10" with "40%"

### Requirement: Sets in target and volume come from the summary

"Series en rango objetivo" SHALL show the summary's `inTarget / evaluated` with its rounded percentage, or "—" when
`evaluated` is 0. "Volumen de fuerza" SHALL show the summary's `volume` in kg in the active language's number format, or
"—" when it is `null`.

#### Scenario: Nothing evaluated

- **WHEN** the summary has `setsInTarget` 0 of 0 and `volume: null`
- **THEN** both cards show "—"

### Requirement: Duration matches the review's elapsed time

"Duración" SHALL show the same value and format as the review's "Tiempo transcurrido" card, including "—" when a date
it needs is missing.

#### Scenario: Missing start

- **WHEN** the workout has no `startedAt`
- **THEN** "Duración" shows "—"

### Requirement: Exercise evolution charts

The tab SHALL show two cards side by side:

- "Evolución de la carga", subtitled "Peso máximo por sesión", with a line chart of each session's max weight;
- "Evolución del volumen", subtitled "Volumen total del ejercicio (peso × repeticiones)", with a bar chart of each
  session's volume.

Both SHALL plot the athlete's last 5 sessions of the selected exercise, oldest to newest, labelled by date.

#### Scenario: Charts for the default exercise

- **WHEN** the tab opens and the first weighted exercise has 5 sessions in its history
- **THEN** both charts show those 5 sessions, oldest on the left, with the newest value labelled on the last point and
  bar

#### Scenario: Missing values

- **WHEN** one session of the history has `maxWeight: null`
- **THEN** the load chart leaves that session out and the volume chart still shows it if its `volume` is not `null`

### Requirement: One selection drives both charts

Each evolution card SHALL show an exercise selector, and both selectors SHALL show the same selection. They SHALL list
the weighted exercises in workout order and select the first one on opening. Changing either SHALL change both and read
that exercise's history again for the workout's athlete.

#### Scenario: Changing the exercise

- **WHEN** the user selects "Sentadilla hack" in the selector of either card
- **THEN** the history of that exercise is requested once for the workout's athlete with a limit of 5
- **AND** both selectors show it and both charts show its sessions

#### Scenario: No weighted exercise

- **WHEN** the summary has no weighted exercise
- **THEN** no history is requested, no selector is shown and both cards show "Sin datos de este ejercicio"

### Requirement: Empty or failed history

When the history is empty, both evolution cards SHALL show "Sin datos de este ejercicio". When the history read fails,
both cards SHALL show a load error with a retry action, and the other blocks of the tab SHALL stay as they are.

#### Scenario: Never done before

- **WHEN** the history answers an empty list
- **THEN** both cards show "Sin datos de este ejercicio"

#### Scenario: History fails

- **WHEN** the history read fails with `FORBIDDEN`
- **THEN** both evolution cards show a load error with retry, and the comparison table is still shown

### Requirement: Per-exercise comparison table

The tab SHALL show a card "Comparativa por ejercicio", subtitled "Resultados de esta sesión frente a la ejecución
anterior". It SHALL hold a table with one row per weighted exercise, in workout order, and these columns:

- "Ejercicio": its position in the workout and its name in the active language;
- "Anterior";
- "Actual";
- "Cambio";
- "Volumen (actual)";
- "Cumplimiento".

#### Scenario: Row with a previous execution

- **WHEN** an exercise's top set is 7 × 130 kg and its previous top set is 6 × 120 kg, with volume 3190 and 3 of 3 sets
  in target
- **THEN** its row shows "6 × 120 kg", "7 × 130 kg" in bold, "▲ +8,3 %", "3.190 kg" and "100%"

#### Scenario: Row without a previous execution

- **WHEN** an exercise's `previous` is `null`
- **THEN** "Anterior" and "Cambio" show "—"

#### Scenario: No weighted exercise

- **WHEN** the summary has no weighted exercise
- **THEN** the card shows "Ningún ejercicio con peso en esta sesión" instead of the table

### Requirement: Weight change and compliance marks

"Cambio" SHALL show the change from the previous top set's weight to the current one, as a percentage with one
decimal: in a success mark with "▲" when it rises, in a warning mark with "▼" when it falls, and "=" when it is equal.
"Cumplimiento" SHALL show `inTarget / evaluated` as a rounded percentage: in a success mark at 100%, in a warning mark
below it, and "—" when nothing was evaluated.

#### Scenario: Weight falls

- **WHEN** the previous top set is 100 kg and the current one 95 kg
- **THEN** "Cambio" shows "▼ -5,0 %" in a warning mark

#### Scenario: Same weight

- **WHEN** both top sets are 100 kg
- **THEN** "Cambio" shows "="

#### Scenario: Partial compliance

- **WHEN** an exercise has 2 of 3 sets in target
- **THEN** "Cumplimiento" shows "67%" in a warning mark

### Requirement: Sets per muscle group

The tab SHALL show a card "Distribución de series por grupo muscular", subtitled "Series efectivas de la sesión". It
SHALL have one row per muscle, taken from each exercise's own muscle, showing the muscle name, a bar and "<n> series".

- `<n>` SHALL be the counted records of that muscle's exercises.
- Muscles with no counted record SHALL NOT appear.
- Rows SHALL be sorted by count, descending, with bars relative to the largest count.

#### Scenario: Trained muscles only

- **WHEN** the quadriceps exercises have 9 counted records, the glutes exercise 4, and a calves exercise has none
- **THEN** the card shows "Cuádriceps" with "9 series" first and "Glúteos" with "4 series" next
- **AND** no calves row is shown

### Requirement: Session observations are a marked preview

The tab SHALL show a card "Observaciones de la sesión", subtitled "Puntos clave y recomendaciones", with a "Vista
previa" mark beside its title. It SHALL list three fixed sample observations, each with an icon, a title in its
severity colour and a text: "Progresión destacada" (positive), "Buen cumplimiento" (info) and "Revisar" (warning). They
SHALL be the same for every workout.

#### Scenario: Preview content

- **WHEN** the tab is shown for any workout
- **THEN** the observations card shows the "Vista previa" mark and the three sample observations in that order

### Requirement: The tab has no role differences and no actions

`ADMIN` and `TRAINER` SHALL see the same tab content. The tab SHALL NOT offer any action that changes data.

#### Scenario: Trainer and admin

- **WHEN** an `ADMIN` and a `TRAINER` open the tab for the same workout
- **THEN** both see the same blocks and values

### Requirement: The tab is translated

Every text of the tab SHALL be available in Spanish and English and follow the selected language, including the
muscle names, and the number and date formats.

#### Scenario: English

- **WHEN** the language is English
- **THEN** the cards read "Session results", "Exercise comparison" and "Sets per muscle group", and the volume reads
  "16,913.4 kg"
