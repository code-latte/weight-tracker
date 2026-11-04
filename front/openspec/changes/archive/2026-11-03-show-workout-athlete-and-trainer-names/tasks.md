# Tasks

Ordering note: the dialog work needs no `core` change, so it comes first and can land before the API change
`add-names-to-workouts-list`. The core contract refresh (group 3) is still done before the backoffice work that reads it
(group 4).

## 1. Backoffice: create dialog options

- [x] 1.1 Add `useWorkoutAssignees` at `backoffice/src/presentation/workouts/hooks/use-workout-assignees.ts`, modelled
      on `trainer-assignments/hooks/use-trainer-assignments.ts`: `TRAINER` → `getMyAthletesUseCase` mapped to
      `{ id: athleteId, name, surname, email }`; `ADMIN` → one `getUsersUseCase` split into active `USER`s and active
      `TRAINER`s; `status`, `errorCode`, `reload`, late answers ignored. Verify with `pnpm --filter backoffice
typecheck` and lint (behaviour covered in 1.5).
- [x] 1.2 Add the `PersonPicker` molecule at `backoffice/src/presentation/workouts/components/molecules/person-picker.tsx`
      over the `Autocomplete` atom: "name · email" label (email only without a name), case- and accent-insensitive
      name/email filter via `normalized`, `noOptionsText`, `disabled`, `error`. Verify it imports no MUI widget and
      that `pnpm --filter backoffice lint` passes.
- [x] 1.3 Change `CreateWorkoutDialog` to render `PersonPicker` for Atleta (both roles) and Entrenador (`ADMIN` only),
      fed by `useWorkoutAssignees`; disable pickers and "Crear" until options load; on load failure show the
      `errors:<code>` `Alert` with a "Reintentar" button calling `reload`; map a `required` violation to
      `athleteRequired` / `trainerRequired` and clear it on selection. Keep `validateCreateWorkoutForm` and
      `toRequest` unchanged. Verify with `pnpm --filter backoffice typecheck`.
- [x] 1.4 Update `backoffice/src/i18n/es/workouts.json` and `en/workouts.json` with the `createWorkout` keys in
      `design.md` (changed field labels, `validation.athleteRequired`, `validation.trainerRequired`, `noAthletes`,
      `noTrainers`, `retry`). Verify the translation parity test passes.
- [x] 1.5 Update `backoffice/test/presentation/workouts/components/organisms/create-workout-dialog.test.tsx`, keeping
      its structure and Arrange / Act / Assert phases, to cover every scenario of the `backoffice-workout-creation`
      delta per role: trainer options from `getMyAthletesUseCase` and no `getUsersUseCase` call; admin split by role
      and `isActive`; labels and filtering; required messages and clearing; request carries the picked ids; loading,
      `NETWORK` with retry, `FORBIDDEN`; failure keeps selections; cancel resets; English copy; the trainer picker is
      absent for `TRAINER`. Verify with `pnpm --filter backoffice test`.
- [x] 1.6 Review the dialog, molecule and hook against `assign-trainer-dialog.tsx`, `week-field.tsx` and
      `use-trainer-assignments.ts`. Verify atomic levels, downward imports, no business rule, no `core/infrastructure`
      import, and no change to either `container.ts`.

## 2. Precondition for the list

- [x] 2.1 Confirm the API branch with `add-names-to-workouts-list` is running on `http://localhost:5199`. Verify
      `GET /workouts` returns `athlete` and `trainer` (`id`, `email`, `name`, `surname`, `role`, or `null` for the
      trainer) on each item, and that `POST /workouts` still returns the plain workout. If it is not available, stop
      here: groups 3–5 wait for it.

## 3. Core: contract refresh and list type

- [x] 3.1 Run `pnpm --filter @weight-tracker/core openapi:refresh` and `openapi:generate`. Verify the diff of
      `core/openapi/v1.json` and `core/src/contract/generated` adds `WorkoutListItemResponse`, points the `GET /workouts`
      items at it, and leaves `WorkoutResponse` (and `POST /workouts`) unchanged.
- [x] 3.2 Add `WorkoutListItem` (= `WorkoutListItemResponse`) in `core/src/domain/workouts/workout.ts`, export it from
      `core/src/domain/index.ts`, and retype the list path to it: `WorkoutRepositoryInterface.getAll`,
      `WorkoutRepository.getAll`, `GetWorkoutsUseCase` and `Dependencies.getWorkoutsUseCase`. Verify with
      `pnpm --filter @weight-tracker/core typecheck`.
- [x] 3.3 Update the `getAll` of the workout fakes in `core/test` and the list fixtures (with `athlete` / `trainer`),
      and add a case to the repository and use case tests that the people are passed through, `trainer: null`
      included. Verify with `pnpm --filter @weight-tracker/core test`.
- [ ] 3.4 Commit the snapshot, the generated types, the alias and the retyping as a step of their own. Verify
      `pnpm typecheck` passes for every workspace, the app included.
- [x] 3.5 Review core against `core/src/domain/workouts/workout.ts` and the existing workout repository tests. Verify
      `Workout` is unchanged, `WorkoutListItem` is the only new alias, there is no new port, use case or `Dependencies`
      key, and no hand edit under `contract/generated`.

## 4. Backoffice: names in the list

- [x] 4.1 Retype `useWorkouts` and the `WorkoutsTable` prop to `WorkoutListItem[]`, and change `WorkoutsTable` to show `fullNameOf(workout.athlete) ?? workout.athlete.email` and, for `ADMIN`, the
      same for `workout.trainer` or `assignedPlans.noTrainer` when it is `null`. Verify no id is rendered and
      `pnpm --filter backoffice typecheck` passes.
- [x] 4.2 Add `assignedPlans.noTrainer` ("Sin entrenador" / "No trainer") to both `workouts.json` files. Verify the
      parity test passes.
- [x] 4.3 Update `backoffice/test/presentation/workouts/views/assigned-plans-view.test.tsx` fixtures with names and
      cover the `backoffice-assigned-plans` delta: athlete name and email fallback, admin trainer name and
      "Sin entrenador", ids absent, trainer column and names absent for `TRAINER`, English "No trainer". Verify with
      `pnpm --filter backoffice test`.
- [x] 4.4 Update the backoffice test helpers that fake `getWorkoutsUseCase` (`test/support/render-with-providers.tsx`,
      `test/infrastructure/container.test.ts`) and any other list fixture so they typecheck; `workout-view.test.tsx`
      keeps `Workout` / `WorkoutDetail`. Verify with `pnpm --filter backoffice typecheck && pnpm --filter backoffice test`.
- [x] 4.5 Review the table against `trainer-assignments-table.tsx`. Verify `fullNameOf` is reused and no join or
      extra request was added to the list.

## 5. Backoffice E2E and final verification

- [x] 5.1 Update `backoffice/e2e/assigned-plans.spec.ts`: workout fixtures carry names; `page.route` serves
      `GET /trainer-athletes` and `GET /users`; a trainer and an admin each create a workout by picking people in the
      autocompletes; the list shows names. Also update `e2e/workout.spec.ts`, whose create flow typed the athlete id
      and whose list mock lacked `athlete`. Verify with `pnpm test:e2e`.
- [x] 5.2 Run the drift check from `AGENTS.md`. Verify `.stpr/template/` is byte-identical to its counterparts.
- [x] 5.3 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`. Verify every command passes.

## Workflow follow-up

- Plan and land `add-names-to-workouts-list` in `api/.stpr/plan/openspec/` before group 2.
- Archive the change after review.
