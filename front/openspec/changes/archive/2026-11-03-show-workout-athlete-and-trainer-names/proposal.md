# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-show-workout-athlete-and-trainer-names.md`.

## Why

"Planes asignados" prints each workout's `userId` and `trainerId`, and the create workout dialog asks staff to type raw
UUIDs. Nobody can tell whose workout a row is, and creating one needs an id copied from another screen. The athletes,
the trainers and an `Autocomplete` atom already exist in the backoffice, so the dialog can offer people by name; the
list needs names the workouts endpoint does not return yet.

## What Changes

- **List (`/assigned-plans`).** The "Atleta" column shows the athlete's full name instead of the id. For `ADMIN`, the
  "Entrenador" column shows the trainer's full name, or "Sin entrenador" when the workout has none. A person without a
  name shows their email. Names come from the
  `GET /workouts` response; the front does no join.
- **Create workout dialog.** "ID del atleta" and "ID del entrenador" become `Autocomplete` pickers labelled "Atleta"
  and "Entrenador", searchable by full name or email, each option shown as "Full name · email":
  - `TRAINER`: athlete options are their current athletes (`GET /trainer-athletes`); no trainer picker (unchanged).
  - `ADMIN`: athlete options are active users with role `USER`, trainer options are active users with role `TRAINER`,
    both from one `GET /users` call.
  - The chosen option's id fills `userId` / `trainerId`; the `POST /workouts` request is unchanged.
  - While options load, the pickers are disabled. If they fail to load, the dialog shows the error with "Reintentar"
    and "Crear" is disabled; Nombre, Descripción and Semana stay editable.
- **Snapshot refresh.** `core/openapi/v1.json` and the generated types are refreshed to pick up
  `WorkoutListItemResponse`, the new item schema of `GET /workouts`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `backoffice-assigned-plans`: rows show the athlete's full name instead of the id; the admin-only trainer column shows
  the trainer's full name or "Sin entrenador" instead of the id.
- `backoffice-workout-creation`: the athlete and trainer id fields become pickers over people loaded per role, with
  loading and load-failure states; required messages and copy change accordingly.

### Fronts and roles

- **backoffice**, `ADMIN` and `TRAINER`:
  - `TRAINER` sees athlete names; must **not** see the "Entrenador" column nor the "Entrenador" picker, and the
    creation carries no `trainerId` (all unchanged). Picks only among their current athletes.
  - `ADMIN` sees athlete and trainer names in the list, and both pickers in the dialog.
- **app**: does not land. No screen, hook or copy changes; the regenerated `core` types must keep its typecheck and
  tests green.

Which athlete a trainer may assign, whether a trainer and an athlete fit together, or whether an inactive user can
receive a workout stays an API rule. Splitting `GET /users` by role and keeping active users is display filtering.

### Canonical references

`.stpr/template/README.md` lists atom, molecule and organism references as `pending`; the backoffice references
below are followed, and no new pattern is established.

- Pickers in a form dialog -> `backoffice/src/presentation/trainer-assignments/components/organisms/assign-trainer-dialog.tsx`.
- Options hook -> `backoffice/src/presentation/trainer-assignments/hooks/use-trainer-assignments.ts` (one `GET /users`
  split by role) and `backoffice/src/presentation/athletes/hooks/use-athletes.ts` (`GET /trainer-athletes`).
- Names in the table -> `backoffice/src/presentation/trainer-assignments/components/organisms/trainer-assignments-table.tsx`
  (`fullNameOf`, "Sin entrenador").
- Domain alias -> `core/src/domain/workouts/workout.ts`: gains `WorkoutListItem` (= `WorkoutListItemResponse`); `Workout`
  (= `WorkoutResponse`) is unchanged and keeps serving create and update.

### Components

- `WorkoutsTable` -> organism, changed.
- `CreateWorkoutDialog` -> organism, changed.
- `PersonPicker` -> new molecule in `backoffice/src/presentation/workouts/components/molecules/` wrapping the
  `Autocomplete` atom for `{ id, fullName, email }` options, used twice by the dialog.
- Reused: `Autocomplete`, `Alert`, `Button`, dialog atoms, table atoms, `TextField`, `WeekField`, `fullNameOf`.

### API contract

- `GET /workouts` -> **not yet with names** -> returns `WorkoutListItemResponse[]`: the `WorkoutResponse` fields plus
  `athlete` (`UserProfileResponse`: `id`, `email`, `name`, `surname`, `role`) and `trainer` (same, or `null`). Provided
  by the API change `add-names-to-workouts-list`
  (`api/.stpr/plan/openspec/proposal-change-add-names-to-workouts-list.md`). `email` is part of the profile so a person
  without a name still shows something other than an id, as in the pickers.
- `POST /workouts` -> present -> `CreateWorkoutRequest` unchanged; still returns `WorkoutResponse`, of which only `id` is read.
- `GET /users` (`ADMIN`) -> present -> `UserListItemResponse[]`.
- `GET /trainer-athletes` (`TRAINER`) -> present -> `TrainerAthleteListItemResponse[]`.

### Stop case

The list part (and the snapshot refresh) must not start until `add-names-to-workouts-list` is merged and running.
The dialog part needs no API change and may be implemented first; tasks are ordered so it can be.

## Impact

- `core`: `core/openapi/v1.json` and `core/src/contract/generated` regenerated; new `WorkoutListItem` alias, and
  the list path retyped to it (repository port and implementation, `GetWorkoutsUseCase`, the type of
  `Dependencies.getWorkoutsUseCase`); workout test fakes and fixtures follow. No new port, repository method, use case
  or `Dependencies` key.
- `backoffice`: `workouts` domain area (table, dialog, new molecule, new state hook), `workouts` translations in `es`
  and `en`, the assigned plans and create dialog component tests, `e2e/assigned-plans.spec.ts`.
- `app`: none beyond staying green with the regenerated types.
- No new dependency, no new error code, the HTTP error mapper is untouched.
- Out of scope: changing the athlete or trainer from the workout screen (`PUT /workouts/{id}`), names on the workout
  screen or in the app, filtering the list by person, server-side search of the pickers.
