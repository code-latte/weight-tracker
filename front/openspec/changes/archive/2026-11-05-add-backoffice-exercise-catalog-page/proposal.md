# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-exercise-catalog-page.md`.

## Why

The menu item "Catálogo de ejercicios" is in the drawer but leads nowhere, and the catalog can only be seen while
building a workout, in a dialog whose cards exist to be chosen. A trainer should be able to browse the catalog, manage
their favorites and add exercises of their own without being in the middle of a workout. The API already creates
custom exercises owned by the caller (`POST /exercises`) and the catalog already has the "Mis ejercicios" section that
will list them, so the front only lacks the page and the creation flow.

## What Changes

- **Catalog page**: a new route `/exercises`, reached from "Catálogo de ejercicios" in the "Biblioteca" group. It shows
  the same content as the catalog dialog (sections "Todos", "Mis ejercicios" and "Favoritos", the text filter, the
  type / body-part / muscle pills and the exercise cards) with the same favorite star, but no "Elegir ejercicio"
  button, because nothing is being chosen.
- **Shared catalog browser**: that content is extracted from `ExerciseCatalogDialog` into one organism that the dialog
  and the page both use, so they cannot drift. The dialog's behaviour and tests do not change.
- **Add exercise**: on the page only, an "Añadir ejercicio" button opens a dialog with the new exercise's parameters
  (name, muscle, type and an optional video URL). On success the dialog closes and the new exercise is in the list and
  under "Mis ejercicios", without reading the catalog again. A failure keeps the dialog open with the typed values and
  shows the error.
- **Create in core**: `ExerciseRepository` gains `create`, and a new `CreateExerciseUseCase` returns the created
  `Exercise` as a `Result`. A structural form validator mirrors the API's limits.
- **Roles**: the API answers `403` to an `ADMIN`'s `POST /exercises`, so an `ADMIN` browses the page and marks
  favorites but does not see "Añadir ejercicio". A `TRAINER` sees all of it.
- **Menu**: "Catálogo de ejercicios" links to `/exercises` and is selected while the visitor is on it.

## Capabilities

### New Capabilities

- `exercise-creation`: creating a custom exercise through `core`, the request it sends, what it returns, how its
  failures are returned as results, and the structural limits of its form.
- `backoffice-exercise-catalog`: the exercise catalog screen of the backoffice: its content, the absence of a choose
  button, favorites, and the add-exercise dialog with its role rule.

### Modified Capabilities

- `backoffice-navigation`: "Catálogo de ejercicios" is no longer an item without a screen; it goes to the catalog
  screen and is shown as selected there.

## Canonical References

`.stpr/template/README.md` lists the atomic UI references as `pending`, so each UI piece takes the closest backoffice
piece.

- `CreateExerciseUseCase` -> `create-workout-use-case.ts` and `SignupUseCase` (skeleton): a use case that takes an
  input and returns the created entity.
- `ExerciseRepository.create` -> `WorkoutRepository` create (`POST` with a body) and `ExerciseRepository.getAll`.
- Exercise alias and request alias -> `core/src/domain/exercises/exercise.ts` (aliases over generated schemas).
- Form validator -> `core/src/application/validation/workout-rules.ts` (limits as constants, violations as codes).
- Wiring -> each front's `src/infrastructure/container.ts`, under one new `Dependencies` key.
- Route -> `backoffice/src/app/assigned-plans/page.tsx` (routing only).
- Catalog view -> `assigned-plans-view.tsx` (page with a title, a header button and a dialog).
- Catalog browser -> the content of `exercise-catalog-dialog.tsx`, moved without changing its behaviour.
- Add-exercise dialog -> `create-workout-dialog.tsx` (form inside the dialog, validation on submit, error alert).
- Create state hook -> `use-create-workout.ts`.
- Menu item -> `navigation-drawer.tsx`, as `Planes asignados` is linked.
- Tests:
  - core: `exercise-repository.test.ts` (extended), a use-case test beside `get-exercises-use-case.test.ts`, and a
    validator test beside the workout rules test;
  - backoffice: `assigned-plans-view.test.tsx` and `create-workout-dialog.test.tsx` for the shape, plus the existing
    `workout-view.test.tsx` as the regression guard for the dialog;
  - E2E: `workout.spec.ts` for the shape of `signIn` and the mocked routes;
  - both fronts' `container.test.ts` and `render-with-providers.tsx` (the new key).

## Fronts And Roles

- backoffice: `ADMIN` and `TRAINER` see the page, the sections, the filters and the star. Only the `TRAINER` sees
  "Añadir ejercicio". No role sees "Elegir ejercicio" on the page, and the workout screen's dialog does not get
  "Añadir ejercicio".
- app (`USER`): no UI change. Its `container.ts` registers the new use case, because `Dependencies` types both
  containers, so a missing registration fails to compile.

## API Endpoints

All are already in `core/openapi/v1.json`; the snapshot is not refreshed.

- `GET /exercises` (`getExercisesUseCase`) -> `ExerciseResponse[]`; already used.
- `POST /exercises` (`createExerciseUseCase`) -> request `ExerciseRequest` (`languageCode`, `name`, `muscle`, `type`,
  optional `videoUrl`), `201` with the `ExerciseResponse`; `400`, `401`, `403`.
- `GET /exercise-favorites`, `PUT` and `DELETE /exercise-favorites/{exerciseId}` -> already wired.
- `PUT` and `DELETE /exercises/{id}` -> in the snapshot, not used by this change.

## Constraints And Conflicts

- **Name and URL limits (decided while proposing).** The API validates the name after trimming, between 3 and 150
  characters, and the video URL as an absolute `http` or `https` address with no whitespace, a dot in its host and at
  most 500 characters (`ExerciseName`, `Url`). The form mirrors those limits as structural checks, as the workout
  forms mirror theirs. The API keeps the rule and answers `400`.
- **Language.** The request needs `languageCode`. The form has no language field: the exercise is created in the
  language the screen is shown in, which the API accepts as one translation.
- **Editing is out.** The brief says "viewing and/or editing" but describes only creation, so editing and archiving
  (`PUT` and `DELETE /exercises/{id}`) are deferred.
- **No business rule.** Who may create (an `ADMIN` may not), name uniqueness (none) and ownership are the API's rules.
  Hiding the button for an `ADMIN` mirrors `403`, as the workout form mirrors who needs a trainer.
- **Dialog unchanged.** Extracting the browser must leave the workout screen's dialog, its tests, its fixed height
  and its 90vw width as they are. The page does not use the dialog's height: it takes the height left under its heading, and only its list scrolls.
- **No new error code.** `VALIDATION`, `UNAUTHORIZED`, `FORBIDDEN`, `UNEXPECTED` and `NETWORK` are already mapped and
  translated; the HTTP error mapper is not touched.
- **Stop case.** If `POST /exercises` or `ExerciseRequest` differs from the snapshot read while proposing (the fields
  above), the change stops.

## Acceptance Criteria

- The delta specs' scenarios pass:
  - core unit tests for the create method (success and every error code) and the use case;
  - the validator's limits;
  - component tests for the page for both roles (no choose button anywhere, no "Añadir ejercicio" for an `ADMIN`) and
    for the add-exercise dialog;
  - the existing workout screen tests pass unchanged.
- `workout.spec.ts` or a new E2E opens the page from the menu, creates an exercise and finds it under "Mis
  ejercicios", and marks a favorite from the page.
- `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` pass from `front/`.

## Impact

- `core`: `domain/exercises` (request alias), `application/ports/exercise-repository-interface.ts`,
  `application/use-cases/exercises` (new use case), `application/validation` (exercise rules),
  `application/dependencies.ts`, `infrastructure/repositories/exercise-repository.ts`, and tests.
- `backoffice`:
  - `app/exercises/page.tsx` (new);
  - `presentation/workouts/views/exercise-catalog-view.tsx` (new);
  - `components/organisms/exercise-catalog-browser.tsx` (new, extracted) and `exercise-catalog-dialog.tsx` (reduced to
    the dialog around it);
  - `components/organisms/create-exercise-dialog.tsx` (new);
  - `hooks/use-create-exercise.ts` (new) and a hook for the shared filters;
  - `workout-view.tsx` (uses the shared pieces);
  - `navigation-drawer.tsx`;
  - `infrastructure/container.ts`;
  - `i18n/{es,en}` (common and workouts);
  - tests, test support and E2E.
- `app`: `infrastructure/container.ts`, its container test and test support; no UI.
- API: none.
