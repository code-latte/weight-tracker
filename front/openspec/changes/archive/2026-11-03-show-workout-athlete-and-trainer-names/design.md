# Design

## Context

See `proposal.md` for motivation and `specs/` for the required behaviour.

- `Workout` (`core/src/domain/workouts/workout.ts`) aliases `WorkoutResponse`, which today carries only `userId` and
  `trainerId` and serves the list, create and update. `WorkoutsTable` prints the ids as-is.
- `CreateWorkoutDialog` keeps `CreateWorkoutFormValues` (`userId`, `trainerId` as strings) and validates them with
  `validateCreateWorkoutForm` in `core`, which reports `required` or `uuid` per id field.
- The backoffice container already registers `getUsersUseCase` (`UserSummary[]`, `ADMIN` only) and
  `getMyAthletesUseCase` (`TrainerAthlete[]`, `TRAINER` only).
- `assign-trainer-dialog.tsx` already shows the target picker shape: the `Autocomplete` atom, `fullNameOf`, a
  "name · email" label and a filter built on `normalized` from `presentation/common/search-text.ts`.

## Goals / Non-Goals

**Goals:**

- Reuse the existing use cases, `Autocomplete` atom and helpers; add no `core` code beyond the regenerated contract.
- Let the dialog part ship before the API change if needed.

**Non-Goals:**

- No change to `validateCreateWorkoutForm` or the `CreateWorkoutRequest` shape.
- No shared "people" abstraction across domain areas; the `workouts` area owns its hook and molecule.

## Decisions

### Core

- **Contract.** The API change `add-names-to-workouts-list` adds `WorkoutListItemResponse` (the `WorkoutResponse`
  fields plus `athlete: UserProfileResponse` and `trainer: UserProfileResponse | null`) as the item of `GET /workouts`;
  `POST /workouts` keeps `WorkoutResponse`. Run `pnpm --filter @weight-tracker/core openapi:refresh` + `openapi:generate`.
  `workout.ts` gains `WorkoutListItem = components["schemas"]["WorkoutListItemResponse"]`; `Workout` is unchanged and
  keeps serving create and update. The people are read as `WorkoutListItem["athlete"]` / `WorkoutListItem["trainer"]`,
  with no extra alias.
- **List path retyped, nothing added.** `WorkoutRepositoryInterface.getAll()`, `WorkoutRepository.getAll`,
  `GetWorkoutsUseCase` and the type of `Dependencies.getWorkoutsUseCase` move from `Workout[]` to `WorkoutListItem[]`.
  `WorkoutListItem` is exported from `core/src/domain/index.ts`. The `core/test` workout fakes (`getAll`) and the list
  fixtures follow.
- **No new `Dependencies` key, port, repository method or use case.** `getWorkoutsUseCase` already exists; only its
  type changes. Neither `container.ts` is edited. The app registers the use case but no screen reads it, so it only has
  to keep typechecking.
- **Validation unchanged.** The pickers write the selected id into `userId` / `trainerId`, so `required` still fires
  when nothing is selected and `uuid` cannot fire from the UI (the ids come from the API). Keeping the core function
  avoids touching the shared validator and its tests. _Alternative:_ drop the `uuid` check — rejected, it is shared
  core code and harmless.

### Backoffice — `workouts` domain area

- **`useWorkoutAssignees`** — state hook, `backoffice/src/presentation/workouts/hooks/use-workout-assignees.ts`.
  - Input: the role (`"ADMIN" | "TRAINER"`), read by the dialog from `useCurrentUser()`.
  - `TRAINER`: `getMyAthletesUseCase` → `athletes` mapped to `{ id: athleteId, name, surname, email }`; `trainers = []`.
  - `ADMIN`: one `getUsersUseCase` → `athletes` = active `USER`s, `trainers` = active `TRAINER`s, API order kept.
  - Exposes `status` (`loading | loaded | failed`), `athletes`, `trainers`, `errorCode`, `reload`.
  - Follows `use-trainer-assignments.ts`: `runWithSession`, a generation ref to drop late answers, request on mount.
  - Mapping both sources to one `Assignee` shape (`{ id, name, surname, email }`, a presentation type local to the
    hook) keeps the molecule and dialog role-agnostic. _Alternative:_ two hooks and a role branch in the dialog —
    rejected, it duplicates loading/error handling.
  - The role split and `isActive` filter are display filtering, like `useTrainerAssignments`; the API still decides
    through the create errors.
- **`PersonPicker`** — molecule, `backoffice/src/presentation/workouts/components/molecules/person-picker.tsx`.
  - Wraps the `Autocomplete` atom for `Assignee` options: `label`, `options`, `value` (selected id or empty),
    `onChange(id)`, `noOptionsText`, `disabled`, `loading`, `error` message.
  - Owns the "name · email" label (email only when `fullNameOf` returns null) and the case- and accent-insensitive
    name/email filter, mirroring `assign-trainer-dialog.tsx`.
  - Imports only the atom and helpers; no MUI widget import.
- **`CreateWorkoutDialog`** — organism, changed.
  - Replaces the two `TextField`s with `PersonPicker`s fed by `useWorkoutAssignees`. The hook lives in the form
    component, so it mounts and resets with the dialog (no state outlives a closed dialog).
  - While `status !== "loaded"`, pickers are disabled and "Crear" is disabled. On `failed`, an `Alert` above the
    fields shows `errors:<code>` with a "Reintentar" `Button` calling `reload`.
  - Picker helper text: a `required` violation on `userId` / `trainerId` shows `createWorkout.validation.athleteRequired`
    / `trainerRequired`; any other violation falls back to the generic `createWorkout.validation.<code>`. Selecting a
    person clears that field's error.
  - Create errors keep their current `Alert`; the selections stay because form state is untouched on failure.
- **`WorkoutsTable`** — organism, changed. Its prop is `WorkoutListItem[]` (as is what `useWorkouts` returns). Cells read `fullNameOf(workout.athlete) ?? workout.athlete.email` and, for
  `ADMIN`, the same for `workout.trainer`, or `assignedPlans.noTrainer` when it is `null`.

### Held state

- `useWorkoutAssignees` → options, status, error code → changed by mount and `reload` → resets when the dialog closes.
- Dialog form state → values, selected ids, violations → resets on close (unchanged).
- Nothing is cached across screens or kept after logout.

### Errors and translations

No new error code; the HTTP error mapper is untouched. Options load failures surface `UNAUTHORIZED` (session handling),
`FORBIDDEN`, `NETWORK`, `UNEXPECTED` through existing `errors:` copy.

`workouts` namespace, `backoffice/src/i18n/{es,en}/workouts.json`:

| Key                                        | es                          | en                 |
| ------------------------------------------ | --------------------------- | ------------------ |
| `createWorkout.fields.userId` (changed)    | Atleta                      | Athlete            |
| `createWorkout.fields.trainerId` (changed) | Entrenador                  | Trainer            |
| `createWorkout.validation.athleteRequired` | Selecciona un atleta.       | Select an athlete. |
| `createWorkout.validation.trainerRequired` | Selecciona un entrenador.   | Select a trainer.  |
| `createWorkout.noAthletes`                 | Ningún atleta coincide.     | No athletes match. |
| `createWorkout.noTrainers`                 | Ningún entrenador coincide. | No trainers match. |
| `createWorkout.retry`                      | Reintentar                  | Retry              |
| `assignedPlans.noTrainer`                  | Sin entrenador              | No trainer         |

The `createWorkout.validation.uuid` key stays: the validator can still return it.

### Components and imports

- Atoms: `Autocomplete`, `Alert`, `Button`, dialog and table atoms, `TextField` — reused, unchanged.
- Molecule: `PersonPicker` (new) — imports atoms and `presentation/common` helpers only.
- Organisms: `CreateWorkoutDialog`, `WorkoutsTable` — import molecules and atoms.
- Imports flow downward; only atoms import MUI widgets. Nothing is shared with the app.

### Tests

Template parity with the existing files; no helper or pattern change.

- `backoffice/test/presentation/workouts/components/organisms/create-workout-dialog.test.tsx`: fakes for
  `getMyAthletesUseCase` / `getUsersUseCase` through the container, per role; covers every scenario of the
  `backoffice-workout-creation` delta, asserting the trainer picker is absent for `TRAINER` and that `getUsersUseCase`
  is never called for a trainer.
- `backoffice/test/presentation/workouts/views/assigned-plans-view.test.tsx`: fixtures gain names; names shown, ids
  absent, "Sin entrenador", trainer column absent for `TRAINER`.
- `backoffice/e2e/assigned-plans.spec.ts`: `page.route` adds `GET /users` and `GET /trainer-athletes`; a trainer and
  an admin each create a workout by picking people; the list shows names.

## Risks / Trade-offs

- [The API change is not ready] → Dialog tasks come first and do not depend on the snapshot; the list tasks wait for
  the refresh.
- [The item schema is not named `WorkoutListItemResponse`] → Only the `WorkoutListItem` alias line points at the name; adjust there.
- [An athlete removed from the trainer between opening and creating] → The API answers with an existing error code;
  the dialog shows it and keeps the selection.
- [Large user lists for `ADMIN`] → Client-side filtering over one request, as in trainer assignment; server-side
  search is out of scope.

## Migration Plan

Front-only deployment after the API change is live. Rolling back the front restores id columns and text fields; the
extra response fields are ignored by older fronts.
