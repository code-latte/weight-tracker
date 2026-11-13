# Design

## Context

See proposal.md (Why). Facts the approach builds on:

- `WorkoutExerciseView` reads `id` and `exerciseId` with `useLocalSearchParams` and switches exercise with
  `router.setParams`, so the view stays mounted across exercises; only `exerciseId` changes.
- `useWorkoutExercise` exposes `exercise.restMinutes` as a `number | null` and `exercise` is `null` while the workout
  is loading. A save calls `reload()`, which re-reads the workout.
- The view renders `RestCard` only when `status === "ready"`, there is an exercise and the keyboard is hidden, so the
  card unmounts and remounts during normal use.
- `RestCard` is a molecule taking a single `label`. `restTimeOf(minutes)` in `set-rows.ts` formats the full rest as
  `M:SS` on top of core's `minutesToText`.
- Copy lives under `exercise.*` in `app/src/i18n/{es,en}/workouts.json`; `exercise.rest` is "Descanso: {{time}}".
- Jest fake timers are already used in the app tests (`workouts-view.test.tsx`); modern fake timers also fake
  `Date.now`.

## Goals / Non-Goals

**Goals:**

- A countdown whose shown value is always derived from the clock, robust to late ticks and app suspension.
- Keep `RestCard` presentational and the timer state in a presentation hook owned by the view.
- No change outside the app's `workouts` presentation and its copy.

**Non-Goals:**

- Background execution, notifications, sound, haptics or keep-awake.
- A shared or reusable timer for other screens: the hook lives in the `workouts` area until a second use appears.

## Decisions

### Core and wiring

Nothing. No domain alias, port, use case, repository, `Dependencies` key or `container.ts` registration. The
countdown is presentation state over a value the screen already reads.

### Mechanism: end timestamp, derived remaining time

- The hook holds a reducer-like state: `{ status: "idle" }`, `{ status: "running", endsAt }`,
  `{ status: "paused", remainingMs }` or `{ status: "finished" }`.
- `play`: from idle, `endsAt = Date.now() + totalSeconds × 1000`; from paused, `endsAt = Date.now() + remainingMs`.
  Ignored when finished.
- `pause`: `remainingMs = max(0, endsAt − Date.now())`.
- `reset`: back to idle.
- While running, a `setInterval` of 250 ms recomputes `remainingMs = max(0, endsAt − Date.now())` and stores
  `remainingSeconds = ceil(remainingMs / 1000)` only when it differs from the stored one, so the screen renders
  once per displayed second. At `0` the state becomes finished and the interval is cleared (effect cleanup).
- `ceil` makes the full value show right after play and `0:00` show exactly at the end.
- An `AppState` `change` listener recomputes immediately on `active`, so the first frame after returning from the
  background is right instead of waiting for the next tick.

Alternatives considered:

- _Decrement a counter every 1000 ms_: drifts when the JS thread is busy and freezes while the app is suspended,
  because `setInterval` only guarantees a minimum delay and JS timers do not run in the background. Rejected.
- _Store the start timestamp plus accumulated paused time_: equivalent, but needs two values to resume. Storing the
  end timestamp needs one. Rejected for simplicity.
- _Tick every 1000 ms_: an interval near the second boundary can skip or repeat a displayed second. 250 ms costs
  nothing visible because renders are limited to changes.
- _`performance.now()`_: monotonic, but its behaviour across app suspension differs by platform. `Date.now()` is
  chosen; a manual device clock change during a rest is an accepted edge case.

### Reset rules: keyed by exercise

- The hook takes `(exerciseKey: string, totalSeconds: number | null)`. The view passes the route's `exerciseId` and
  `restSecondsOf(exercise.restMinutes)` (or `null`).
- It returns to idle when `exerciseKey` changes, or when `totalSeconds` changes to a different non-null value for
  the same key (the trainer changed the rest and a reload brought it).
- A `null` total while the workout reloads after a save does not reset it, so the countdown keeps running across a
  save (spec: "Saving a set").
- The hook is called unconditionally in the view, above the conditional render of `RestCard`, so the card hiding for
  the keyboard or a reload never touches the state. Unmounting the view clears the interval and the listener.

### App: helpers

- `restSecondsOf(minutes: number | string): number` -> `Math.round(Number(minutes) × 60)`, the same rounding as
  `minutesToText`.
- `countdownTextOf(seconds: number): string` -> `M:SS` (`0` -> `0:00`, `90` -> `1:30`, `600` -> `10:00`).
- `restTimeOf` keeps its output; it can become `countdownTextOf(restSecondsOf(minutes))`, verified by its existing
  tests.

### App: components

- `RestCard` (molecule, `workouts/components/molecules/rest-card.tsx`) props become `label`, `finished?: boolean`
  and `controls?: { running: boolean; canPlay: boolean; canReset: boolean; onPlay; onPause; onReset;
playLabel; pauseLabel; resetLabel }`. Without `controls` it renders as today. With them it renders the existing
  icon and text plus two `IconButton` atoms: `play` / `pause` and `restore`, with the labels as accessibility labels.
  `finished` switches the icon to `timer-off-outline` and the text to the primary tone.
- Only the atoms import React Native Paper; `RestCard` imports `card`, `icon`, `icon-button`, `text` atoms. Imports
  flow downward.
- The view builds the label: `exercise.noRest`, `exercise.restFinished`, or `exercise.rest` with
  `countdownTextOf(remainingSeconds)`.

### State

- `useRestCountdown` -> status, `endsAt` or `remainingMs`, `remainingSeconds` -> the athlete (play / pause / reset)
  and the tick -> idle on exercise change or a new total; discarded on screen exit. Not persisted.

### Errors and copy

No error code is involved; nothing is sent. New keys under `exercise.*` in `workouts.json`:

| Key            | es                 | en            |
| -------------- | ------------------ | ------------- |
| `restFinished` | Descanso terminado | Rest finished |
| `restPlay`     | Iniciar descanso   | Start rest    |
| `restPause`    | Pausar descanso    | Pause rest    |
| `restReset`    | Reiniciar descanso | Reset rest    |

### Tests

- `set-rows.test.ts`: `restSecondsOf` and `countdownTextOf`; existing `restTimeOf` cases unchanged.
- `workout-exercise-view.test.tsx`: the spec scenarios with `jest.useFakeTimers({ now })` and `act` around
  `jest.advanceTimersByTime`. The late-tick scenario uses `jest.setSystemTime` to move the clock, then advances one
  tick. Background scenarios mock `AppState.addEventListener` to capture the handler, move the clock with
  `jest.setSystemTime` and call it with `"active"`. No app test covers the keyboard yet: the keyboard scenario mocks
  `@/presentation/common/hooks/use-keyboard-visible` with a `jest.fn` returning `true` then `false` across
  re-renders, local to this file. Real timers are restored after each test. No change to test helpers.

## Risks / Trade-offs

- [Interval keeps running while the app is backgrounded on Android] → it only recomputes a derived value and
  renders on change; cost is negligible and it stops at finish, pause, reset or exit.
- [Device clock changed during a rest] → the remaining time jumps; accepted, documented above.
- [Fake timers and `AppState` in tests] → the hook reads `Date.now()` and `AppState` directly so both can be faked
  without new seams or helpers.
- [No alert at the end] → the athlete must look at the screen; an alert is a later change (out of scope).
