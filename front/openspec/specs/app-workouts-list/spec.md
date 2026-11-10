# app-workouts-list Specification

## Purpose

The athlete's Entrenamientos week view in the app: weekly progress, the workouts of the viewed week with their status,
author and day, and navigation between weeks.

## Requirements

### Requirement: The screen shows one week at a time

The Entrenamientos screen SHALL show one week, Monday to Sunday in the device's local time. It SHALL open on the week
that contains today every time the screen is entered. A workout SHALL belong to the week that contains its
`workoutDate`. The screen SHALL show every workout the API returns to the athlete for that week, and no other.

#### Scenario: Opens on the current week

- **GIVEN** today is Monday 9 November 2026
- **WHEN** the athlete opens the Entrenamientos tab
- **THEN** the viewed week is Monday 9 to Sunday 15 November 2026

#### Scenario: Sunday belongs to the week that started on Monday

- **GIVEN** today is Sunday 15 November 2026
- **WHEN** the athlete opens the Entrenamientos tab
- **THEN** the viewed week is Monday 9 to Sunday 15 November 2026

#### Scenario: Only the viewed week's workouts are listed

- **GIVEN** the athlete has workouts dated 8, 9, 15 and 16 November 2026
- **WHEN** the viewed week is 9 to 15 November 2026
- **THEN** only the workouts dated 9 and 15 November are listed

### Requirement: The week range format

A week range SHALL read as weekday, day and month of its Monday and of its Sunday, in the active language: `Lun, 9 nov

- Dom, 15 nov`in Spanish and`Mon, Nov 9 - Sun, Nov 15` in English.

#### Scenario: Spanish range

- **WHEN** the active language is Spanish and the week starts on 9 November 2026
- **THEN** its range reads "Lun, 9 nov - Dom, 15 nov"

### Requirement: The week-progress card summarises the viewed week

A card fixed at the top of the screen SHALL show, on its left (up to 70% of its width), a title and a subtitle. The
title SHALL be "Esta semana" when the viewed week contains today, "Próxima semana" when it is the week after, and the
week range otherwise. The subtitle SHALL be the week range under "Esta semana" or "Próxima semana", and the week's
year otherwise.

#### Scenario: Current week title

- **GIVEN** today is 9 November 2026
- **WHEN** the screen shows the current week
- **THEN** the card shows "Esta semana" and "Lun, 9 nov - Dom, 15 nov"

#### Scenario: Next week

- **GIVEN** today is 9 November 2026
- **WHEN** the screen shows the week of 16 November 2026
- **THEN** the card shows "Próxima semana" and "Lun, 16 nov - Dom, 22 nov"

#### Scenario: Any other week

- **GIVEN** today is 9 November 2026
- **WHEN** the screen shows the week of 23 November 2026, or of 2 November 2026
- **THEN** the card title is that week's range and its subtitle is "2026"

#### Scenario: English

- **GIVEN** the active language is English and today is 9 November 2026
- **WHEN** the screen shows the current week
- **THEN** the card shows "This week" and "Mon, Nov 9 - Sun, Nov 15"

### Requirement: The week-progress card shows the week's completion

On its right, the week-progress card SHALL show `completed / total` and a progress bar filled at that ratio. Total is
the number of workouts in the viewed week; completed is how many of them are `COMPLETED` or `REVIEWED`. With no
workouts the card SHALL show `0 / 0` and an empty bar.

#### Scenario: Current week completion

- **GIVEN** the viewed week has 5 workouts, 1 completed and 1 reviewed
- **WHEN** the screen shows that week
- **THEN** the card shows "2 / 5" and a bar at 40%

#### Scenario: Empty week

- **WHEN** the viewed week has no workouts
- **THEN** the card shows "0 / 0" and an empty bar

### Requirement: Each workout of the week is a card

Between the week-progress card and the week navigator, the screen SHALL list the viewed week's workouts, one card
each, ordered by `workoutDate` ascending (same date keeps the API's order). Only the list SHALL scroll; the
week-progress card and the week navigator SHALL stay in place. Tapping a card SHALL open that workout's screen.
Coming back from it SHALL show the list on the same week the athlete left.

#### Scenario: Order

- **WHEN** the week has workouts dated 13, 9 and 11 November
- **THEN** they are listed 9, 11, 13

#### Scenario: Tapping a card opens the workout

- **WHEN** the athlete taps the card of a workout
- **THEN** that workout's screen is shown

#### Scenario: Coming back keeps the week

- **GIVEN** the athlete moved to next week and opened one of its workouts
- **WHEN** the athlete goes back
- **THEN** the list shows next week

### Requirement: The workout card shows its status

On its left, each workout card SHALL show a status icon: a filled circle with a tick in the secondary colour for
`COMPLETED`; a filled circle with a tick in the primary colour for `REVIEWED`; a circle with a play sign in the
secondary colour for `STARTED`; an empty outlined circle for `PUBLISHED` and `DRAFT`. Each icon SHALL carry an
accessible label: "Completado", "Revisado", "En curso" or "Pendiente".

#### Scenario: Status icons

- **WHEN** the week has one workout in each status `COMPLETED`, `REVIEWED`, `STARTED`, `PUBLISHED` and `DRAFT`
- **THEN** their icons are labelled "Completado", "Revisado", "En curso", "Pendiente" and "Pendiente"

### Requirement: The workout card shows its name and author

In its middle, the widest column, each workout card SHALL show the workout name and, below it, the author: the
trainer's name and surname when the workout has a trainer, and "Creado por mí" when it has none, never the athlete's
own name.

#### Scenario: Prescribed workout

- **WHEN** a workout has a trainer named Ana López
- **THEN** its card shows "Ana López" under the workout name

#### Scenario: Own workout

- **WHEN** a workout has no trainer
- **THEN** its card shows "Creado por mí" under the workout name and not the athlete's name

### Requirement: The workout card shows its day

On its right, each workout card SHALL show a small pill with the short weekday (`lun`, `mar`, `mié`, `jue`, `vie`,
`sáb`, `dom`) of the day the workout was completed, `completedAt` read in the device's local time, when the workout is
`COMPLETED` or `REVIEWED` and has a `completedAt`, and `--` otherwise. The planned `workoutDate` SHALL NOT set the
pill; it only places the workout in its week.

#### Scenario: Day pill

- **WHEN** a `COMPLETED` workout dated Wednesday 11 November 2026 was completed on Friday 13 and a `STARTED` one is
  dated Thursday 12
- **THEN** the first card's pill shows "vie" and the second's shows "--"

#### Scenario: Done workout without a completion time

- **WHEN** a `COMPLETED` workout has no `completedAt`
- **THEN** its card's pill shows "--"

### Requirement: An empty week says so

When the viewed week has no workouts, the list area SHALL show "No tienes entrenamientos esta semana".

#### Scenario: Empty week list

- **WHEN** the viewed week has no workouts
- **THEN** the list area shows "No tienes entrenamientos esta semana"

### Requirement: The week navigator moves between weeks

A navigator fixed at the bottom of the screen, just above the tab bar, SHALL show a left caret, the viewed week's
range and a right caret. The range SHALL always be shown, whatever the week. The left caret SHALL move to the previous
week and the right caret to the next one, with no limit in either direction. The carets SHALL be labelled "Semana
anterior" and "Semana siguiente". Changing week SHALL update the card and the list in place.

#### Scenario: Range on the current week

- **GIVEN** today is 9 November 2026
- **WHEN** the screen shows the current week
- **THEN** the navigator shows "Lun, 9 nov - Dom, 15 nov"

#### Scenario: Move forward and back

- **GIVEN** the screen shows the current week
- **WHEN** the athlete taps the right caret twice and then the left caret once
- **THEN** the screen shows next week, titled "Próxima semana"

#### Scenario: Far past

- **WHEN** the athlete moves to a week five years back
- **THEN** the navigator and the card title show that week's range and the card subtitle shows its year

### Requirement: Loading and failure

While the workouts are fetched for the first time, the screen SHALL show a loading state. If the fetch fails, the
screen SHALL show the error state with a retry action that fetches again. Changing week SHALL NOT fetch again; only
retry and pull to refresh do.

#### Scenario: Loading

- **WHEN** the workouts have not arrived yet
- **THEN** the screen shows the loading state

#### Scenario: Failure and retry

- **GIVEN** fetching the workouts fails
- **WHEN** the athlete taps retry and the fetch succeeds
- **THEN** the screen shows the current week with its workouts

#### Scenario: No refetch on week change

- **WHEN** the athlete changes week
- **THEN** no new request for workouts is made

### Requirement: Pull to refresh

When the workouts have been read, pulling the list down SHALL read them again, showing the platform's refresh spinner
for at least 800 ms. Meanwhile the current list and the viewed week SHALL stay on screen. When the read ends, the
screen SHALL show the new workouts for the same week, or the error state with retry if it failed.

#### Scenario: Refresh shows what changed

- **GIVEN** the athlete is viewing next week and a workout was added to it since the screen opened
- **WHEN** the athlete pulls the list down
- **THEN** the spinner shows, and once it ends the screen still shows next week, now with the new workout

#### Scenario: Minimum spinner time

- **GIVEN** the API answers at once
- **WHEN** the athlete pulls the list down
- **THEN** the spinner stays for 800 ms and the previous list stays visible meanwhile

#### Scenario: Refresh fails

- **WHEN** the athlete pulls the list down and the read fails
- **THEN** the screen shows the error state with retry
