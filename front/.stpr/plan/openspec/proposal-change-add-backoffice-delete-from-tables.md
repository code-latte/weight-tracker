# Change Proposal: Delete From Workout Tables

## Change

Add a trash-icon delete action to every row of the workout templates table, the workouts table of the
"Entrenamientos" page and the workouts table inside each collapsible week of a mesocycle, and wire workout deletion
in `core`.

## Objective

Templates can only be deleted from inside their own screen, and workouts cannot be deleted from the front at all, so
mistaken or abandoned drafts stay in the lists. The API now exposes `DELETE /workouts/{workoutId}` (see
`api/.stpr/plan/openspec/proposal-change-add-delete-workout-endpoint.md`); the front needs the operation and a direct
way to use it, and the same direct way for templates.

## Capabilities

- A trainer or admin deletes a workout template from its row in the templates table, after confirming.
- A trainer or admin deletes a workout from its row in the "Entrenamientos" table, after confirming.
- A trainer or admin deletes a workout from its row in a mesocycle week's table, after confirming.
- `core` exposes a `DeleteWorkoutUseCase` returning `Result<void, DomainError>`, registered in both fronts.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: ADMIN, TRAINER -> both see the trash button on every row they already see
  in the three tables; the API decides whether the deletion is allowed (ownership, draft-only) and the screen shows
  its refusal.
- app -> lands here: no (UI) -> roles: USER -> nothing changes for the athlete. Only the `core` use case is
  registered in `app/src/infrastructure/container.ts`, as every core use case is.

No role-specific hiding: the button set is the same for ADMIN and TRAINER, because each role already only sees the
rows the API returns to it.

## Classification

- Delete workout template -> use case + repository method -> `core` -> **already wired**:
  `DeleteWorkoutTemplateUseCase` (`core/src/application/use-cases/workout-templates/delete-workout-template-use-case.ts`),
  `WorkoutTemplateRepository.delete` (`core/src/infrastructure/repositories/workout-template-repository.ts`), registered
  in `backoffice` and `app` `container.ts` as `deleteWorkoutTemplateUseCase`. Nothing to add in `core` for templates.
- `WorkoutRepositoryInterface.delete(id)` -> repository contract -> `core/src/application` (beside the existing workout contract)
- `WorkoutRepository.delete(id)` -> repository implementation -> `core/src/infrastructure/repositories/workout-repository.ts`
- `DeleteWorkoutUseCase` -> use case -> `core/src/application/use-cases/workouts/delete-workout-use-case.ts` (+ `index.ts` export)
- `deleteWorkoutUseCase` registration -> wiring -> `backoffice/src/infrastructure/container.ts`, `app/src/infrastructure/container.ts`
- `useDeleteWorkout` -> state hook -> `backoffice/src/presentation/workouts/hooks/use-delete-workout.ts`
- `DeleteWorkoutDialog` -> organism -> `backoffice/src/presentation/workouts/components/organisms/delete-workout-dialog.tsx`
- `WorkoutTemplatesTable` (trash button + `onDelete` prop) -> organism (existing) -> `backoffice/src/presentation/workouts/components/organisms/workout-templates-table.tsx`
- `WorkoutsTable` (trash button + `onDelete` prop) -> organism (existing) -> `backoffice/src/presentation/workouts/components/organisms/workouts-table.tsx`
- `MesocycleWeekWorkoutsTable` (trash button; `WeekWorkoutRow` gains `deleteLabel`, table gains `onDelete`) -> molecule (existing) -> `backoffice/src/presentation/mesocycles/components/molecules/mesocycle-week-workouts-table.tsx`
- `MesocycleWeeksCard` (passes the delete callback through) -> organism (existing) -> `backoffice/src/presentation/mesocycles/components/organisms/mesocycle-weeks-card.tsx`
- `WorkoutTemplatesView`, `AssignedWorkoutsView`, `MesocycleView` (own the confirm dialog, call the hook, reload on success) -> views (existing) -> `backoffice/src/presentation/{workouts,mesocycles}/views`

## Canonical Template Mapping

- `WorkoutRepository.delete` -> `core/src/infrastructure/repositories/workout-template-repository.ts` `delete` -> same `DELETE` + `ok(undefined)` / `mapHttpError` shape.
- `DeleteWorkoutUseCase` -> `core/src/application/use-cases/workout-templates/delete-workout-template-use-case.ts` -> same single-call delete use case.
- `useDeleteWorkout` -> `backoffice/src/presentation/workouts/hooks/use-delete-workout-template.ts` -> same `deleting` / `errorCode` / `remove(id): Promise<boolean>` contract through `runWithSession`.
- `DeleteWorkoutDialog` -> `backoffice/src/presentation/workouts/components/organisms/delete-workout-template-dialog.tsx` -> same confirm dialog that sends nothing itself.
- Templates table delete -> reuses `useDeleteWorkoutTemplate` and `DeleteWorkoutTemplateDialog` as they are; the view wiring follows `backoffice/src/presentation/workouts/views/workout-template-view.tsx` (`deletion` + `confirmingDelete` state).
- Trash button -> the existing edit `IconButton` atom in the same three tables -> same atom, a second icon in the row (MUI `DeleteOutline`), with an `aria-label` that names the row.
- Core tests -> the existing `workout-template-repository` and `delete-workout-template-use-case` tests in `core/test`.
- Component tests -> the existing tests of `workout-templates-view`, `assigned-workouts-view` and `mesocycle-view` in `backoffice/test`.

No reference is `pending`.

## Screens And Navigation

- backoffice -> workout templates page -> `WorkoutTemplatesView` -> trash on a row -> confirm dialog -> on success the
  dialog closes and the list reloads (`useWorkoutTemplates().reload`); on failure the dialog closes and the error is
  shown above the table; the user stays on the page.
- backoffice -> "Entrenamientos" page -> `AssignedWorkoutsView` -> trash on a row -> confirm dialog -> on success the
  list reloads (`useWorkouts().reload`); on failure the error is shown; the user stays on the page.
- backoffice -> mesocycle screen -> `MesocycleView` -> expand a week -> trash on a workout row -> confirm dialog -> on
  success the mesocycle reloads (`useMesocycle(id).reload`) so the week shows one workout fewer; on failure the error is
  shown; the user stays on the screen with the week still expanded.

## API Contract

- `DELETE /workout-templates/{id}` -> in `core/openapi/v1.json`: yes -> 204, 401, 403, 404. Already used.
- `DELETE /workouts/{workoutId}` -> in `core/openapi/v1.json`: **no** -> 204, 401, 403, 404, 409. Provided by the API
  change `add-delete-workout-endpoint` (implemented in `api/`, not yet committed). This change refreshes the snapshot
  (`openapi:refresh` with the API running, then `openapi:generate`) and commits `v1.json` plus the regenerated types
  as a step of their own, before any code. No request or response body, so no new domain alias is needed.

## Held State

- `useDeleteWorkout` / `useDeleteWorkoutTemplate` -> `deleting`, `errorCode` -> the view, through `remove(id)` -> reset
  on screen exit and on the next `remove`.
- The view -> which row is pending confirmation (`id` + `name`, or `null`) -> the trash button and the dialog's
  cancel/confirm -> reset on close and on screen exit.

## Forms And Structural Validation

None. The action takes only the row's id.

## Errors And Copy

- Workout is no longer a draft (published, started, completed, reviewed) -> `CONFLICT` (409) -> a specific message on
  the two workout screens: "Solo se pueden eliminar entrenamientos en borrador."
- Workout changed since it was read -> `CONFLICT` (409) -> same message is acceptable; the API does not distinguish
  the two 409s by code.
- Caller may not modify the row -> `FORBIDDEN` (403) -> the existing generic error copy.
- Row already gone -> `NOT_FOUND` (404) -> the existing generic error copy, and the list reloads.
- Session / network -> existing codes and copy.

No new error code in the HTTP error mapper: `409 -> CONFLICT` already exists.

Copy (namespace `workouts` unless noted; final keys follow the existing naming in each file):

- `workoutTemplates.delete` -> "Eliminar {{name}}" -> "Delete {{name}}"
- `assignedWorkouts.delete` -> "Eliminar {{name}}" -> "Delete {{name}}"
- `workout.deleteConfirm.title` -> "¿Eliminar entrenamiento?" -> "Delete workout?"
- `workout.deleteConfirm.body` -> "Se eliminará el entrenamiento con sus ejercicios. Esta acción no se puede deshacer." -> "The workout and its exercises will be deleted. This cannot be undone."
- `workout.deleteConfirm.cancel` -> "Cancelar" -> "Cancel"
- `workout.deleteConfirm.confirm` -> "Eliminar" -> "Delete"
- `workout.deleteConflict` -> "Solo se pueden eliminar entrenamientos en borrador." -> "Only draft workouts can be deleted."
- `mesocycles` namespace: week row delete label -> "Eliminar {{name}}" -> "Delete {{name}}"

The template confirm dialog reuses its existing `workoutTemplate.deleteConfirm.*` keys.

## Platform Capabilities And Dependencies

None. `@mui/icons-material` is already a backoffice dependency.

## Out Of Scope

- Any app (Expo) UI for deleting workouts.
- Hiding or disabling the trash button by workout status: the draft-only rule belongs to the API, and the front shows
  its refusal instead of reproducing it.
- A delete button on the workout screen (`workout-view`). Only the three tables get one.
- Bulk delete, undo or soft delete.
- Deleting mesocycles, weeks or exercises.
- No change to the API, or to the existing template delete use case, repository or dialog.

## Notes

- Delete template is confirmed as fully wired in `core` and both containers; the only new template work is the table
  button and the view wiring.
- Workout delete steps, in order: snapshot refresh + regenerate (own commit) -> repository contract + implementation
  -> use case + export -> registration in both `container.ts` -> backoffice hook, dialog, tables, views -> copy in
  `es` and `en`.
- The row action cell holds edit and trash side by side; keep `padding="checkbox"` or widen the cell so both fit.
- Verification tiers:
  - core unit: `WorkoutRepository.delete` (204, and each error code: 401, 403, 404, 409) and `DeleteWorkoutUseCase`.
  - backoffice component: in each of the three views, the trash button opens the dialog; confirm calls the use case
    with the row's id and reloads; cancel sends nothing; a `CONFLICT` shows the draft-only message; another failure
    shows the generic copy. Cover ADMIN and TRAINER where the view already branches by role (trainer column).
  - translation parity tests pass.
  - backoffice E2E: not required.
- No stop case in `STPR_INVARIANTS.md` is hit: no business rule in the front, no hand-written domain type.
- No deviations from the mapped references are allowed.
