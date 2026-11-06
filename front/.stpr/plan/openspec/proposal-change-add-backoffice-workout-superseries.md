# Change Proposal: Group Workout Exercises In Superseries

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-workout-superseries/`.

## Change

Wire the superseries endpoints (`POST /workouts/{workoutId}/superseries` and
`DELETE /workouts/{workoutId}/superseries/{superSeriesId}`) into `core` and let trainers and admins group and ungroup
workout exercises on the backoffice workout screen.

## Objective

The API can group workout exercises in a superseries and break that group (`api/` change
`add-superseries-usecases-endpoints`), and `WorkoutExerciseDetailResponse` already carries `superSeriesId`, but the
front never reads it nor offers the actions. Trainers build workouts on the backoffice workout screen, so that is where
superseries are created and broken.

## Capabilities

- Every saved exercise in the workout column that is **not** in a superseries shows a checkbox.
- When two or more exercises are checked, a button with the `Link` icon and the label "Agrupar en superserie" appears
  above the list. With fewer than two checked it is not rendered.
- Pressing the button sends `POST /workouts/{workoutId}/superseries` with the entry `id` of every checked exercise.
- On success the new group is kept contiguous: the first checked exercise in the current order keeps its position and
  the other members are placed right after it, in their current relative order. The new full order is sent with the
  existing `ReorderWorkoutExercisesUseCase`. The checkboxes are cleared.
- Exercises that share a `superSeriesId` are rendered together inside one card, with a `Link` icon button in its
  top-left corner. Members inside the group card show no checkbox.
- Pressing the group's `Link` icon opens a confirmation dialog to break the superseries. Confirming sends
  `DELETE /workouts/{workoutId}/superseries/{superSeriesId}`; on success the members leave the card, stay in their
  current positions and get their checkbox back. Cancelling closes the dialog and sends nothing.
- On any failure the list keeps its previous state and an error is shown, like the existing reorder and remove errors.
- While a create or break request is in progress, the group button and the dialog's confirm button are disabled.
- Pending exercises (added from the catalog, not saved yet) have no entry `id`, so they show no checkbox.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both group and ungroup exercises of any workout the
  screen opens for them.
- app -> lands here: no -> roles: `USER` -> no screen changes. Its container registers the new use cases only because
  the `Dependencies` keys are shared in `core`, as with every workout use case so far.

Both roles see the same controls. Who may group, and whether the workout is locked, is the API's rule: the front shows
no role or status check and reports the API's `FORBIDDEN` / `CONFLICT`.

## Classification

Core:

- `CreateSuperSeriesRequest`, `SuperSeries` (alias of `SuperSeriesResponse`) -> entity types (aliases of the generated
  schemas) -> `core/src/domain/workouts/workout.ts`
- `CreateSuperSeriesInput` (`{ workoutId: string; workoutExerciseIds: string[] }`) and `BreakSuperSeriesInput`
  (`{ workoutId: string; superSeriesId: number }`) -> use case input types -> `core/src/domain/workouts/workout.ts`
- `WorkoutRepositoryInterface.createSuperSeries(...)`: `Promise<Result<SuperSeries, DomainError>>` and
  `breakSuperSeries(...)`: `Promise<Result<void, DomainError>>` -> repository contract ->
  `core/src/application/ports/workout-repository-interface.ts`
- `WorkoutRepository.createSuperSeries` / `breakSuperSeries` -> repository implementation ->
  `core/src/infrastructure/repositories/workout-repository.ts`
- `CreateSuperSeriesUseCase`, `BreakSuperSeriesUseCase` -> use cases ->
  `core/src/application/use-cases/workouts/`, exported from its `index.ts`

Backoffice:

- `createSuperSeriesUseCase`, `breakSuperSeriesUseCase` keys -> `core/src/application/dependencies.ts`; registration ->
  wiring -> `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`
- `useSuperSeries()` (pending, errorCode, create, breakUp) -> state hook ->
  `backoffice/src/presentation/workouts/hooks/use-super-series.ts`
- `Checkbox` -> atom (new) -> `backoffice/src/presentation/common/components/atoms/checkbox.tsx`, wraps MUI `Checkbox`
- `SuperSeriesCard` -> molecule (new) -> `backoffice/src/presentation/workouts/components/molecules/super-series-card.tsx`:
  `Card` atom with the `Link` `IconButton` in its top-left corner, holding the members' list items
- `BreakSuperSeriesDialog` -> organism (new) ->
  `backoffice/src/presentation/workouts/components/organisms/break-super-series-dialog.tsx`
- `ExerciseListItem` / `SortableExerciseListItem` -> molecules (extended in place) -> optional checkbox slot
- `WorkoutExercisesList` -> organism (extended in place) -> groups consecutive members into `SuperSeriesCard`, renders
  the "Agrupar en superserie" `Button`, holds no request logic
- `WorkoutView` / `LoadedWorkout` -> view (extended in place) -> holds the checked ids, calls the hook, computes the
  contiguous order, updates `superSeriesId` on `savedExercises` after success
- Copy -> `backoffice/src/i18n/{es,en}/workouts.json`

## Canonical Template Mapping

- Use cases -> `core/src/application/use-cases/workouts/reorder-workout-exercises-use-case.ts` -> input object passed
  to one repository method, returning its `Result`.
- `createSuperSeries` -> `WorkoutRepository.reorderExercises` -> JSON body, response mapped to the alias, errors through
  the HTTP error mapper.
- `breakSuperSeries` -> `WorkoutRepository` remove-exercise method -> `DELETE` with no body, `204` mapped to `ok(void)`.
- Core tests -> `core/test/application/use-cases/workouts/reorder-workout-exercises-use-case.test.ts` and
  `core/test/infrastructure/repositories/workout-repository.test.ts`.
- `useSuperSeries` -> `backoffice/src/presentation/workouts/hooks/use-reorder-workout-exercises.ts` -> in-progress flag,
  errorCode, async actions through `runWithSession` resolving to the outcome.
- `Checkbox` -> `backoffice/src/presentation/common/components/atoms/switch.tsx` -> closest form-control atom.
- `SuperSeriesCard` -> `backoffice/src/presentation/workouts/components/molecules/exercise-list-item.tsx` -> same level,
  composes atoms only.
- `BreakSuperSeriesDialog` -> `backoffice/src/presentation/common/components/organisms/end-trainer-athlete-dialog.tsx`
  -> existing confirmation dialog with cancel / confirm and a pending state.
- View test -> `backoffice/test/presentation/workouts/views/workout-view.test.tsx` (extended).
- E2E -> `backoffice/e2e/workout.spec.ts` (extended).

## Screens And Navigation

- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> unchanged entry points -> group / break: list updated in place,
  no navigation; failure: previous state kept and an error alert above the list; the user stays on the screen.

## API Contract

- `POST /workouts/{workoutId}/superseries` -> in `core/openapi/v1.json`: yes -> body `CreateSuperSeriesRequest`
  (`{ exerciseIds: string[] }`, **workout entry ids**, not catalog ids), response `201` `SuperSeriesResponse`
  (`{ id: number }`); failures `400`, `401`, `403`, `404`, `409`.
- `DELETE /workouts/{workoutId}/superseries/{superSeriesId}` -> yes -> `superSeriesId` is an `int32`, response `204`;
  failures `400`, `401`, `403`, `404`, `409`.
- `PUT /workouts/{workoutId}/exercises/order` -> yes -> reused through `ReorderWorkoutExercisesUseCase`, unchanged.
- `GET /workouts/{id}` -> yes -> `WorkoutExerciseDetailResponse.superSeriesId` read, unchanged.

The snapshot already contains the endpoints: no refresh is needed.

## Held State

- `LoadedWorkout` checked entry ids -> the exercises selected for grouping -> the checkboxes -> cleared after a
  successful group; screen exit.
- `LoadedWorkout` `savedExercises` -> now also the `superSeriesId` of each entry -> a successful create (members get the
  returned id and the contiguous order) or break (members get `null`) -> screen exit.
- `useSuperSeries` -> pending, errorCode -> the view -> cleared when the next request starts; screen exit.
- `BreakSuperSeriesDialog` open / target `superSeriesId` -> the view -> closed on confirm success or cancel.

## Forms And Structural Validation

None. The button only appears with two or more checked exercises, which is a UI affordance, not a rule. Whether the
members have the same number of sets, or are already grouped, is the API's rule (`400` / `409`).

## Errors And Copy

- Members with different set totals, fewer than two, duplicated -> `VALIDATION` -> error alert with the existing copy
- Exercise already in a superseries, workout locked -> `CONFLICT` -> existing copy
- Not allowed -> `FORBIDDEN` -> existing copy
- Workout, entry or superseries gone -> `NOT_FOUND` -> existing copy
- Session -> `UNAUTHORIZED` -> `runWithSession`
- No response / other -> `NETWORK` / `UNEXPECTED` -> existing copy

No new error code: the HTTP error mapper is not touched.

New copy (`workouts` namespace):

- `workout.superSeries.group` (button) -> "Agrupar en superserie" -> "Group in superset"
- `workout.superSeries.select` (checkbox label) -> "Seleccionar para superserie" -> "Select for superset"
- `workout.superSeries.break` (group icon label) -> "Romper superserie" -> "Break superset"
- `workout.superSeries.breakDialog.title` / `.message` / `.confirm` / `.cancel` -> to be proposed in the design, es and en

## Platform Capabilities And Dependencies

None. `@mui/icons-material/Link` is already available; no port or adapter.

## Out Of Scope

- Superseries in the app.
- Adding exercises to, or removing a single exercise from, an existing superseries; only create and break.
- Dragging a single exercise into or out of a group card, or reordering members inside a group.
- Matching set counts in the front before sending; the API decides.
- Any change to the API, the HTTP error mapper or the catalog column.

## Notes

- **Decided: the contiguous order is set by the front.** The API's `CreateSuperSeries` does not reorder exercises, and
  nothing in the API keeps a superseries contiguous. The front sends create, then reorder. The two calls are not
  atomic: if the reorder fails, the superseries exists but its members are apart. The view then shows the reorder
  error and keeps the members with their `superSeriesId` in the old order. An `api/` change that reorders inside
  `CreateSuperSeries` was considered and not chosen.
- **Decided: a superseries is dragged as a unit.** The group card is one sortable item with its own drag handle; its
  members show no handle of their own and are never dragged one by one. Dropping the card moves all its members
  together, keeping their internal order, and sends the full order as today. Ungrouped exercises cannot be dropped
  between two members of a group, because the group is a single sortable item.
- The list renders a group card per run of consecutive entries sharing a `superSeriesId`. If the API returns members
  apart (data created before this change), each run is its own card with the same id; breaking any of them breaks the
  whole superseries.
- Ids sent to `POST` are entry `id`s (`workoutExercise.id`); ids sent to the reorder are catalog `exerciseId`s, as today.
- Verification tiers:
  - core unit: both use cases (delegate and return the repository `Result`); `createSuperSeries` and
    `breakSuperSeries` success (method, path, body, mapped response) and every error code.
  - backoffice component (`workout-view.test.tsx`, use cases faked through the container): checkboxes only on
    ungrouped saved exercises; the button is hidden with zero or one checked and shown with two; grouping calls create
    with the entry ids and then reorder with the contiguous order; grouped members render inside one card with the
    `Link` icon and no checkbox; the icon opens the dialog, cancel calls nothing, confirm calls break and the members
    get their checkbox back; each failure keeps the previous state and shows the error.
  - backoffice E2E (`workout.spec.ts`, API intercepted with `page.route`): group two exercises and assert the
    `POST .../superseries` and `PUT .../exercises/order` bodies; break the group and assert the `DELETE` path.
- No deviations from the mapped references are allowed.
