# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-app-rest-countdown.md`.

## Why

The exercise screen shows the rest between sets ("Descanso: 1:30") but only as text, so the athlete has to time the
rest with another app. The exercise screen change left the countdown out of scope on purpose. This change makes the
rest card the timer, so the athlete can rest and record sets on the same screen.

## What Changes

- **app, exercise screen** (`/workouts/[id]/exercises/[exerciseId]`), rest card:
  - when the exercise has `restMinutes`, the card gains a play / pause button and a reset button next to the time;
  - play starts the countdown from the full rest time, or resumes it from a pause; the shown time drops by one second
    every second, and play becomes pause;
  - pause freezes the remaining time; reset stops the countdown and restores the full rest time (disabled while idle
    at the full value);
  - at `0:00` the countdown stops, the card reads "Descanso terminado" and play is disabled until reset;
  - the remaining time is derived from timestamps, so it stays right when the JS thread is busy, when ticks are late
    and after the app returns from the background;
  - the countdown survives the card hiding while the keyboard is open, and resets when the athlete switches exercise
    or leaves the screen;
  - without `restMinutes` the card still reads "Sin descanso" with no buttons.
- **core**: nothing. **backoffice**: nothing. **API**: nothing.

### Fronts and roles

- app, `USER`: everything above, in every workout status. The countdown sends no request and records nothing, so it
  is not tied to whether set records are allowed.
- backoffice, `ADMIN` / `TRAINER`: does not land here.

### Components (atomic level, reuse)

- Atoms reused, none added: `icon-button`, `icon`, `text`, `card`.
- Molecules: `RestCard` gains the controls and the finished state; it stays presentational.
- No organism. The state lives in a new presentation hook used by `WorkoutExerciseView`.

### Canonical references

UI references come from the app only. Atom, molecule and organism references are `pending` in
`.stpr/template/README.md`, so this change follows the app shapes already in place.

- Countdown hook -> `app/src/presentation/common/hooks/use-keyboard-visible.ts`: subscribes to a platform source and
  cleans up on unmount; no container, no use case.
- Time helpers -> `restTimeOf` in `app/src/presentation/workouts/set-rows.ts`.
- `RestCard` controls -> `workout-hero-panel.tsx`: a molecule taking an action with label, handler and disabled state.
- Tests -> `app/test/presentation/workouts/views/workout-exercise-view.test.tsx` (fake timers as in
  `workouts-view.test.tsx`) and `app/test/presentation/workouts/set-rows.test.ts`.

### Conflicts and stop cases

None. The rest time comes from the API; the front only counts it down. No business rule, no form, no new error code,
no new dependency or native module (Expo Go is enough).

### Out of scope

Sound, vibration or a notification at the end; keeping the screen awake; starting the countdown automatically after
saving a set; keeping a countdown across exercises, screens or an app restart; editing the rest time.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `app-workout-exercise`: the rest card stops being display only. Its requirement now covers the remaining time, and
  new requirements cover play, pause, reset, the finished state, timing accuracy and when the countdown resets.

## Impact

- App: `presentation/workouts/hooks/use-rest-countdown.ts` (new), `presentation/workouts/set-rows.ts`,
  `presentation/workouts/components/molecules/rest-card.tsx`, `presentation/workouts/views/workout-exercise-view.tsx`,
  `i18n/{es,en}/workouts.json`; tests in `test/presentation/workouts/`.
- No change to `core`, the backoffice, the website, the API contract or dependencies.
