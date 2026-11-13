# Spec Delta

## Purpose

Gives trainers and admins a read-only backoffice screen to follow and review a workout the athlete has started,
showing how far the session went and what was recorded against the plan.

## ADDED Requirements

### Requirement: Started workouts open the review

On `/workout?id=<id>`, a loaded workout whose status is `STARTED`, `COMPLETED` or `REVIEWED` SHALL show the review
screen and SHALL NOT show the editor. The review SHALL be shown even when the exercise catalog is not available, and SHALL
NOT read the workout again while open.

#### Scenario: Started workout opens the review

- **WHEN** a trainer opens `/workout?id=w-1` and the API answers with a `STARTED` workout
- **THEN** the heading "Revisión del entrenamiento" is shown
- **AND** "Editar entrenamiento", "Guardar" and "Añadir ejercicio" are not shown

#### Scenario: Completed and reviewed workouts open the review

- **WHEN** the loaded workout is `COMPLETED`, or it is `REVIEWED`
- **THEN** the heading "Revisión del entrenamiento" is shown

#### Scenario: No polling

- **WHEN** the review of a `STARTED` workout has been open for a while
- **THEN** exactly one workout read has been sent

### Requirement: The review header shows the workout and its status

The review SHALL show a back link, the title "Revisión del entrenamiento", a subtitle and the workout status chip. The
back link SHALL lead to `/mesocycle?id=<mesocycleId>` for a placed workout and to `/assigned-workouts` otherwise.

The subtitle SHALL read "<name> · <mesocycle name> · Semana <n>" for a placed workout and "<name>" for a standalone
one. The chip SHALL use the status text and colour of the workouts list, except that a `STARTED` workout's chip is live.

#### Scenario: Placed workout header

- **WHEN** `STARTED` workout "Tren inferior B" in week 1 of mesocycle "Bloque 1" (`m-1`) is loaded
- **THEN** the subtitle reads "Tren inferior B · Bloque 1 · Semana 1"
- **AND** the back link leads to `/mesocycle?id=m-1`
- **AND** a chip "En curso" is shown

#### Scenario: Standalone workout header

- **WHEN** standalone `COMPLETED` workout "Pierna" is loaded
- **THEN** the subtitle reads "Pierna"
- **AND** the back link leads to `/assigned-workouts`
- **AND** a chip "Completado" is shown

### Requirement: A started workout's status chip is live

For a `STARTED` workout, the status chip SHALL be outlined red with a live (broadcast) icon, and SHALL fade slowly in
and out in a loop. The fade SHALL stop for a visitor who prefers reduced motion. The chip for any other status SHALL be
as in the workouts list, without the icon or the fade. No separate "En directo" chip SHALL be shown.

#### Scenario: Started workout

- **WHEN** a `STARTED` workout is loaded
- **THEN** the status chip "En curso" is outlined red and shows the live icon
- **AND** no "En directo" chip is shown

#### Scenario: Completed workout

- **WHEN** a `COMPLETED` workout is loaded
- **THEN** the status chip "Completado" is not red and shows no live icon

### Requirement: The person strip shows the athlete and the last update

The review SHALL show a strip with the athlete's initials, full name (their email when nameless) and "Atleta". It
SHALL also show the start time placeholder "Inicio: —". When `resultsUpdatedAt` is set, it SHALL show "Última
actualización: <relative time>" in the active language; otherwise that text SHALL NOT be shown.

#### Scenario: Athlete and placeholders

- **WHEN** a `STARTED` workout of athlete Víctor Lendínez is loaded
- **THEN** the strip shows "VL", "Víctor Lendínez", "Atleta" and "Inicio: —"

#### Scenario: Last update shown

- **WHEN** the workout's `resultsUpdatedAt` is 30 seconds ago
- **THEN** the strip shows "Última actualización: hace 30 segundos"

#### Scenario: No results yet

- **WHEN** the workout's `resultsUpdatedAt` is `null`
- **THEN** no "Última actualización" text is shown

### Requirement: Only admins see the trainer in the review

The person strip SHALL show the workout's trainer name only to an `ADMIN`. A `TRAINER` SHALL NOT see it.

#### Scenario: Admin sees the trainer

- **WHEN** an admin opens the review of a workout whose trainer is Ana Ruiz
- **THEN** the strip shows "Ana Ruiz"

#### Scenario: Trainer does not see the trainer

- **WHEN** a trainer opens the review of a workout whose trainer is Ana Ruiz
- **THEN** "Ana Ruiz" is not shown

### Requirement: Recorded sets are counted up to the planned sets

An exercise's planned sets SHALL be the sum of its set groups' counts. Its counted records SHALL be its first records,
in order, up to its planned sets. Any record beyond them SHALL be discarded from every figure, row and state on the
review.

#### Scenario: Extra record discarded

- **WHEN** an exercise plans 3 sets and has 4 records
- **THEN** its counted records are the first 3
- **AND** the fourth appears in no figure, row or state

### Requirement: Each exercise has a state

An exercise SHALL be "Completado" when its counted records equal its planned sets and it plans at least one set. It
SHALL be "En progreso" when it has some counted records but fewer than planned. It SHALL be "Pendiente" otherwise.

#### Scenario: States

- **WHEN** exercise A plans 3 sets with 3 records, B plans 4 with 1 record, and C plans 3 with none
- **THEN** A shows "Completado", B shows "En progreso" and C shows "Pendiente"

### Requirement: Summary cards show the session figures

The review SHALL show four cards:

- "Tiempo transcurrido" with "—", as a placeholder;
- "Series completadas" as "<counted records> / <planned sets>" over all exercises;
- "Ejercicios terminados" as "<completed> / <exercises>";
- "Volumen registrado" in kg.

#### Scenario: Figures

- **WHEN** the workout has exercises A (3 planned, 3 records), B (4 planned, 1 record) and C (3 planned, none)
- **THEN** "Tiempo transcurrido" shows "—"
- **AND** "Series completadas" shows "4 / 10"
- **AND** "Ejercicios terminados" shows "1 / 3"

### Requirement: Volume sums weighted reps

"Volumen registrado" SHALL be the sum of `reps × weight` over the counted records that have both. Records without reps
or without weight add nothing. A per-side exercise SHALL NOT be doubled.

#### Scenario: Volume

- **WHEN** the counted records are 8 × 80 kg, 8 × 85 kg, a 60-second record and 10 reps with no weight
- **THEN** "Volumen registrado" shows "1.320 kg"

#### Scenario: Discarded record adds nothing

- **WHEN** an exercise plans 1 set and its records are 5 × 100 kg and 5 × 100 kg
- **THEN** "Volumen registrado" shows "500 kg"

### Requirement: Session progress shows done and pending sets

The review SHALL show a card "Progreso de la sesión" with:

- the percentage of counted records over planned sets, rounded, and 0% when nothing is planned;
- a progress bar at that value;
- "<n> series realizadas";
- "<n> pendientes", where n is planned minus counted.

#### Scenario: Progress

- **WHEN** the workout has 8 counted records out of 29 planned sets
- **THEN** the card shows "28%", "8 series realizadas" and "21 pendientes"
- **AND** the progress bar is at 28

#### Scenario: Nothing planned

- **WHEN** the workout plans no sets
- **THEN** the card shows "0%"

### Requirement: The review has three tabs

The review SHALL show the tabs "Ejercicios", "Resumen y métricas" and "Feedback", with "Ejercicios" selected on
opening. "Resumen y métricas" and "Feedback" SHALL show an empty panel. The tab row SHALL NOT offer "Expandir todo" or
a menu.

#### Scenario: Default tab

- **WHEN** the review opens
- **THEN** "Ejercicios" is selected and the exercise cards are shown
- **AND** no "Expandir todo" button is shown

#### Scenario: Empty tabs

- **WHEN** the user selects "Resumen y métricas", or selects "Feedback"
- **THEN** the exercise cards are not shown and the panel is empty

### Requirement: Each exercise is a collapsible card

The "Ejercicios" tab SHALL list one card per exercise, flat in workout order, regardless of superseries. Each card
SHALL show:

- an icon for its state;
- its position number;
- its name in the active language;
- its body-part and muscle chips;
- its state chip;
- a button to expand or collapse it, labelled "Expandir <name>" or "Contraer <name>".

#### Scenario: Collapsed card

- **WHEN** the review lists "Peso muerto rumano", second in the workout, with body part Piernas and muscle
  Isquiotibiales
- **THEN** its card shows "2", "Peso muerto rumano", "Piernas", "Isquiotibiales" and its state chip

#### Scenario: Toggling

- **WHEN** the user presses "Expandir Sentadilla" on the collapsed card of "Sentadilla"
- **THEN** its sets table is shown and the button reads "Contraer Sentadilla"

### Requirement: One card starts expanded

On opening, the first exercise "En progreso" SHALL be expanded. If there is none, the first exercise that is not
"Completado" SHALL be expanded instead. Every other card SHALL start collapsed, and all cards SHALL start collapsed when
every exercise is "Completado".

#### Scenario: In-progress exercise expanded

- **WHEN** exercise 1 is "Completado", 2 is "En progreso" and 3 is "Pendiente"
- **THEN** only exercise 2 starts expanded

#### Scenario: No exercise in progress

- **WHEN** exercise 1 is "Completado" and 2 and 3 are "Pendiente"
- **THEN** only exercise 2 starts expanded

#### Scenario: All completed

- **WHEN** every exercise is "Completado"
- **THEN** every card starts collapsed

### Requirement: The expanded card shows the exercise's effort

An expanded card SHALL show "Esfuerzo: <effort>" above its table. The effort SHALL be translated by the exercise type's
scale. The line SHALL NOT be shown when the exercise has no effort.

#### Scenario: Strength effort

- **WHEN** a strength exercise with effort RIR 3 is expanded
- **THEN** "Esfuerzo: RIR 3" is shown

#### Scenario: No effort

- **WHEN** an exercise with an empty effort is expanded
- **THEN** no "Esfuerzo" line is shown

### Requirement: The sets table pairs each planned set with its record

An expanded card SHALL show a table with the columns "Serie", "Objetivo (plan)", "Realizado" and "Descanso". It SHALL
have one row per planned set, numbered from 1. The counted records SHALL be paired with the rows in order. No row SHALL
be shown for a discarded record.

#### Scenario: Rows

- **WHEN** an exercise plans 2 sets of 8 reps and 2 sets of 6 reps, and has 3 records
- **THEN** the table has 4 rows numbered 1 to 4
- **AND** rows 1 to 3 show the records and row 4 is pending

### Requirement: Objetivo shows the planned target without weight

"Objetivo (plan)" SHALL show the planned target only, never a weight:

- "N reps" for repetitions;
- "N–M reps" for a range;
- "Máx. reps" for max reps;
- "N s" for a duration;
- "N min" for minutes.

#### Scenario: Targets

- **WHEN** an exercise's planned sets are 8 reps, 8 to 10 reps, max reps, 30 seconds and 1 minute
- **THEN** the rows show "8 reps", "8–10 reps", "Máx. reps", "30 s" and "1 min"

### Requirement: Realizado shows the record or Pendiente

For a row with a record, "Realizado" SHALL show a done mark and one of:

- "<reps> × <weight> kg" when the record has both;
- "<reps> reps" when it has no weight;
- the duration when it is timed.

A row without a record SHALL show "Pendiente".

#### Scenario: Recorded and pending

- **WHEN** the rows' records are 8 reps at 80 kg, 10 reps with no weight, and none
- **THEN** "Realizado" shows "8 × 80 kg", "10 reps" and "Pendiente"

#### Scenario: Timed record

- **WHEN** a row's record is a 45-second duration
- **THEN** "Realizado" shows "45 s"

### Requirement: Descanso shows the exercise's rest

"Descanso" SHALL show the exercise's rest as `m:ss` on every row, or "—" when the exercise has no rest.

#### Scenario: Rest

- **WHEN** the exercise rests 2 minutes
- **THEN** every row shows "2:00" under "Descanso"

#### Scenario: No rest

- **WHEN** the exercise has no rest
- **THEN** every row shows "—" under "Descanso"

### Requirement: The review is read-only

The review SHALL NOT offer any action that changes the workout. That includes saving its data, changing its status,
marking it reviewed, and adding, editing, reordering or removing exercises.

#### Scenario: No actions

- **WHEN** a `COMPLETED` workout's review is shown
- **THEN** "Guardar", "Publicar", "Volver a borrador" and "Añadir ejercicio" are not shown

### Requirement: The review can save the workout as a template

The review header SHALL offer "Guardar como plantilla". Activating it SHALL send exactly one template creation from the
workout's id, with no body, and no workout update. On success the review SHALL show "Plantilla creada." with a "Ver
plantilla" link to `/workout-template?id=<new id>`, and the user SHALL stay on the review.

#### Scenario: Button shown

- **WHEN** a `COMPLETED` workout's review is shown
- **THEN** "Guardar como plantilla" is shown and enabled

#### Scenario: Template created

- **WHEN** the user clicks "Guardar como plantilla" on workout `w-1` and the API answers with template `wt-9`
- **THEN** exactly one template creation from workout `w-1` is sent and no workout update
- **AND** "Plantilla creada." is shown with "Ver plantilla" leading to `/workout-template?id=wt-9`
- **AND** the user is still on `/workout?id=w-1`

#### Scenario: Template creation refused

- **WHEN** the creation fails with `VALIDATION`
- **THEN** the message for `VALIDATION` is shown and the review stays unchanged

### Requirement: The review is translated

Every text of the review SHALL be available in Spanish and English and follow the selected language.

#### Scenario: English

- **WHEN** the user has English selected and opens a started workout
- **THEN** the review shows "Workout review", "Sets completed", "Exercises finished", "Volume recorded", "Session
  progress", "Exercises", "Summary and metrics" and "Feedback"
