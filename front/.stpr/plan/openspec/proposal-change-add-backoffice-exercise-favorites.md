# Change Proposal: Exercise Favorites And Catalog Sections In The Backoffice

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-exercise-favorites/`.

## Change

The front wires the new `/exercise-favorites` endpoints through `core` (contract snapshot, domain alias, repository,
use cases) and uses them, together with the new `ownerId` on exercises, in the backoffice catalog dialog:

- a star toggle on every exercise card of the catalog to mark or unmark it as a favorite;
- a section sidebar on the left of the catalog dialog with three items: "Todos", "Mis ejercicios" and "Favoritos".

## Objective

The catalog grows with system exercises and, now, with exercises users create themselves. A trainer building a
workout keeps reaching for the same few exercises and for their own ones, and today has to find them by text or pills
every time. Favorites and the "Mis ejercicios" section give them a one-click way to narrow the catalog to what they use.

## Capabilities

- **Favorite toggle**: every exercise card in the catalog dialog has a star icon button among its actions.
  - Outlined star while the exercise is not a favorite, filled star while it is.
  - Clicking it calls `PUT /exercise-favorites/{exerciseId}` (mark) or `DELETE /exercise-favorites/{exerciseId}`
    (unmark). The star changes once the call succeeds; while the call is in flight the button is disabled.
  - On failure the star keeps its previous state and the dialog shows the error.
  - The button has an accessible label: "Añadir a favoritos" / "Quitar de favoritos".
- **Section sidebar**: a vertical list on the left of the catalog dialog, beside the filters and the exercise list.
  Each item is an icon and a name; exactly one is selected, "Todos" by default.
  - **Todos** -> every exercise from `GET /exercises`, as today.
  - **Mis ejercicios** -> only the exercises from `GET /exercises` the current user owns (`ownerId` equal to the
    current user's id; see Notes).
  - **Favoritos** -> only the exercises from `GET /exercises` whose id is in `GET /exercise-favorites`. A favorite
    whose exercise is not in the catalog is ignored.
  - The section combines with the text, type, body-part and muscle filters: the section narrows the catalog first,
    and the filters apply on top. The pill rows are computed as today.
  - An empty section shows the existing empty-list message.
- **Favorites load with the catalog**: `GET /exercise-favorites` is called when the workout screen loads its
  catalog. If it fails, the catalog still works; the stars and the "Favoritos" section are unavailable and the
  dialog shows the error.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both use the star toggle and the sidebar the same way;
  each only ever sees and changes their own favorites.
- app -> lands here: no -> roles: `USER` -> no UI change. The new use cases and repository live in `core` but are not
  registered in the app's `container.ts`. The app keeps compiling: the regenerated `Exercise` alias gains fields, and
  the app builds no exercises in its code or tests.

No role-dependent UI.

## Classification

- Contract snapshot -> `core/openapi/v1.json` and `core/src/contract/generated` (refreshed and regenerated, never
  hand-edited).
- `Exercise` -> entity type (alias of `ExerciseResponse`, unchanged file) -> gains `ownerId` and `archivedAtUtc` from
  the regenerated schema.
- `ExerciseFavorite` -> entity type (alias of `ExerciseFavoriteResponse`) -> `core/src/domain/exercise-favorites`.
- `ExerciseFavoriteRepositoryInterface` -> repository contract -> `core/src/application/ports` -> `getAll`, `mark`,
  `unmark`.
- `ExerciseFavoriteRepository` -> repository implementation -> `core/src/infrastructure/repositories`.
- `GetExerciseFavoritesUseCase`, `MarkExerciseFavoriteUseCase`, `UnmarkExerciseFavoriteUseCase` -> use cases ->
  `core/src/application/use-cases/exercise-favorites`.
- Wiring -> `backoffice/src/infrastructure/container.ts` -> the repository and the three use cases.
- `ExerciseSectionList` (sidebar) -> molecule -> `backoffice/src/presentation/workouts/components/molecules`, built from
  the existing `List`, `ListItem`, `ListItemIcon` and `ListItemText` atoms; options, selected value and `onChange` by
  props, no use cases.
- `ExerciseCatalogDialog` -> organism (extended) -> the sidebar, the section filter, the star action per card, and a
  wider dialog on `md` and up.
- `ExerciseListItem` -> molecule (unchanged) -> the star goes in its existing `actions` slot.
- Favorites state -> state hook (new, e.g. `useExerciseFavorites`) ->
  `backoffice/src/presentation/workouts/hooks` -> loads favorites, exposes the id set, `toggle`, pending ids and the
  last error.
- `WorkoutView` / `LoadedWorkout` -> view (extended) -> holds `selectedSection` beside the other catalog filters and
  passes favorites to the dialog.
- Copy -> `backoffice/src/i18n/{es,en}/workouts.json`.

The HTTP error mapper does not change.

## Canonical Template Mapping

- Snapshot refresh -> the refresh in archived change `show-exercise-type` -> the same steps: refresh, generate, and
  commit the snapshot on its own.
- `ExerciseFavorite` alias -> `core/src/domain/exercises/exercise.ts` -> a one-line alias over the generated schema.
- Repository contract and implementation -> `AuthRepositoryInterface` / `AuthRepository` (skeleton) and, for `PUT`
  and `DELETE` with a path parameter, `core/src/infrastructure/repositories/workout-repository.ts` -> per-area port
  over `HttpClientInterface`, errors through the mapper.
- Use cases -> `SignupUseCase` (skeleton) and `core/src/application/use-cases/exercises/get-exercises-use-case.ts` ->
  one operation, delegates to the repository, returns `Result`.
- Sidebar molecule -> the list atoms as composed in the backoffice navigation drawer (archived change
  `add-backoffice-navigation-drawer`) -> icon + text items with a selected state, same front.
- Section filter -> the body-part filter in `components/organisms/exercise-catalog-dialog.tsx` -> the same filtering
  flow, applied before the pill filters.
- Star action -> the existing icon-button actions of `ExerciseListItem` in the catalog -> same slot and atom.
- Favorites state hook -> the closest existing data hook in `backoffice/src/presentation/workouts/hooks` -> use cases
  resolved through the container, `Result` to state.
- Tests:
  - `core/test/infrastructure/repositories/exercise-repository.test.ts` -> new `exercise-favorite-repository.test.ts`;
  - `core/test/application/use-cases/exercises/get-exercises-use-case.test.ts` -> the three new use-case tests;
  - `backoffice/test/presentation/workouts/views/workout-view.test.tsx` (extended);
  - `backoffice/e2e/workout.spec.ts` (extended).

`.stpr/template/README.md` lists the atomic UI references as `pending`; this change adds one molecule, following the
existing molecules of the same front.

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> unchanged entry points.
  - The catalog dialog gains the left sidebar and a star on each card, and is wider on `md` and up to fit the sidebar.
  - There is no new route and no new navigation.

## API Contract

- `GET /exercises` -> in `core/openapi/v1.json`: **no** (`ExerciseResponse` lacks `ownerId` and `archivedAtUtc` in the
  current snapshot) -> `ExerciseResponse.ownerId: string | null` (`null` for a system exercise),
  `ExerciseResponse.archivedAtUtc: string | null`.
- `GET /exercise-favorites` -> in snapshot: **no** -> `ExerciseFavoriteResponse[]` (`exerciseId`, `createdAtUtc`),
  newest first. `401` without a valid session.
- `PUT /exercise-favorites/{exerciseId}` -> in snapshot: **no** -> no body; `200` with `ExerciseFavoriteResponse`.
  Idempotent. `404` unknown or invisible exercise, `409` archived exercise.
- `DELETE /exercise-favorites/{exerciseId}` -> in snapshot: **no** -> no body; `204`. Idempotent. `404` unknown or
  invisible exercise.

The API already serves them: `ExerciseFavoritesController` and `ExerciseResponse` in
`api/src/WeightTracker.Api/Endpoints/`, from the API change `add-exercise-favorites` (and the custom-exercises change
that added `ownerId`). This change **refreshes the snapshot**:

1. With the API running (Debug API, port 5199), run `pnpm --filter @weight-tracker/core openapi:refresh`, then
   `openapi:generate`.
2. Review the diff. Expected: the three `/exercise-favorites` operations, `ExerciseFavoriteResponse`, and `ownerId` /
   `archivedAtUtc` on `ExerciseResponse`, plus whatever the custom-exercises API change added to `/exercises`
   (create, update, archive). Anything else stops the change.
3. Commit them as a step of their own, before the code that uses them.

`GET /exercises` sends the session token when there is one (`FetchHttpClient` adds it), which is what makes the API
include the caller's own exercises. No hand-written domain type.

## Held State

- Favorites state hook -> the set of favorite exercise ids, the ids with a call in flight, the last error -> loaded with
  the catalog, changed by the star toggle on success -> screen exit. Kept when the catalog dialog closes and opens
  again.
- `LoadedWorkout` `selectedSection` -> `ALL`, `MINE` or `FAVORITES` -> the sidebar -> screen exit; kept across dialog
  open/close like the other filters.

## Forms And Structural Validation

None.

## Errors And Copy

- Favorites fail to load -> `DomainError` from `GET /exercise-favorites` (`NETWORK`, `UNEXPECTED`, `UNAUTHORIZED`) ->
  an alert in the dialog; stars and "Favoritos" disabled; the rest of the catalog works.
- Mark an archived exercise -> `CONFLICT` -> alert with the existing conflict copy; the star stays outlined.
- Mark or unmark an unknown or invisible exercise -> `NOT_FOUND` -> alert with the existing not-found copy.
- Network or unexpected failure on toggle -> `NETWORK` / `UNEXPECTED` -> alert with the existing copy.
- No new error code; the HTTP error mapper is not touched. Existing `errors.*` keys are reused.
- New copy (`workouts` namespace):
  - `workout.catalog.sections.ALL` -> "Todos" -> "All"
  - `workout.catalog.sections.MINE` -> "Mis ejercicios" -> "My exercises"
  - `workout.catalog.sections.FAVORITES` -> "Favoritos" -> "Favorites"
  - `workout.catalog.sections.label` (accessible name of the sidebar) -> "Secciones" -> "Sections"
  - `workout.catalog.favorite.add` -> "Añadir a favoritos" -> "Add to favorites"
  - `workout.catalog.favorite.remove` -> "Quitar de favoritos" -> "Remove from favorites"
  - `workout.catalog.favorite.loadError` -> "No se pudieron cargar los favoritos" -> "Favorites could not be loaded"

## Platform Capabilities And Dependencies

None. Icons come from `@mui/icons-material`, already a backoffice dependency (e.g. `AppsOutlined` for "Todos",
`PersonOutlined` for "Mis ejercicios", `StarOutlined` / `Star` for "Favoritos" and the toggle; final choice in the
design).

## Out Of Scope

- A dialog or form to create, edit or archive user exercises.
- Any UI in the app; the new use cases are not registered in the app's container.
- A star on the exercises already added to the workout (the workout exercise list) or in the configuration dialog;
  only the cards of the catalog dialog get it.
- Ordering by favorite date, or a favorite indicator outside the catalog.
- Showing archived exercises differently or hiding them; the catalog keeps today's behaviour for what
  `GET /exercises` returns.
- Optimistic updates of the star.
- Any change to the workout endpoints or to the existing type, body-part and muscle filters.

## Notes

- **"Mis ejercicios" means the exercises the current user owns.** `GET /exercises` returns system exercises, the
  caller's own, **and custom exercises used in workouts the caller can see**, which may belong to someone else (for
  example an athlete's or another trainer's). So the filter is `ownerId === currentUser.id`, read from the existing
  `CurrentUserProvider`, not `ownerId !== null`. Custom exercises owned by others appear only under "Todos".
- **Sidebar layout and dialog width.** On `md` and up the sidebar is a fixed-width column on the left of the dialog
  content, and the filters and list sit to its right. The dialog grows to make room for it: its paper width on `md`
  and up goes from `75vw` to a wider value (around `90vw`, exact value settled in the design), so the filters and the
  exercise grid keep roughly today's width. On small widths the dialog stays as today, and the exact fallback for the
  sidebar (stacked above, or a horizontal row) is settled in the design; no new breakpoints beyond the theme's.
- The section filter is a pure derivation in the dialog from the catalog, the favorite ids and the current user id; no
  extra API call per section.
- `useExerciseFavorites.toggle(id)` calls mark or unmark depending on the current set, and updates the set only on
  `ok`. `PUT` returning `200` and `DELETE` returning `204` are both treated as success.
- Verification tiers:
  - core unit:
    - `ExerciseFavoriteRepository`: success for `getAll`, `mark` and `unmark` (method, path, mapped response) and every
      error code through the mapper;
    - each use case delegates to the repository and returns its `Result`;
    - the exercise repository and use-case fixtures gain `ownerId` and `archivedAtUtc`.
  - backoffice component (`workout-view.test.tsx`, use cases faked through the container), for `ADMIN` and `TRAINER`:
    - the sidebar shows the three items with "Todos" selected by default;
    - "Mis ejercicios" shows only the current user's exercises and hides system ones and others' custom ones;
    - "Favoritos" shows only favorites, ignores favorites missing from the catalog, and shows the empty message;
    - a section combines with the text and pill filters, and is kept when the dialog opens again;
    - the star reflects the favorite state; marking and unmarking call the right use case and flip the star;
    - a failed toggle keeps the star and shows the error (`CONFLICT`, `NOT_FOUND`, `NETWORK`);
    - favorites failing to load keep the catalog usable and show the alert;
    - English labels.
  - backoffice E2E (`workout.spec.ts`, API intercepted with `page.route`): catalog fixtures gain `ownerId`; one flow
    marks an exercise as a favorite, switches to "Favoritos" and adds it to the workout.
- No deviations from the mapped references are allowed.
