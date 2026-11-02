# Proposal

## Why

`core` already wires linking an athlete to a trainer and ending a relationship (`linkAthleteToTrainerUseCase`,
`endTrainerAthleteUseCase`), but no screen uses them: a trainer cannot stop training an athlete, and an admin cannot
link athletes to trainers at all. The API change `add-trainer-to-users-list` makes `GET /users` return each athlete's
current trainer, so the admin can see and manage assignments from one request without the front joining
relationships.

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-trainer-assignment.md`. Its decisions are kept.

## What Changes

- Refresh the OpenAPI snapshot so the `GET /users` item carries `trainer` (`null` or the trainer's `id`, `email`,
  `name`, `surname`, `role`). No hand-written type; `UserSummary` follows the regenerated schema.
- **`TRAINER`, "Todos los atletas"** (`/athletes`): each row gains a "Finalizar relación" action. After a confirmation
  dialog it sends `PUT /trainer-athletes/{trainerId}/end` with `{ athleteId }`, using the signed-in trainer's id. On
  success the dialog closes, "Relación finalizada." shows and the list reloads.
- **`ADMIN`, new "Asignación de entrenadores"** (`/trainer-assignments`): a table of every `USER` from `GET /users`
  with "Nombre completo", "Email", "Entrenador" (full name or "Sin entrenador") and an actions column; a search
  field (athlete name, athlete email, trainer name) and "Con entrenador" / "Sin entrenador" pills.
  - Rows with a trainer: "Finalizar relación", the same dialog, sending `PUT /trainer-athletes/{trainer.id}/end` with
    `{ athleteId: user.id }`.
  - Rows without one: "Asignar entrenador", a modal with an MUI `Autocomplete` over the `TRAINER` users of the same
    response. Accepting sends `POST /trainer-athletes/{trainerId}` with `{ athleteId: user.id }`.
- **Drawer (`ADMIN`)**: "Usuarios" section becomes "Todos los usuarios", "Asignación de entrenadores",
  "Invitaciones". The trainer drawer is unchanged.
- Shared presentation pieces move to or are created in `presentation/common/`: the end-relationship dialog and its
  hook (used by two areas), a new `Autocomplete` atom, and the row-actions menu (`UserRowActions` → `RowActions`),
  now used by three areas.
- Copy in `es` and `en`: a new `trainerAssignments` namespace, plus keys in `athletes` and `common`.

Not breaking. No new error code; the HTTP error mapper is untouched.

### Fronts and roles

- **backoffice**:
  - `TRAINER` sees "Finalizar relación" on their athletes. They have no "Asignación de entrenadores" entry, and
    opening `/trainer-assignments` by URL shows the `FORBIDDEN` load error from `GET /users`.
  - `ADMIN` sees "Asignación de entrenadores". Each row shows exactly one of "Asignar entrenador" / "Finalizar
    relación", decided only by `trainer` being `null`. Admins must not see an end action on a row without a trainer,
    or an assign action on a row with one. Their `/athletes` behaviour is unchanged (`FORBIDDEN`).
- **app**: no wiring, screen or copy change. The regenerated types must keep its typecheck and tests green.

The front decides no permission. Hiding actions per role is a presentation choice; the API enforces every rule
(`403`, `404`, `409`).

### Canonical references

`.stpr/template/README.md` lists the atom, molecule and organism references as `pending`, so the backoffice shapes are
used:

- view → `presentation/users/views/users-view.tsx`;
- list hook → `users/hooks/use-users.ts`;
- action hooks → `use-change-user-activation.ts` and `use-create-user.ts`;
- tables → `users/components/organisms/users-table.tsx`;
- end dialog → `change-user-activation-dialog.tsx`;
- assign dialog → `create-user-dialog.tsx`;
- pills → `users/components/molecules/role-filter.tsx`;
- filter → `users/filter-users.ts`;
- `Autocomplete` atom → `common/components/atoms/text-field.tsx`;
- route → `app/users/page.tsx`.

UI references come from the backoffice only.

### Component classification

- New atom: `Autocomplete`.
- New molecule: `AssignmentFilter`.
- Moved molecule: `UserRowActions` becomes `RowActions` in `common/`.
- New organisms: `EndTrainerAthleteDialog`, `TrainerAssignmentsTable`, `AssignTrainerDialog`.
- Extended organism: `AthletesTable`.
- Reused: table, text field, chip, button, dialog and alert atoms, plus `HomeSkeleton`, `LoadError` and `AppShell`.

### Stop case and dependencies

- Implementation must not start until the API change `add-trainer-to-users-list` is running for the snapshot refresh.
- This change builds on `add-backoffice-athletes-and-invitations`. That change introduces `backoffice-athletes` and
  modifies `backoffice-navigation`, so it must be archived before this one.

No conflict with the template or the invariants.

## Capabilities

### New Capabilities

- `backoffice-trainer-assignment`: the admin screen that lists athletes with their current trainer and lets an admin
  assign a trainer or end the current relationship.

### Modified Capabilities

- `backoffice-athletes`: the trainer's athletes table gains the "Finalizar relación" action and its confirmation
  flow.
- `backoffice-navigation`: the admin's "Usuarios" section gains "Asignación de entrenadores" between "Todos los
  usuarios" and "Invitaciones", linking to `/trainer-assignments`.
- `user-administration`: listing users also returns each user's current trainer as the API answers it.

## Impact

- **API contract**: `GET /users` item gains `trainer`, from API change `add-trainer-to-users-list`. Not yet in
  `core/openapi/v1.json`, so a refresh is needed. `POST /trainer-athletes/{trainerId}` and
  `PUT /trainer-athletes/{trainerId}/end` are already in the snapshot and in `core`. `GET /trainer-athletes/{trainerId}`
  is not used.
- **core**: regenerated `core/openapi/v1.json` and `core/src/contract/generated`. No new port, repository, use case or
  `Dependencies` key.
- **backoffice**:
  - new route `src/app/trainer-assignments/page.tsx` and new area `src/presentation/trainer-assignments/`;
  - changes in `presentation/athletes/` and in `presentation/common/` (atom, molecule, organism, hook, drawer);
  - an import update in `presentation/users/` for the moved row actions;
  - i18n in `src/i18n/{es,en}` and `resources.ts`.
- **Tests**:
  - backoffice component tests for both views, both dialogs, the atom, the filter and the drawer;
  - E2E `e2e/trainer-assignments.spec.ts` and a new case in `e2e/athletes.spec.ts`;
  - fixtures that build `UserSummary` gain `trainer`.
- **app**: no change beyond regenerated types.
- **Dependencies**: none (`Autocomplete` ships in `@mui/material`).
