# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-exercise-catalog-cache.md`.

## Why

Every visit to the exercise catalog page, a workout or a workout template reads `GET /exercises` again, and the editor
waits for it. For a signed-in caller that endpoint scans every workout the caller can see, so it is the slowest read of
those screens and it repeats on every navigation. Reading it once in the background right after sign-in makes those
screens open on data already in memory.

## What Changes

- After the session is confirmed, the backoffice reads the exercise catalog in the background and keeps it in memory
  for the rest of the signed-in session. Nothing waits for that read.
- The exercise catalog page, the workout editor and the workout template editor use the kept catalog instead of
  reading it when they open. A screen opened while the first read is still running waits for that same read.
- When a trainer creates an exercise, it is listed at once (as today) and the kept catalog is then read again in the
  background. **Behaviour change:** today a creation explicitly sends no new catalog read.
- The cache offers a refresh for an own-exercise update; the backoffice has no update flow yet, so nothing calls it in
  this change.
- A failed background read shows nothing at that moment; a screen that needs the catalog and has none shows today's
  error with "Reintentar". A failed refresh keeps the kept catalog. Sign-out or session end drops it.
- No core, API or app change. No new copy, error code, package or route.

## Capabilities

### New Capabilities

- `backoffice-exercise-catalog-cache`: the session-wide exercise catalog of the backoffice: the background read after
  sign-in, a single read shared by every screen, the refresh after an own exercise is created, failure handling and
  reset at session end.

### Modified Capabilities

- `backoffice-exercise-catalog`: "The exercise catalog screen lists the catalog" no longer reads the catalog when the
  screen opens (it still reads the favorites); "Crear adds the exercise to the user's own" now refreshes the kept
  catalog in the background after the exercise is listed.
- `backoffice-workout-editor`: "The workout screen is reached by the workout id" reads the workout on opening and uses
  the kept catalog instead of reading it.
- `backoffice-workout-template-editor`: "The template screen is reached by the template id" reads the template and
  the favorites on opening and uses the kept catalog instead of reading it.

## Fronts, Roles And References

- **Fronts:** backoffice only. The app is unchanged (it no longer reads the catalog).
- **Roles:** `ADMIN` and `TRAINER` both get the background read and the kept catalog, each with what
  `GET /exercises` returns to them. Only a `TRAINER` can create exercises (the API refuses an admin and the admin sees
  no "Nuevo ejercicio"), so the refresh after a creation only happens for trainers. Nothing new is shown or hidden for
  either role.
- **Canonical references** (backoffice; none is `pending`):
  - context, provider and hook -> `backoffice/src/presentation/common/context/current-user-context.ts`,
    `backoffice/src/presentation/common/components/current-user-provider.tsx`,
    `backoffice/src/presentation/common/hooks/use-current-user.ts` (session-scoped provider, idle until `load`);
  - background-read trigger -> `backoffice/src/presentation/common/components/organisms/app-shell.tsx`;
  - `useExercises` over the provider -> its own file, keeping its return contract;
  - provider test -> `backoffice/test/presentation/common/components/theme-mode-provider.test.tsx` (a probe
    component under `renderWithProviders`, use cases faked through the container);
  - view tests -> `backoffice/test/presentation/workouts/views/`; E2E -> `backoffice/e2e/workout.spec.ts`.
- **Components:** no new UI component. The existing organism `AppShell` gains the `load()` call; views are unchanged
  except the catalog page's `onCreated`, which also refreshes.
- **API endpoints**, all in `core/openapi/v1.json`, no snapshot refresh: `GET /exercises` (`Exercise[]`),
  `POST /exercises` (unchanged, its success triggers the refresh). `PUT /exercises/{id}` exists but no front flow
  calls it.
- **Business rules:** none. Caching is a client-side read strategy; the API remains the authority on what each caller
  sees.
- **Conflicts with the template, invariants or specs:**
  - `backoffice-exercise-catalog` currently requires that a creation sends no new catalog read; this change modifies
    that requirement on purpose.
  - The request names "ADMIN or TRAINER" for creation, but only trainers can create; recorded above, no conflict with
    the API.
  - The request also asks for a refresh on update, but no update flow exists; the refresh is provided and wired when
    that flow is planned (see `design.md`). No other conflict with the template or invariants.

## Impact

- Backoffice: new `common/context/exercise-catalog-context.ts`, `common/components/exercise-catalog-provider.tsx`,
  `common/hooks/use-exercise-catalog.ts`; `app-providers.tsx` mounts the provider; `app-shell.tsx` triggers `load`;
  `workouts/hooks/use-exercises.ts` reads the provider; `exercise-catalog-view.tsx` refreshes after a creation.
- Tests: new provider test; the catalog page, workout and template view tests updated for "no catalog read on
  opening" and "one read after a creation"; a new backoffice E2E for one catalog read across the three screens.
- Not affected: core, the API, the app, routes, copy.
