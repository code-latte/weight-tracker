# Change Proposal: Athlete And Trainer Names In Workouts

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/show-workout-athlete-and-trainer-names/`.

## Change

In the backoffice "Planes asignados" screen, show the athlete's full name (and, for `ADMIN`, the trainer's) instead
of their ids, and replace the free-text "ID del atleta" / "ID del entrenador" fields of the create workout modal with
MUI `Autocomplete` pickers over the athletes (and, for `ADMIN`, the trainers).

## Objective

`WorkoutsTable` prints `workout.userId` and `workout.trainerId`, and `CreateWorkoutDialog` asks the user to type raw
UUIDs. Neither is usable: nobody can tell whose workout a row is, and creating a workout needs the id copied from
somewhere else. The athletes, trainers and the `Autocomplete` atom already exist (change
`add-backoffice-trainer-assignment`), so the modal only needs to use them. The list needs names the workouts
endpoint does not return today.

## Capabilities

Backoffice, "Planes asignados" (`/assigned-plans`):

- **List.** The "Atleta" column shows the athlete's full name. For `ADMIN`, the "Entrenador" column shows the
  trainer's full name, or "Sin entrenador" when the workout has none. Names come from the `GET /workouts` response;
  the front does no join.
- **Create workout modal.**
  - "Atleta" becomes an `Autocomplete`, searchable by full name or email, labelled "Full name · email".
    - `TRAINER`: options are their current athletes (`GET /trainer-athletes`).
    - `ADMIN`: options are users with role `USER` from `GET /users`.
  - "Entrenador" (`ADMIN` only) becomes an `Autocomplete` over users with role `TRAINER` from the same `GET /users`
    response, searchable and labelled the same way.
  - The selected option's id fills `userId` / `trainerId`; the request sent by `POST /workouts` is unchanged.
  - While options load, the pickers are disabled with a loading indicator. If loading fails, the modal shows the
    error with a retry and the create button stays disabled. Name, description and week stay editable.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER`
  - `TRAINER`:
    - sees the "Atleta" column with names; must **not** see the "Entrenador" column (unchanged);
    - in the modal, picks among their own athletes; must **not** see the "Entrenador" picker, and the request
      carries no `trainerId` (unchanged).
  - `ADMIN`:
    - sees both "Atleta" and "Entrenador" columns with names;
    - in the modal, picks an athlete and a trainer from `GET /users`.
- app -> lands here: no -> no screen, hook or copy changes. The snapshot refresh regenerates types shared through
  `core`, so the app's typecheck and tests must stay green.

Which athlete a trainer may assign, and whether a trainer and an athlete fit together, stays an API rule. The pickers
only narrow the choice as a presentation aid.

## Classification

Core:

- `Workout` alias -> entity type -> `core/src/domain/workouts/` -> unchanged source; the regenerated schema gains the
  athlete and trainer summaries.
- No new repository method, use case or `Dependencies` key: `getWorkoutsUseCase`, `createWorkoutUseCase`,
  `getUsersUseCase` and `getMyAthletesUseCase` already exist and are registered in the backoffice container.
- `validateCreateWorkoutForm` -> unchanged; `userId` / `trainerId` remain required strings filled by the pickers.

Backoffice, `workouts` domain area:

- `WorkoutsTable` -> organism, changed to render full names ->
  `backoffice/src/presentation/workouts/components/organisms/workouts-table.tsx`.
- `CreateWorkoutDialog` -> organism, changed to replace the two `TextField`s with pickers ->
  `backoffice/src/presentation/workouts/components/organisms/create-workout-dialog.tsx`.
- `PersonPicker` (labelled `Autocomplete` over `{ id, fullName, email }` options, with loading and no-options copy)
  -> molecule -> `backoffice/src/presentation/workouts/components/molecules/person-picker.tsx`. Used twice by the
  dialog; drop it and use the atom directly if the proposal finds it adds nothing.
- `useWorkoutAssignees` (athletes, trainers, status, errorCode, reload; per role: `getMyAthletesUseCase` for
  `TRAINER`, one `getUsersUseCase` call split by role for `ADMIN`) -> state hook ->
  `backoffice/src/presentation/workouts/hooks/use-workout-assignees.ts`.

Shared pieces (reused, not changed):

- `Autocomplete` atom -> `backoffice/src/presentation/common/components/atoms/autocomplete.tsx`.
- `fullNameOf` -> `backoffice/src/presentation/common/full-name.ts`.
- `useCurrentUser` for the role.
- Copy: `workouts` namespace (`backoffice/src/i18n/{es,en}/workouts.json`).

## Canonical Template Mapping

- Picker usage inside a dialog -> `backoffice/src/presentation/trainer-assignments/components/organisms/assign-trainer-dialog.tsx`
  -> the existing `Autocomplete` in a form dialog, with "Full name · email" labels and name/email filtering.
- `CreateWorkoutDialog` structure -> itself -> form unmounted on close, error `Alert`, structural validation.
- `useWorkoutAssignees` -> `backoffice/src/presentation/trainer-assignments/hooks/use-trainer-assignments.ts` (one
  `GET /users` split by role) and `backoffice/src/presentation/athletes/` list hook (`GET /trainer-athletes`).
- `WorkoutsTable` names -> `backoffice/src/presentation/trainer-assignments/components/organisms/trainer-assignments-table.tsx`
  -> full name via `fullNameOf`, "Sin entrenador" fallback.
- `PersonPicker` -> the molecules in `backoffice/src/presentation/workouts/components/molecules/` (e.g. `week-field.tsx`)
  -> labelled field wrapping one atom, value and error passed in.
- Atom, molecule and organism references are `pending` in `.stpr/template/README.md`. Follow the backoffice shapes
  above.
- Tests -> `backoffice/test/presentation/workouts/views/assigned-plans-view.test.tsx`,
  `backoffice/test/presentation/workouts/components/organisms/create-workout-dialog.test.tsx`,
  `backoffice/e2e/assigned-plans.spec.ts`.

UI references come from the backoffice only. `.stpr/template/` and its README are not updated here.

## Screens And Navigation

- backoffice -> `/assigned-plans` -> `AssignedPlansView` -> drawer -> list with names; unchanged loading, empty and
  error states.
  - the existing create button -> `CreateWorkoutDialog`:
    - on open: options load; failure shows the error with retry inside the modal;
    - success: unchanged, the modal closes and `/workout?id=…` opens;
    - failure: unchanged, the modal stays open with the error and the selections kept;
    - Cancel: closes without a request.

## API Contract

- `GET /workouts` -> in `core/openapi/v1.json`: **no, not with names** -> each `WorkoutResponse` gains the athlete
  (`id`, `name`, `surname`) and the trainer (`id`, `name`, `surname`, or `null`). Provided by a new API change,
  `add-names-to-workouts-list` (to be planned in `api/.stpr/plan/openspec/`). Failures: `401`, `403`.
- `POST /workouts` -> yes -> `CreateWorkoutRequest` unchanged (`userId`, `trainerId`, `name`, `description`,
  `workoutDate`). Its response shape may gain the same summaries; the front only reads `id`.
- `GET /users` (`ADMIN`) -> yes -> `UserListItemResponse[]`, split by `role`. Failures: `401`, `403`.
- `GET /trainer-athletes` (`TRAINER`) -> yes -> `TrainerAthleteListItemResponse[]` (`athleteId`, `name`, `surname`,
  `email`). Failures: `401`, `403`.

Run `openapi:refresh` + `openapi:generate` once, with the API running from the branch that has
`add-names-to-workouts-list`. Commit the snapshot and the generated types as a step of their own. No hand-written type.

## Held State

- `useWorkoutAssignees` -> athlete and trainer options, status, error code -> loaded when the create form mounts and on
  `reload` -> resets when the modal closes (the form unmounts).
- `CreateWorkoutDialog` form state -> `userId` / `trainerId` now set from the picked option -> resets on close
  (unchanged).
- Nothing is cached across screens or kept after logout.

## Forms And Structural Validation

- Create workout -> athlete -> required (existing `userId` rule, now "Selecciona un atleta.").
- Create workout -> trainer (`ADMIN`) -> required (existing `trainerId` rule, now "Selecciona un entrenador.").
- Name, description, week -> unchanged.

Whether the trainer may train that athlete, or the athlete is active, is the API's answer through the existing
create errors.

## Errors And Copy

- Options fail to load -> `FORBIDDEN` / `NETWORK` / `UNEXPECTED` -> `Alert` inside the modal with the existing
  `errors:` message and a "Reintentar" button; create disabled.
- Session expired -> `UNAUTHORIZED` -> existing session handling.
- Create failures -> unchanged.
- No new error code. The HTTP error mapper is not touched.

Copy changes, `workouts` namespace:

- `createWorkout.fields.userId` -> "Atleta" -> "Athlete" (was "ID del atleta")
- `createWorkout.fields.trainerId` -> "Entrenador" -> "Trainer" (was "ID del entrenador")
- `createWorkout.validation.*` for the two pickers -> "Selecciona un atleta.", "Selecciona un entrenador." ->
  "Select an athlete.", "Select a trainer." (decide at proposal time whether these replace the generic required key)
- `createWorkout.noAthletes` -> "Ningún atleta coincide." -> "No athletes match."
- `createWorkout.noTrainers` -> "Ningún entrenador coincide." -> "No trainers match."
- `createWorkout.loadingOptions` -> "Cargando…" -> "Loading…"
- `createWorkout.retry` -> "Reintentar" -> "Retry"
- `assignedPlans.noTrainer` -> "Sin entrenador" -> "No trainer"

## Platform Capabilities And Dependencies

None. The `Autocomplete` atom and `@mui/material` already exist.

## Out Of Scope

- Changing the athlete or trainer from the workout screen (`workout-details-card.tsx`) and `PUT /workouts/{id}`.
- Showing names on the workout screen or in the app.
- Filtering or searching the workouts list by athlete or trainer.
- Server-side search or pagination of the pickers.
- Must not change: `POST /workouts` request, `validateCreateWorkoutForm` rules, the app.

## Notes

- **Stop case.** The list part must not start until `add-names-to-workouts-list` is merged and running for the
  snapshot refresh. The modal part needs no API change and could ship first if the proposal splits it.
- **Why an API change for the list.** A front-side join would need `GET /users` for `ADMIN` and `GET /trainer-athletes`
  for `TRAINER`, but the latter only returns _current_ athletes: workouts of a former athlete would fall back to an
  id. Names belong in the workouts response. Confirm this before the proposal; the fallback is the join, accepting
  that gap.
- **Option filtering.** Recommended: for `ADMIN`, list only active users (`isActive`) in both pickers, as in
  `assign-trainer-dialog.tsx`. Role split and active filtering are display choices; the API still decides.
- **Trainer and athlete coupling (`ADMIN`).** The pickers are independent; the front does not narrow trainers by the
  chosen athlete's current trainer. Optional presentation aid to confirm at proposal time: pre-select the athlete's
  current trainer (`UserListItemResponse.trainer`) when the trainer field is still empty.
- **Verification tiers:**
  - core unit: none new; workout repository fixtures gain the summaries where the regenerated types require them.
  - backoffice component tests:
    - `assigned-plans-view.test.tsx`: athlete full name per row; as `ADMIN`, trainer full name and "Sin entrenador";
      as `TRAINER`, the trainer column is absent.
    - `create-workout-dialog.test.tsx`: as `TRAINER`, athlete options come from `getMyAthletesUseCase` and the
      trainer picker is absent; as `ADMIN`, athlete options are `USER`s and trainer options are `TRAINER`s from one
      `getUsersUseCase` call; filtering by name and email; required errors block sending; the request carries the
      picked ids; options load failure shows the error, disables create, and retry reloads.
  - backoffice E2E (`e2e/assigned-plans.spec.ts`): the list shows names; a trainer and an admin each create a workout
    picking from the autocompletes. All API calls are intercepted with `page.route`.
  - translation parity passes.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed.
