# Spec Delta

## MODIFIED Requirements

### Requirement: The rest card shows the exercise's rest time

A full-width band kept against the tab bar SHALL read "Descanso: M:SS" when the exercise has `restMinutes`, with
the minutes converted to minutes and seconds (`1.5` -> `1:30`, `2` -> `2:00`, rounded to the second), and "Sin
descanso" when it does not. While a countdown is running or paused, the time shown SHALL be the remaining time
instead of the full rest time.

#### Scenario: With rest

- **WHEN** the exercise has `restMinutes` 1.5
- **THEN** the band reads "Descanso: 1:30"

#### Scenario: Without rest

- **WHEN** the exercise has no `restMinutes`
- **THEN** the card reads "Sin descanso"

#### Scenario: Remaining time while counting

- **WHEN** the exercise has `restMinutes` 1.5 and the countdown has run for 3 seconds
- **THEN** the band reads "Descanso: 1:27"

## ADDED Requirements

### Requirement: The rest card offers countdown controls

When the exercise has `restMinutes`, the rest card SHALL show a play button and a reset button, in every workout
status. When it has none, the card SHALL show no buttons.

#### Scenario: Controls with rest

- **WHEN** the exercise has `restMinutes` 1.5
- **THEN** the card shows "Iniciar descanso" and a disabled "Reiniciar descanso"

#### Scenario: No controls without rest

- **WHEN** the exercise has no `restMinutes`
- **THEN** the card shows neither "Iniciar descanso" nor "Pausar descanso" nor "Reiniciar descanso"

#### Scenario: Controls on a read-only workout

- **WHEN** the workout is `PUBLISHED` and the exercise has `restMinutes` 1
- **THEN** the card shows an enabled "Iniciar descanso"

### Requirement: Play starts or resumes the countdown

Pressing play SHALL start the countdown from the full rest time, or resume it from the remaining time when paused.
While running, the shown time SHALL drop by one second every second, the play button SHALL become a pause button and
reset SHALL be enabled.

#### Scenario: Start

- **WHEN** the exercise has `restMinutes` 1 and the athlete presses "Iniciar descanso" and 5 seconds pass
- **THEN** the band reads "Descanso: 0:55", "Pausar descanso" is shown and "Reiniciar descanso" is enabled

#### Scenario: Resume

- **WHEN** the countdown was paused at "0:55" and the athlete presses "Iniciar descanso" and 5 seconds pass
- **THEN** the band reads "Descanso: 0:50"

### Requirement: Pause freezes the countdown

Pressing pause SHALL stop the countdown at its remaining time, which SHALL not change until play or reset is pressed.
The pause button SHALL become the play button again.

#### Scenario: Pause

- **WHEN** the countdown reads "0:55", the athlete presses "Pausar descanso" and 10 seconds pass
- **THEN** the band still reads "Descanso: 0:55" and "Iniciar descanso" is shown

### Requirement: Reset restores the full rest time

Pressing reset SHALL stop the countdown, running, paused or finished, and show the full rest time with the play
button. Reset SHALL be disabled while the countdown is idle at the full rest time.

#### Scenario: Reset while running

- **WHEN** the countdown of a 1-minute rest is running at "0:40" and the athlete presses "Reiniciar descanso"
- **THEN** the band reads "Descanso: 1:00", "Iniciar descanso" is shown and "Reiniciar descanso" is disabled

#### Scenario: Reset while paused

- **WHEN** the countdown of a 1-minute rest is paused at "0:40" and the athlete presses "Reiniciar descanso"
- **THEN** the band reads "Descanso: 1:00" and stays there as time passes

### Requirement: The countdown finishes at zero

When the remaining time reaches zero the countdown SHALL stop at zero and the card SHALL read "Descanso terminado".
Play SHALL be disabled until reset.

#### Scenario: Finish

- **WHEN** the countdown of a 1-minute rest has run for 60 seconds
- **THEN** the card reads "Descanso terminado", "Iniciar descanso" is disabled and "Reiniciar descanso" is enabled

#### Scenario: Reset after finishing

- **WHEN** the countdown has finished and the athlete presses "Reiniciar descanso"
- **THEN** the band reads "Descanso: 1:00" and "Iniciar descanso" is enabled

### Requirement: The countdown follows the clock

The remaining time SHALL be the time left until the rest ends by the device clock, not a count of elapsed ticks. A
late tick, a busy screen or time spent in the background SHALL not leave the countdown behind; on return to the
foreground the card SHALL show the time left, or the finished state if the rest ended meanwhile.

#### Scenario: Late tick

- **WHEN** a 1-minute countdown is running and the clock advances 20 seconds before the next tick runs
- **THEN** after that tick the band reads "Descanso: 0:40"

#### Scenario: Back from the background

- **WHEN** a 1-minute countdown is running, the app goes to the background and returns 30 seconds later
- **THEN** the band reads "Descanso: 0:30"

#### Scenario: Rest ended in the background

- **WHEN** a 1-minute countdown is running, the app goes to the background and returns 90 seconds later
- **THEN** the card reads "Descanso terminado"

### Requirement: The countdown belongs to the shown exercise

The countdown SHALL keep running while the rest card is hidden by the keyboard and after a set is saved. It SHALL
return to idle with the full rest time when the athlete switches to another exercise, and SHALL be discarded when
the screen closes.

#### Scenario: Keyboard

- **WHEN** a 1-minute countdown is running, the keyboard opens for 10 seconds and closes
- **THEN** the band reads "Descanso: 0:50"

#### Scenario: Saving a set

- **WHEN** a 1-minute countdown is running at "0:50" and the athlete saves a set
- **THEN** the countdown keeps running from "0:50"

#### Scenario: Switching exercise

- **WHEN** a countdown is running and the athlete selects another exercise with `restMinutes` 2 in the navigator
- **THEN** the band reads "Descanso: 2:00" and "Iniciar descanso" is shown

### Requirement: The countdown follows the active language

In English the card SHALL read "Rest finished" when finished, and the controls SHALL be labelled "Start rest",
"Pause rest" and "Reset rest".

#### Scenario: English controls

- **WHEN** the active language is English and the exercise has `restMinutes` 1
- **THEN** the card shows "Start rest" and "Reset rest", and "Pause rest" once started
