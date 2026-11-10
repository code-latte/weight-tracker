# Tasks

## 1. Contract snapshot

- [x] 1.1 With the API branch holding `add-delete-workout-endpoint` running on `http://localhost:5199`, run
      `pnpm --filter @weight-tracker/core openapi:refresh` then `openapi:generate`; verify `core/openapi/v1.json`
      now has `delete` under `/workouts/{workoutId}` with 204/401/403/404/409, review the diff, and commit
      `v1.json` plus `core/src/contract/generated` as a commit of their own. Stop here if the API is not available.
      The refresh also brings the API's new required `completedAt` (date-time or null) on the workout responses;
      as agreed, the same commit adds `completedAt: null` to the 21 workout fixtures in `core`, `backoffice` and
      `app` tests that typecheck flags, and nothing else.

## 2. Core

- [x] 2.1 Confirm template deletion needs no change: `DeleteWorkoutTemplateUseCase`, `WorkoutTemplateRepository.delete`
      and `deleteWorkoutTemplateUseCase` in `dependencies.ts` and both `container.ts`; verify
      `git diff core/src/application/use-cases/workout-templates core/src/infrastructure/repositories/workout-template-repository.ts`
      stays empty at the end of the change.
- [x] 2.2 Add `delete(id)` to `core/src/application/ports/workout-repository-interface.ts` and implement it in
      `core/src/infrastructure/repositories/workout-repository.ts` like `WorkoutTemplateRepository.delete`; add
      repository tests (204 success with one bodiless `DELETE /workouts/{id}`, and 401, 403, 404, 409, 500, no
      response mapped to their codes) beside the existing workout repository tests; verify they pass with
      `pnpm --filter @weight-tracker/core test`.
- [x] 2.3 Add `core/src/application/use-cases/workouts/delete-workout-use-case.ts` (input `{ id }`) modelled on
      `delete-workout-template-use-case.ts`, export it from `use-cases/workouts/index.ts`, add
      `deleteWorkoutUseCase` to `core/src/application/dependencies.ts`, and add
      `core/test/application/use-cases/workouts/delete-workout-use-case.test.ts` modelled on the delete-template
      use-case test; verify `pnpm --filter @weight-tracker/core test` and `typecheck` pass.
- [x] 2.4 Review core against its references (`workout-template-repository.ts` `delete`,
      `delete-workout-template-use-case.ts` and their tests): same shape, naming and Arrange/Act/Assert phases.

## 3. Wiring and translations

- [x] 3.1 Register `deleteWorkoutUseCase: (c) => new DeleteWorkoutUseCase(c.resolve("workoutRepository"))` in
      `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`, next to
      `deleteWorkoutTemplateUseCase`; verify `pnpm typecheck` passes from `front/`.
- [x] 3.2 Add the keys from `design.md` to backoffice `es` and `en`: `common.json` `deleteWorkout.*`,
      `workouts.json` `workoutTemplates.delete` and `assignedWorkouts.delete`, `mesocycles.json`
      `weeks.deleteWorkout`; verify the backoffice translation parity test passes.

## 4. Backoffice shared hook and organism

- [x] 4.1 Add `backoffice/src/presentation/common/hooks/use-delete-workout.ts` modelled on
      `workouts/hooks/use-delete-workout-template.ts` (`deleting`, `errorCode`, `remove(id)` through
      `runWithSession`, resolving `deleteWorkoutUseCase`); verify `pnpm --filter backoffice typecheck` passes.
- [x] 4.2 Add `backoffice/src/presentation/common/components/organisms/delete-workout-dialog.tsx` modelled on
      `delete-workout-template-dialog.tsx` (`open`, `pending`, `onCancel`, `onConfirm`; not dismissible while
      pending; copy from `common` `deleteWorkout.*`); verify `pnpm --filter backoffice typecheck` and `lint` pass.

## 5. Backoffice tables

- [x] 5.1 Extend the `MesocycleWeekWorkoutsTable` molecule: `WeekWorkoutRow.deleteLabel`, an `onDelete(id)` prop, and
      a `DeleteOutline` trash `IconButton` after the edit one in the same action cell (no wrap); verify
      `pnpm --filter backoffice typecheck` passes.
- [x] 5.2 Extend the `WorkoutTemplatesTable` and `WorkoutsTable` organisms with `onDelete(row)` and the trash
      `IconButton` labelled `workoutTemplates.delete` / `assignedWorkouts.delete`; verify typecheck passes.
- [x] 5.3 Extend the `MesocycleWeeksCard` organism with optional `onDeleteWorkout(workout)` (passes
      `deleteLabel` from `weeks.deleteWorkout` and the callback to the table) and optional `deleteError`, shown as
      an error `Alert` at the top of the card body; verify typecheck passes and the existing `mesocycle-view.test.tsx`
      still passes unchanged.

## 6. Backoffice views and their tests

- [x] 6.1 `WorkoutTemplatesView`: hold `pendingDelete`, open `DeleteWorkoutTemplateDialog` from the row,
      on confirm `useDeleteWorkoutTemplate().remove` then `reload` on success, show the error `Alert` above the
      table on failure. Add cases to `backoffice/test/presentation/workouts/views/workout-templates-view.test.tsx`:
      the row action opens the dialog and sends nothing, cancel sends nothing, confirm deletes the row id and reloads,
      `FORBIDDEN` shows the generic message, and both the admin and the trainer see the action. Verify with
      `pnpm --filter backoffice test`.
- [x] 6.2 `AssignedWorkoutsView`: same flow with `useDeleteWorkout` and `DeleteWorkoutDialog`, `useWorkouts().reload`
      on success, `deleteWorkout.conflict` for `CONFLICT` and the generic message otherwise. Add the same cases plus
      `CONFLICT` to `assigned-workouts-view.test.tsx`, including a `PUBLISHED` row still showing the action; verify
      with `pnpm --filter backoffice test`.
- [x] 6.3 `MesocycleView` (edit branch): same flow, passing `onDeleteWorkout` and `deleteError` to
      `MesocycleWeeksCard`, calling `useMesocycleWorkouts().reload` on success and never the mesocycle's `reload`.
      Add cases to `backoffice/test/presentation/mesocycles/views/mesocycle-view.test.tsx`: dialog, cancel, confirm
      reads the workouts again (and not the mesocycle), deleting a week's last workout leaves it non-collapsible with
      "Nuevo entrenamiento", `CONFLICT` and another error, and admin and trainer. Verify with
      `pnpm --filter backoffice test`.
- [x] 6.4 Review the backoffice against its references (`workout-template-view.tsx` deletion flow,
      `use-delete-workout-template.ts`, `delete-workout-template-dialog.tsx`, the existing view tests): atomic levels
      and folders, downward imports, only atoms importing MUI widgets (icons as in the existing tables), no
      status-based logic, and every string in `es` and `en`.

## 8. Workout status label

- [x] 8.1 Add `backoffice/src/presentation/common/workout-status.ts` (status → chip colour: `DRAFT` default/grey,
      `PUBLISHED` info, `STARTED` warning, `COMPLETED` success, `REVIEWED` secondary, unknown grey) and the
      `common.json` `workoutStatus.*` keys in `es` and `en`; verify the translation parity test passes.
- [x] 8.2 Show an outlined small `Chip` with the translated status after the name in `WorkoutsTable` and in
      `MesocycleWeekWorkoutsTable` (rows gain `status` and `statusLabel`, filled by `MesocycleWeeksCard`); update the
      name-cell expectations and add per-status label and colour cases to `assigned-workouts-view.test.tsx` and
      `mesocycle-view.test.tsx`; verify with `pnpm --filter backoffice test`.

## 7. Verification

- [x] 7.1 Check `.stpr/template/` for drift with the loop in `AGENTS.md`; verify it prints nothing.
- [x] 7.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`; verify everything passes. No E2E flow is
      planned.

## Workflow follow-up

- Run `.stpr/review/CHECKLIST_REVISION.md` before opening the PR.
- Archive the change after review.
