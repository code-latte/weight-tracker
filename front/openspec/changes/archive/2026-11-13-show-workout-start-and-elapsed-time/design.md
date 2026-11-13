# Design

## Context

See `proposal.md` for the motivation. Today:

- `WorkoutReviewPersonStrip` renders `t("review.startedAt", { time: t("review.notAvailable") })` and freezes `now`
  once with `useState(() => Date.now())` for the "Última actualización" relative time.
- `WorkoutReview` (`workout-review-view.tsx`) passes `notAvailable` as the value of the "Tiempo transcurrido"
  `ReviewStatCard`.
- `workout-review.ts` holds the review's pure helpers (`reviewSummaryOf`, `restOf`, …), unit-tested in
  `backoffice/test/presentation/workouts/workout-review.test.ts`.
- `completedAt` is generated as a required `null | string` on the four workout schemas; `startedAt` is expected to be
  generated the same way by the API change `add-workout-started-at`.

## Goals / Non-Goals

**Goals:**

- Regenerate the contract so `startedAt` reaches the domain types without hand-written types.
- Replace the two placeholders with values derived only from `status`, `startedAt` and `completedAt`.

**Non-Goals:**

- No change to `useWorkout` (no polling), to the container or to any use case.
- No new UI component; the existing organism, molecule and view change in place.
- The "Última actualización" relative time stays frozen at mount, as today.

## Decisions

### Core: refresh only

`openapi:refresh` + `openapi:generate`, committed alone. The domain aliases (`WorkoutById`, `WorkoutListItem`, …)
alias the generated schemas and pick up `startedAt` with no edit. No new `Dependencies` key, no `container.ts` change in
either front. Typed fixtures in `core/test`, `backoffice/test` and `app/test` gain `startedAt: null` (in their shared
builder where one exists), exactly as the earlier refresh added `resultsUpdatedAt: null`. The core repository test for
`GET /workouts/{id}` gains a case that the answer's `startedAt` and `completedAt` reach the result unchanged.

If the regenerated diff shows anything beyond `startedAt` on the workout responses, stop and report it.

### Backoffice: elapsed as a pure helper

Add to `presentation/workouts/workout-review.ts`:

- `elapsedMsOf(workout: Pick<WorkoutById, "status" | "startedAt" | "completedAt">, now: number): number | null`
  - `STARTED` → `now − startedAt`; `COMPLETED` / `REVIEWED` → `completedAt − startedAt`;
  - `null` when a needed date is `null`, when the result is negative, or for any other status.
- `elapsedPartsOf(ms: number): { hours: number; minutes: number }` (truncated), so the view formats with i18n keys.

Why helpers and not inline in the view: they match the existing review derivations and are unit-testable without
rendering. Alternative considered: `Intl.DurationFormat` — rejected because its availability in jsdom/Node is uneven
and the copy "1 h 05 min" is simple enough for translation keys.

### Backoffice: the live clock

A new state hook `presentation/workouts/hooks/use-now.ts`: `useNow(live: boolean, intervalMs = 60_000): number`. It
returns `Date.now()` captured at mount and, only when `live` is true, refreshes it with `setInterval` and clears it on
unmount or when `live` turns false. The view calls `useNow(workout.status === "STARTED")`.

Held state: the current time → changed by the interval → reset on screen exit. A 60 s tick matches minute resolution;
the first value can lag up to a minute behind the real minute boundary, which is acceptable for a session timer.
Alternative considered: aligning the tick to the next minute boundary — rejected as unneeded complexity.

### Backoffice: the start time

`WorkoutReviewPersonStrip` gains a `startedAt: string | null` prop. It formats with
`new Intl.DateTimeFormat(i18n.language, { dateStyle: "medium", timeStyle: "short" })` in the local time zone (no
`timeZone` option: `startedAt` is an instant, unlike the UTC calendar days in `use-week-label.ts`), or uses
`review.notAvailable` when `null`. The view passes `workout.startedAt`.

### Copy

New keys in `backoffice/src/i18n/{es,en}/workouts.json` under `review.elapsed`:

| Key                         | es                             | en                             |
| --------------------------- | ------------------------------ | ------------------------------ |
| `review.elapsed.hours`      | `{{hours}} h {{minutes}} min`  | `{{hours}} h {{minutes}} min`  |
| `review.elapsed.minutes`    | `{{minutes}} min`              | `{{minutes}} min`              |
| `review.elapsed.lessThanOne`| `< 1 min`                      | `< 1 min`                      |

`minutes` is passed padded to two digits for the hours form. `review.startedAt` and `review.notAvailable` are reused.
`review.stats.elapsed` is an existing key; `review.elapsed` is a new sibling object, so no key clashes. No error codes
are involved and the HTTP error mapper does not change.

### Components and imports

- `WorkoutReviewPersonStrip` → organism, `presentation/workouts/components/organisms/` (changed).
- `ReviewStatCard` → molecule, reused unchanged.
- `WorkoutReview` → view, changed. Imports keep flowing downward; no MUI widget is imported outside atoms beyond what
  these files already import. The app gets no presentation code.

### Tests

- `workout-review.test.ts`: `elapsedMsOf` for each status branch, `null` dates, negative result and an unrelated
  status; `elapsedPartsOf` truncation.
- `workout-view.test.tsx` (review branch): "Inicio" with a date and with `null`; elapsed for `COMPLETED`, `REVIEWED`
  and `STARTED`; the `STARTED` tick with `vi.useFakeTimers()` advancing 60 s, asserting the use case is called once;
  a `COMPLETED` workout not changing after 60 s. The expected date string is built with the same `Intl` options to
  avoid depending on the exact ICU output.
- The existing scenario "Athlete and placeholders" keeps its name and becomes the `startedAt: null` case, beside a
  new "Athlete and start time"; the "Figures" scenario no
  longer asserts "—".

## Risks / Trade-offs

- [The API change is not merged or not running when refreshing] → task 1.1 requires the API from the branch carrying
  `add-workout-started-at`; stop if `startedAt` is missing from the regenerated schema.
- [`startedAt` generated as optional instead of required-nullable] → the helpers accept `null | undefined` by treating
  both as missing; fixtures then do not need the field. Record which case happened in task 1.1.
- [Backfilled `startedAt` is an estimate] → shown as is, by decision in the brief.
- [Fake timers and React effects in tests] → fake only what the test needs (`setInterval`, `Date`) and wrap the advance
  in `act`.

## Migration Plan

Front-only and additive. Deploy after the API exposes `startedAt`; before that, the field arrives `undefined` and both
values render "—". Rollback is reverting the commits.
