# Design

## Context

- The configuration dialog (`backoffice/src/presentation/workouts/components/organisms/exercise-configuration-dialog.tsx`)
  holds the effort, the listed sets (`SetFormValues[]`, every field a string) and a set draft.
  - `toFormValues` turns the API's sets into form values.
  - `toPrescription` turns them back into numbers, guided by `targetFieldsOf`.
  - `TARGET_TYPES` lists the four current types.
- Structural rules live in `core/src/application/validation/workout-rules.ts`: `validateSetForm` (positive whole
  numbers) and `validateExerciseConfiguration` (effort and at least one set).
- `useExerciseLabels` builds `setSummary` / `compactSetSummary` from `workout.configuration.summary.<type>` and
  `workout.exercises.compactSummary.<type>`, with an `other` fallback chosen through `SUMMARY_TYPES`.
- `WorkoutExercisesList.linesOf` builds the sets and effort lines of each card.
- The add and update use cases and `WorkoutRepository` pass the request through unchanged. They take their types from
  the aliases in `core/src/domain/workouts/workout.ts`, which point at the generated schemas. Once regenerated, they
  carry the new fields with no code change.
- A `Switch` atom already exists (`backoffice/src/presentation/common/components/atoms/switch.tsx`), labelled through
  `aria-labelledby` as in `users/components/molecules/inactive-filter.tsx`.

## Goals / Non-Goals

**Goals:**

- One conversion between `M` / `M:SS` and decimal minutes, in `core`, used for both the minutes target and the rest.
- The dialog's form state stays string-only, as today. Numbers appear only in `toPrescription` and the save body.

**Non-Goals:**

- New use cases, repository methods, ports, adapters or container keys. `Dependencies` and both `container.ts` files
  are unchanged.
- Any change in `app/` beyond compiling against the regenerated types.

## Decisions

### Snapshot first, as its own commit

- Steps:
  - run `pnpm --filter @weight-tracker/core openapi:refresh`, then `openapi:generate`, with the API change running;
  - review the diff;
  - commit it alone, following the archived change `show-exercise-type`.
- Expected diff:
  - `perSide` and `restMinutes` on `AddOrUpdateExerciseRequest`, `WorkoutExerciseResponse` and
    `WorkoutExerciseDetailResponse`;
  - `targetMinutes` on `SetPrescriptionRequest`;
  - `minutes` on `SetTargetResponse`.
- Anything else stops the change.
- Decimals may be typed `number | string` by the generator, as integers already are. The front always writes
  `number`. When reading, the formatter accepts both and applies `Number(...)`.

### `time-in-minutes.ts` in `core/src/application/validation`

- It follows `calendar-date.ts`: pure, no dependencies.
- It exports three functions and the maximum:
  - `isMinutesText(text): boolean`: trims, then matches `^(\d+)(?::([0-5]\d))?$`. The value must be greater than zero
    and at most `MINUTES_MAX_TEXT_SECONDS` (999 × 60 + 59).
  - `minutesFromText(text): number`: `Math.round(((M × 60 + SS) / 60) × 100) / 100`. Called only after
    `isMinutesText`.
  - `minutesToText(minutes: number | string): string`:
    - `seconds = Math.round(Number(minutes) × 60)`;
    - return `${Math.floor(seconds / 60)}` when `seconds % 60 === 0`;
    - otherwise return `${M}:${String(SS).padStart(2, "0")}`.
- It is exported from `validation/index.ts`, unlike `calendar-date.ts`, because the backoffice formats and converts with
  it.
- Alternatives considered:
  - **Keep the conversion in the backoffice.** Rejected: the app will need the same format to show these values, and
    `core` is where shared, framework-free logic lives.
  - **Reject seconds that are not multiples of 3**, so the stored value is exact. Rejected: trainers type any second.
    Rounding to the nearest second when formatting gives the typed value back for every second (the error is at most
    0.3 s).
  - **Accept a decimal input such as `1.5`.** Rejected: the brief keeps a single format, and `1.5` versus `1:50` would
    confuse trainers.

### Rules in `workout-rules.ts`

- `SetFieldViolation` gains `"minutesFormat"`.
- `targetFieldsOf` stays as is: `Minutes` needs `value` and no `maxValue`, which is the current default.
- `validateSetForm`: when `type === "Minutes"`, `value` is checked with `isMinutesText` and reports `minutesFormat`.
  Every other type keeps `positiveInteger`.
- `validateExerciseConfiguration` gains an optional `rest: string` argument. A non-empty rest that fails
  `isMinutesText` reports `rest: "minutesFormat"`. The type is `ExerciseConfigurationViolations.rest?: "minutesFormat"`.
- These remain structural format checks. The API keeps every business limit and answers 400 (`VALIDATION`) otherwise.

### Dialog state and conversion

- New state: `rest: string`, initialised with `item.workoutExercise.restMinutes == null ? "" : minutesToText(...)`, and
  `perSide: boolean`, initialised with `item.workoutExercise.perSide ?? false`. Both reset on close, because the caller
  keys the dialog by item, as it does for effort and sets.
- `toFormValues`: for `target.type === "Minutes"`, `value = minutesToText(target.minutes)`.
- `toPrescription`: for `Minutes`, it sets `targetMinutes = minutesFromText(set.value)` instead of `target`.
- `handleSave` passes `rest` to `validateExerciseConfiguration`. It adds `perSide` to the body, and adds
  `restMinutes: minutesFromText(rest)` only when `rest.trim() !== ""`.
- The rest `TextField` (atom) goes right below the effort. Its label is "Descanso entre series" and its helper text the
  hint. The error message replaces the hint while invalid. It uses `type="text"` with `inputMode="numeric"` so `:` can
  be typed.
- The minutes value field is the existing Valor `TextField`. For `Minutes`, it switches to `type="text"`, shows the
  "m:ss" hint and the `minutesFormat` message.
- The "Por lado" `Switch` (atom) and its `Typography` label are the last cell of the set form's grid, so they fill the
  free cell beside Valor (under Tipo). They are labelled with `aria-labelledby`.
  It is disabled while saving, like the other inputs.
- Components and levels:
  - `ExerciseConfigurationDialog` and `WorkoutExercisesList` stay organisms in
    `backoffice/src/presentation/workouts/components/organisms`;
  - they import only atoms (`TextField`, `Switch`, `Typography`, `MenuItem`) and existing molecules;
  - no new atom or molecule;
  - imports flow downward.

### Summaries

- `TARGET_TYPES` and `SUMMARY_TYPES` gain `"Minutes"`.
- New keys `summary.Minutes` = "{{series}} × {{value}} min" and `compactSummary.Minutes` = "{{series}} × {{value}} min".
- The caller passes `value` already formatted:
  - the dialog passes the typed text;
  - the list passes `minutesToText(target.minutes)`.
- So `setSummary` stays format-agnostic.
- `SetSummaryValues` gains an optional `perSide?: boolean`. When true, `setSummary` appends `workout.configuration.perSideSuffix`
  (" por lado").
- In `WorkoutExercisesList.linesOf`:
  - the sets line ends with `workout.exercises.perSideSuffix` (" · por lado") when `perSide`;
  - a third line `workout.exercises.restLine` ("Descanso: {{rest}} min") is added when `restMinutes != null`.
- The labels hook stays the single place that turns codes into text.

### Errors and translations

- No new error code. A 400 maps to the existing `VALIDATION` in `http-error-mapper.ts`, already translated in `errors`
  ("Revisa los datos introducidos." / its `en`). The mapper is not touched.
- New `workouts` keys, `es` / `en`:
  - `targetTypes.Minutes`: "Minutos" / "Minutes"
  - `workout.configuration.rest`: "Descanso entre series" / "Rest between sets"
  - `workout.configuration.restHint`: "Minutos o m:ss, p. ej. 1:30" / "Minutes or m:ss, e.g. 1:30"
  - `workout.configuration.minutesHint`: "m:ss" / "m:ss"
  - `workout.configuration.perSide`: "Por lado" / "Per side"
  - `workout.configuration.perSideSuffix`: " por lado" / " per side"
  - `workout.configuration.validation.minutesFormat`: "Usa minutos (2) o minutos:segundos (1:30)." / "Use minutes (2)
    or minutes:seconds (1:30)."
  - `workout.configuration.summary.Minutes` and `workout.exercises.compactSummary.Minutes`: "{{series}} × {{value}} min"
    (same in `en`)
  - `workout.exercises.perSideSuffix`: " · por lado" / " · per side"
  - `workout.exercises.restLine`: "Descanso: {{rest}} min" / "Rest: {{rest}} min"
- The parity test covers both languages.

### Held state

| Holder                        | Holds                      | Changed by                        | Resets                         |
| ----------------------------- | -------------------------- | --------------------------------- | ------------------------------ |
| `ExerciseConfigurationDialog` | `rest` (text), `perSide`   | the rest field and the switch     | on close (keyed by item)       |
| `ExerciseConfigurationDialog` | set draft `value` (text)   | the Valor field, also for minutes | after "Añadir serie", on close |
| `WorkoutView`                 | workout exercises (as now) | `onSaved` with the API's answer   | screen exit / refresh          |

No provider or global state is added.

### Tests

The tests follow the existing files, with no change to helpers or patterns.

- **Core unit:**
  - new `core/test/application/validation/time-in-minutes.test.ts`, modelled on `workout-rules.test.ts`: accepted and
    rejected texts, conversions, formatting, and a round trip from `0:01` to `2:00`;
  - `workout-rules.test.ts`: the `Minutes` branch and rest;
  - the add and update use case tests and `workout-repository.test.ts`: fixtures gain `perSide` / `restMinutes` and a
    minutes set, checked to pass through unchanged.
- **Backoffice component:** `workout-view.test.tsx`, with use cases faked through the container, covering the spec
  scenarios.
- **Backoffice E2E:** `workout.spec.ts`, with fixtures gaining `perSide: false, restMinutes: null`. One flow saves a
  `1:30` minutes set, `2` minutes of rest and per side, and checks the intercepted body and the updated card.

## Risks / Trade-offs

- [`1:20` is stored as `1.33`, not exactly 80 s] → Formatting rounds to the nearest second, so the backoffice always
  shows what was typed. A future reader of the decimal (the app) must use `minutesToText`, which is why it lives in
  `core`.
- [The generator types decimals as `number | string`] → `minutesToText` accepts both. The front never sends a string.
- [Snapshot refreshed against an API that is still uncommitted] → The change starts only once the API change is
  running. A diff beyond the expected fields stops the change.
- [Per side on the list line is easy to miss] → It is an accepted trade-off for a compact card. The dialog repeats it
  per set.

## Migration Plan

- No data migration on the front.
- Deploy after the API change. An older backoffice keeps working against the new API: the new fields are optional on
  requests and ignored when read. Saving an exercise from an older backoffice omits them, so the API clears that
  exercise's rest and per side. This is acceptable for the short window of a deploy.
- Rollback is reverting this change. Existing stored values stay in the API.
