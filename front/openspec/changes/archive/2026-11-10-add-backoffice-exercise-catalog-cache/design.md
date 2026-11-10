# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. Current state that shapes the approach:

- `useExercises` (`backoffice/src/presentation/workouts/hooks/use-exercises.ts`) holds the catalog per screen: it reads
  `getExercisesUseCase` through `runWithSession` on mount and returns `{ status, exercises, errorCode, reload, add }`.
  Its callers are `exercise-catalog-view.tsx`, `workout-view.tsx` and `workout-template-view.tsx`; only the catalog
  page creates exercises (`CreateExerciseDialog`'s `onCreated` calls `catalog.add`).
- `CurrentUserProvider` is the backoffice's session-scoped provider: mounted in `AppProviders` inside
  `SessionProvider`, idle until `load()`, a generation ref against late answers, and `useEffect(() => onEnd(clear))` to
  drop its state when the session ends. `AppShell`'s `SignedInContent` calls its `load()` once the session is
  confirmed.
- View tests render a view directly through `test/support/render-with-providers.tsx` (`SessionProvider` +
  `CurrentUserProvider`), without `AppShell`.
- Core already has `GetExercisesUseCase` and `CreateExerciseUseCase`; there is no `UpdateExerciseUseCase`.

## Goals / Non-Goals

**Goals:**

- One catalog read per signed-in session, started after sign-in, shared by every screen.
- Keep the three views and `useExercises`' return contract unchanged, so the change stays in the hook and a provider.

**Non-Goals:**

- Persisting the catalog across reloads or tabs, or any time-based expiry.
- Caching favorites or any other read.
- An exercise update flow (only the refresh it will call).

## Decisions

### Core and container

No change. No new domain alias, port, use case or `Dependencies` key; no `container.ts` change. No new error code:
failures carry the codes the HTTP error mapper already returns, shown by the views' existing error states.

### A session-scoped provider, modelled on `CurrentUserProvider`

New files (backoffice, `presentation/common`):

- `context/exercise-catalog-context.ts`: `ExerciseCatalogContext` (`createContext<ExerciseCatalogContextValue | null>`)
  with the value `{ status, exercises, errorCode, load, reload, refresh, add, clear }`, `status` being
  `"idle" | "loading" | "loaded" | "failed"`.
- `components/exercise-catalog-provider.tsx`: `ExerciseCatalogProvider`, mounted in `AppProviders` inside
  `SessionProvider`, beside `CurrentUserProvider`.
- `hooks/use-exercise-catalog.ts`: `useExerciseCatalog()`, reads the context and throws when the provider is missing,
  like `useCurrentUser`.

Held state: `exercises`, `status`, `errorCode`, plus refs for the status and for the read in flight. Who changes it:

- `load()`: reads only from `idle` (first caller wins; later calls are no-ops).
- `reload()`: after a failure, sets `loading`, clears the error and reads again (the views' "Reintentar").
- `refresh()`: reads again without leaving `loaded`; on success replaces `exercises`, on failure keeps them and the
  status.
- `add(exercise)`: appends at once (optimistic, as today).
- `clear()`: back to `idle` with an empty list; called by `onEnd`.

Resets: on sign-out and session end (`onEnd(clear)`), and on a full page reload (memory only).

**Single flight.** A read already in flight is reused: `load`, `reload` and `refresh` attach to it instead of
sending another. A generation ref is bumped by every new read and by `clear`, so an answer for an older read or after
the session ended is dropped. A `refresh` requested while a read is running starts one more read when it ends, so a
creation made during the first read is never lost.

Alternative considered: keep `useExercises` per screen and memoise the answer in a module-level variable. Rejected:
module state outlives the session (a second user on the same tab would see it) and is invisible to React, while the
provider follows the existing `CurrentUserProvider` pattern and clears on `onEnd`.

### Triggering the background read

- `AppShell` (`SignedInContent`, an organism) calls `useExerciseCatalog().load()` in the same effect that loads the
  current user, so the read starts as soon as the session is confirmed and does not block rendering.
- `useExercises` also calls `load()` on mount. React runs a child's effects before its parent's, so a screen's hook
  cannot rely on the shell having started the read; since `load` only reads from `idle`, both calls send one read.
  It also makes the views self-sufficient in their tests.

### `useExercises` becomes a reader

`backoffice/src/presentation/workouts/hooks/use-exercises.ts` keeps its return shape, mapped from the provider:

- `status`: `idle` and `loading` -> `"loading"`; `loaded` -> `"loaded"`; `failed` -> `"failed"`.
- `exercises`, `errorCode`, `add`: passed through.
- `reload`: the provider's `reload`.
- It also exposes `refresh` for the catalog page.

Its comment changes from "Held by the screen only, so it resets on exit" to "the session's catalog".

### Refresh after a creation

`exercise-catalog-view.tsx`'s `onCreated` keeps `catalog.add(exercise)` and then calls `catalog.refresh()`. Only the
catalog page creates exercises, and only for a `TRAINER` (an admin sees no "Nuevo ejercicio"); no role check is added,
since the button's visibility already decides it.

### Refresh after an update

`refresh()` is the hook the future update flow calls after `PUT /exercises/{id}` succeeds. Nothing calls it for an
update in this change; the spec states the contract.

### Components and imports

No new UI component. `AppShell` is an organism and imports the hook (downward); the provider sits at the root of
`common/components` like `CurrentUserProvider`. Only atoms import MUI widgets; the provider and hooks import none.

### Translations

None. The views keep their existing copy and error messages (`errors:<code>` in `es` and `en`).

### Tests

- **Test helper exception:** `test/support/render-with-providers.tsx` mounts `ExerciseCatalogProvider` inside
  `SessionProvider`, beside `CurrentUserProvider`, mirroring `AppProviders`. Without it every view using
  `useExercises` would throw. A new `test/support/load-exercise-catalog.tsx`, modelled on `load-profile.tsx`, stands in
  for `AppShell` in the view tests that open a screen after the catalog was read, and the helper's default
  `getExercisesUseCase` answers an empty catalog instead of nothing, since every test that renders `AppShell` now
  reads it. No other helper or pattern changes.
- New `backoffice/test/presentation/common/components/exercise-catalog-provider.test.tsx`: renders a small probe
  component under `renderWithProviders`; covers idle until `load`, `load` twice -> one read, two consumers share one
  read, `reload` after failure, `refresh` keeps the list and replaces it, failed `refresh` keeps it, `refresh` during
  a read sends one more read after it, `clear` on session end drops the list and ignores a late answer. Modelled on
  `theme-mode-provider.test.tsx` (probe component, use cases faked through the container); Arrange / Act / Assert with
  one Act.
- View tests updated:
  - `exercise-catalog-view.test.tsx`: the creation case now expects exactly one more `GET /exercises` and the list
    still visible; the opening case still sees one catalog read (the view's own `load`, since the test has no shell).
  - `workout-view.test.tsx`, `workout-template-view.test.tsx`: unchanged expectations of one catalog read per render,
    since each test has a fresh provider; a new case per view renders it under a provider whose catalog was already
    loaded and asserts no catalog read on opening.
- New E2E `backoffice/e2e/exercise-catalog-cache.spec.ts`, modelled on `backoffice/e2e/workout.spec.ts`: sign in, open
  `/exercises`, a workout and a template, and count one intercepted `GET /exercises`; create an exercise and count one
  more.

## Risks / Trade-offs

- [Exercises created by other users during the session are not seen] → Accepted: they appear after the user's next
  creation, a retry, or a page reload. A time-based refresh is out of scope.
- [The read runs for every signed-in page load, even when no catalog screen is visited] → Accepted: it is one
  background read per session, the cost this change trades for instant screens.
- [A refresh lands while the user filters the catalog page] → The list is replaced in place without a loading state;
  the section and filters are view state and are kept.
- [Admins get the whole catalog in memory] → Same payload as today's single read, held once instead of per screen.

## Open Questions

- When the update flow is planned, it must call `refresh()` after a successful `PUT /exercises/{id}`; no question
  blocks this change.
