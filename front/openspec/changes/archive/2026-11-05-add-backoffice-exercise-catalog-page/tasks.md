# Tasks

## 1. Core: create an exercise

- [x] 1.1 Add `CreateExerciseRequest` (alias of the generated `ExerciseRequest`) to `core/src/domain/exercises/exercise.ts` and export it from `core/src/domain/index.ts`. Verify with the core typecheck, and that `core/openapi/v1.json` and `core/src/contract/generated` are untouched (`git diff --stat` shows neither).
- [x] 1.2 Add `create(request)` to `ExerciseRepositoryInterface` and implement it in `ExerciseRepository` (`POST /exercises` with the request as the body, `ok(response)`, `fail(mapHttpError(error))` in a `try`). Verify with the extended `core/test/infrastructure/repositories/exercise-repository.test.ts`, in AAA form with one Act per test: method, path and body, the returned exercise unchanged, no `videoUrl` in the body when the request has none, and 400, 401, 403, 500 and no response.
- [x] 1.3 Add `CreateExerciseUseCase` in `core/src/application/use-cases/exercises/create-exercise-use-case.ts`, modelled on `create-workout-use-case.ts`, export it from the folder's `index.ts`, and add `createExerciseUseCase` to `core/src/application/dependencies.ts`. Verify with `core/test/application/use-cases/exercises/create-exercise-use-case.test.ts`, which mirrors `get-exercises-use-case.test.ts` (delegation with a faked repository, the success `Result` and a failure `Result`), and that both fronts' typecheck now fails only on their containers and test supports.
- [x] 1.4 Add `core/src/application/validation/exercise-rules.ts` (`EXERCISE_NAME_MIN_LENGTH` 3, `EXERCISE_NAME_MAX_LENGTH` 150, `EXERCISE_VIDEO_URL_MAX_LENGTH` 500, `validateExerciseForm`), modelled on `workout-rules.ts`, and export it from `validation/index.ts`. Verify with `core/test/application/validation/exercise-rules.test.ts` covering the valid form, an empty and a spaces-only name, names of 2 and 3 and 150 and 151 characters, the missing muscle and type, the links "ftp://example.com/video", "example", "https://example", "https://exa mple.com", a link over 500 characters and "https://www.youtube.com/watch?v=abc". Run `pnpm --filter @weight-tracker/core test`.
- [x] 1.5 Review core against its mapped references (`create-workout-use-case.ts`, `WorkoutRepository` create and `workout-rules.ts`). Verify there are no differences beyond names, paths and methods, and that nothing throws.

## 2. Wiring in both fronts

- [x] 2.1 Register `createExerciseUseCase` in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`, add the key to both `test/infrastructure/container.test.ts`, and add a no-op fake to `unusedDependencies` in both `test/support/render-with-providers.tsx` (the backoffice's overridable like the others). Verify with both fronts' typecheck and container tests.

## 3. Backoffice: extract the catalog browser (no new behaviour)

- [x] 3.1 Add `presentation/workouts/catalog.ts` with `singleMuscleBodyPartsOf(exercises)` (the computation `LoadedWorkout` does today) and use it in `workout-view.tsx`. Verify that `workout-view.test.tsx` passes unchanged.
- [x] 3.2 Add `useExerciseCatalogFilters` in `presentation/workouts/hooks` (section, text, type, body part, muscle and their toggles, clearing the muscle when the body part changes) and use it in `LoadedWorkout` in place of its five `useState` calls. Verify that `workout-view.test.tsx` passes unchanged.
- [x] 3.3 Add the `ExerciseCatalogBrowser` organism with the content of `ExerciseCatalogDialog` (sidebar, alerts, filters, cards with the star), an optional `onChoose` (the plus button and `watermarkInsetRight={5}` only when present, else `2.5`) and a `contained` flag (fills its parent and scrolls the list inside, or flows with the page; the catalog page ends up using `contained` too). Reduce `ExerciseCatalogDialog` to the `Dialog`, its title and Cerrar, the `90vw` and `min(85vh, 860px)` paper, and the browser with `onChoose` and `contained`. Verify that `workout-view.test.tsx` passes unchanged and that `pnpm --filter backoffice test:e2e` still passes its catalog flows (the 90vw and the same-height tests included). Stop and report if either needs an edit.
- [x] 3.4 Review the extraction: only atoms import MUI widgets, imports flow downward, no behaviour or copy changed. Verify with `pnpm --filter backoffice typecheck` and lint.

## 4. Backoffice: the creating state

- [x] 4.1 Add `add(exercise)` to `useExercises`, appending to its held list. Verify with a new case in the hook's tests (or in the view test if the hook has none): the added exercise is listed, and a later `reload` replaces the list with the API's.
- [x] 4.2 Add `EXERCISE_MUSCLES` beside `EXERCISE_TYPES` in `use-exercise-labels.ts`, typed `NonNullable<CreateExerciseRequest["muscle"]>[]`, listing the 17 muscles the contract declares. Verify with the typecheck, and that every code has a label under `muscles` in both languages (the existing label coverage or a new case).
- [x] 4.3 Add `useCreateExercise` in `presentation/workouts/hooks/use-create-exercise.ts`, modelled on `use-create-workout.ts` (`creating`, `errorCode`, `create` resolving to the exercise or `null`, `reset`, through `runWithSession`). Verify with the typecheck and the dialog tests below.

## 5. Backoffice: copy

- [x] 5.1 Add the `exerciseCatalog` keys to `backoffice/src/i18n/es/workouts.json` and `en/workouts.json`: `title`, `add`, `create.title`, `create.fields.{name,muscle,type,videoUrl}`, `create.actions.{cancel,create}` and `create.validation.{required,minLength,maxLength,url}`. Verify with the translation parity test.

## 6. Backoffice: the add-exercise dialog

- [x] 6.1 Add the `CreateExerciseDialog` organism in `components/organisms/create-exercise-dialog.tsx`, modelled on `create-workout-dialog.tsx`: the form inside the dialog, `Nombre`, `Músculo` and `Tipo` (select with the `MenuItem` atom; the muscles grouped by body part under unselectable `ListSubheader` headers written "— Brazos —", groups and muscles sorted by label) and `Enlace de vídeo`, `validateExerciseForm` on submit, the request built with the trimmed values, the link only when typed and `languageCode` from `i18n.language`, the error `Alert`, disabled controls and ignored dismissal while creating, and `onCreated(exercise)` on success. Verify with the typecheck and lint.

## 7. Backoffice: the page, route and menu

- [x] 7.1 Add `ExerciseCatalogView` in `presentation/workouts/views/exercise-catalog-view.tsx`, modelled on `assigned-plans-view.tsx`: `AppShell`, heading, skeleton while the catalog and the current user load, `LoadError` with retry, the browser with the whole catalog, no `onChoose`, `contained` inside a box of `calc(100vh - 220px)` (at least 480px) so only the list scrolls, the favorites never blocking, the "Añadir ejercicio" button for a `TRAINER` only, and the `CreateExerciseDialog` whose `onCreated` appends with `add` and closes. Verify with the typecheck.
- [x] 7.2 Add the route `backoffice/src/app/exercises/page.tsx` (`export default ExerciseCatalogView;`). Verify that it holds routing only and that `pnpm --filter backoffice build` produces the static export.
- [x] 7.3 Give the "Catálogo de ejercicios" item of `navigation-drawer.tsx` the `href` `/exercises`. Verify that it is selected on `/exercises` and on no other screen, and update the drawer's existing test for "Items without a screen do nothing" if it names this item.

## 8. Backoffice: tests

- [x] 8.1 Add `backoffice/test/presentation/workouts/views/exercise-catalog-view.test.tsx`, modelled on `assigned-plans-view.test.tsx` and the setup of `workout-view.test.tsx`, with use cases faked through the container. Cover the "Exercise catalog screen lists the catalog", "No exercise can be chosen" and "Favorites are managed" scenarios for both roles, asserting what is hidden (no "Elegir ejercicio" for either role, no "Añadir ejercicio" for an `ADMIN`). Verify that it passes.
- [x] 8.2 In the same file, cover the "Only a trainer can add an exercise", "The new exercise form is checked before sending", "Crear adds the exercise", "A refused creation is shown", "The new exercise dialog can be dismissed" and "The exercise catalog screen is translated" scenarios: the request sent, the exercise under "Todos" and "Mis ejercicios" with no second catalog read, disabled controls while in progress, `VALIDATION` and `NETWORK` refusals keeping the values, reopening empty, and English. Verify that it passes, and that breaking the owner or the `add` call makes it fail (restore the file afterwards).
- [x] 8.3 Add the backoffice E2E flows (extending `workout.spec.ts` mocks or a new `exercise-catalog.spec.ts` that shares them): open the page from the drawer, create an exercise and find it under "Mis ejercicios", and mark a favorite from the page. The mock answers `POST /exercises` with an exercise owned by the signed-in user and records the body. Verify with `pnpm test:e2e`.

## 9. Final verification

- [x] 9.1 Check the template has not drifted with the loop in `AGENTS.md`. Verify that it prints nothing.
- [x] 9.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`. Verify that all pass.

## 10. Finder

- [x] 10.1 Change the catalog finder to match words: split the text, keep the exercises whose names contain every word in any order (case, accent and space insensitive), looking in the English name only with English selected, else in the name shown plus the English name (`searchTextOf` in `useExerciseLabels`, used by `ExerciseCatalogBrowser`). Verify with the `searchTextOf` hook tests and the finder cases of `exercise-catalog-view.test.tsx` ("Curl bíceps", "Curl Polea", any order, partial word, "Squat" from Spanish, English only), and that `workout-view.test.tsx` still passes.

## 11. English name and video

- [x] 11.1 Show the English name under the name on the catalog cards, in the page and the selector, when the language is not English: `englishNameOf` in `useExerciseLabels`, a `subtitle` prop on `ExerciseListItem` and its use in `ExerciseCatalogBrowser`. Verify with the `englishNameOf` hook tests, the "english name" cases of `exercise-catalog-view.test.tsx` and the selector case of `workout-view.test.tsx`; the workout's own list is unchanged.
- [x] 11.2 Add "Ver vídeo" to the catalog page only: a `footer` prop on `ExerciseListItem`, an optional `onWatchVideo` on the browser, an `ExerciseVideoDialog` organism around the `ExerciseVideo` molecule, and the `exerciseCatalog.watchVideo` copy in `es` and `en`. Verify with the "video" cases of `exercise-catalog-view.test.tsx`, the selector case, and the E2E that plays a video from the page and finds none in the selector.
