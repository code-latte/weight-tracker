# Change Proposal: Show Exercise Last Performance In The App

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/show-exercise-last-performance/`.

## Change

Refresh the OpenAPI snapshot to pick up `GET /users/me/exercises/{exerciseId}/last-performance` (API change
`add-exercise-last-performance-endpoint`), wire it as a `core` use case, and add a "Marcas anteriores" link under the video
link of the app's exercise detail screen that opens a "Última sesión" dialog with the date, the effort and the sets of
the athlete's last session of that exercise.

## Objective

When the athlete records a set they have no reference of what they lifted last time, so they guess the weight or keep
notes elsewhere. The API now answers that question for the caller. This change brings it to the screen where the sets
are recorded, one tap away.

## Capabilities

- Contract sync: `ExerciseLastPerformanceResponse` (`workoutDate`, `effort`, `sets[]`) and
  `LastPerformanceSetResponse` (`reps?`, `durationSeconds?`, `weight?`) enter the snapshot and the generated types.
- Core: `GetMyExerciseLastPerformanceUseCase` returns `Result<ExerciseLastPerformance | null, DomainError>`; `null`
  when the API answers `204` (the athlete never finished a workout with that exercise).
- App, exercise detail screen: between the exercise navigator and the list of sets, a link with an icon (history) and the
  text "Marcas anteriores".
  It is shown for every exercise, with or without video.
- App, exercise detail screen: the last performance of the catalog exercise is read when the exercise is shown. The
  link is shown only when at least one set of that session has a weight (hidden while reading, on a failed read, with
  no session, or when no set has a weight). A skeleton replaces the link and the sets while it is read.
- App, "Última sesión" dialog: opens on the link with what was already read, and shows:
  - a small calendar icon and the date of that session, short, in the app language ("11 nov 2026"), then a divider;
  - a row of two cards: "Series realizadas" with the number of sets, big, and "Esfuerzo" with the effort in the scale of
    the exercise's type (`STRENGTH` -> RIR, `CARDIO` -> RPE, `STRETCHING` -> descriptive), big, the generic label for an
    unknown type; that card is left out when the response has no effort;
  - the title "Series registradas" and one card laid out as a table, a separator between rows. Each row has the set
    number on the left, reps ("10 reps") or duration ("45 s") centred, and the weight ("20 kg") on the right, or "sin
    peso" when `weight` is `null` (exercise done without weight; `0` is shown as "0 kg", not as "sin peso").
  - a close button.

## Fronts And Roles

- app -> lands here: yes -> roles: `USER` -> the athlete opens the dialog from the exercise detail screen and sees
  their own last session. The endpoint is always about the caller (`/me`); there is no role branch.
- backoffice -> lands here: no -> no screen, copy or behaviour change (see Notes on the effort labels).

## Classification

Core (`users` domain area, since the route is under `/users/me`):

- `ExerciseLastPerformance`, `LastPerformanceSet` -> entity type -> aliases of the generated types in
  `core/src/domain/users/`
- `UserRepositoryInterface.getMyExerciseLastPerformance(exerciseId)` -> repository contract ->
  `core/src/application/ports`
- `UserRepository.getMyExerciseLastPerformance` -> repository implementation ->
  `core/src/infrastructure/repositories/user-repository.ts` (`204` -> `ok(null)`)
- `GetMyExerciseLastPerformanceUseCase` -> use case ->
  `core/src/application/use-cases/users/get-my-exercise-last-performance-use-case.ts`, exported from its `index.ts`
- `Dependencies` key `getMyExerciseLastPerformanceUseCase` -> registered in `app/src/infrastructure/container.ts`
  only (the backoffice does not use it).

App (`workouts` domain area):

- `useExerciseLastPerformance()` (`state`, `load(exerciseId)`, `reset()`) -> state hook ->
  `app/src/presentation/workouts/hooks/use-exercise-last-performance.ts`
- `effortLabel(code, type)` -> presentation helper -> `app/src/presentation/workouts/` beside `set-rows.ts`, same
  lookup as the backoffice's `useExerciseLabels.effortLabel` (by-type key, falling back to the generic one, then the
  code).
- `LastPerformanceDialog` (title, date, stat cards, sets table, loading / empty / error, close) -> organism ->
  `app/src/presentation/workouts/components/organisms/last-performance-dialog.tsx`, built on the `dialog` atom.
- `LastPerformanceSetLine` -> molecule -> `app/src/presentation/workouts/components/molecules/last-performance-set-line.tsx`
- `LastPerformanceStatCard` -> molecule -> `app/src/presentation/workouts/components/molecules/last-performance-stat-card.tsx`
- `Divider` -> atom (new, wraps Paper's `Divider`) -> `app/src/presentation/common/components/atoms/divider.tsx`
- `WorkoutExerciseView` renders the link (`text-link` atom plus `icon`) between `ExerciseNavigator` and the list of sets.
  `ExerciseHeroPanel` does not change.
- `WorkoutExerciseView` holds the dialog's visibility and passes the catalog `exerciseId` and the exercise `type`.
- Copy -> new keys in `app/src/i18n/{es,en}/workouts.json`.
- Atoms reused: `dialog`, `text`, `text-link`, `icon`, `button`. Molecules reused: `LoadingState`, `ErrorState`.

## Canonical Template Mapping

- Aliases, contract, implementation, use case -> `get-user-trainer-use-case.ts` and `UserRepository.getTrainer` ->
  same `/users/...` repository, same `204` -> `null` read.
- Core tests -> `core/test/application/use-cases/users/get-user-trainer-use-case.test.ts` and the `getTrainer` cases
  of `core/test/infrastructure/repositories/user-repository.test.ts` -> `200`, `204`, `401`, `404`, network.
- `useExerciseLastPerformance` -> `app/src/presentation/workouts/hooks/use-workout.ts` -> container + `Result`
  handling, local `loading` / `errorCode`, never throws.
- `LastPerformanceDialog` -> `app/src/presentation/common/components/molecules/confirm-dialog.tsx` and its use in
  `workout-view.tsx` -> the app's dialog shape.
- `LastPerformanceSetLine` -> `set-record-row.tsx` -> set number + values on one line (read-only).
- Link above the sets -> `ExerciseHeroPanel`'s `video` link -> same `text-link` atom with an icon.
- `effortLabel` -> `backoffice/src/presentation/workouts/hooks/use-exercise-labels.ts` (`effortLabel`) -> same
  lookup and copy. This is the one case where the backoffice is a reference for the app: logic and texts only, no UI.
- View test -> `app/test/presentation/workouts/views/workout-exercise-view.test.tsx` -> fakes through the container.
- Atom/molecule/organism references are `pending` in `.stpr/template/README.md`; the app shapes above apply.

## Screens And Navigation

- app -> `/workouts/[id]/exercises/[exerciseId]` -> `WorkoutExerciseView` -> "Marcas anteriores" above the sets opens the
  dialog over the screen -> close returns to the screen unchanged; switching exercise with the navigator closes it.

## API Contract

- `GET /users/me/exercises/{exerciseId}/last-performance` -> in `core/openapi/v1.json`: **no, refresh needed** ->
  `200 ExerciseLastPerformanceResponse` (`workoutDate: date`, `effort: "LOW" | "MIDLOW" | "MID" | "MIDHIGH" | "HIGH"`,
  `sets: LastPerformanceSetResponse[]` with `reps?`, `durationSeconds?`, `weight?` in kg), `204` when never done,
  `401`, `404` (`NOT_FOUND`: exercise missing or not visible to the caller).
- `exerciseId` is the catalog exercise id (`WorkoutByIdExerciseResponse.exerciseId`), not the workout-exercise `id`
  of the route; the view resolves it as it already does for set records.
- `GET /workouts/{id}` -> yes -> already read; its exercise `type` picks the effort scale.
- The refresh (`openapi:refresh` + `openapi:generate`) runs against the API working tree of
  `add-exercise-last-performance-endpoint`, in a commit of its own. Confirm in the refreshed snapshot whether `effort`
  is a typed enum and whether it is nullable; the dialog hides the effort line if it is.

## Held State

- `useExerciseLastPerformance` -> the last read, loading or failed state -> `load`, called when the exercise is shown
  -> reset when the exercise changes or the screen exits.
- `WorkoutExerciseView` -> dialog visible -> the link and close -> reset on exercise change and on screen exit.

## Forms And Structural Validation

None. The dialog is read-only.

## Errors And Copy

- Never done -> `204` -> `ok(null)` -> no link. Not an error.
- Any failed read (`NOT_FOUND`, `UNAUTHORIZED`, `NETWORK`, `UNEXPECTED`) -> no link and no message: the screen works
  without the last session.
- No new error code in the HTTP error mapper.

New keys, `workouts` namespace (es -> en):

- `exercise.lastPerformance.link` -> "Marcas anteriores" -> "Previous sets"
- `exercise.lastPerformance.title` -> "Última sesión" -> "Last session"
- `exercise.lastPerformance.setsDone` -> "Series realizadas" -> "Sets done"
- `exercise.lastPerformance.effort` -> "Esfuerzo" -> "Effort"
- `exercise.lastPerformance.setsRecorded` -> "Series registradas" -> "Recorded sets"
- `exercise.lastPerformance.highest` -> "Mayor carga" -> "Highest weight" (under the highest weight, with a star)
- `exercise.lastPerformance.reps` -> "{{count}} reps" -> "{{count}} reps"
- `exercise.lastPerformance.seconds` -> "{{count}} s" -> "{{count}} s"
- `exercise.lastPerformance.weight` -> "{{weight}} kg" -> "{{weight}} kg"
- `exercise.lastPerformance.noWeight` -> "sin peso" -> "no weight"
- `exercise.lastPerformance.empty` -> "Todavía no has completado este ejercicio." -> "You have not completed this
  exercise yet."
- `exercise.lastPerformance.close` -> "Cerrar" -> "Close" (or reuse a `common` key if one exists)
- `efforts.*` and `effortsByType.{STRENGTH,CARDIO,STRETCHING}.*` -> copied verbatim from
  `backoffice/src/i18n/{es,en}/workouts.json` (e.g. `STRENGTH.HIGH` -> "RIR 0"; `CARDIO.MID` -> "RPE 5-6";
  `STRETCHING.LOW` -> "Muy suave" / its `en` value).

## Platform Capabilities And Dependencies

None. The history icon comes from the icon set the app's `icon` atom already uses. Expo Go is enough.

## Out Of Scope

- Showing the last performance anywhere else (workout list, backoffice), prefilling the set inputs from it, history
  beyond the last session, charts.
- Any backoffice change (see Notes).
- Must not change: the API, the set-record flow, existing `core` use cases.

## Notes

- **Effort labels: decision needed before `/opsx:propose`.** The request asked to move the effort descriptions into
  `core` and make the backoffice read them from there. That hits a stop case in `STPR_INVARIANTS.md` (section 1:
  "requires `backoffice` and `app` to share UI, styles, translations or presentation code"; section on
  internationalisation: "Each front owns its own translations"), and `core` holds no copy or i18n today. This brief
  therefore plans the allowed route: the app gets its own copy of `efforts` / `effortsByType` and its own `effortLabel`
  helper mirroring the backoffice's lookup; the backoffice stays as it is. Moving the labels into `core` would first
  need an amendment to the invariants, as a separate change.
- **Which type picks the scale.** The response carries the effort the past workout prescribed, but not the exercise
  type; the type comes from the current workout's exercise (`WorkoutByIdExerciseResponse.type`). A catalog exercise
  has one type, so they match.
- **Null weight** means "not tracked", different from `0`, as the API documents. The dialog never shows "null kg".
- **Reps vs duration.** A set shows reps when `reps` is set, seconds when `durationSeconds` is set; a set with neither
  shows only the weight part.
- **No stop case** in the plan as written: no business rule moves to the front (which workouts count as "last" is the
  API's), and no type is hand-written.
- **Verification tiers:**
  - core unit: `get-my-exercise-last-performance-use-case.test.ts`; `user-repository.test.ts` gains
    `getMyExerciseLastPerformance` for `200`, `204` -> `null`, `401`, `404`, network.
  - app component: `workout-exercise-view.test.tsx` gains: link shown with and without video; tap opens the dialog
    and calls the use case with the catalog `exerciseId`; date, effort per type (RIR for `STRENGTH`, RPE for
    `CARDIO`, descriptive for `STRETCHING`, generic for unknown), set lines with reps + kg, reps + "sin peso", seconds,
    `0 kg`; no link when no set has a weight, on `null`, on a failed read and while reading; skeleton in place of the
    sets and the link while reading and when switching exercise; close hides the dialog. Small tests for
    `effortKeysOf`, `highestWeightIndexOf` and `hasWeightIn`.
  - Translation parity covers the new keys. No E2E (app E2E is a known gap). Manual check in Expo Go.
- No deviation from the mapped references is allowed.
