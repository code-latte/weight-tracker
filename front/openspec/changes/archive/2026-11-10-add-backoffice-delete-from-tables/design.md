# Design

## Context

See `proposal.md` for why. The current state:

- Deleting a template is fully wired: `DeleteWorkoutTemplateUseCase`, `WorkoutTemplateRepository.delete`, the
  `deleteWorkoutTemplateUseCase` dependency key in `core/src/application/dependencies.ts`, and its registration in
  both `container.ts` files. The backoffice uses it only from `workout-template-view.tsx`, through
  `workouts/hooks/use-delete-workout-template.ts` and `workouts/components/organisms/delete-workout-template-dialog.tsx`.
- Deleting a workout exists in the API (`WorkoutsController.Delete`, uncommitted in `api/`) but not in
  `core/openapi/v1.json`.
- The three tables (`WorkoutTemplatesTable`, `WorkoutsTable`, `MesocycleWeekWorkoutsTable`) each end in one action
  cell holding an edit `IconButton`. The views read their lists through hooks that expose `reload`: `useWorkoutTemplates`,
  `useWorkouts`, and `useMesocycleWorkouts` for the mesocycle's workouts.
- Presentation used by more than one domain area already lives in `presentation/common` (for example
  `common/hooks/use-create-workout-from-template.ts` and `common/components/organisms/workout-template-picker.tsx`),
  and its organisms take copy from the default `common` namespace.

## Goals / Non-Goals

**Goals:**

- One workout delete hook and one workout confirm dialog, shared by the "Entrenamientos" screen and the mesocycle
  screen.
- The tables stay presentational: they report which row asked for deletion and own no request or dialog.

**Non-Goals:**

- Changing `DeleteWorkoutTemplateDialog`, `useDeleteWorkoutTemplate` or the template screen's own delete button.
- Any status-based logic in the front (see Decisions).
- App UI.

## Decisions

### core

- **Contract snapshot.** With the API running, run `pnpm --filter @weight-tracker/core openapi:refresh`, then
  `openapi:generate`, and commit `core/openapi/v1.json` plus `core/src/contract/generated` alone. The endpoint has no
  body in either direction, so no domain alias is added.
- **Repository.** Add `delete(id: string): Promise<Result<void, DomainError>>` to
  `core/src/application/ports/workout-repository-interface.ts`. Implement it in
  `core/src/infrastructure/repositories/workout-repository.ts` with the same `DELETE` → `ok(undefined)` /
  `fail(mapHttpError(error))` body as `WorkoutTemplateRepository.delete`. The mapper already turns 403/404/409 into
  `FORBIDDEN`/`NOT_FOUND`/`CONFLICT`, so it needs no change.
- **Use case.** `core/src/application/use-cases/workouts/delete-workout-use-case.ts` takes `{ id }` and returns the
  repository's result, a copy of `delete-workout-template-use-case.ts`. Export it from `use-cases/workouts/index.ts`.
- **Dependencies.** Add `deleteWorkoutUseCase: UseCaseInterface<DeleteWorkoutInput, void>` to
  `core/src/application/dependencies.ts`. Register it in `backoffice/src/infrastructure/container.ts` and
  `app/src/infrastructure/container.ts` as `new DeleteWorkoutUseCase(c.resolve("workoutRepository"))`, next to
  `deleteWorkoutTemplateUseCase`.

### backoffice: shared pieces in `presentation/common`

- **`common/hooks/use-delete-workout.ts`** (state hook). It copies `useDeleteWorkoutTemplate`: `deleting`,
  `errorCode`, and `remove(id): Promise<boolean>` through `runWithSession`, resolving `deleteWorkoutUseCase`. It lives
  in `common` because two areas use it, `workouts` and `mesocycles`.
  - _Alternative considered:_ putting it in `workouts/hooks` and importing it from `mesocycles`. Rejected because no
    area imports another today, and `common` is where cross-area hooks already live.
- **`common/components/organisms/delete-workout-dialog.tsx`** (organism). It copies `DeleteWorkoutTemplateDialog`:
  `open`, `pending`, `onCancel`, `onConfirm`, sends nothing itself, and can't be dismissed while pending. It takes its
  copy from the default namespace under `deleteWorkout.*`.

### backoffice: tables (presentational, labels by props or own namespace)

- **`WorkoutTemplatesTable`** and **`WorkoutsTable`** (organisms) gain `onDelete: (row) => void`, where the row is a
  template or a list item. The action cell holds edit and then a trash `IconButton` with `DeleteOutline` from
  `@mui/icons-material`, labelled from their existing namespace: `workoutTemplates.delete` and
  `assignedWorkouts.delete`. The action cell keeps `padding="checkbox"` and lays the two buttons out in a row with no
  wrap.
- **`MesocycleWeekWorkoutsTable`** (molecule) takes no i18n, so `WeekWorkoutRow` gains `deleteLabel: string` and the
  table gains `onDelete: (id: string) => void`.
- **`MesocycleWeeksCard`** (organism) gains `onDeleteWorkout?: (workout: WorkoutListItem) => void`. It builds
  `deleteLabel` from `mesocycles` `weeks.deleteWorkout` and passes the callback through. It also gains an optional
  `deleteError?: ReactNode`, shown as an error `Alert` at the top of the card body. This keeps the card unaware of
  which error codes exist.
- Imports keep flowing downward: atoms ← molecule ← organisms ← views. Only atoms import MUI widgets. The icon import
  follows the existing `EditOutlined` usage in the same tables.

### backoffice: views (own state, dialog and refresh)

The three views follow the same pattern, taken from `workout-template-view.tsx`:

- **View-local state:** `pendingDelete: { id, name } | null`. The trash button sets it, and closing or confirming the
  dialog clears it.
- **On confirm:** `const ok = await deletion.remove(id)`, then clear `pendingDelete`, and if `ok` call the list hook's
  `reload`.
  - `WorkoutTemplatesView` uses `useDeleteWorkoutTemplate` and `DeleteWorkoutTemplateDialog` (both reused from
    `workouts`), and calls `useWorkoutTemplates().reload`.
  - `AssignedWorkoutsView` uses `useDeleteWorkout` and `DeleteWorkoutDialog`, and calls `useWorkouts().reload`.
  - `MesocycleView` (in the edit branch, where `useMesocycleWorkouts` already lives) uses `useDeleteWorkout` and
    `DeleteWorkoutDialog`, and calls `workouts.reload`. It does not call `useMesocycle(id).reload`, because the
    mesocycle's own data does not change.
- **Error display:** when `deletion.errorCode` is set, `WorkoutTemplatesView` and `AssignedWorkoutsView` show an
  error `Alert` above the table. `MesocycleView` passes the message to `MesocycleWeeksCard` as `deleteError`. The
  message is `deleteWorkout.conflict` (in the `common` namespace) when the code is `CONFLICT` on a workout deletion,
  and the generic `errors:<code>` otherwise. The next `remove` clears the error.

### Held state

- `useDeleteWorkout` and `useDeleteWorkoutTemplate` hold `deleting` and `errorCode`. Only `remove` changes them, and
  they reset when the screen is left and when the next `remove` starts.
- Each view holds `pendingDelete`. The trash button and the dialog change it, and it resets when the dialog closes and
  when the screen is left.
- No provider or context is added.

### Translations (`es` / `en`)

- `workouts.json`: `workoutTemplates.delete` "Eliminar {{name}}" / "Delete {{name}}"; `assignedWorkouts.delete`
  "Eliminar {{name}}" / "Delete {{name}}".
- `mesocycles.json`: `weeks.deleteWorkout` "Eliminar {{name}}" / "Delete {{name}}".
- `common.json`:
  - `deleteWorkout.title`: "¿Eliminar entrenamiento?" / "Delete workout?"
  - `deleteWorkout.body`: "Se eliminará el entrenamiento con sus ejercicios. Esta acción no se puede deshacer." /
    "The workout and its exercises will be deleted. This cannot be undone."
  - `deleteWorkout.cancel`: "Cancelar" / "Cancel"
  - `deleteWorkout.confirm`: "Eliminar" / "Delete"
  - `deleteWorkout.conflict`: "Solo se pueden eliminar entrenamientos en borrador." / "Only draft workouts can be
    deleted."
- The template dialog keeps `workoutTemplate.deleteConfirm.*`.

### Error codes

- **Workout deletion:** `CONFLICT` (409) shows `deleteWorkout.conflict`. `FORBIDDEN`, `NOT_FOUND`, `UNAUTHORIZED`,
  `NETWORK` and `UNEXPECTED` show their existing `errors.json` copy.
- **Template deletion:** `FORBIDDEN`, `NOT_FOUND` and the session/network codes show their existing copy.
- No new code is added to the HTTP error mapper or to `errors.json`.

### Tests (API mocked everywhere)

- **core:** `core/test/infrastructure/repositories/workout-repository` gains `delete` cases: 204 success, then 401,
  403, 404, 409, 500 and no response, each mapped to its code. Add
  `core/test/application/use-cases/workouts/delete-workout-use-case.test.ts`, modelled on the existing delete-template
  use-case test.
- **backoffice component**, in `workout-templates-view.test.tsx`, `assigned-workouts-view.test.tsx` and
  `mesocycle-view.test.tsx`. Fake use cases come through the container, following each file's existing structure, and
  every test has `// Arrange` / `// Act` / `// Assert` phases with one Act. The cases:
  - the trash button opens the dialog and sends nothing;
  - cancel sends nothing;
  - confirm sends one deletion with the row id and reloads the list (or, for the mesocycle, the workouts only);
  - `CONFLICT` shows the draft-only message (workout screens only);
  - another error shows the generic copy;
  - the admin and trainer render both show the action.
- **Translation parity** covers the new keys. No E2E.
- No test helper or pattern changes.

### Workout status label

- The status colours live in a plain module, `common/workout-status.ts`, shared by the `workouts` and `mesocycles`
  areas the way `common/full-name.ts` is. They follow the `TrainingWeekTypeChip` pattern (outlined small chip, one
  theme colour per value).
- `WorkoutsTable` (organism) translates the status itself from `common` `workoutStatus.*`. `MesocycleWeekWorkoutsTable`
  (molecule) takes no i18n, so each row carries `status` (for the colour) and `statusLabel`, translated by
  `MesocycleWeeksCard`. Both tables use the `Chip` atom directly; a chip molecule would make the molecule table import
  a sibling molecule.
- Showing the status reads it as data; it does not gate any action, so the delete button still shows on every row.

## Risks / Trade-offs

- [Deleting the last workout of an expanded week makes that week non-collapsible, and a reload sets the workouts to
  "loading", which can briefly collapse the weeks.] → Accepted. It matches how the screen behaves on its first read,
  and the spec only asks for the post-reload state.
- [The trash button shows on non-draft workouts that the API will refuse.] → Intended. The draft-only message tells
  the user why. Copying the rule into the front would break the invariant that business rules belong to the API.
- [The snapshot refresh depends on uncommitted API work.] → The refresh is its own first commit, and it needs the API
  branch with `add-delete-workout-endpoint` running. If the API isn't ready, implementation stops at that step.
- [The two 409 causes, not a draft and a concurrency conflict, share one message.] → Accepted. The API returns the
  same code for both, and a concurrent change on a list is rare.
