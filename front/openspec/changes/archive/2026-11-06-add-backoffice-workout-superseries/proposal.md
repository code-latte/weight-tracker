# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-workout-superseries.md`.

## Why

The API can group the exercises of a workout in a superseries and break that group (`api/` change
`add-superseries-usecases-endpoints`). `WorkoutExerciseDetailResponse` already carries `superSeriesId`, but the front
never reads it and offers no action for it. Trainers build workouts on the backoffice workout screen, so that is where
superseries are created and broken.

## What Changes

- **Core**: aliases for `CreateSuperSeriesRequest` and `SuperSeriesResponse`, two repository methods, and
  `CreateSuperSeriesUseCase` and `BreakSuperSeriesUseCase`, registered under new `Dependencies` keys in both fronts'
  containers.
- **Backoffice workout screen**:
  - Every saved exercise that is not in a superseries shows a checkbox. With two or more checked, an
    "Agrupar en superserie" button with the `Link` icon appears above the list.
  - Grouping creates the superseries, then moves its members together: the first checked exercise in the current order
    keeps its place and the others go right after it, in their current relative order. The full order is sent with the
    existing reorder use case.
  - The members of a superseries are shown together inside one group card, with a `Link` icon button in its top-left
    corner. Pressing it opens a confirmation dialog. Confirming breaks the superseries; the members stay where they are
    and get their checkbox back.
  - **Drag and drop changes**: a group card is one sortable item with its own handle. Dropping it moves all its members
    together, in their order. Members inside a group card have no handle of their own, and no exercise can be dropped
    between two members of a group.
  - Every failure keeps the previous state and shows the error in the exercises card, like the existing reorder and
    removal errors.
- **App**: no screen change. Its container registers the two use cases because the `Dependencies` keys are shared.

No API change, no snapshot refresh (both endpoints are already in `core/openapi/v1.json`), no new error code, and no
new package.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-management`: adds creating a superseries and breaking a superseries as core workout operations.
- `backoffice-workout-editor`: adds selecting exercises, grouping them in a superseries, showing group cards and
  breaking a superseries; changes the card layout and drag and drop so a superseries moves as one unit.

## Fronts, Roles And References

- backoffice -> `ADMIN`, `TRAINER` -> both group and break superseries of any workout the screen opens for them, with
  the same controls. Who may do it, and whether the workout is locked, is the API's rule: the front shows no role or
  status check and reports `FORBIDDEN` / `CONFLICT`.
- app -> `USER` -> nothing visible. It must not show superseries controls.

Canonical references (from `.stpr/template/README.md` and the closest code in the backoffice):

- Use cases -> `core/src/application/use-cases/workouts/reorder-workout-exercises-use-case.ts`.
- Repository methods -> `WorkoutRepository.reorderExercises` (POST with body) and `removeExercise` (DELETE, `204`).
- State hook -> `backoffice/src/presentation/workouts/hooks/use-reorder-workout-exercises.ts`.
- `Checkbox` -> atom, new -> `common/components/atoms/switch.tsx` (closest form-control atom).
- `SuperSeriesCard` -> molecule, new -> `workouts/components/molecules/sortable-exercise-list-item.tsx` (sortable
  wrapper around atoms).
- `BreakSuperSeriesDialog` -> organism, new -> `common/components/organisms/end-trainer-athlete-dialog.tsx`
  (confirmation dialog with a pending state).
- `ExerciseListItem`, `SortableExerciseListItem` (molecules) and `WorkoutExercisesList` (organism) are extended in
  place. `Card`, `IconButton`, `Button` and `Dialog*` atoms are reused.

## Constraints And Known Trade-offs

- **Two calls, not atomic.** The API's `CreateSuperSeries` does not reorder, so the front sends create, then reorder.
  If the reorder fails, the superseries exists but its members stay apart: the members keep their new
  `superSeriesId` in the old order and the reorder error is shown. The user chose this over an `api/` change.
- **No business rule in the front.** The front does not check set counts, membership or locks before sending; the API
  answers `400` / `409`. Needing two checked exercises before the button appears is a UI affordance, and the API checks
  it too.
- **Members apart.** A superseries whose members are not next to each other is shown as one group card per run of
  consecutive members. A `superSeriesId` held by a single listed entry is shown as no group, which is what the API
  answers once a removal leaves one member.

## Impact

- `core`: `src/domain/workouts/workout.ts`, `src/application/ports/workout-repository-interface.ts`,
  `src/infrastructure/repositories/workout-repository.ts`, `src/application/use-cases/workouts/`,
  `src/application/dependencies.ts`, and their tests.
- `backoffice`: `src/infrastructure/container.ts`, the workout hooks, molecules, organisms and view, a new `Checkbox`
  atom, `src/i18n/{es,en}/workouts.json`, `test/presentation/workouts/views/workout-view.test.tsx` and
  `e2e/workout.spec.ts`.
- `app`: `src/infrastructure/container.ts` only.

## Out Of Scope

- Superseries in the app.
- Adding an exercise to, or removing one from, an existing superseries; only create and break.
- Dragging a single exercise into or out of a group card, or reordering members inside a group.
- Checking set counts in the front before sending.
- Any change to the API, the HTTP error mapper or the catalog.
