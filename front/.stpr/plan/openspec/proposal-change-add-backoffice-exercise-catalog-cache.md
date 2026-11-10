# Change Proposal: Add Backoffice Exercise Catalog Cache

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-exercise-catalog-cache/`.

## Change

Read the exercise catalog once, in the background, right after the ADMIN or TRAINER signs in, and keep it for the
whole signed-in session, so the screens that need it (exercise catalog page, workout editor, workout template editor)
stop reading `GET /exercises` every time they open. The kept catalog is read again whenever the signed-in user creates
an exercise of their own.

## Objective

`useExercises` (`backoffice/src/presentation/workouts/hooks/use-exercises.ts`) is held by each screen, so every visit
to the catalog page, a workout or a template reads `GET /exercises` again and waits for it before showing the editor.
For a signed-in caller that endpoint also scans every workout the caller can see (see
`api/.../GetAllExercisesUseCase.cs`), so it is the slowest read of those screens and it repeats on every navigation.
Reading it once after sign-in, while the user is still on the home screen, makes those screens open on data already
in memory.

## Capabilities

1. **Warm-up after sign-in:** once the session is confirmed (the same moment `AppShell` loads the current user), the
   backoffice starts reading the exercise catalog in the background. Nothing waits for it: the home screen, the drawer
   and the header render as today.
2. **One catalog per session:** the read catalog is kept in memory for the rest of the signed-in session. Opening the
   exercise catalog page, a workout or a workout template uses it without a new read.
3. **Screens opened before it arrives:** a screen that needs the catalog while the warm-up read is still running shows
   its loading state and waits for that same read; it does not start a second one.
4. **Refresh on own exercise creation:** when the signed-in ADMIN or TRAINER creates an exercise (the "Nuevo ejercicio"
   flow on the catalog page), the catalog is read again in the background. Until the new read arrives, the screens keep
   showing the current catalog with the created exercise already in it, as today.
5. **Refresh on own exercise update:** the same background read happens when the signed-in user updates one of their
   own exercises. The backoffice has no update flow yet (see Notes): this change provides the refresh the update flow
   must call, and it is wired when that flow exists.
6. **Failure:** if the warm-up or a refresh fails, nothing is shown at that moment. A screen that needs the catalog and
   has none shows today's error state; its "Reintentar" reads the catalog again for everyone. A failed refresh keeps
   the catalog that was already kept.
7. **Session end:** signing out, or the session ending, drops the kept catalog, so the next user who signs in on the
   same tab gets their own.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both get the warm-up and the kept catalog; the catalog
  each one sees is still what `GET /exercises` returns to them (an admin every exercise, a trainer the system ones,
  their own and the ones used in workouts they can see).
- app -> lands here: no -> unchanged. The app no longer reads the catalog (its workout screen uses the enriched
  `GET /workouts/{id}`).

No difference between ADMIN and TRAINER beyond what the API returns. Nothing new is shown or hidden for either role.

## Classification

Core: none. `GetExercisesUseCase` and `CreateExerciseUseCase` exist and are registered in
`backoffice/src/infrastructure/container.ts`.

Backoffice:

- Exercise catalog context (value: `status`, `exercises`, `errorCode`, `load`, `reload`, `refresh`, `add`, `clear`) ->
  context -> `backoffice/src/presentation/common/context/exercise-catalog-context.ts`
- `ExerciseCatalogProvider` (holds the session's catalog; reads nothing until `load`; one read in flight at a time;
  clears on session end) -> state hook (provider) -> `backoffice/src/presentation/common/components/exercise-catalog-provider.tsx`
- `useExerciseCatalog` (reads the context, guards a missing provider) -> state hook ->
  `backoffice/src/presentation/common/hooks/use-exercise-catalog.ts`
- `AppProviders` -> composition -> `ExerciseCatalogProvider` mounted inside `SessionProvider`, beside
  `CurrentUserProvider`
- `AppShell` (`SignedInContent`) -> organism -> also calls the catalog's `load()` once the session is confirmed
- `useExercises` -> state hook -> `backoffice/src/presentation/workouts/hooks/use-exercises.ts` keeps its return shape
  (`status`, `exercises`, `errorCode`, `reload`, `add`) but reads the provider instead of the API, so
  `exercise-catalog-view.tsx`, `workout-view.tsx` and `workout-template-view.tsx` do not change
- Exercise creation -> view -> `exercise-catalog-view.tsx`'s `onCreated` keeps calling `catalog.add(exercise)` and
  also calls `refresh()`

No new UI component, no new copy, no route change.

## Canonical Template Mapping

- Context, provider and hook -> `backoffice/src/presentation/common/context/current-user-context.ts`,
  `backoffice/src/presentation/common/components/current-user-provider.tsx`,
  `backoffice/src/presentation/common/hooks/use-current-user.ts` -> the backoffice's session-scoped provider: idle
  until `load`, generation ref against late answers, `runWithSession`, `clear` on session end.
- Warm-up trigger -> `backoffice/src/presentation/common/components/organisms/app-shell.tsx` -> where the current user
  is already loaded after the session is confirmed.
- `useExercises` over the provider -> its current file -> same return contract, so its callers are untouched.
- Tests -> provider test modelled on `backoffice/test/presentation/common/components/theme-mode-provider.test.tsx`;
  the existing view tests of the catalog page, workout and template (`backoffice/test/presentation/workouts/views/`);
  E2E modelled on `backoffice/e2e/workout.spec.ts`.

The app is not a reference. `.stpr/template/` and its README are not updated.

## Screens And Navigation

- backoffice -> every signed-in route -> `AppShell` -> after sign-in or a page load with a valid session -> the
  catalog warms up in the background; no navigation changes.
- backoffice -> `/exercises`, `/workout`, `/workout-template` -> unchanged views -> open on the kept catalog, or wait
  for the warm-up read if it is still running.

## API Contract

- `GET /exercises` -> in `core/openapi/v1.json`: yes -> `Exercise[]`. Read at warm-up, on "Reintentar" and on refresh.
- `POST /exercises` -> yes -> `CreateExerciseRequest` / `Exercise`. Unchanged; its success triggers the refresh.
- `PUT /exercises/{id}` -> yes, but no front use case or flow uses it yet. Not called by this change.

No snapshot refresh. No API change.

## Held State

- `ExerciseCatalogProvider` -> the session's exercise catalog, its status (`idle`, `loading`, `loaded`, `failed`), the
  last error code, and whether a refresh is running -> changed by `load` (first read only), `reload` (after a
  failure), `refresh` (after an own create or update), `add` (optimistic append of a created exercise) -> resets on
  sign-out and on session end (`onEnd`), and on a full page reload (memory only).

The per-screen state `useExercises` held is removed.

## Forms And Structural Validation

None.

## Errors And Copy

- Warm-up fails (network, server) -> the codes the mapper already returns -> nothing at that moment; the next screen
  that needs the catalog shows today's error state with "Reintentar".
- Refresh fails -> same codes -> nothing shown; the kept catalog stays.

No new error code. No new copy.

## Platform Capabilities And Dependencies

None. The cache is in memory; no browser storage, no new package.

## Out Of Scope

- An update-exercise flow in the backoffice (`UpdateExerciseUseCase`, repository method, screen). The refresh is
  ready for it; wiring it is part of that change.
- Deleting or archiving exercises.
- Persisting the catalog across page reloads or tabs (`localStorage`, IndexedDB).
- Caching exercise favorites, workouts, templates or any other read.
- Picking up exercises created by other users during the session (a trainer's new exercise used in a shared workout
  appears after the next refresh or page reload).
- Must not change: core, the API, the app, the three views' behaviour other than not reading the catalog again.

## Notes

- **Update refresh (to confirm).** The request asks for a refresh when an own exercise is updated, but the backoffice
  cannot update exercises today: core has no `UpdateExerciseUseCase` and no screen edits an exercise, although
  `PUT /exercises/{id}` exists in the contract. This brief exposes `refresh()` and requires the future update flow to
  call it. If an update flow should be part of this change, it is a larger change (core use case, repository, edit
  dialog) and this brief must be widened before `/opsx:propose`.
- **Refresh after create (decided).** The created exercise is appended at once, as today (`add`), and the catalog is
  then read again in the background, so it also picks up anything the API derives (body part, archived state) and
  other users' changes. While refreshing, the screens are not put back into loading.
- **Single flight (decided).** `load` only reads from `idle`; `reload` and `refresh` reuse a read already in flight
  instead of starting another, and a generation ref drops answers that arrive after `clear` or a newer read.
- **Background means non-blocking (decided).** The warm-up is a normal `runWithSession` read started from `AppShell`;
  it does not delay the shell, the profile read or onboarding. It runs for users still in onboarding too, since they
  are signed in.
- **Session refresh.** Reads go through `runWithSession`, so an expired access token during the warm-up is refreshed
  and retried exactly as for the profile read.
- **Specs.** New `backoffice-exercise-catalog-cache` capability (warm-up, one read per session, refresh on own
  create, failure, session end). Modified: `backoffice-exercise-catalog`, whose page "SHALL read the catalog and the
  user's favorites once when it opens", now uses the session's catalog and still reads the favorites. The
  "Reintentar" scenario in `backoffice-workout-editor` ("the catalog is read again") stays true.
- **Verification tiers:**
  - core unit: none (no core change).
  - backoffice component:
    - provider: reads nothing until `load`; `load` twice reads once; screens mounted during the read share it;
      `refresh` reads again and keeps the current list meanwhile; a failed refresh keeps the list; `clear` on session
      end drops it and ignores a late answer;
    - catalog page: creating an exercise lists it at once and triggers one more `GET /exercises`;
    - workout and template views: opening them with a loaded catalog sends no `GET /exercises`; with none, they show
      the loading state, then the error state with "Reintentar" on failure.
  - backoffice E2E (`pnpm test:e2e`): sign in, open the catalog page, a workout and a template in turn, and assert
    `GET /exercises` was intercepted once; then create an exercise and assert one more read.
  - No app tests.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviations from the mapped references are allowed.
