# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-exercise-favorites.md`.

## Why

A trainer building a workout keeps reaching for the same few exercises, and now also for exercises they created
themselves, and today has to find them again by text or pills every time. The API now serves per-user favorites
(`/exercise-favorites`, API change `add-exercise-favorites`) and an owner on every exercise (`ExerciseResponse.ownerId`),
but the front's contract snapshot has neither. Wiring them lets the catalog dialog offer a one-click way to narrow the
catalog to the exercises the trainer uses.

## What Changes

- **Contract**: refresh the snapshot (`core/openapi/v1.json`, `core/src/contract/generated`), so the `Exercise` alias
  gains `ownerId` and `archivedAtUtc`, and `ExerciseFavoriteResponse` and the three `/exercise-favorites` operations
  appear. It is committed as a step of its own.
- **core**: a new `ExerciseFavorite` alias, an `ExerciseFavoriteRepositoryInterface` port and its
  `ExerciseFavoriteRepository`, and three use cases: `GetExerciseFavoritesUseCase`, `MarkExerciseFavoriteUseCase` and
  `UnmarkExerciseFavoriteUseCase`, each returning `Result`.
- **Favorite toggle**: every card of the catalog dialog gets a star icon button beside "Elegir ejercicio": outlined
  while not a favorite, filled while it is. It marks or unmarks the exercise; the star changes only when the API
  confirms, the button is disabled while its call is in flight, and a failure keeps the star and shows the error.
- **Section sidebar**: the catalog dialog gets a left column with three items, each an icon and a name: "Todos"
  (the whole catalog, selected by default), "Mis ejercicios" (the exercises the current user owns) and "Favoritos"
  (the user's favorites). The section narrows the catalog first; the text and pill filters apply on top. It is kept
  when the dialog closes and opens again.
- **Wider dialog**: on wide screens the catalog dialog grows from at least 75% to at least 90% of the viewport width,
  so the filters and the exercise grid keep about today's width beside the sidebar.
- **Favorites load with the catalog**: the workout screen reads `GET /exercise-favorites` when it opens. If that read
  fails, the catalog still works; the stars and "Favoritos" are unavailable and an alert explains why.

## Capabilities

### New Capabilities

- `exercise-favorites`: listing, marking and unmarking the caller's favorite exercises through `core`, the requests
  each one sends, and how their failures are returned as results.

### Modified Capabilities

- `exercise-catalog`: listed exercises now carry their owner (`ownerId`, `null` for a system exercise) and their
  archive date, unchanged.
- `backoffice-workout-editor`:
  - the catalog dialog gains the section sidebar ("Todos", "Mis ejercicios", "Favoritos");
  - the catalog filters combine the section with the text and pills;
  - each catalog card gains the favorite star beside "Elegir ejercicio";
  - the catalog dialog is at least 90% of the viewport width on wide screens (was 75%);
  - favorites are read on opening, and their failure leaves the catalog usable.

## Canonical References

`.stpr/template/README.md` lists the atomic UI references as `pending`, so each UI piece takes the closest backoffice
piece.

- Snapshot refresh -> the refresh steps of the archived change `show-exercise-type`.
- `ExerciseFavorite` alias -> `core/src/domain/exercises/exercise.ts` (one alias over the generated schema).
- Repository contract and implementation -> `AuthRepositoryInterface` / `AuthRepository` (skeleton); `PUT` and `DELETE`
  with a path id -> `core/src/infrastructure/repositories/workout-repository.ts`.
- Use cases -> `SignupUseCase` (skeleton) and `get-exercises-use-case.ts`.
- Wiring -> each front's `src/infrastructure/container.ts`, under new `Dependencies` keys.
- Favorites state hook (new) -> `use-exercises.ts` for the read, `use-remove-workout-exercise.ts` for the in-flight
  id and error code.
- Sidebar -> a new molecule, `ExerciseSectionList`, built from the existing `List`, `ListItem`, `ListItemIcon` and
  `ListItemText` atoms, as the navigation drawer composes them.
- Star -> the existing `IconButton` atom in `ExerciseListItem`'s `actions` slot (molecule, unchanged).
- Extended in place: `ExerciseCatalogDialog` (organism) and `WorkoutView` / `LoadedWorkout` (view).
- Tests:
  - `core/test/infrastructure/repositories/exercise-repository.test.ts` -> new
    `exercise-favorite-repository.test.ts`;
  - `core/test/application/use-cases/exercises/get-exercises-use-case.test.ts` -> the three new use-case tests;
  - `backoffice/test/presentation/workouts/views/workout-view.test.tsx` (extended);
  - `backoffice/e2e/workout.spec.ts` (extended);
  - both fronts' `test/infrastructure/container.test.ts` and `test/support/render-with-providers.tsx` (new keys).

## Fronts And Roles

- backoffice: `ADMIN` and `TRAINER` see the star, the sidebar and the wider dialog the same way. Each only ever sees
  and changes their own favorites, and "Mis ejercicios" lists only the exercises they own. Custom exercises owned by
  someone else (reachable through workouts the user can see) appear only under "Todos", never under "Mis ejercicios".
- app (`USER`): no UI change. Its `container.ts` registers the new repository and use cases, because `Dependencies`
  types both containers (see Constraints). It compiles against the regenerated `Exercise` alias and builds no
  exercises.

## API Endpoints

None of these is in the current snapshot. All are provided by the API change `add-exercise-favorites` (and the
custom-exercises change that added `ownerId`), already served by the API. This change refreshes the snapshot.

- `GET /exercises` (`getExercisesUseCase`) -> `ExerciseResponse.ownerId: string | null`,
  `ExerciseResponse.archivedAtUtc: string | null`. It sends the session token when there is one, which is what makes
  the API include the caller's own exercises.
- `GET /exercise-favorites` (`getExerciseFavoritesUseCase`) -> `ExerciseFavoriteResponse[]` (`exerciseId`,
  `createdAtUtc`), newest first; `401` without a session.
- `PUT /exercise-favorites/{exerciseId}` (`markExerciseFavoriteUseCase`) -> `200` with `ExerciseFavoriteResponse`,
  idempotent; `404` unknown or invisible exercise, `409` archived exercise.
- `DELETE /exercise-favorites/{exerciseId}` (`unmarkExerciseFavoriteUseCase`) -> `204`, idempotent; `404` unknown or
  invisible exercise.

## Constraints And Conflicts

- **App registration (deviation from the brief).** The brief says the new use cases are not registered in the app.
  The invariants type every container against the single `Dependencies` interface, so a missing registration fails to
  compile. The app's `container.ts` therefore registers the repository and the three use cases, and its container
  test lists the new keys. The app gets no UI.
- **No business rule.** Sections and the star state are local presentation derived from the API's lists. Which
  exercises a user may favorite, idempotency and archived rejection are the API's rules; the front only reports their
  error codes.
- **"Mis ejercicios" is ownership.** The filter is `ownerId === current user id`, not `ownerId !== null`, as decided
  after the brief.
- **No new error code.** `NOT_FOUND`, `CONFLICT`, `UNAUTHORIZED`, `NETWORK` and `UNEXPECTED` are already mapped and
  translated; the HTTP error mapper is not touched.
- **Stop case.** If the refreshed snapshot differs from the expected diff (the `/exercise-favorites` operations,
  `ExerciseFavoriteResponse`, `ownerId` / `archivedAtUtc`, plus the custom-exercise operations on `/exercises`), the
  change stops.

## Acceptance Criteria

- The snapshot diff is the expected one, committed on its own.
- The delta specs' scenarios pass:
  - core unit tests for the favorites repository (success and every error code) and its three use cases, and the
    exercise fixtures carrying `ownerId`;
  - `workout-view.test.tsx` with use cases faked through the container, for both roles.
- `workout.spec.ts` marks an exercise as a favorite, switches to "Favoritos" and adds it to the workout.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` pass from `front/`.

## Impact

- `core`: `openapi/v1.json`, `src/contract/generated` (regenerated), `domain/exercise-favorites`,
  `application/ports`, `application/use-cases/exercise-favorites`, `application/dependencies.ts`,
  `infrastructure/repositories`, and tests.
- `backoffice`:
  - `infrastructure/container.ts`;
  - `presentation/workouts/hooks/use-exercise-favorites.ts` (new);
  - `components/molecules/exercise-section-list.tsx` (new);
  - `components/organisms/exercise-catalog-dialog.tsx`;
  - `views/workout-view.tsx`;
  - `i18n/{es,en}/workouts.json`;
  - tests, test support and E2E fixtures.
- `app`: `infrastructure/container.ts`, its container test and test support; no UI.
- API: none. It depends on `add-exercise-favorites`, already served.
