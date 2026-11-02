# Tasks

## 1. Preconditions

- [x] 1.1 Confirm `add-backoffice-athletes-and-invitations` is archived (`openspec list` no longer shows it, and
      `openspec/specs/backoffice-athletes/spec.md` exists). Confirm the API branch with `add-trainer-to-users-list` is
      running on `http://localhost:5199`. Verify `GET /users` as an admin returns `trainer` on each item.

## 2. Core: contract refresh

- [x] 2.1 Run `pnpm --filter @weight-tracker/core openapi:refresh` and `openapi:generate`, then check the diff of
      `core/openapi/v1.json` and `core/src/contract/generated`. - Verify that the `GET /users` item schema has a nullable `trainer` referencing the user profile schema. - Verify that nothing else changed unexpectedly.
- [x] 2.2 If the `GET /users` item schema name changed, repoint `UserSummary` in `core/src/domain/users/user.ts` to it.
      Keep the activate/deactivate responses typed as before. Verify with `pnpm --filter @weight-tracker/core typecheck`.
- [x] 2.3 Add `trainer` to the `UserSummary` fixtures in `core/test`. Add a "current trainer kept" case to
      `get-users-use-case.test.ts` and `user-repository.test.ts`, per the `user-administration` delta. Verify with
      `pnpm --filter @weight-tracker/core test`.
- [ ] 2.4 Commit the snapshot, the generated types and the fixture updates as a step of their own. Verify that
      `pnpm typecheck` passes for every workspace (app included).
- [x] 2.5 Review core against `core/src/domain/users/user.ts` and the existing user tests. Verify there is no new
      port, use case or `Dependencies` key, and no hand edit under `contract/generated`.

## 3. Backoffice: shared pieces in `presentation/common/`

- [x] 3.1 Move `users/components/molecules/user-row-actions.tsx` to `common/components/molecules/row-actions.tsx` as
      `RowActions`, and update `users-table.tsx`'s import. Verify that `users-view.test.tsx` and
      `change-user-activation-dialog.test.tsx` still pass.
- [x] 3.2 Add the `Autocomplete` atom at `common/components/atoms/autocomplete.tsx`, over MUI `Autocomplete` with the
      input through the `TextField` atom. Verify with `pnpm --filter backoffice typecheck` and lint. Behaviour is
      covered in 5.6.
- [x] 3.3 Add the `common` copy in `es` and `en`: `menu.users.trainerAssignments` and `endRelationship.*`. Verify the
      translation parity test passes.
- [x] 3.4 Add `common/hooks/use-end-trainer-athlete.ts` over `endTrainerAthleteUseCase`, modelled on
      `use-change-user-activation.ts`. Verify through 3.5.
- [x] 3.5 Add `common/components/organisms/end-trainer-athlete-dialog.tsx`, modelled on
      `change-user-activation-dialog.tsx`, with its test `end-trainer-athlete-dialog.test.tsx`. The test covers: - the title with the athlete's name, the destructive "Finalizar" button, and Cancelar sending nothing; - success forwarding `{ trainerId, athleteId }` once; - `NOT_FOUND` / `CONFLICT` messages, and the stale close; - `NETWORK` falling back to the common message.

      Verify that the test passes.

- [x] 3.6 Extend the admin branch of `navigation-drawer.tsx`: "Todos los usuarios", "Asignación de entrenadores"
      (`/trainer-assignments`), "Invitaciones". Update `navigation-drawer.test.tsx` per the `backoffice-navigation`
      delta. It covers: - the admin order and the link; - the selected state on `/trainer-assignments`; - the trainer not seeing the entry.

      Verify that the test passes.

## 4. Backoffice: `athletes` area

- [x] 4.1 Add the `athletes` copy in `es` and `en`: `list.columns.actions`, `list.actions.open` and
      `list.actions.endRelationship`. Verify translation parity.
- [x] 4.2 Give `AthletesTable` an optional `onEnd` that renders an actions column of `RowActions`, as `UsersTable`
      does. Verify with typecheck and through 4.3.
- [x] 4.3 Wire `AthletesView`: - end target, success message and `EndTrainerAthleteDialog`; - `trainerId` from `useCurrentUser().profile.id`, and `onEnd` only once the profile is loaded; - reload on success and on a stale close.

      Extend `athletes-view.test.tsx` per the `backoffice-athletes` delta:
      - the action is offered;
      - the request carries the trainer's own id and the row's `athleteId`;
      - success message and reload;
      - `NOT_FOUND` keeps the dialog open, and the stale close reloads;
      - the English copy.

      Verify that the test passes.

- [x] 4.4 Add the trainer end-relationship case to `backoffice/e2e/athletes.spec.ts`, with `page.route` intercepting
      `GET /trainer-athletes` and `PUT /trainer-athletes/{id}/end`. Verify with `pnpm test:e2e`.
- [x] 4.5 Review the athletes changes against `users-view.tsx` and `users-table.tsx`. Verify that imports flow downward
      and nothing is imported from another area.

## 5. Backoffice: `trainer-assignments` area

- [x] 5.1 Add the `trainerAssignments` namespace (`es` and `en`) and register it in `src/i18n/resources.ts`. Verify
      translation parity.
- [x] 5.2 Add `filterTrainerAssignments` in `presentation/trainer-assignments/filter-trainer-assignments.ts`, modelled
      on `filter-users.ts`, with `test/presentation/trainer-assignments/filter-trainer-assignments.test.ts`. The test
      covers: - search by athlete name, email and trainer name, ignoring case and accents; - with / without / no pill; - combined filters and the API order kept.

      Verify that the test passes.

- [x] 5.3 Add `useTrainerAssignments` (one `getUsersUseCase` call, split into `USER` athletes and active `TRAINER`
      options, API order kept, `reload`) and `useAssignTrainer` (over `linkAthleteToTrainerUseCase`). Model them on
      `use-users.ts` and `use-create-user.ts`. Verify through 5.6 and 5.7.
- [x] 5.4 Add the `AssignmentFilter` molecule ("Con entrenador" / "Sin entrenador", single selection, press again to
      clear), modelled on `role-filter.tsx`. Verify through 5.7.
- [x] 5.5 Add the `TrainerAssignmentsTable` organism (full name, email, trainer or "Sin entrenador", and `RowActions`
      with exactly one action per row), modelled on `users-table.tsx`. Verify through 5.7.
- [x] 5.6 Add the `AssignTrainerDialog` organism, modelled on `create-user-dialog.tsx`, with
      `assign-trainer-dialog.test.tsx`. The test covers: - only the given trainers are offered, filterable by name and email; - the required error blocks sending; - one request with the chosen `trainerId` and the athlete's id; - success; - `VALIDATION`, `NOT_FOUND` and `CONFLICT` keep the selection, and the stale close; - Cancelar sends nothing.

      Verify that the test passes.

- [x] 5.7 Add `TrainerAssignmentsView` and the route `src/app/trainer-assignments/page.tsx` (default export only),
      modelled on `users-view.tsx` and `app/users/page.tsx`. Add `trainer-assignments-view.test.tsx` per the
      `backoffice-trainer-assignment` delta. As an admin it covers: - only `USER` rows, the trainer name or "Sin entrenador"; - exactly one action per row, asserting the other is absent; - search, pills, empty, no-match, and `LoadError` with retry; - assign and end success messages with a reload; - inactive trainers are not offered; - the English copy.

      As a trainer it covers the `FORBIDDEN` error with no actions. Verify that the test passes.

- [x] 5.8 Add `backoffice/e2e/trainer-assignments.spec.ts` with `page.route` intercepting every API call. An admin
      opens the screen from the drawer, assigns a trainer to an athlete without one, and ends the relationship of an
      athlete with one. Verify with `pnpm test:e2e`.
- [x] 5.9 Review the `trainer-assignments` area against `users-view.tsx`, `use-users.ts`, `users-table.tsx`,
      `create-user-dialog.tsx` and `role-filter.tsx`. Verify: - atomic levels and folders are correct; - only atoms import MUI widgets, and there is no cross-area import; - every string is in `es` and `en`.

## 6. Integration checks

- [x] 6.1 Run the drift check of `.stpr/template/` from `AGENTS.md`. Verify there is no `DRIFT:` output.
- [x] 6.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`. Verify that all pass, app
      included.

## Workflow follow-up

- Archive `add-backoffice-athletes-and-invitations` before archiving this change, so the `backoffice-navigation`
  MODIFIED blocks apply over its text.
- Archive this change after review, and verify `openspec/specs/backoffice-trainer-assignment/spec.md` exists.
