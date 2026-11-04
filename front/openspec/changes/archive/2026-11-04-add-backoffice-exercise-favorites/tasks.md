# Tasks

## 1. Contract snapshot

- [x] 1.1 With the API running on its Debug API port 5199 (start it, or ask the user to), run `pnpm --filter @weight-tracker/core openapi:refresh` and then `openapi:generate`. Format the generated `api.ts` with Prettier if it lost its committed formatting. Verify that the diff of `core/openapi/v1.json` and `core/src/contract/generated` adds only:
  - nullable `ownerId` and `archivedAtUtc` on `ExerciseResponse`;
  - `ExerciseFavoriteResponse`;
  - `GET`, `PUT {exerciseId}` and `DELETE {exerciseId}` on `/exercise-favorites`;
  - the custom-exercise operations on `/exercises` and their schemas.

  Stop and report any other diff.

- [x] 1.2 Confirm nothing under `core/src/contract/generated` was edited by hand, and commit the snapshot as a step of its own. Verify with `pnpm --filter @weight-tracker/core typecheck`.

## 2. Core: exercise fixtures

- [x] 2.1 Add `ownerId` and `archivedAtUtc` to the exercise fixtures in `core/test/infrastructure/repositories/exercise-repository.test.ts` and `core/test/application/use-cases/exercises/get-exercises-use-case.test.ts`. Add one repository test showing a system exercise (`ownerId: null`) and a custom one (an id and an archive date) are returned unchanged, in AAA form with one Act. Verify with `pnpm --filter @weight-tracker/core test`.

## 3. Core: exercise favorites

- [x] 3.1 Add `core/src/domain/exercise-favorites/exercise-favorite.ts` (`ExerciseFavorite` alias of `ExerciseFavoriteResponse`) and export it from `core/src/domain/index.ts`. Verify with the core typecheck.
- [x] 3.2 Add `ExerciseFavoriteRepositoryInterface` (`getAll`, `mark(exerciseId)`, `unmark(exerciseId)`) in `core/src/application/ports/exercise-favorite-repository-interface.ts` and export it from `ports/index.ts`. Verify with the core typecheck.
- [x] 3.3 Add `ExerciseFavoriteRepository` in `core/src/infrastructure/repositories/exercise-favorite-repository.ts`, modelled on `exercise-repository.ts` and on `WorkoutRepository.removeExercise` for `DELETE`, and export it from `repositories/index.ts`. Verify that the new `core/test/infrastructure/repositories/exercise-favorite-repository.test.ts` passes. It mirrors `exercise-repository.test.ts` in AAA form and covers each method's method, path, no body and returned value, and 401, 404, 409, 500 and no response for each method.
- [x] 3.4 Add `GetExerciseFavoritesUseCase`, `MarkExerciseFavoriteUseCase` (input: exercise id) and `UnmarkExerciseFavoriteUseCase` (input: exercise id) in `core/src/application/use-cases/exercise-favorites/`, with an `index.ts` barrel exported from `use-cases/index.ts`. Verify that the three new tests under `core/test/application/use-cases/exercise-favorites/` pass. They mirror `get-exercises-use-case.test.ts` and cover delegation with a faked repository and the success and failure `Result`.
- [x] 3.5 Add the four keys (`exerciseFavoriteRepository`, `getExerciseFavoritesUseCase`, `markExerciseFavoriteUseCase`, `unmarkExerciseFavoriteUseCase`) to `core/src/application/dependencies.ts`. Verify that the core typecheck passes and that both fronts' typecheck now fails only on their containers and test supports.
- [x] 3.6 Review core against its mapped references (`AuthRepository` / `SignupUseCase` from `.stpr/template/`, and `exercise-repository.ts` / `get-exercises-use-case.ts`). Verify there are no differences beyond names, paths and methods, and nothing throws.

## 4. Wiring in both fronts

- [x] 4.1 Register the repository and the three use cases in `backoffice/src/infrastructure/container.ts`, add the keys to `backoffice/test/infrastructure/container.test.ts`, and add no-op fakes to `unusedDependencies` in `backoffice/test/support/render-with-providers.tsx`. Verify with `pnpm --filter backoffice typecheck` and the container test.
- [x] 4.2 Do the same in `app/src/infrastructure/container.ts`, `app/test/infrastructure/container.test.ts` and `app/test/support/render-with-providers.tsx`, with no app UI. Verify with `pnpm --filter app typecheck` and `pnpm --filter app test`.

## 5. Backoffice: state and copy

- [x] 5.1 Add the keys `workout.catalog.sections.{label,ALL,MINE,FAVORITES}` and `workout.catalog.favorite.{add,remove,loadError}` to `backoffice/src/i18n/{es,en}/workouts.json`, with the texts in `design.md`. Verify that the translation parity test passes.
- [x] 5.2 Add `backoffice/src/presentation/workouts/hooks/use-exercise-favorites.ts`. Its read follows `use-exercises.ts` (with the `generation` guard), and its toggle follows `use-remove-workout-exercise.ts` (with a set of pending ids). It changes `favoriteIds` only on success and holds `loadErrorCode` and `toggleErrorCode`. Verify with the backoffice typecheck and lint. Its behaviour is covered through the view tests in 6.5.

## 6. Backoffice: components and view

- [x] 6.1 Add the atom `backoffice/src/presentation/common/components/atoms/list-item-button.tsx`, wrapping MUI `ListItemButton` the same way `list-item.tsx` wraps `ListItem`. Verify with the backoffice typecheck and lint.
- [x] 6.2 Add the molecule `backoffice/src/presentation/workouts/components/molecules/exercise-section-list.tsx`, exporting `ExerciseSection`. It renders the sections as `List` > `ListItem` > `ListItemButton` (selected, optional disabled) > `ListItemIcon` + `ListItemText`, with an `aria-label`, imports atoms only, and has no i18n or use cases. Verify with the backoffice typecheck and lint.
- [x] 6.3 Extend `ExerciseCatalogDialog`:
  - the paper width becomes `{ md: "90vw" }`;
  - a flex layout puts the sidebar on the left on `md` and up and stacks it below `md`;
  - the sections are built with their icons, and "Favoritos" is disabled when the load failed;
  - the section condition is applied first in `visible`;
  - a star `IconButton` sits before "Elegir ejercicio", with its label, the disabled rules, and `warning` colour while marked;
  - `watermarkInsetRight` is widened;
  - the load-warning and toggle-error alerts are added.

  Verify with the backoffice typecheck and lint.

- [x] 6.4 Extend `workout-view.tsx`: call `useExerciseFavorites()` in `WorkoutEditor` beside `useExercises()` and pass it to `LoadedWorkout`, which holds `selectedSection` (default `"ALL"`) beside `selectedType`, calls `useCurrentUser()`, and passes the new props to the dialog. Verify with the backoffice typecheck.
- [x] 6.5 Extend `backoffice/test/presentation/workouts/views/workout-view.test.tsx` with one test per scenario of `specs/backoffice-workout-editor`, in AAA form with one Act and in `es` unless the test is about language:
  - setup: `catalogExercise(...)` gains `ownerId`, and the favorites use cases are faked through the container;
  - sections: order and default, "Mis ejercicios" with system, own and others' exercises (assert the others are hidden), "Favoritos" including a favorite missing from the catalog, and unmark while in "Favoritos";
  - filters: a section combined with a pill, and the section kept on reopening;
  - favorites read: exactly once, and the load failure keeping the catalog usable with stars and "Favoritos" disabled;
  - star: mark and unmark, no star on the workout list, in progress, `CONFLICT` and `NETWORK`;
  - English labels;
  - roles: one case with an `ADMIN` profile.

  Verify with `pnpm --filter backoffice test`.

- [x] 6.6 Review the backoffice against its mapped references: `list-item.tsx` for the atom, the existing workouts molecules for the molecule, `use-exercises.ts` and `use-remove-workout-exercise.ts` for the hook, and the type row change for the dialog. Verify that imports flow downward, only atoms import MUI widgets, there is no colour literal, and every string is in `es` and `en`.

## 7. Backoffice E2E

- [x] 7.1 Extend `backoffice/e2e/workout.spec.ts`:
  - catalog fixtures gain `ownerId`;
  - `page.route` answers `GET`, `PUT` and `DELETE` on `**/exercise-favorites**`;
  - add one flow that marks an exercise with "Añadir a favoritos", selects "Favoritos", sees only that exercise, and adds it to the workout.

  Verify with `pnpm test:e2e`.

## 8. Final verification

- [x] 8.1 Run the template drift check from `AGENTS.md`. Verify there is no `DRIFT:` line.
- [x] 8.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`. Verify that all pass.
