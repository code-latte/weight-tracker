# Spec Delta

## MODIFIED Requirements

### Requirement: The person strip shows the athlete and the last update

The review SHALL show a strip with the athlete's initials, full name (their email when nameless) and "Atleta". It
SHALL also show "Inicio: <start>", where `<start>` is the workout's `startedAt` as a medium date and a short time in
the active language and the user's time zone, or "—" when `startedAt` is `null`. When `resultsUpdatedAt` is set, it
SHALL show "Última actualización: <relative time>" in the active language; otherwise that text SHALL NOT be shown.

#### Scenario: Athlete and start time

- **WHEN** a `STARTED` workout of athlete Víctor Lendínez, started on 13 November 2026 at 10:42 local time, is loaded
- **THEN** the strip shows "VL", "Víctor Lendínez", "Atleta" and "Inicio: 13 nov 2026, 10:42"

#### Scenario: Athlete and placeholders

- **WHEN** a `STARTED` workout of athlete Víctor Lendínez whose `startedAt` is `null` is loaded
- **THEN** the strip shows "VL", "Víctor Lendínez", "Atleta" and "Inicio: —"

#### Scenario: Last update shown

- **WHEN** the workout's `resultsUpdatedAt` is 30 seconds ago
- **THEN** the strip shows "Última actualización: hace 30 segundos"

#### Scenario: No results yet

- **WHEN** the workout's `resultsUpdatedAt` is `null`
- **THEN** no "Última actualización" text is shown

### Requirement: Summary cards show the session figures

The review SHALL show four cards:

- "Tiempo transcurrido" with the workout's elapsed time;
- "Series completadas" as "<counted records> / <planned sets>" over all exercises;
- "Ejercicios terminados" as "<completed> / <exercises>";
- "Volumen registrado" in kg.

#### Scenario: Figures

- **WHEN** the workout has exercises A (3 planned, 3 records), B (4 planned, 1 record) and C (3 planned, none)
- **THEN** "Series completadas" shows "4 / 10"
- **AND** "Ejercicios terminados" shows "1 / 3"

## ADDED Requirements

### Requirement: Elapsed time measures the session

For a `STARTED` workout the elapsed time SHALL be the current time minus `startedAt`. For a `COMPLETED` or `REVIEWED`
workout it SHALL be `completedAt` minus `startedAt`. The card SHALL show "—" when a date it needs is `null` or when
the result is negative.

#### Scenario: Started workout

- **WHEN** a `STARTED` workout's `startedAt` is 25 minutes ago
- **THEN** "Tiempo transcurrido" shows "25 min"

#### Scenario: Completed workout

- **WHEN** a `COMPLETED` workout was started at 10:00 and completed at 11:05
- **THEN** "Tiempo transcurrido" shows "1 h 05 min"

#### Scenario: Reviewed workout

- **WHEN** a `REVIEWED` workout was started at 10:00 and completed at 10:40
- **THEN** "Tiempo transcurrido" shows "40 min"

#### Scenario: Start unknown

- **WHEN** the workout's `startedAt` is `null`
- **THEN** "Tiempo transcurrido" shows "—"

#### Scenario: Completion unknown

- **WHEN** a `COMPLETED` workout has a `startedAt` and a `null` `completedAt`
- **THEN** "Tiempo transcurrido" shows "—"

#### Scenario: Dates out of order

- **WHEN** a `COMPLETED` workout's `completedAt` is earlier than its `startedAt`
- **THEN** "Tiempo transcurrido" shows "—"

### Requirement: Elapsed time is shown in hours and minutes

The elapsed time SHALL be shown as "<h> h <mm> min" from one hour, with minutes padded to two digits, as "<m> min"
from one minute, and as "< 1 min" below one minute. Seconds SHALL be truncated, not rounded.

#### Scenario: Formats

- **WHEN** the elapsed time is 30 seconds, 12 minutes 59 seconds, and 2 hours 3 minutes
- **THEN** the card shows "< 1 min", "12 min" and "2 h 03 min" respectively

### Requirement: A started workout's elapsed time keeps counting

While the review shows a `STARTED` workout, the elapsed time SHALL be recomputed at least once a minute without
reading the workout again. For `COMPLETED` and `REVIEWED` workouts it SHALL NOT change while the screen is open.

#### Scenario: Minute passes

- **WHEN** a `STARTED` workout shows "25 min" and one minute passes
- **THEN** "Tiempo transcurrido" shows "26 min"
- **AND** the workout is not read again

#### Scenario: Finished workout does not tick

- **WHEN** a `COMPLETED` workout shows "40 min" and one minute passes
- **THEN** "Tiempo transcurrido" still shows "40 min"
