# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-minutes-target-per-side-and-rest.md`.

## Why

A trainer can prescribe reps, a reps range, max reps or a duration in seconds. There is no way to write a target in
minutes (cardio, long holds), the rest between sets, or that a target is per side (a side plank, 2 × 8 on each side).
These end up in the workout description or are left out. The API change `add-minutes-target-per-side-and-rest` now
stores all three, so the backoffice can let the trainer configure them on the exercise itself, in the `1:30` format
trainers already use.

## What Changes

- **Contract snapshot refresh.** `core/openapi/v1.json` and `core/src/contract/generated` gain the API's new fields:
  - `perSide` and `restMinutes` on the add or update exercise request and on both workout exercise responses;
  - the `Minutes` target type, with `targetMinutes` on the set prescription request and `minutes` on the set target
    response.
- **Minutes format helpers in `core`.** New pure functions parse `M` / `M:SS` into a decimal of minutes rounded to 2
  decimals, format a decimal back as `M` / `M:SS`, and check the format structurally.
- **Set form rules.** `Minutes` is a target type whose value uses the minutes format instead of a positive whole number.
- **Exercise configuration rules.** An optional rest, in the minutes format.
- **Backoffice configuration dialog** (`ExerciseConfigurationDialog`, organism):
  - "Minutos" in the target type selector;
  - a minutes value field accepting `2` or `1:30`;
  - an optional "Descanso entre series" field;
  - a "Por lado" switch, reusing the existing `Switch` atom.
  - Each opening loads the API's decimals as `M` / `M:SS`, and Guardar sends them back as decimals.
- **Summaries.** Minutes sets read "3 × 1:30 min". Per side adds "por lado". The workout list card shows
  "Descanso: 1:30 min" when there is rest.
- **Copy** in `backoffice/src/i18n/{es,en}/workouts.json`.

No breaking changes. Use cases, repositories, containers and the HTTP error mapper are unchanged.

## Capabilities

### New Capabilities

- `minutes-format`: the `M` / `M:SS` notation for times in minutes. Covers parsing, formatting and structural
  validation, and rounding to the API's 2 decimals, so that every typed value converts back to itself.

### Modified Capabilities

- `backoffice-workout-editor`:
  - the set form and set list gain the Minutes target;
  - the configuration dialog gains rest and per side, and Guardar sends them;
  - the workout list shows minutes, per side and rest.
- `workout-management`: adding and updating a workout exercise send `perSide` and `restMinutes` with the prescription,
  unchanged.

## Fronts, Roles And References

- backoffice -> lands here: yes -> `ADMIN` and `TRAINER` configure and see the three settings the same way. No
  role-dependent UI and nothing hidden per role.
- app -> lands here: no -> `USER` sees no change. The app keeps compiling with the regenerated types.
- Canonical references:
  - snapshot refresh -> archived change `show-exercise-type`;
  - minutes helpers -> `core/src/application/validation/calendar-date.ts`;
  - set and exercise rules -> `validateSetForm` / `validateExerciseConfiguration` in `workout-rules.ts`;
  - rest field and switch -> the effort field of `exercise-configuration-dialog.tsx`;
  - summaries -> `setSummary` / `compactSetSummary` in `use-exercise-labels.ts`.
  - The atomic UI references in `.stpr/template/README.md` are `pending`. No new atom or molecule is added.

## Constraints

- The 2-decimal rounding is a format conversion, not a business rule. The API stays the authority on limits and
  rejects invalid values with a 400, mapped to the existing `VALIDATION`.
- No hand-written domain type. No edit to `core/src/contract/generated`.
- Blocked until the API change is running, so the snapshot can be refreshed. If the refreshed diff holds more than the
  fields above, the change stops.

## Impact

- `core`: contract snapshot, `application/validation` (new `time-in-minutes.ts`, `workout-rules.ts`), their tests.
- `backoffice`:
  - `exercise-configuration-dialog.tsx`, `workout-exercises-list.tsx`, `use-exercise-labels.ts` and the
    `workouts.json` translations;
  - `test/presentation/workouts/views/workout-view.test.tsx` and `e2e/workout.spec.ts`.
- `app`: none beyond regenerated types.
- API: depends on `api` change `add-minutes-target-per-side-and-rest`.
