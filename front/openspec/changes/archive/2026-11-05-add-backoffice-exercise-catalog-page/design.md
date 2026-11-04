# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. This is the current state that shapes the
approach:

- **Contract.** `POST /exercises` and `ExerciseRequest` (`languageCode`, `name`, `muscle`, `type`, optional `videoUrl`;
  the first four are required) are already in `core/openapi/v1.json`. The API answers `201` with the
  `ExerciseResponse`, `400`, `401`, `403`. No refresh is needed.
- **API rules the form mirrors.** `ExerciseName`: trimmed, 3 to 150 characters. `Url`: trimmed, at most 500
  characters, absolute `http` or `https`, no whitespace, a dot in the host. An `ADMIN` gets `403`.
- **Core.** `ExerciseRepositoryInterface` has only `getAll`. Workout forms keep their structural limits in
  `core/src/application/validation/workout-rules.ts` (constants plus a validator that returns violation codes), and
  `CreateWorkoutDialog` shows them.
- **Catalog dialog.** `ExerciseCatalogDialog` holds the whole catalog UI: the sidebar, the alerts, the filters and the
  grid. Its filter state (`selectedSection`, `filterText`, `selectedType`, `selectedBodyPart`, `selectedMuscle`) lives
  in `LoadedWorkout`, so it survives a reopening, and `singleMuscleBodyParts` is computed there from the whole catalog.
  The dialog's paper is `90vw` wide on `md` and `min(85vh, 860px)` tall, and its list scrolls inside it. Each card's
  `actions` slot holds the star and the "Elegir ejercicio" button, with `watermarkInsetRight={5}`.
- **State hooks.** `useExercises` reads the catalog once and holds it. `useExerciseFavorites` reads the favorites once
  and owns the toggle. `useCreateWorkout` is the model for an in-flight flag and an error code.
- **Labels.** `useExerciseLabels` resolves names with `i18n.language`, labels muscles, types and body parts, and
  exports `EXERCISE_TYPES`. There is no list of muscle codes yet; the labels live in `workouts.json` under `muscles`.
- **Drawer.** `navigation-drawer.tsx` links an item when it has an `href`; "Catálogo de ejercicios" has none.
- **Pages.** `assigned-plans-view.tsx` is a page with a heading, a header button, a loading skeleton, a `LoadError`
  and a dialog, inside `AppShell`. Its route file is `export default AssignedPlansView`.

## Goals / Non-Goals

**Goals:**

- One catalog UI used by the dialog and the page, so they cannot drift.
- The workout screen behaves exactly as before: its tests pass unchanged.
- Creation adds the exercise to the visible list with no extra read.
- No new error code and no change to the HTTP error mapper.

**Non-Goals:**

- Editing or archiving exercises, and any use of `PUT` or `DELETE /exercises/{id}`.
- Creating exercises from the workout screen.
- Fixing the navigation drawer's direct MUI imports.
- Any app presentation.

## Decisions

### core: extend the exercise area

Creation belongs to the same API area and lifecycle as the catalog read, so it extends the existing repository instead
of adding a new one (unlike favorites, which have their own area and controller).

| Piece                 | Path                                                                  | Shape                                                                                                                      |
| --------------------- | --------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Request alias         | `core/src/domain/exercises/exercise.ts`, exported from `domain/index` | `export type CreateExerciseRequest = components["schemas"]["ExerciseRequest"];` beside `Exercise`.                         |
| Repository contract   | `exercise-repository-interface.ts`                                    | `create(request: CreateExerciseRequest): Promise<Result<Exercise, DomainError>>`.                                          |
| Repository impl       | `exercise-repository.ts`                                              | `POST /exercises` with the request as `body`, returns `ok(response)`; every failure is `fail(mapHttpError(error))`.        |
| Use case              | `use-cases/exercises/create-exercise-use-case.ts`                     | `UseCaseInterface<CreateExerciseRequest, Exercise>`; forwards the input unchanged, like `CreateWorkoutUseCase`.            |
| Dependencies          | `core/src/application/dependencies.ts`                                | One new key, `createExerciseUseCase`. `exerciseRepository` already exists.                                                 |
| Structural validation | `core/src/application/validation/exercise-rules.ts`                   | `EXERCISE_NAME_MIN_LENGTH` 3, `EXERCISE_NAME_MAX_LENGTH` 150, `EXERCISE_VIDEO_URL_MAX_LENGTH` 500, `validateExerciseForm`. |

- The repository forwards the body it is given, so `videoUrl` is sent only when the form put it in the request.
- `validateExerciseForm(values)` takes `{ name, muscle, type, videoUrl }` as strings, trims the name and the link, and
  returns `Partial<Record<field, "required" | "minLength" | "maxLength" | "url">>`. The URL check is
  `new URL(...)` inside a `try`, plus the scheme, whitespace and dot-in-host checks, so it needs no dependency and
  throws nothing outward.
- It is exported from `validation/index.ts` and, like `workout-rules.ts`, imported by the backoffice from
  `@weight-tracker/core/application`.
- Errors: `mapHttpError` already maps 400 to `VALIDATION`, 401, 403, and the rest, so it is not touched.

### Wiring in both fronts

Both `container.ts` files register `createExerciseUseCase: (c) => new CreateExerciseUseCase(c.resolve("exerciseRepository"))`.
Both `container.test.ts` files list the key, and both `render-with-providers.tsx` get a no-op fake in
`unusedDependencies`; the backoffice's lets a test override it. The app gets no UI.

### backoffice: extract the catalog browser

`ExerciseCatalogDialog` is split in three, without changing what the workout screen shows:

| Piece                       | Level       | Path                                                | Responsibility                                                                                                                       |
| --------------------------- | ----------- | --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `useExerciseCatalogFilters` | state hook  | `presentation/workouts/hooks`                       | Holds the section, text, type, body part and muscle, and the toggles (including "a muscle only refines its own body part").          |
| `singleMuscleBodyPartsOf`   | pure helper | `presentation/workouts/catalog.ts`                  | The set the view builds today, beside `week.ts`. The view and the page both call it.                                                 |
| `ExerciseCatalogBrowser`    | organism    | `components/organisms/exercise-catalog-browser.tsx` | Everything inside the dialog's content: the sidebar, both alerts, the text field, the three pill rows and the cards with their star. |
| `ExerciseCatalogDialog`     | organism    | unchanged path                                      | The `Dialog`, its title and Cerrar button, the 90vw / fixed height paper, and a `ExerciseCatalogBrowser` that passes `onChoose`.     |

- The state stays in the screen (the hook is called by `LoadedWorkout`), so a reopened dialog keeps its filters.
- `ExerciseCatalogBrowser` takes the exercises, the filters object returned by the hook, `currentUserId`, the favorites
  state's fields (as the dialog does today) and an optional `onChoose`. With `onChoose` it renders the plus button and
  keeps `watermarkInsetRight={5}`; without it, only the star and `watermarkInsetRight={2.5}`, so the type icon does not
  sit under it.
- A `contained` flag chooses the layout: `true` (the dialog) makes the browser fill its parent and the list scroll
  inside it, as today; `false` lets the list flow with its parent. The catalog page passes `true` too (see below).
- The sections stack above the content below `md`, as today.
- Components keep using only atoms for MUI widgets. `Box` is used as in the current dialog.

### backoffice: the page

- Route `backoffice/src/app/exercises/page.tsx`: `export default ExerciseCatalogView;`, routing only.
- `ExerciseCatalogView` (`presentation/workouts/views/exercise-catalog-view.tsx`) mirrors `AssignedPlansView`: `AppShell`,
  a `Container`, a heading, a header button, `HomeSkeleton` while loading and `LoadError` with retry when the catalog
  or the current user fails.
- It calls `useExercises()`, `useExerciseFavorites()`, `useCurrentUser()` and `useExerciseCatalogFilters()`. The
  favorites never block it, as on the workout screen. It waits for the current user as well as the catalog, so the
  button does not appear after rendering.
- The button shows for `profile.role === "TRAINER"` only. This mirrors the API's `403` for an `ADMIN`; the API keeps the
  rule.
- The browser gets the whole catalog and no `onChoose`. It sits in a column box of `calc(100vh - 220px)`, at least 480px,
  and is `contained`, so the sections, the finder and the pills stay fixed and only the list scrolls, as in the dialog.

### backoffice: creating

- `useExercises` gains `add(exercise)`, which appends to its held list. This is the only change to that hook. It uses
  the same generation guard, so a read that answers later still replaces the list, which is the correct reload
  behaviour.
- `useCreateExercise` (state hook) copies `useCreateWorkout`: `creating`, `errorCode`, `create(request)` resolving to the
  created `Exercise` or `null`, and `reset`.
- `CreateExerciseDialog` (organism) copies `CreateWorkoutDialog`: the form inside the dialog so every opening starts
  empty, `validateExerciseForm` on submit, an error `Alert` for the API's code, `Cancelar` and `Crear`.
  - Fields: `Nombre` (text), `Músculo` and `Tipo` (`TextField select` with the `MenuItem` atom, as the configuration
    dialog's effort), `Enlace de vídeo` (text).
  - Muscles come from a new `EXERCISE_MUSCLES` constant beside `EXERCISE_TYPES` in `use-exercise-labels.ts`, typed as
    `NonNullable<CreateExerciseRequest["muscle"]>[]` so a code the API adds or drops fails to compile.
  - The select groups the muscles by body part. A `MUSCLE_BODY_PARTS` record beside the list gives each muscle's body
    part, only for this grouping (the API derives the real one and never receives it). Each group starts with a
    `ListSubheader` atom written "— Brazos —", which MUI's select does not let the user choose; the groups and the
    muscles inside them are sorted by their label.
  - The request is `{ languageCode: i18n.language, name: name.trim(), muscle, type, ...(link !== "" && { videoUrl: link.trim() }) }`.
  - While creating, the fields and both buttons are disabled and the dialog ignores Escape and a click outside.
  - On success it calls `onCreated(exercise)`; the view appends it with `add` and closes the dialog. The section and
    the filters are untouched, so under "Mis ejercicios" the exercise is there because `ownerId` equals the user's id.

### Navigation and copy

- `navigation-drawer.tsx`: `href: "/exercises"` on the catalog item. The selected state uses the logic the other linked
  items use; it is checked while implementing that an exact `/exercises` match is enough.
- New keys in `workouts.json` (`es` / `en`), under `exerciseCatalog`: `title` ("Catálogo de ejercicios" / "Exercise
  catalog"), `add` ("Añadir ejercicio" / "Add exercise"), `create.title` ("Nuevo ejercicio" / "New exercise"),
  `create.fields.{name,muscle,type,videoUrl}` ("Nombre", "Músculo", "Tipo", "Enlace de vídeo" / "Name", "Muscle",
  "Type", "Video link"), `create.actions.{cancel,create}` ("Cancelar", "Crear" / "Cancel", "Create") and
  `create.validation.{required,minLength,maxLength,url}`. The parity test keeps `es` and `en` in step.

### Tests

- core: `exercise-repository.test.ts` (create: method, path, body, returned value, 400, 401, 403, 500, no response),
  `create-exercise-use-case.test.ts` (delegation, success, failure), `exercise-rules.test.ts` (each limit and the
  boundaries 2/3 and 150/151 and the link cases), all in Arrange / Act / Assert.
- backoffice component: a new `exercise-catalog-view.test.tsx` with use cases faked through the container, for both
  roles (no choose button, the button for the trainer only, favorites, sections, creation, refused creation, validation,
  dismissal, English); a `useExercises.add` case; the container test. `workout-view.test.tsx` is not edited except for
  anything the extraction truly forces, which must be justified in the task.
- backoffice E2E: a flow in `workout.spec.ts`'s style (or a new `exercise-catalog.spec.ts` sharing its mocks): open the
  page from the menu, create an exercise and find it under "Mis ejercicios", mark a favorite from the page.

## Risks / Trade-offs

- **Regression in the workout dialog.** The extraction moves the most-tested UI in the backoffice. Mitigation: do the
  extraction first, with no new behaviour, and run `workout-view.test.tsx` and the E2E before anything else is added.
- **Props growth.** The browser keeps the dialog's long prop list, now split between a filters object and the favorites
  fields. Accepted; a favorites object would be a separate cleanup.
- **Client-side URL check is stricter or looser than the API.** The API stays the authority and answers `400`, shown as
  `VALIDATION`; the form check only saves a round trip.
- **Page height.** The page box uses a viewport-based height with a floor, so on a very short window the page itself
  scrolls rather than squeezing the list below a usable size. The 220px approximates the header and the heading.
- **Language.** Creating in the interface language means an exercise made in Spanish has no English translation, so in
  English it shows its Spanish name through the existing fallback. Accepted; translating is out of scope.
- **Appended exercise vs a later reload.** A reload replaces the list with the API's, which includes the new exercise.
