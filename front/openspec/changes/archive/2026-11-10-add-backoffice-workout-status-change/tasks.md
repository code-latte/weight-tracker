# Tasks

## 1. Core

- [x] 1.1 Confirm no `core` change is needed: `ChangeWorkoutStatusUseCase`, `WorkoutRepository.changeStatus`,
      `ChangeWorkoutStatusInput` and `WorkoutStatus` exist, and `WorkoutById` carries `status`; verify by reading
      `core/src/application/use-cases/workouts/change-workout-status-use-case.ts` and `core/src/domain/workouts/`,
      and that `git diff --stat core/` stays empty at the end of the change.

## 2. Backoffice wiring and translations

- [x] 2.1 Confirm `changeWorkoutStatusUseCase` is registered in `backoffice/src/infrastructure/container.ts` and add
      nothing there; verify `git diff backoffice/src/infrastructure/container.ts` is empty.
- [x] 2.2 Add the `workout.status.*` keys from `design.md` to `backoffice/src/i18n/es/workouts.json` and
      `backoffice/src/i18n/en/workouts.json`; verify the backoffice translation parity test passes.

## 3. Backoffice state hook

- [x] 3.1 Add `backoffice/src/presentation/workouts/hooks/use-change-workout-status.ts` modelled on
      `use-update-workout.ts` (`changing`, `errorCode`, `changed`, `change(workoutId, status)`, `clear()`, run inside
      `runWithSession`); verify `pnpm --filter backoffice typecheck` passes.

## 4. Backoffice organisms

- [x] 4.1 Extend `WorkoutDetailsCard` with optional `statusAction` (after "Guardar") and `locked` (hides "Guardar"
      and `statusAction`, shows the compact info `Alert` with `workout.status.locked`, disables every field); verify
      the existing `workout-template-view.test.tsx` still passes unchanged.
- [x] 4.2 Add `ChangeWorkoutStatusDialog` in `backoffice/src/presentation/workouts/components/organisms/` modelled on
      `delete-workout-template-dialog.tsx` (`open`, `target`, `pending`, `onCancel`, `onConfirm`; not dismissible
      while pending; non-destructive confirm button); verify `pnpm --filter backoffice typecheck` and `lint` pass.

## 5. Backoffice view

- [x] 5.1 In `LoadedWorkout` (`workout-view.tsx`), hold `status` seeded from `workout.status` and `confirming`; wire
      `useChangeWorkoutStatus`; pass `statusAction` ("Publicar" for `DRAFT`, "Volver a borrador" for `PUBLISHED`,
      outlined `Button`) or `locked` for `STARTED` / `COMPLETED` / `REVIEWED`; disable the action while
      `saving || changing` and "Guardar" while `saving || changing`; render the dialog; on settle close it and take
      the returned status; show the success / error alerts beside the save alerts; clear outcomes on field edit and
      when the other action starts. Verify by the tests in 5.2.
- [x] 5.2 Extend `backoffice/test/presentation/workouts/views/workout-view.test.tsx` following its existing structure
      and fakes, adding a fake `changeWorkoutStatusUseCase`, with AAA phases and one Act each: `DRAFT` shows
      "Publicar" and hides "Volver a borrador"; `PUBLISHED` the reverse; an admin sees the same; clicking the action
      opens the right dialog and sends nothing; "Cancelar" closes it and the use case is never called; confirming
      calls the use case once with `{ workoutId, status }`; in-flight disables "Cancelar", confirm and "Guardar" and
      Escape does not close; a save in flight disables the action; success swaps the action and shows the message;
      a 400 keeps the action and shows "Revisa los datos introducidos."; `STARTED`, `COMPLETED` and `REVIEWED` show
      the banner, hide "Guardar", "Publicar" and "Volver a borrador", disable the fields and keep "Guardar como
      plantilla". Verify `pnpm --filter backoffice test` passes.
- [x] 5.3 Add an E2E flow to `backoffice/e2e/workout.spec.ts` reusing its route helpers: open a `DRAFT` workout,
      publish through the dialog, then move it back to draft, with `page.route` answering
      `PUT /workouts/{workoutId}/status`; verify `pnpm test:e2e` passes for that flow.

## 6. Review

- [x] 6.1 Review the backoffice against its mapped references (`use-update-workout.ts`,
      `delete-workout-template-dialog.tsx`, the `saveAsTemplate` action and save alerts in `workout-view.tsx`) and the
      quality checklist in `AGENTS.md`: atomic levels, downward imports, no business rule, no change to the exercises
      editor, every string in `es` and `en`; verify by reading the diff.
- [x] 6.2 Review core: confirm the diff touches nothing under `core/`; verify with `git diff --stat core/`.
- [x] 6.3 Run the `.stpr/template/` drift check from `AGENTS.md`; verify it prints no `DRIFT:` line.
- [x] 6.4 Run `pnpm typecheck && pnpm lint && pnpm test` and `pnpm test:e2e` from `front/`; verify all pass.

## Workflow follow-up

- Sync the delta specs and archive the change after review.
