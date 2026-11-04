# Change Proposal: Exercise Catalog Page And Custom Exercise Creation In The Backoffice

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-exercise-catalog-page/`.

## Change

Show the exercise catalog as a page, reached from the menu item "Catálogo de ejercicios", with the same content as
the catalog dialog of the workout screen but nothing to choose, and let a trainer create their own exercise from it.

## Objective

Today the catalog can only be seen while building a workout, and only as a dialog whose cards exist to be chosen. The
menu item "Catálogo de ejercicios" is in the drawer but leads nowhere. A trainer should be able to browse the
catalog, manage their favorites and add exercises of their own without being in the middle of a workout. The API
already creates custom exercises owned by the caller (`POST /exercises`), and the catalog dialog already has the
"Mis ejercicios" section that will list them.

## Capabilities

- Exercise catalog page: the same sections ("Todos", "Mis ejercicios", "Favoritos"), text filter, type / body-part /
  muscle pills and exercise cards as the catalog dialog, but without the "Elegir ejercicio" plus button.
- Favorites from the page: the star on each card marks and unmarks, with the behaviour already built for the dialog.
- "Añadir ejercicio" button, on this page only, that opens a dialog to set the new exercise's parameters.
- Create a custom exercise: the API links it to the signed-in user, so it appears under "Mis ejercicios" as soon as
  it is created.
- Menu item: "Catálogo de ejercicios" navigates to the page.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: ADMIN, TRAINER:
  - TRAINER: browses the catalog, marks favorites, sees "Mis ejercicios", and creates exercises.
  - ADMIN: browses the catalog and marks favorites. The API answers `403` to an ADMIN's `POST /exercises`, so an ADMIN
    must NOT see the "Añadir ejercicio" button. The page is otherwise the same.
- app -> lands here: no -> roles: USER -> nothing changes. The app's `container.ts` registers the new use case,
  because `Dependencies` types both containers, and gets no UI.

On this page no role sees the "Elegir ejercicio" button, and the workout screen's dialog does not get the "Añadir
ejercicio" button of this page.

## Classification

- `CreateExerciseUseCase` -> use case -> `core/src/application/use-cases/exercises`
- Create method on the exercise repository -> repository contract and implementation -> `ExerciseRepositoryInterface`
  and `ExerciseRepository` in `core` (extended, not a new repository)
- Created exercise -> entity type -> the existing `Exercise` alias; no new domain type
- Create-exercise input -> the generated `ExerciseRequest` schema, aliased in `core/src/domain/exercises`
- Registration of the use case -> adapter and wiring -> both fronts' `src/infrastructure/container.ts`
- Exercise catalog route -> route -> `backoffice/src/app/exercises` (routing only)
- Exercise catalog page -> view -> `backoffice/src/presentation/workouts/views`
- Catalog browser (sections, filters, grid) -> organism -> extracted from `ExerciseCatalogDialog`, so the dialog and
  the page share it; the plus button and the star arrive as an actions slot or as optional props
- Create exercise dialog -> organism -> `backoffice/src/presentation/workouts/components/organisms`
- Create exercise state -> state hook -> `backoffice/src/presentation/workouts/hooks`
- Navigation drawer link -> existing organism, edited -> `navigation-drawer.tsx`

## Canonical Template Mapping

- `CreateExerciseUseCase` -> `SignupUseCase` (skeleton) and `create-workout-use-case.ts` -> nearest use cases that
  take an input and return the created entity.
- Repository method -> `workout-repository.ts` `POST` with a body -> nearest create with a request body.
- Catalog browser extraction -> `exercise-catalog-dialog.tsx` itself -> the code to move; behaviour must not change.
- Page view and route -> the closest backoffice list screen, to be chosen while proposing -> UI references are
  `pending` in `.stpr/template/README.md`, so this change takes the closest backoffice piece.
- Create form dialog -> the workout creation form and the exercise configuration dialog -> nearest backoffice forms
  and dialogs.
- Create state hook -> `use-create-workout.ts` (or its nearest equivalent) for the in-flight flag and error code.
- Tests -> `workout-view.test.tsx` and `workout.spec.ts` for the component and E2E shape; the exercise repository
  and use-case tests for core.

## Screens And Navigation

- backoffice -> `/exercises` (route name to confirm while proposing) -> exercise catalog view -> the menu item
  "Catálogo de ejercicios" in the "Biblioteca" group of the drawer -> on creating, the dialog closes and the new
  exercise is in the list; on failure the dialog stays open with the error.
- The page uses the same access guard as the other backoffice screens (`ADMIN` and `TRAINER`).

## API Contract

- `GET /exercises` -> in `core/openapi/v1.json`: yes -> already used; `ownerId` already served.
- `POST /exercises` -> in `core/openapi/v1.json`: yes -> request `ExerciseRequest` (`languageCode`, `name`, `muscle`,
  `type`, optional `videoUrl`), `201` with the exercise; `400`, `401`, `403`.
- `GET /exercise-favorites`, `PUT` and `DELETE /exercise-favorites/{exerciseId}` -> yes -> already wired.
- `PUT /exercises/{id}` and `DELETE /exercises/{id}` -> in the snapshot: yes -> not used by this change.

The snapshot already has everything this change needs, so it is not refreshed. Body part is derived by the API from
the muscle, so the form does not ask for it.

## Held State

- Exercises of the page -> the list `useExercises` reads -> the page; the created exercise is appended from the
  `201` response, with no new read -> resets when the screen is left.
- Favorites -> `useExerciseFavorites`, reused as is -> the star -> resets when the screen is left.
- Section, text and pills -> local to the page, as in the dialog -> reset when the screen is left.
- Create exercise in flight and its error code -> the create state hook -> resets when the dialog closes.

## Forms And Structural Validation

- Create exercise -> name -> required; the maximum length mirrors the API's declared limit (read from the snapshot
  and the `custom-exercises` / `exercises` API specs while proposing).
- Create exercise -> muscle -> required, one of the `Muscles` values, shown with the existing muscle labels.
- Create exercise -> type -> required, one of the `ExerciseTypes` values, shown with the existing type labels.
- Create exercise -> video URL -> optional; a structural URL check only.
- Create exercise -> language -> not a field: the exercise is created in the active interface language
  (`languageCode` `es` or `en`).

Only structural validation here. Names are not unique and an ADMIN cannot create: those are the API's rules.

## Errors And Copy

- Invalid input -> `VALIDATION` -> the create dialog shows the message for `VALIDATION`.
- Not allowed -> `FORBIDDEN` -> the dialog shows the message for `FORBIDDEN` (an ADMIN never gets here, because the
  button is hidden).
- Session expired -> `UNAUTHORIZED` -> as everywhere else in the backoffice.
- Server error -> `UNEXPECTED`, no response -> `NETWORK` -> the dialog shows the message and keeps the typed values.
- No new error code is needed in the HTTP error mapper: all of these are already mapped and translated.
- New copy, in `es` and `en` (final wording to be proposed): page title, "Añadir ejercicio" / "Add exercise", the
  dialog title and its field labels (name, muscle, type, video URL), the submit and cancel buttons.

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- Editing and archiving custom exercises (`PUT` and `DELETE /exercises/{id}`). The brief says "viewing and/or
  editing", but only creation is described. Editing is deferred to a later change unless it is added to this one.
- Choosing an exercise for a workout from the page, and the "Añadir ejercicio" button on the workout screen's dialog.
- Creating an exercise from the workout screen.
- Any change to the API or to the contract snapshot.
- Any UI in the app.
- Translating an exercise into a second language.
- Search that goes to the API: filtering stays local.

## Notes

- The page and the dialog must show the same content, so the extraction of the catalog browser must leave the
  dialog's behaviour and tests unchanged. Moving it is the largest part of the change.
- The fixed dialog height and the 90vw width belong to the dialog. The page fills its own area, and its list scrolls
  or flows with the page; to decide while proposing.
- "Mis ejercicios" needs the signed-in user's id, as the dialog already does through `useCurrentUser`.
- Verification tiers: core unit (repository create method with success and every error code, and the use case);
  component per front (the catalog page for both roles, asserting that the plus button is never shown and that the
  ADMIN has no "Añadir ejercicio"; the create dialog; the container test in both fronts); backoffice E2E (open the
  page from the menu, create an exercise and find it under "Mis ejercicios", mark a favorite from the page).
- The existing dialog tests keep passing unchanged.
- Open decisions for the proposal: the route name, whether the page shows the sidebar as a column or the same
  responsive stacking as the dialog, and whether editing is in or out (out by default).
- No deviations from the mapped template references are allowed.
