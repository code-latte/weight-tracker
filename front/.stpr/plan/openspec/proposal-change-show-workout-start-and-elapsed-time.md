# Change Proposal: Show The Workout Start And Elapsed Time In The Review

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/show-workout-start-and-elapsed-time/`.

## Change

Refresh the API contract snapshot so workouts carry `startedAt`, then fill two of the review screen's placeholders:
"Inicio" in the person strip and "Tiempo transcurrido" in the summary cards.

## Objective

The backoffice workout review (`add-backoffice-workout-review-screen`) rendered the start time and the elapsed time as
"—" because the API did not return when a workout was started. The API change `add-workout-started-at` (planned in
`api/`) now exposes `startedAt` next to `completedAt`. The trainer can see when the session began and how long it has
lasted or took.

## Capabilities

- **Contract refresh (first step, a commit of its own).** Run `openapi:refresh` with the API running, then
  `openapi:generate`. `WorkoutResponse`, `WorkoutById` and `WorkoutListItem` gain `startedAt` (nullable UTC date).
  Review the diff of `core/openapi/v1.json` and the regenerated types; nothing else in `core` changes.
- **"Inicio"** in the person strip: `Inicio: <date and time>` from `startedAt`, in the active language and the
  user's local time zone. It keeps "—" when `startedAt` is `null`.
- **"Tiempo transcurrido"** in the summary cards:
  - `STARTED`: now − `startedAt`;
  - `COMPLETED` or `REVIEWED`: `completedAt` − `startedAt`;
  - "—" when a date it needs is `null` or when the result is negative (the API does not enforce
    `startedAt <= completedAt`: admin overrides and backfilled rows can reorder them).
- **Live elapsed time.** For `STARTED` the card keeps counting while the screen is open (a local clock tick; the
  workout is still read once and not polled). For `COMPLETED` and `REVIEWED` it is fixed.
- The live state placeholder (if any remains) is untouched.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both see the same start time and elapsed time. No
  role difference is added; the existing trainer-name rule in the strip is unchanged.
- app -> lands here: no -> roles: `USER` -> no visible change. It compiles against the refreshed types only.

## Classification

- Contract snapshot -> generated contract -> `core/openapi/v1.json`, `core/src/contract/generated` (regenerated, never
  hand-edited).
- Domain aliases -> entity type -> `core` (unchanged; they alias the generated types and pick up `startedAt`).
- Start time -> organism (changed in place) ->
  `backoffice/src/presentation/workouts/components/organisms/workout-review-person-strip.tsx`.
  - It takes a new `startedAt` prop.
- Elapsed time -> view (changed in place) -> `backoffice/src/presentation/workouts/views/workout-review-view.tsx`.
  - It passes the computed value to the existing `ReviewStatCard`.
- Elapsed derivation and format -> pure presentation helpers -> `backoffice/src/presentation/workouts/workout-review.ts`.
  - `elapsedMsOf(workout, now)` returns `null` for the "—" cases; a formatter renders it.
- Live clock -> state hook (new, if no equivalent exists) -> `backoffice/src/presentation/workouts/` beside the view.
  - It ticks only while the workout is `STARTED`.

## Canonical Template Mapping

- Contract refresh -> the previous snapshot refresh change (`sync-contract-and-add-workout-results-usecases`) -> same
  steps and commit split.
- Person strip -> `workout-review-person-strip.tsx` itself -> the "Inicio" `Fact` and the relative time of
  `resultsUpdatedAt` already live there.
- Elapsed card -> `workout-review-view.tsx` and `ReviewStatCard` -> the same summary row.
- Helpers -> `presentation/workouts/workout-review.ts` -> the existing review derivations and their unit tests.
- Date and time format -> the `Intl.DateTimeFormat` use in `athletes-table.tsx` -> the backoffice's way to show a date.
- No `pending` reference in `.stpr/template/README.md` applies.

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> `WorkoutReview` for `STARTED`, `COMPLETED`, `REVIEWED`.
  Navigation, loading and error handling are unchanged.

## API Contract

- `GET /workouts/{id}` -> in `core/openapi/v1.json`: after the refresh -> `WorkoutById`, now with `startedAt`.
  - The screen uses `startedAt`, `completedAt` and `status`.
- `startedAt` also appears on `WorkoutResponse` and `WorkoutListItem`; no screen uses it there in this change.

Depends on the API change `add-workout-started-at` (`api/openspec/changes/add-workout-started-at/`). It must be running
locally for `openapi:refresh`. `startedAt` is "the last time the workout reached `STARTED`", kept after it leaves
`STARTED`, and for older workouts a backfilled estimate.

## Held State

- `useWorkout` -> unchanged (read once, no polling).
- The live clock -> the current time -> ticks while the review shows a `STARTED` workout -> stops on screen exit.

## Forms And Structural Validation

None.

## Errors And Copy

No new error codes and no mapper change.

- `review.startedAt` -> reuse "Inicio: {{time}}" / "Start: {{time}}", now with the real date.
- `review.notAvailable` -> reuse "—" for the `null` and negative cases.
- Elapsed format -> new keys if the formatter needs words, e.g. `review.elapsed.hoursMinutes` -> "{{hours}} h {{minutes}} min"
  / "{{hours}} h {{minutes}} min", and `review.elapsed.minutes` -> "{{minutes}} min" / "{{minutes}} min". Final keys
  are fixed in the proposal.

## Platform Capabilities And Dependencies

None. `Intl.DateTimeFormat` and a `setInterval` clock need no package.

## Decisions Taken

- Elapsed rule: `STARTED` uses now − `startedAt`; `COMPLETED` and `REVIEWED` use `completedAt` − `startedAt`.
- Missing or reordered dates show "—"; nothing is invented.
- Elapsed format: "1 h 05 min" from one hour, "12 min" below it, "< 1 min" under a minute. For `STARTED` the clock ticks
  every minute, so the value changes as the minutes go by.
- Start format: medium date and short time in the active language (e.g. "13 nov 2026, 10:42").
- The backfilled estimate is shown as is; the front does not mark it as an estimate.

## Out Of Scope

- Using `startedAt` in the workouts list, the mesocycle screen or the app.
- The live state indicator, the "Resumen y métricas" and "Feedback" tabs.
- Polling or refreshing the workout while it is `STARTED`.
- Any change to the API or its contract beyond the refresh.

## Notes

- Commit split: (1) contract refresh and regenerated types; (2) the backoffice review changes.
- Verification tiers:
  - unit tests for the elapsed helper: `STARTED` against a fixed now, `COMPLETED` and `REVIEWED` from both dates,
    `null` `startedAt`, `null` `completedAt` for a finished workout, a negative result, and each format branch;
  - the backoffice review component test: "Inicio" with a date, "Inicio: —" when `null`, the elapsed card for
    `STARTED` (fake timers, advancing the clock) and for `COMPLETED`;
  - existing core, backoffice and app tests keep passing against the refreshed types (fixtures gain `startedAt`);
  - no new E2E flow.
- The elapsed time is display arithmetic over dates the API returns, not a business rule.
- No deviations from the mapped references are allowed.
