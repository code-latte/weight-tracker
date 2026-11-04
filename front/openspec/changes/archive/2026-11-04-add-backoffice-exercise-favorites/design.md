# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. This is the current state that shapes the
approach:

- **Contract.** `ExerciseResponse` in `core/openapi/v1.json` has no `ownerId` or `archivedAtUtc`, and there is no
  `/exercise-favorites` path. The API serves all of them (`ExerciseFavoritesController`, `ExerciseResponse.cs`).
  `GET /exercises` is anonymous with an optional caller: with a token it adds the caller's own exercises and the
  custom ones used in workouts the caller can see.
- **Token.** `FetchHttpClient` attaches the stored access token to every request when one exists, so `GET /exercises`
  already sends it. `useExercises` already wraps the read in `runWithSession`.
- **Wiring.** `Dependencies` (`core/src/application/dependencies.ts`) is one interface, and both fronts'
  `container.ts` are `new Container<Dependencies>(...)`. Each front's `test/support/render-with-providers.tsx` holds
  `unusedDependencies: Registrations<Dependencies>`, and each `test/infrastructure/container.test.ts` lists every key.
- **Catalog dialog.** `ExerciseCatalogDialog` (organism) receives `available` exercises (catalog minus the workout's)
  and every filter value from `LoadedWorkout`, which holds them in `useState` so they survive a reopening. Its paper
  width is `{ md: "75vw" }`. Each card is an `ExerciseListItem` whose `actions` slot holds the "Elegir ejercicio"
  `IconButton`, with `watermarkInsetRight={2.5}` so the type icon does not sit under it.
- **Current user.** `useCurrentUser()` exposes the signed-in `profile` (with `id`) for every signed-in screen.
- **Error copy.** Views show `errors:<code>` from the `common` errors namespace in an `Alert` atom.
- **List atoms.** `List`, `ListItem`, `ListItemIcon` and `ListItemText` exist as atoms. There is no `ListItemButton`
  atom; the navigation drawer organism imports MUI's `ListItemButton` directly.

## Goals / Non-Goals

**Goals:**

- One read of favorites per screen opening. Every later change to the set comes from the mark or unmark answers.
- Section filtering is a pure derivation inside the dialog, with no request.
- No new error code and no change to the HTTP error mapper.

**Non-Goals:**

- Fixing the navigation drawer's direct MUI import. It is noted, not changed.
- Any app presentation.
- Sorting by favorite date, or a "favorite" flag on `Exercise`.

## Decisions

### Contract first, committed alone

With the API running on its Debug API port 5199, run `pnpm --filter @weight-tracker/core openapi:refresh`, then
`openapi:generate`. Format `api.ts` with Prettier if it loses the committed formatting, as earlier refreshes did.

- Expected diff: `ownerId` and `archivedAtUtc` on `ExerciseResponse` (both nullable); `ExerciseFavoriteResponse`
  (`exerciseId`, `createdAtUtc`); `GET`, `PUT {exerciseId}` and `DELETE {exerciseId}` on `/exercise-favorites`. The
  custom-exercise operations on `/exercises` (create, update, archive) and their request schemas are expected too,
  and are not used here. Anything else stops the change.
- If `ownerId` is generated as optional (`ownerId?: string | null`) rather than required, keep it as generated. The
  filter compares with `===` and handles both.

### core: a domain area of its own, `exercise-favorites`

The API models favorites as their own area (`ExerciseFavorites`), with their own controller and lifecycle. The front
mirrors that rather than adding methods to `ExerciseRepositoryInterface`:

| Piece                                 | Path                                                                   | Shape                                                                                                                                                                                |
| ------------------------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `ExerciseFavorite`                    | `core/src/domain/exercise-favorites/exercise-favorite.ts`              | `components["schemas"]["ExerciseFavoriteResponse"]`, exported from `domain/index.ts`                                                                                                 |
| `ExerciseFavoriteRepositoryInterface` | `core/src/application/ports/exercise-favorite-repository-interface.ts` | `getAll(): Result<ExerciseFavorite[]>`, `mark(exerciseId: string): Result<ExerciseFavorite>`, `unmark(exerciseId: string): Result<void>`                                             |
| `ExerciseFavoriteRepository`          | `core/src/infrastructure/repositories/exercise-favorite-repository.ts` | `GET /exercise-favorites`, `PUT /exercise-favorites/${id}`, `DELETE /exercise-favorites/${id}`, each in `try` / `fail(mapHttpError(error))`, like `WorkoutRepository.removeExercise` |
| `GetExerciseFavoritesUseCase`         | `core/src/application/use-cases/exercise-favorites/`                   | `UseCaseInterface<void, ExerciseFavorite[]>`                                                                                                                                         |
| `MarkExerciseFavoriteUseCase`         | same                                                                   | `UseCaseInterface<string, ExerciseFavorite>`, the input is the exercise id                                                                                                           |
| `UnmarkExerciseFavoriteUseCase`       | same                                                                   | `UseCaseInterface<string, void>`                                                                                                                                                     |

The use cases only delegate, like `GetExercisesUseCase`. A plain `string` input follows `activateUserUseCase` and
`revokeAthleteInvitationUseCase`, which also take one id. Barrels export them: `ports/index.ts`,
`use-cases/exercise-favorites/index.ts`, `use-cases/index.ts` and `repositories/index.ts`.

**Alternative considered:** extending `ExerciseRepositoryInterface`. Rejected because the API keeps favorites as a
separate area, and one repository per area is the template's rule.

### `Dependencies` and both containers

New keys in `Dependencies`:

- `exerciseFavoriteRepository: ExerciseFavoriteRepositoryInterface`
- `getExerciseFavoritesUseCase: UseCaseInterface<void, ExerciseFavorite[]>`
- `markExerciseFavoriteUseCase: UseCaseInterface<string, ExerciseFavorite>`
- `unmarkExerciseFavoriteUseCase: UseCaseInterface<string, void>`

Both `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts` register them:
`exerciseFavoriteRepository: (c) => new ExerciseFavoriteRepository(c.resolve("httpClient"))`, and each use case
over `c.resolve("exerciseFavoriteRepository")`. The app has to do this so it compiles (see the proposal's
Constraints). Both `unusedDependencies` and both container tests gain the keys.

### Backoffice state: `useExerciseFavorites`

A new state hook in `backoffice/src/presentation/workouts/hooks/use-exercise-favorites.ts`, held by the screen
only, so it resets on screen exit:

```ts
interface ExerciseFavoritesState {
  status: "loading" | "loaded" | "failed";
  favoriteIds: ReadonlySet<string>;
  loadErrorCode: DomainErrorCode | null;
  /** The exercises whose mark or unmark is in progress. */
  pendingIds: ReadonlySet<string>;
  toggleErrorCode: DomainErrorCode | null;
  toggle: (exerciseId: string) => Promise<void>;
}
```

- **Read.** On mount it runs `getExerciseFavoritesUseCase` through `runWithSession`. It uses the `generation`
  ref guard from `useExercises`, so an answer that arrives after unmount is ignored. It keeps only the ids, because
  the screen never shows `createdAtUtc`.
- **Toggle.** It chooses mark or unmark from `favoriteIds`. It adds the id to `pendingIds` and clears
  `toggleErrorCode`. On `ok` it adds or removes the id; on failure it keeps the set and stores the code. Then it
  removes the id from `pendingIds`. This follows `useRemoveWorkoutExercise`, with a set of pending ids instead of one
  id, because each star is independent.
- **No retry.** A failed read stays failed until the screen is opened again. The catalog, not favorites, decides
  whether the screen can be used.

**Alternative considered:** optimistic toggling with rollback. Rejected because the brief asks for the star to change
on success only, and it keeps the failure path simple.

### Sections: a value held by the view, a derivation in the dialog

- `type ExerciseSection = "ALL" | "MINE" | "FAVORITES"`, exported from the molecule.
- `LoadedWorkout` holds `const [selectedSection, setSelectedSection] = useState<ExerciseSection>("ALL")` beside
  `selectedType`, so it survives a reopening and resets on screen exit.
- `WorkoutEditor` calls `useExerciseFavorites()` beside `useWorkout` and `useExercises`, so the three reads go out
  together, and hands the state to `LoadedWorkout` as a prop. A failed favorites read never blocks the screen.
  `LoadedWorkout` calls `useCurrentUser()` and passes the dialog `favoriteIds`, `favoritesStatus`,
  `pendingFavoriteIds`, `favoriteErrorCode`, `onToggleFavorite`, `currentUserId`, `selectedSection` and
  `onSectionChange`. The load error is shown from `favoritesStatus`.
- In the dialog, `visible` gains one first condition:
  - `ALL`: true;
  - `MINE`: `exercise.ownerId === currentUserId`;
  - `FAVORITES`: `favoriteIds.has(exercise.id)`.

  Because the dialog already receives `available`, favorites already in the workout are hidden like any other
  exercise, and favorites missing from the catalog never show up.

- The pill rows keep being computed from `exercises` (all available), as today. Recomputing them per section is out of
  scope.

### Components and atomic levels

Imports flow downward, and only atoms import MUI widgets.

| Component                       | Level                | Folder                                                                                | Change                                                                                                                                                                                                                                                 |
| ------------------------------- | -------------------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `ListItemButton`                | atom (new)           | `backoffice/src/presentation/common/components/atoms/list-item-button.tsx`            | Wraps MUI `ListItemButton`, same shape as `list-item.tsx`. It is needed because the sidebar items are selectable and the molecule may not import MUI.                                                                                                  |
| `ExerciseSectionList`           | molecule (new)       | `backoffice/src/presentation/workouts/components/molecules/exercise-section-list.tsx` | Props: `sections: { value, label, icon: ReactNode, disabled? }[]`, `selected`, `onChange`, `aria-label`. It renders `List` > `ListItem disablePadding` > `ListItemButton selected` > `ListItemIcon` + `ListItemText`. No hooks besides props, no i18n. |
| `ExerciseListItem`              | molecule (unchanged) | existing                                                                              | The star is a second `IconButton` in `actions`.                                                                                                                                                                                                        |
| `ExerciseCatalogDialog`         | organism (extended)  | existing                                                                              | Adds the sidebar, the section condition, the star per card, the two alerts, and the width.                                                                                                                                                             |
| `WorkoutView` / `LoadedWorkout` | view (extended)      | existing                                                                              | Holds the section and calls the favorites hook.                                                                                                                                                                                                        |

**Icons** come from `@mui/icons-material`, already a dependency: `AppsOutlined` (Todos), `PersonOutlined` (Mis
ejercicios), `StarBorder` for Favoritos and an unmarked star (`StarOutline` does not exist in this version of
`@mui/icons-material`), and `Star` for a marked star. The organism builds the
`sections` array with translated labels and these icons. The "Favoritos" item gets `disabled` while
`favoritesStatus === "failed"`.

**Star button:**

- `IconButton` with `aria-label` `workout.catalog.favorite.add` or `.remove`. There is no `aria-pressed`: the label
  already changes with the state, and a toggle that does both is announced twice over.
- `disabled` while the favorites are not `loaded` or the id is in `pendingIds`.
- `color="warning"` while it is marked, a theme token with no literal.
- `watermarkInsetRight` grows from `2.5` to about `5`, so the type icon clears two buttons. The exact value is
  tuned visually.

### Dialog layout and width

- The paper width goes from `{ md: "75vw" }` to `{ md: "90vw" }`.
- Inside `DialogContent`, a `Box` with `display: { md: "flex" }` and `gap: 3`:
  - the sidebar is in a column of fixed width (about 200px, `flexShrink: 0`);
  - the existing filters and list sit in a `Box` with `flex: 1, minWidth: 0`.
- Below `md`, the `Box` is block, so the sidebar stacks above the filters as a full-width list. No new breakpoint.
- `EXERCISE_GRID_COLUMNS` and `EXERCISE_LIST_MAX_HEIGHT` are unchanged. The extra 15vw goes to the sidebar, so the
  grid keeps about its width.

### Errors and copy

Both alerts sit at the top of the right-hand column, use the `Alert` atom, and read `errors:<code>` where a code is
shown:

- **Load failure:** `severity="warning"`, text `workout.catalog.favorite.loadError`. It is shown while
  `favoritesStatus === "failed"`.
- **Toggle failure:** `severity="error"`, text `errors:<toggleErrorCode>`. It is cleared by the next toggle.

| Code                    | Where                                                     | es / en                     |
| ----------------------- | --------------------------------------------------------- | --------------------------- |
| `CONFLICT`              | mark of an archived exercise                              | existing `errors:CONFLICT`  |
| `NOT_FOUND`             | mark or unmark of an unknown or invisible exercise        | existing `errors:NOT_FOUND` |
| `UNAUTHORIZED`          | session lost; `runWithSession` handles refresh and logout | existing                    |
| `NETWORK`, `UNEXPECTED` | any of the three calls                                    | existing                    |

New keys in `backoffice/src/i18n/{es,en}/workouts.json` (es / en):

- `workout.catalog.sections.label`: Secciones / Sections
- `workout.catalog.sections.ALL`: Todos / All
- `workout.catalog.sections.MINE`: Mis ejercicios / My exercises
- `workout.catalog.sections.FAVORITES`: Favoritos / Favorites
- `workout.catalog.favorite.add`: Añadir a favoritos / Add to favorites
- `workout.catalog.favorite.remove`: Quitar de favoritos / Remove from favorites
- `workout.catalog.favorite.loadError`: No se pudieron cargar los favoritos / Favorites could not be loaded

### Tests

These follow the templates, with no change to test helpers or patterns beyond the new `Dependencies` keys:

- **core:**
  - `exercise-favorite-repository.test.ts` follows `exercise-repository.test.ts`. It covers success for each method
    (method, path, no body, mapped value) and each method through 401, 404, 409, 500 and network.
  - Three use-case tests follow `get-exercises-use-case.test.ts`.
  - The exercise fixtures gain `ownerId` and `archivedAtUtc`.
- **backoffice component** (`workout-view.test.tsx`):
  - `catalogExercise(...)` gains `ownerId`.
  - The favorites use cases are faked through the container.
  - The signed-in profile is `completeProfile` (TRAINER). One case per role overrides it with `ADMIN`, because the UI
    is the same for both.
  - The scenarios are the ones in `specs/backoffice-workout-editor`.
- **backoffice E2E** (`workout.spec.ts`): the catalog fixtures gain `ownerId`, and `page.route` answers
  `GET`, `PUT` and `DELETE` on `**/exercise-favorites**`. The flow marks an exercise, selects "Favoritos" and adds it.
- **Fakes:** both `container.test.ts` and both `render-with-providers.tsx` gain the four keys. The app needs no other
  test.

## Risks / Trade-offs

- [The snapshot brings the custom-exercise operations too] → They are expected in the diff and left unused. No use case
  is added for them here.
- [`ownerId === currentUserId` while the profile is still loading] → The workout screen is reached only once the
  profile is loaded, so `profile.id` is set. If `profile` is `null`, "Mis ejercicios" lists nothing, which is the
  safe side.
- [Two buttons in a card's first row on narrow cards] → Both are compact `IconButton`s. The longer name wraps as
  today, and the watermark inset is widened.
- [Favorites read in parallel with the catalog adds a request on opening] → It is one small request with no
  dependency, and it does not block the screen.
- [`ListItemButton` atom while the drawer bypasses it] → The new atom is the compliant path. Moving the drawer to it is
  left for a later cleanup, so this change stays scoped.

## Migration Plan

No data migration. The order is:

1. Commit the snapshot.
2. Add the core pieces with their tests.
3. Wire both containers.
4. Add the backoffice atom, molecule, organism and view, and the copy.
5. Add the E2E flow.

Rolling back means reverting the change. The API endpoints keep working without the front.
