# Design

## Context

See `proposal.md` for the motivation and scope.

- Both endpoints are in `core/openapi/v1.json`. The generated types are `CreateSuperSeriesRequest`
  (`{ exerciseIds: null | string[] }`), `SuperSeriesResponse` (`{ id: number | string }`) and
  `WorkoutExerciseDetailResponse.superSeriesId` (`null | number | string`); the `DELETE` path parameter is `number`.
  The `number | string` unions come from the API's int32 pattern; the API sends numbers.
- The workout screen's list (`WorkoutExercisesList`) is one dnd-kit `SortableContext` over the entry ids, laid out as a
  CSS grid of `EXERCISE_GRID_COLUMNS`. `LoadedWorkout` owns `savedExercises`, applies a drop with `arrayMove`, sends the
  catalog ids through `useReorderWorkoutExercises` and restores with `inOrderOf` on failure.
- Removal mirrors nothing: the view filters the entry out. After a removal leaves a superseries with one member, the
  API clears that member's `superSeriesId`, but the screen's copy still holds it.

## Goals / Non-Goals

**Goals:**

- Follow the existing core shape for one more pair of workout operations.
- Keep the screen's pattern: hooks and requests in the view, components presentational.
- Turn the sortable list into a list of units (an ungrouped exercise or a group run) without changing how a reorder is
  sent.

**Non-Goals:**

- Nested sortable contexts or reordering inside a group.
- Re-reading the workout after a mutation; local state is updated from what each answer implies, as today.

## Decisions

### Core

- `core/src/domain/workouts/workout.ts`:
  - `CreateSuperSeriesRequest = components["schemas"]["CreateSuperSeriesRequest"]`;
    `SuperSeries = components["schemas"]["SuperSeriesResponse"]`.
  - `SuperSeriesId = NonNullable<WorkoutExerciseDetail["superSeriesId"]>`.
  - `CreateSuperSeriesInput = { workoutId: string; exerciseIds: string[] }` (workout entry ids; the doc comment says so,
    since `ReorderWorkoutExercisesInput.exerciseIds` holds catalog ids).
  - `BreakSuperSeriesInput = { workoutId: string; superSeriesId: SuperSeriesId }`.
- `WorkoutRepositoryInterface`: `createSuperSeries(workoutId, request: CreateSuperSeriesRequest):
Promise<Result<SuperSeries, DomainError>>` and `breakSuperSeries(workoutId, superSeriesId):
Promise<Result<void, DomainError>>`.
- `WorkoutRepository`: `createSuperSeries` modelled on `reorderExercises` (`POST /workouts/{workoutId}/superseries`,
  JSON body); `breakSuperSeries` modelled on `removeExercise` (`DELETE /workouts/{workoutId}/superseries/{id}`, no
  body, `ok(undefined)`). Errors go through `mapHttpError`; no new code.
- `CreateSuperSeriesUseCase` and `BreakSuperSeriesUseCase` in `core/src/application/use-cases/workouts/`, modelled on
  `ReorderWorkoutExercisesUseCase`, exported from `index.ts`.
- `Dependencies` (`core/src/application/dependencies.ts`): `createSuperSeriesUseCase:
UseCaseInterface<CreateSuperSeriesInput, SuperSeries>` and `breakSuperSeriesUseCase:
UseCaseInterface<BreakSuperSeriesInput, void>`, registered in `backoffice/src/infrastructure/container.ts` and
  `app/src/infrastructure/container.ts` next to `reorderWorkoutExercisesUseCase`.

### Backoffice components

Imports flow downward; only atoms import MUI widgets. Icons come from `@mui/icons-material`, as in the existing
molecules and organisms.

- `Checkbox` -> atom, new -> `backoffice/src/presentation/common/components/atoms/checkbox.tsx`, a pass-through
  wrapper of MUI `Checkbox` modelled on `switch.tsx`.
- `ExerciseListItem` -> molecule, extended -> its `leading` slot already takes any node; no change unless the slot
  needs to hold two controls, in which case it stays a `ReactNode`.
- `SortableExerciseListItem` -> molecule, extended -> optional `selection` prop (`{ checked, label, onChange }`) that
  renders the `Checkbox` atom after the handle in `leading`.
- `SuperSeriesCard` -> molecule, new -> `backoffice/src/presentation/workouts/components/molecules/super-series-card.tsx`.
  Uses `useSortable` like `SortableExerciseListItem`; renders the `Card` atom spanning `gridColumn: "1 / -1"`, a top
  row with the `IconButton` + `Link` icon ("Romper superserie") in the top-left corner and the drag handle
  ("Reordenar superserie") beside it, and an inner grid of `EXERCISE_GRID_COLUMNS` holding `children`.
- `BreakSuperSeriesDialog` -> organism, new ->
  `backoffice/src/presentation/workouts/components/organisms/break-super-series-dialog.tsx`. Layout from
  `end-trainer-athlete-dialog.tsx` (`Dialog`, `DialogTitle`, `DialogContent`, `DialogActions`, Cancelar / confirm
  `Button`s). **Deviation:** it is presentational (`open`, `pending`, `onCancel`, `onConfirm`), and the error is shown
  in the exercises card, because every request of the workout screen lives in the view and its errors in the list.
- `WorkoutExercisesList` -> organism, extended:
  - New props: `selectedIds: ReadonlySet<string>`, `onToggleSelected(id)`, `onGroup()`, `grouping: boolean`,
    `superSeriesErrorCode`, `onBreak(superSeriesId)`.
  - Builds **units** from `items`: consecutive items sharing a `superSeriesId` (compared with `String()`) form a group
    run, even a run of one when other members sit elsewhere; an item whose `superSeriesId` no other item holds is
    ungrouped. Unit sortable ids are the entry id for an ungrouped item and
    `group:<first member's entry id>` for a run, so two runs of the same superseries never collide.
  - `SortableContext items` = unit ids. Ungrouped units render `SortableExerciseListItem` with `selection`; runs render
    `SuperSeriesCard` holding plain `ExerciseListItem`s (no handle, no checkbox). Positions stay `index + 1` over
    `items`.
  - Renders the `Button` atom with the `Link` start icon above the grid when `selectedIds.size >= 2`, disabled while
    `grouping`, and the `superSeriesErrorCode` alert beside the other two.
  - `onReorder(activeId, overId)` now carries unit ids. dnd-kit announcements name a group by its members' names.

### Backoffice view and hook

- `useSuperSeries()` -> state hook -> `backoffice/src/presentation/workouts/hooks/use-super-series.ts`, modelled on
  `use-reorder-workout-exercises.ts`: `{ pending, errorCode, create(workoutId, exerciseIds): Promise<SuperSeriesId |
null>, breakUp(workoutId, superSeriesId): Promise<boolean> }`, both through `runWithSession`.
- `LoadedWorkout` (`workout-view.tsx`):
  - `selectedIds` state (`Set<string>`), toggled by the checkboxes.
  - `handleGroup`: members = `savedExercises` filtered by `selectedIds`, in list order. On `create` success: set the
    returned id on the members, compute the contiguous order (everything before the first member, the members, then
    the rest without the members), clear `selectedIds`, and, if the order changed, call `reordering.reorder` with the
    catalog ids. On reorder failure, restore with `inOrderOf(previousIds)`; the members keep their id. On `create`
    failure nothing changes.
  - `handleReorder(activeId, overId)` maps unit ids to entry spans: it moves the span of `activeId` to the position of
    `overId`'s span (before it when moving up, after it when moving down), then sends and restores as today. A plain
    `arrayMove` stays the path when both units are single entries.
  - Break: `breakTarget` state (`SuperSeriesId | null`) opens `BreakSuperSeriesDialog`; on confirm, `breakUp`, close,
    and on success set `superSeriesId: null` on every entry holding that id.
  - "Grouping in progress" = `superSeries.pending || reordering.reordering`.
- Pure helpers (`unitsOf`, contiguous order, span move) live as module functions in the files that use them, like
  `inOrderOf`; no new shared module.

### Held state

- `savedExercises` (view) -> entries with order and `superSeriesId` -> drops, grouping, breaking, saves, removals ->
  screen exit.
- `selectedIds` (view) -> checked entry ids -> checkboxes; cleared on grouping success; an id that leaves the list
  (removal) or joins a group is dropped from it -> screen exit.
- `breakTarget` (view) -> superseries whose dialog is open -> the link icon; cleared on cancel and after the answer.
- `useSuperSeries` -> `pending`, `errorCode` -> cleared when the next request starts -> screen exit.

### Copy

`workouts` namespace, es / en:

- `workout.superSeries.group` -> "Agrupar en superserie" / "Group in superset"
- `workout.superSeries.select` -> "Seleccionar para superserie" / "Select for superset"
- `workout.superSeries.break` -> "Romper superserie" / "Break superset"
- `workout.superSeries.reorder` -> "Reordenar superserie" / "Reorder superset"
- `workout.superSeries.breakDialog.title` -> "¿Romper superserie?" / "Break superset?"
- `workout.superSeries.breakDialog.message` -> "Los ejercicios seguirán en el entrenamiento, cada uno por separado." /
  "The exercises stay in the workout, each on its own."
- `workout.superSeries.breakDialog.confirm` -> "Romper" / "Break"; `.cancel` -> "Cancelar" / "Cancel", like the other
  workout dialogs (no shared cancel key exists).
- dnd-kit group announcements -> `workout.exercises.dnd.groupName` -> "superserie {{names}}" / "superset {{names}}".

Errors reuse `errors:{VALIDATION, FORBIDDEN, NOT_FOUND, CONFLICT, NETWORK, UNEXPECTED}`; `UNAUTHORIZED` goes through
`runWithSession`. No new error code; the HTTP error mapper is not touched.

### Tests

- Core: `workout-repository.test.ts` gains `createSuperSeries` and `breakSuperSeries` (method, path, body, mapped
  result, every error code); one test file per use case, modelled on `reorder-workout-exercises-use-case.test.ts`.
- Backoffice component: `workout-view.test.tsx` extended with faked `createSuperSeriesUseCase` and
  `breakSuperSeriesUseCase` through the container, one test per spec scenario.
- Backoffice E2E: `workout.spec.ts` extended with one flow grouping two exercises and breaking the group, asserting the
  `POST`, `PUT` and `DELETE` requests intercepted with `page.route`.
- No change to test helpers or patterns.

## Risks / Trade-offs

- [Create succeeds, reorder fails: members apart] → The list shows one group card per run, the error is shown, and the
  user can drag the cards together or break the superseries.
- [`superSeriesId` is `number | string`] → Compare with `String()` and pass the value through unchanged; no parsing.
- [Lone member after a removal keeps a stale id] → An id no other entry holds renders as ungrouped, with its checkbox. Grouping it
  again sends its entry id; the API, which already cleared it, accepts it.
- [Group card spans the full row] → A group of one or two leaves empty columns in its row. Accepted: it makes the group
  boundary obvious and keeps the inner layout identical to the outer one.
- [Changing a member's sets can break the API's same-set-count rule] → The configuration dialog already shows the API's
  `400` message; no front check is added.

## Migration Plan

None. Front-only, no data migration; rollback is reverting the change.
