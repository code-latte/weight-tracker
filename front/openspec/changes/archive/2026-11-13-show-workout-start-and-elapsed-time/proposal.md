# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-show-workout-start-and-elapsed-time.md`.

## Why

The backoffice workout review renders "Inicio: —" and "Tiempo transcurrido: —" because the API did not say when a
workout was started. The API change `add-workout-started-at` (`api/openspec/changes/add-workout-started-at/`) now
returns `startedAt` next to `completedAt`, so the trainer can see when the session began and how long it has lasted or
took.

## What Changes

- **Contract refresh, first and as a commit of its own:** `openapi:refresh` (API running locally) and
  `openapi:generate`. `WorkoutResponse`, `WorkoutById` and `WorkoutListItem` gain `startedAt` (nullable UTC date). The
  domain aliases pick it up unchanged; typed fixtures in core, backoffice and app gain `startedAt: null`.
- **"Inicio"** in the review's person strip shows `startedAt` as a medium date and short time in the active language
  and the user's time zone (e.g. "13 nov 2026, 10:42"). It keeps "—" when `startedAt` is `null`.
- **"Tiempo transcurrido"** in the review's summary cards:
  - `STARTED`: now − `startedAt`, recounted every minute while the screen is open (local clock; the workout is still
    read once, no polling);
  - `COMPLETED` / `REVIEWED`: `completedAt` − `startedAt`, fixed;
  - "—" when a needed date is `null` or the result is negative (the API does not enforce `startedAt <= completedAt`).
  - Format: "1 h 05 min" from one hour, "12 min" below it, "< 1 min" under a minute.
- The backfilled estimate the API returns for older workouts is shown as is, with no "estimated" mark.

No breaking change. No new endpoint, use case, error code or dependency.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `backoffice-workout-review`: the person strip shows the real start time, and the "Tiempo transcurrido" card shows
  the elapsed time instead of the placeholder.
- `workout-management`: reading a workout by id also returns `startedAt` and `completedAt` as the API answers them.

## Fronts And Roles

- backoffice -> yes -> `ADMIN`, `TRAINER` -> both see the same start and elapsed time; no role difference is added,
  and the existing rule that a `TRAINER` must not see the trainer's name is unchanged.
- app -> no visible change -> `USER` -> it only compiles against the refreshed types.

## Canonical References

- Contract refresh -> archived change `2026-11-08-sync-contract-and-add-workout-results-usecases` -> same refresh and
  fixture steps.
- Start time -> `backoffice/src/presentation/workouts/components/organisms/workout-review-person-strip.tsx` (organism,
  changed in place) -> the "Inicio" fact and the relative time already live there.
- Elapsed card -> `backoffice/src/presentation/workouts/views/workout-review-view.tsx` with the existing
  `ReviewStatCard` molecule -> the same summary row.
- Elapsed derivation and format -> `backoffice/src/presentation/workouts/workout-review.ts` -> the existing review
  helpers and their unit tests.
- Live clock -> state hook beside the existing ones in `backoffice/src/presentation/workouts/hooks/` -> no pending
  reference; a small local hook, no existing equivalent.
- No new atom, molecule or organism. No `pending` reference in `.stpr/template/README.md` applies.

## API Contract

- `GET /workouts/{id}` -> in `core/openapi/v1.json` after the refresh -> `WorkoutById` with `startedAt`, covered by
  the API change `add-workout-started-at`. The screen uses `status`, `startedAt` and `completedAt`.

## Impact

- `core/openapi/v1.json`, `core/src/contract/generated` (regenerated only).
- Typed workout fixtures in `core/test`, `backoffice/test` and `app/test`.
- Backoffice: the review person strip, the review view, `workout-review.ts`, a new clock hook, `src/i18n/{es,en}/workouts.json`.
- No business rule enters the front: the elapsed time is display arithmetic over dates the API returns.

## Out Of Scope

- `startedAt` in the workouts list, the mesocycle screen or the app.
- Polling or refreshing the workout while it is `STARTED`.
- The "Resumen y métricas" and "Feedback" tabs.
- Any API change beyond the snapshot refresh.
