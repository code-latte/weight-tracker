# Proposal

## Why

Templates can only be deleted from inside their own screen, and workouts cannot be deleted from the front at all, so
mistaken or abandoned drafts stay in every list. The API now exposes `DELETE /workouts/{workoutId}` (API change
`add-delete-workout-endpoint`), so the front can offer deletion directly from the three tables where trainers and
admins manage templates and workouts.

## What Changes

- Refresh the contract snapshot so `core/openapi/v1.json` holds `DELETE /workouts/{workoutId}` (204, 401, 403, 404,
  409), and regenerate the contract types. This is a commit of its own, made before any code.
- Add workout deletion to `core`: `delete(id)` on the workout repository contract and implementation, a
  `DeleteWorkoutUseCase`, the `deleteWorkoutUseCase` dependency key, and its registration in the backoffice and app
  `container.ts`.
- Backoffice: add a trash icon button to every row of:
  - the workout templates table (`/workout-templates`);
  - the workouts table of "Entrenamientos" (`/assigned-workouts`);
  - the workouts table inside each expanded week of a mesocycle (`/mesocycle?id=…`).
- Every deletion is confirmed in a dialog first. On success the list is read again. On failure the screen shows the
  error. A refused workout deletion (`CONFLICT`) shows "Solo se pueden eliminar entrenamientos en borrador."
- Deleting a template is already wired in `core` and in both containers, and the backoffice uses it from the template
  screen. This change reuses that use case, its hook and its confirm dialog unchanged.

Decisions carried from the brief (`.stpr/plan/openspec/proposal-change-add-backoffice-delete-from-tables.md`):

- The trash button is shown on every row, whatever the workout's status. Only the API applies the draft-only rule and
  the ownership rule. The front shows the API's refusal instead of copying either rule.
- No new error code. `409 → CONFLICT` already exists. The draft-only message is screen copy, not a mapper entry.
- ADMIN and TRAINER see the same button on every row they already see. No role hides it.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `workout-management`: adds deleting a workout through `core` (`DELETE /workouts/{workoutId}`).
- `backoffice-workout-templates`: each row gains a confirmed delete action.
- `backoffice-assigned-workouts`: each row gains a confirmed delete action.
- `backoffice-mesocycle-editor`: each row of an expanded week's workouts table gains a confirmed delete action.

`workout-template-management` does not change. Its "Deleting a template sends its id" requirement already covers the
operation.

Fronts and roles, per capability:

- `workout-management` -> `core`, so it is available to both fronts -> registered in backoffice and app. The app shows
  no UI for it.
- The three backoffice capabilities -> backoffice only -> ADMIN and TRAINER behave the same. The app (USER) gets no
  new UI.

Canonical references, per capability (no reference is `pending`):

- `workout-management` delete -> `core/src/infrastructure/repositories/workout-template-repository.ts` (`delete`) and
  `core/src/application/use-cases/workout-templates/delete-workout-template-use-case.ts`.
- Table delete actions -> `backoffice/src/presentation/workouts/views/workout-template-view.tsx` (deletion state and
  confirm flow), `.../hooks/use-delete-workout-template.ts` (state hook) and
  `.../components/organisms/delete-workout-template-dialog.tsx` (organism).

UI components and their atomic levels:

- Reused unchanged: the `IconButton`, `Alert`, `Dialog*` and `Button` atoms, and the `DeleteWorkoutTemplateDialog`
  organism.
- New: the `DeleteWorkoutDialog` organism.
- Modified: the `WorkoutTemplatesTable` and `WorkoutsTable` organisms, the `MesocycleWeekWorkoutsTable` molecule and
  the `MesocycleWeeksCard` organism.

## Impact

- **API contract**: `core/openapi/v1.json` and `core/src/contract/generated` are refreshed. The endpoint comes from
  the API change `add-delete-workout-endpoint`, which is implemented in `api/` but not committed yet. The refresh
  needs that API running.
- **core**: `workout-repository-interface.ts`, `workout-repository.ts`, new `delete-workout-use-case.ts`,
  `use-cases/workouts/index.ts`, `dependencies.ts`, and tests in `core/test`.
- **backoffice**: `container.ts`; a new `presentation/common/hooks/use-delete-workout.ts` and
  `presentation/common/components/organisms/delete-workout-dialog.tsx` (shared by the `workouts` and `mesocycles`
  areas, so they live in `common` like `use-create-workout-from-template`); the three tables;
  `mesocycle-weeks-card.tsx`; three views; `i18n/{es,en}/{common,workouts,mesocycles}.json`; and three view tests.
- **app**: `container.ts` registration only.
- **No conflict** with the invariants or a stop case: the front holds no business rule and no hand-written domain
  type, and no new dependency is added.
