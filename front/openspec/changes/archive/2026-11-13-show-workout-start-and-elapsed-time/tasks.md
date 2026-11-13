# Tasks

## 1. Contract snapshot (commit of its own)

- [x] 1.1 Run the API from the branch carrying `add-workout-started-at` (Debug API, port 5199), then
  `pnpm --filter @weight-tracker/core openapi:refresh` and `pnpm --filter @weight-tracker/core openapi:generate`.
  Verify the diff of `core/openapi/v1.json` and `core/src/contract/generated` shows only `startedAt` added to the
  workout responses (`WorkoutResponse`, `WorkoutById`, `WorkoutListItem` and the related records), and record whether
  it is required-nullable like `completedAt`. Stop and report anything else.
- [x] 1.2 Confirm nothing under `core/src/contract/generated` was edited by hand, then run `pnpm typecheck` and record
  the fixture files that fail on the missing `startedAt`.
- [x] 1.3 Add `startedAt: null` to each typed workout fixture listed in 1.2 (core, backoffice, app tests), in the
  shared builder where a file has one. Verify that `pnpm typecheck` reports no fixture error and `pnpm test` passes.

## 2. Core

- [x] 2.1 In `core/test/infrastructure/repositories/workout-repository.test.ts`, add the "Workout read with its session
  dates" case for `GET /workouts/{id}`: the API answers a completed workout with `startedAt` and `completedAt` set, and
  the result holds both unchanged (Arrange / Act / Assert). Verify with `pnpm --filter @weight-tracker/core test`.
- [x] 2.2 Review core against the archived refresh change (`2026-11-08-sync-contract-and-add-workout-results-usecases`):
  no domain alias, port, use case or repository changed, and no hand-written type was added.

## 3. Backoffice helpers and clock

- [x] 3.1 Add `elapsedMsOf` and `elapsedPartsOf` to `backoffice/src/presentation/workouts/workout-review.ts` as in the
  design, and cover them in `backoffice/test/presentation/workouts/workout-review.test.ts`: `STARTED` against a fixed
  now, `COMPLETED` and `REVIEWED` from both dates, `null` `startedAt`, `null` `completedAt` on a finished workout, a
  negative result, an unrelated status, and truncation to hours and minutes. Verify with
  `pnpm --filter backoffice test workout-review`.
- [x] 3.2 Add the state hook `backoffice/src/presentation/workouts/hooks/use-now.ts` (`useNow(live, intervalMs = 60_000)`)
  and a hook test in `backoffice/test/presentation/workouts/hooks/use-now.test.ts` modelled on the existing hook
  tests: it ticks when `live`, does not tick when not, and clears its interval on unmount. Verify with
  `pnpm --filter backoffice test use-now`.

## 4. Backoffice copy

- [x] 4.1 Add `review.elapsed.hours`, `review.elapsed.minutes` and `review.elapsed.lessThanOne` to
  `backoffice/src/i18n/es/workouts.json` and `backoffice/src/i18n/en/workouts.json` with the values in the design.
  Verify the translation parity test passes.

## 5. Backoffice review screen

- [x] 5.1 In `components/organisms/workout-review-person-strip.tsx`, add the `startedAt: string | null` prop and render
  "Inicio: <medium date, short time>" in the active language and local time zone, or "—" when `null`. Verify with the
  backoffice typecheck.
- [x] 5.2 In `views/workout-review-view.tsx`, pass `workout.startedAt` to the strip, call
  `useNow(workout.status === "STARTED")`, and render "Tiempo transcurrido" from `elapsedMsOf` / `elapsedPartsOf` with
  the new keys, or `review.notAvailable` when `null`. Verify with the backoffice typecheck.
- [x] 5.3 Update the review tests in `backoffice/test/presentation/workouts/views/workout-view.test.tsx`: turn "Athlete and
  placeholders" into the `startedAt: null` case and add "Athlete and start time"; drop the "—" assertion from the figures test;
  add the elapsed scenarios for `STARTED`, `COMPLETED`, `REVIEWED`, start unknown, completion unknown and dates out of
  order; add the `STARTED` tick (fake timers, advance 60 s, value goes up one minute, `getWorkoutByIdUseCase` called
  once) and the `COMPLETED` no-tick case. Every test keeps one labelled Arrange / Act / Assert. Verify with
  `pnpm --filter backoffice test workout-view`.
- [x] 5.4 Review the backoffice against its mapped references (`workout-review-person-strip.tsx`,
  `workout-review-view.tsx`, `workout-review.ts`, the existing hooks): no new component, atomic levels unchanged, only
  atoms import MUI widgets beyond the existing imports, the `ADMIN`/`TRAINER` strip tests still pass, and nothing was
  shared with the app.

## 6. Verification

- [x] 6.1 Run the `.stpr/template/` drift check from `front/` and verify it prints nothing.
- [x] 6.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes. No E2E flow is planned.
