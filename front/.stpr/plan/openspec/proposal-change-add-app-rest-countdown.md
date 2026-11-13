# Change Proposal: Add App Rest Countdown

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-app-rest-countdown/`.

## Change

Turn the app's static rest card on the exercise detail screen into a countdown the athlete controls: a play button
that starts it, a pause button that freezes it, and a reset button that stops it and restores the exercise's rest
time.

## Objective

The exercise detail screen shows the rest between sets ("Descanso: 1:30") but the athlete has to time it with another
app. The previous change left the countdown explicitly out of scope. This change makes the rest card the timer, so
the athlete can rest and record sets from the same screen.

## Capabilities

- Countdown display: the card shows the remaining time as `m:ss`, starting from the exercise's `restMinutes`
  (minutes, possibly fractional, e.g. `1.5` -> `1:30`). Idle, it reads as today ("Descanso: 1:30").
- Play: starts the countdown, or resumes it from where it was paused. The remaining time drops by one second every
  second. While running, the play button becomes the pause button.
- Pause: freezes the remaining time. Play resumes from that value.
- Reset: stops the countdown (running or paused) and restores the full rest time. Disabled while the countdown is idle
  at the full value.
- Finish: when the remaining time reaches `0:00` the countdown stops at `0:00` and the card shows a finished state
  (same card, primary tone and a "Descanso terminado" label). Play is disabled until reset; reset restores the full
  value.
- Accuracy (decided): the remaining time is derived from timestamps, never from counting ticks (see Notes). It stays
  correct when the JS thread is busy, when ticks are delayed or dropped, and when the app goes to the background and
  comes back while running.
- No rest: when `restMinutes` is `null` the card reads "Sin descanso" as today, with no buttons.

## Fronts And Roles

- app -> lands here: yes -> roles: `USER` -> the athlete starts, pauses and resets the rest countdown on the exercise
  detail screen. Available in every workout status: the timer records nothing and sends no request.
- backoffice -> lands here: no.

## Classification

App (`workouts` domain area):

- `useRestCountdown(totalSeconds)` (holds `status: "idle" | "running" | "paused" | "finished"`, the end timestamp
  while running and the remaining milliseconds while paused; exposes `remainingSeconds`, `status`, `play`, `pause`,
  `reset`) -> state hook -> `app/src/presentation/workouts/hooks/use-rest-countdown.ts`
- `countdownTextOf(seconds)` (`90` -> `1:30`) and `restSecondsOf(minutes)` (`1.5` -> `90`) -> pure helpers ->
  `app/src/presentation/workouts/set-rows.ts`, next to `restTimeOf`
- `RestCard` -> molecule (existing) -> `app/src/presentation/workouts/components/molecules/rest-card.tsx`; gains the
  controls: `label`, `status`, `onPlay`, `onPause`, `onReset` (controls omitted when there is no rest). It stays
  presentational and holds no timer state.
- `WorkoutExerciseView` -> view (existing) -> calls `useRestCountdown` and passes its state to `RestCard`.
- Atoms reused: `icon-button` (play / pause / reset, icons `play`, `pause`, `restore`), `icon`, `text`, `card`. No new
  atom.
- Copy -> new keys under `exercise.*` in `app/src/i18n/{es,en}/workouts.json`.

## Canonical Template Mapping

- `useRestCountdown` -> `app/src/presentation/common/hooks/use-keyboard-visible.ts` -> the app's nearest hook that
  subscribes to a platform source and cleans up on unmount. It has no use case or container: the timer is pure
  presentation state.
- `countdownTextOf`, `restSecondsOf` -> `restTimeOf` in `app/src/presentation/workouts/set-rows.ts` -> same
  formatting family; `restTimeOf` may be reimplemented on top of them, keeping its output unchanged.
- `RestCard` controls -> `app/src/presentation/workouts/components/molecules/workout-hero-panel.tsx` -> the area's
  molecule that already takes an action prop with label, handler and disabled state.
- View test -> `app/test/presentation/workouts/views/workout-exercise-view.test.tsx` (existing) -> extended;
  fake timers as in `app/test/presentation/workouts/views/workouts-view.test.tsx`.
- Helper tests -> `app/test/presentation/workouts/set-rows.test.ts` (existing) -> extended.
- Atom/molecule/organism references are `pending` in `.stpr/template/README.md`; the app shapes above apply.

## Screens And Navigation

- app -> `/workouts/[id]/exercises/[exerciseId]` -> `WorkoutExerciseView` -> unchanged entry. The countdown never
  navigates. Switching exercise through the navigator or leaving the screen discards the countdown (see Held State).

## API Contract

None. Uses `restMinutes` from `GET /workouts/{id}` (`WorkoutByIdResponse`), already read by `useWorkoutExercise`.

## Held State

- `useRestCountdown` -> status, end timestamp (running) or remaining ms (paused) -> the athlete's play / pause / reset
  and the tick -> resets to idle with the full value when the exercise changes (its `totalSeconds` changes or the
  route's `exerciseId` changes) and on screen exit. Not persisted.
- The hook lives in the view, not in `RestCard`: the card unmounts while the keyboard is visible (the rest band gives
  way to the keyboard), and the countdown must keep running and show the right value when it comes back.
- A reload after saving a set does not reset the countdown when `restMinutes` is unchanged.

## Forms And Structural Validation

None.

## Errors And Copy

No failure path: the timer sends no request. No new error code.

New keys, `workouts` namespace, `exercise.*` (es -> en):

- `rest` -> unchanged ("Descanso: {{time}}"), now fed the remaining time.
- `restFinished` -> "Descanso terminado" -> "Rest finished"
- `restPlay` -> "Iniciar descanso" -> "Start rest" (accessible label)
- `restPause` -> "Pausar descanso" -> "Pause rest" (accessible label)
- `restReset` -> "Reiniciar descanso" -> "Reset rest" (accessible label)

## Platform Capabilities And Dependencies

- Return from background -> no port: React Native's `AppState` (`change` to `active`) triggers an immediate
  recomputation so the value is right on the first frame back. Used inside the presentation hook only.
- No package. Expo Go is enough.

## Out Of Scope

- Sound, vibration, haptics or a local notification when the rest ends (would need `expo-haptics` /
  `expo-notifications` and, for notifications, a platform port; a later change).
- Keeping the screen awake while counting (`expo-keep-awake`).
- Starting the countdown automatically after saving a set.
- Keeping a running countdown across exercises or screens, or after the app is killed.
- Editing the rest time from the app.
- Must not change: the API, `core`, the set rows, the navigator, the backoffice.

## Notes

- **Countdown mechanism (decided).** The common, robust pattern is timestamp-based: the tick only triggers a
  recomputation; the value comes from the clock.
  - `play` stores `endsAt = now() + remainingMs` (from idle, `remainingMs = totalSeconds × 1000`).
  - Every tick computes `remainingMs = max(0, endsAt - now())` and shows `ceil(remainingMs / 1000)` seconds, so it
    shows the full value right after play and reaches `0:00` exactly at the end.
  - `pause` stores `remainingMs = endsAt - now()` and drops `endsAt`; `reset` drops both.
  - Reaching `0` sets `finished` and clears the interval.
  - This is the same idea as storing the start timestamp; storing the end timestamp avoids accumulating paused time.
- **Why not decrement a counter.** `setInterval(fn, 1000)` is a minimum delay, not a guarantee: a busy JS thread
  (renders, list layouts, the keyboard) delays ticks and they drift, and in the background JS timers are suspended on
  iOS and throttled on Android, so a counter would freeze. Deriving from timestamps makes a late or missed tick only
  delay the repaint, never corrupt the value.
- **Tick rate.** Tick every 250 ms rather than 1000 ms: an interval aligned to whole seconds can skip or repeat a
  displayed second when it fires just before or after the boundary. The hook only updates state when the displayed
  second changes, so the screen still renders once per second.
- **Clock.** Use `Date.now()`. `performance.now()` is monotonic but its behaviour across app suspension differs by
  platform; a manual change of the device clock during a rest is an accepted edge case.
- **Background.** No background task: when the app returns to `active`, the next recomputation (forced by the
  `AppState` listener) yields the right remaining time, or `finished` if it elapsed meanwhile.
- **No stop case.** The countdown is presentation state with no business rule: the rest value comes from the API and
  the front only counts it down.
- **Verification tiers:**
  - app component: `workout-exercise-view.test.tsx` gains, with Jest fake timers (`jest.useFakeTimers({ now })`,
    which also fakes `Date.now`): idle shows the full rest and play; play then advancing 3 s shows 3 s less and the
    pause button; pause freezes the value across further time; play resumes from it; reset restores the full value
    from running and from paused; reaching zero shows `0:00` and "Descanso terminado" with play disabled; a delayed
    tick (advance the clock without running timers, then one tick) still shows the right value; the countdown
    survives the keyboard hiding and showing the card; switching exercise resets it; no buttons with "Sin descanso".
  - `set-rows.test.ts`: `countdownTextOf` (`0`, `9`, `60`, `90`, `600`) and `restSecondsOf` (whole, fractional,
    string input); `restTimeOf` output unchanged.
  - Translation parity for the new keys. No E2E (app E2E is a known gap). Manual check in Expo Go, including
    backgrounding the app mid-rest.
- No deviation from the mapped references is allowed.
