# Change Proposal: Minutes Target, Rest And Per Side In The Exercise Configuration

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-minutes-target-per-side-and-rest/`.

## Change

The workout exercise configuration dialog in the backoffice gains three settings, which the API change
`add-minutes-target-per-side-and-rest` already supports:

- a **Minutes** set target type ("Minutos"), typed as whole minutes (`2`) or minutes and seconds (`1:30`);
- a **rest** between sets for the exercise, typed in the same format;
- a **per side** switch for the exercise ("2 × 8 por lado" for a side plank).

## Objective

Today a trainer can only prescribe reps, a reps range, max reps or a duration in seconds. Cardio and long holds are
naturally written in minutes, and the rest between sets and unilateral work cannot be written at all. Trainers end
up putting this in the workout description or leaving it out. These settings let the trainer write them on the
exercise itself, in the format trainers already use (`1:30`).

## Capabilities

- **Minutes target**
  - "Minutos" is a new option in the set form's target type selector, after the current four.
  - Its value field accepts `M` or `M:SS`, such as `2`, `1:30` or `0:45`. Other target types keep their whole-number
    field: the `M:SS` format applies only to minutes.
  - The set is listed and summarised as, for example, "3 × 1:30 min" or "1 × 20 min".
- **Rest**
  - A new optional "Descanso entre series" field at exercise level, beside effort, in the same `M` / `M:SS` format,
    with a "min" hint.
  - Empty means no rest and is sent as no value.
- **Per side**
  - A new "Por lado" switch at exercise level, off by default.
  - When on, the targets of every set are per side. The dialog's set list and the workout list summaries add "por
    lado", such as "2 × 8 repeticiones por lado".
- **Conversion between the form and the API.** The API stores minutes as a decimal with at most 2 decimals.
  - Form to API: `M:SS` becomes `M + SS / 60`, rounded to 2 decimals. `1:30` becomes `1.5`, `2` becomes `2`, `0:45`
    becomes `0.75`, `1:20` becomes `1.33`.
  - API to form: the decimal becomes a whole number of seconds (`value × 60`, rounded). It shows as `M` when the
    seconds part is 0, else as `M:SS` with two-digit seconds. `1` shows `1`, `1.5` shows `1:30`, `1.33` shows `1:20`.
  - Every `M:SS` with whole seconds converts back to itself, so editing and saving without changes keeps the value.
  - The same conversion applies to the minutes target and to rest.
- **Workout list.** The card of each exercise shows the minutes target, "por lado", and the rest when there is one,
  such as "Descanso: 1:30 min".

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both configure and see the three settings the same way.
- app -> lands here: no -> roles: `USER` -> no UI change. The app keeps compiling with the regenerated types.

No role-dependent UI.

## Classification

- Contract snapshot -> `core/openapi/v1.json` and `core/src/contract/generated` (refreshed and regenerated, never
  hand-edited).
- `WorkoutExerciseDetail`, `SetPrescription` and the add or update request -> entity types (existing aliases in
  `core/src/domain/workouts`) -> they gain `perSide`, `restMinutes`, `targetMinutes` and `target.minutes` from the
  regenerated schemas.
- Minutes format: parse `M` / `M:SS` to a decimal, format a decimal as `M` / `M:SS`, and check the format -> pure
  functions in `core/src/application/validation`, in a new `time-in-minutes.ts` beside `calendar-date.ts`, exported
  like the other validation helpers.
- `validateSetForm` and `targetFieldsOf` -> `core/src/application/validation/workout-rules.ts` (extended): `Minutes`
  needs a value in minutes format instead of a positive whole number.
- `validateExerciseConfiguration` -> same file (extended): optional rest in minutes format.
- `useExerciseLabels` -> state hook (extended) -> `targetTypes.Minutes`, `setSummary` and `compactSetSummary` aware of
  minutes and per side, and a rest label.
- `ExerciseConfigurationDialog` -> organism (extended) -> "Minutos" option, minutes value field, rest field, per side
  switch, and the conversion when loading and saving.
- `WorkoutExercisesList` -> organism (extended) -> per side and rest in each card.
- `useSaveWorkoutExercise`, the save use cases and the workout repository -> unchanged, unless the regenerated request
  type forces a type-only adjustment.
- Copy -> `backoffice/src/i18n/{es,en}/workouts.json`.

## Canonical Template Mapping

- Snapshot refresh -> the earlier refresh in archived change `show-exercise-type` -> the same steps: refresh, generate,
  and commit the snapshot on its own.
- `time-in-minutes.ts` -> `core/src/application/validation/calendar-date.ts` -> a pure, framework-free format helper used
  by the validation rules.
- Set form rules -> `validateSetForm` / `targetFieldsOf` in `workout-rules.ts` -> same shape, one more target type.
- Rest field and per side switch -> the effort field in `exercise-configuration-dialog.tsx` -> an exercise-level field
  in the same dialog. The switch reuses the existing atoms, or adds a `Switch` atom if none exists.
- Summaries -> `setSummary` / `compactSetSummary` in `use-exercise-labels.ts` -> the same lookup per target type.
- Tests:
  - `core/test/application/validation/` -> the existing `workout-rules` tests -> parse, format and round trip;
  - `backoffice/test/presentation/workouts/views/workout-view.test.tsx` (extended);
  - `backoffice/e2e/workout.spec.ts` (extended).

`.stpr/template/README.md` lists the atomic UI references as `pending`. If a `Switch` atom is added, it follows the
existing atoms in `backoffice/src/presentation/common/components/atoms`.

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> unchanged entry points (configure a listed exercise, or add one
  from the catalog) -> on save the dialog closes and the list shows the new values. On failure the dialog stays open
  with the error, as today.

## API Contract

- `GET` workout detail -> in `core/openapi/v1.json`: **no** -> `WorkoutExerciseResponse` and
  `WorkoutExerciseDetailResponse` gain `perSide: boolean` and `restMinutes: number | null`. `SetTargetResponse` gains
  `minutes` (only for `type: "Minutes"`).
- Add and update workout exercise (the endpoints `useSaveWorkoutExercise` calls) -> in the snapshot: **no** ->
  `AddOrUpdateExerciseRequest` gains `perSide?: boolean` and `restMinutes?: number`. `SetPrescriptionRequest.targetType`
  accepts `"Minutes"` and gains `targetMinutes?: number`.

The API provides these fields through its change `add-minutes-target-per-side-and-rest` (`api/openspec/changes/`). That
change must be finished and running before this one starts. This change **refreshes the snapshot**:

1. With the API running (Debug API, port 5199), run `pnpm --filter @weight-tracker/core openapi:refresh`, then
   `openapi:generate`.
2. Review the diff. The expected diff is the fields above. Anything else stops the change.
3. Commit them as a step of their own, before the code that uses them.

The generator may type the decimals as `number | string`, as it does for the integers. The front always sends numbers.
No hand-written domain type.

## Held State

- `ExerciseConfigurationDialog` -> `rest` (as typed) and `perSide` -> the trainer, in the dialog -> on close. Each
  opening starts from the stored values, like effort and sets.
- The set draft keeps the minutes value as typed, in its existing `value` field.

## Forms And Structural Validation

- Set form -> value, when the type is `Minutes` -> required. Format `M` or `M:SS`: whole minutes, then optionally `:` and
  two-digit seconds from `00` to `59`. Greater than 0 (`0` and `0:00` are rejected). At most `999:59`, the API's
  `999.99` maximum.
- Exercise form -> rest -> optional. When filled, same format and limits as above. Surrounding spaces are trimmed.
- Exercise form -> per side -> boolean. No validation.
- Other target types keep their positive whole number rules.

The 2-decimal rounding is a format conversion, not a business rule. The API stays the authority on the limits.

## Errors And Copy

- No new error code. An API rejection (`InvalidTimeInMinutesException` is a 400) maps to the existing `VALIDATION` and
  shows the dialog's existing error message. The HTTP error mapper is not touched.
- New copy (`workouts` namespace):
  - `targetTypes.Minutes` -> "Minutos" -> "Minutes"
  - `workout.configuration.rest` -> "Descanso entre series" -> "Rest between sets"
  - `workout.configuration.restHint` -> "Minutos o m:ss, p. ej. 1:30" -> "Minutes or m:ss, e.g. 1:30"
  - `workout.configuration.perSide` -> "Por lado" -> "Per side"
  - `workout.configuration.validation.minutesFormat` -> "Usa minutos (2) o minutos:segundos (1:30)" -> "Use minutes (2)
    or minutes:seconds (1:30)"
  - minutes summary -> "{{count}} × {{minutes}} min" -> same in en
  - per side suffix -> "por lado" -> "per side"
  - rest line in the workout list -> "Descanso: {{rest}} min" -> "Rest: {{rest}} min"
- The exact key names follow the existing layout of `workouts.json` and are settled in the design.

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- The app: showing minutes, rest or per side to the athlete is a later change.
- `M:SS` input for the `Duration` (seconds) target or for reps. They keep whole numbers.
- Hours, or any format other than `M` and `M:SS`.
- A rest timer, or rest between superseries or between exercises.
- Any change to superseries, effort or reordering.

## Notes

- **Rounding is lossy in storage but reversible in the form.** Seconds that are not a multiple of 3 cannot be stored
  exactly in 2 decimals: `1:20` is stored as `1.33`. Rounding back to the nearest second always gives the typed value,
  since the error is at most 0.3 s. A value written to the API by another client with more precision cannot exist,
  since the API rejects it.
- Formatting from the API: `seconds = round(value × 60)`, `M = floor(seconds / 60)`, `SS = seconds % 60`. Parsing
  uses `round((M × 60 + SS) / 60, 2)`. Use the same pure functions in both directions, and do not depend on the locale.
  The form always uses `:`, never a decimal comma.
- The decimal is never shown to the trainer. The list and the dialog always show `M` or `M:SS`.
- Placement in the dialog: rest and per side sit with effort, as exercise-level settings, above the set form. Confirm
  before proposing if they should go elsewhere.
- Verification tiers:
  - core unit: parse and format (`2`, `1:30`, `0:45`, `1:20` ↔ `1.33`, `999:59`), rejected inputs (`0`, `0:00`,
    `1:60`, `1:5`, `1.5`, `-1`, `abc`, empty when required), round trip for every second from `0:01` to `2:00`, and
    the `Minutes` branch of `validateSetForm` plus rest in `validateExerciseConfiguration`;
  - backoffice component (`workout-view.test.tsx`, use cases faked through the container):
    - adding a minutes set with `1:30` sends `targetMinutes: 1.5`;
    - an exercise loaded with `restMinutes: 1.5` and a `minutes: 2` target shows `1:30` and `2`;
    - rest empty sends no `restMinutes`, and `perSide` is sent as set;
    - invalid formats show the validation message and send nothing;
    - summaries with "por lado" and the rest line in the list;
    - English labels;
  - backoffice E2E (`workout.spec.ts`, API intercepted with `page.route`): fixtures gain `perSide` and `restMinutes`.
    One flow configures an exercise with a `1:30` minutes set, `2` minutes of rest and per side on, and checks the
    intercepted request body and the updated card.
- No deviations from the mapped references are allowed, apart from adding a `Switch` atom if none exists.
