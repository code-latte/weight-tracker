# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-athletes-and-invitations.md`.

## Why

The API now links trainers to their athletes and lets trainers invite people to become their athletes (API change
`add-trainer-athletes-and-invitations`). `core` knows none of these operations. In the backoffice, the trainer's
"Todos los atletas" and "Invitaciones pendientes" drawer entries lead nowhere, and an admin cannot see which
invitations are being sent.

## What Changes

- **Snapshot refresh**: `openapi:refresh` + `openapi:generate` pick up `/trainer-athletes` (4 operations) and
  `/athlete-invitations` (5 operations), in a commit of their own. No hand-written type.
- **core**:
  - aliases for every new request and response;
  - two repository contracts with their implementations;
  - nine use cases;
  - a shared structural rule set for the invite form, with the email check moved to one shared place;
  - the new keys in `Dependencies`.

  Both fronts register them. The app only gets the wiring.

- **backoffice `/athletes`** (`TRAINER`, "Todos los atletas"):
  - a table of the trainer's current athletes: "Nombre completo", "Email", "Atleta desde";
  - one search field filtering by full name or email.
- **backoffice `/invitations`** ("Invitaciones", one route rendered per role):
  - `TRAINER`:
    - a table with "Email", "Estado", "Enviada", "Caduca";
    - a search field by email and status pills (Pendiente, Aceptada, Rechazada, Revocada);
    - an "Invitar atleta" button that opens a modal with an email field;
    - "Revocar" on `PENDING` rows only, after a confirmation dialog.
  - `ADMIN`:
    - a read-only table with "Entrenador" (`trainerName` + `trainerSurname`), "Email del atleta", "Estado",
      "Enviada", "Caduca";
    - a search field by trainer full name or athlete email, and the same pills.
  - Both: loading, empty, no-match and error states as in the users screen.
- **Drawer**:
  - `TRAINER`: "Atletas" > "Todos los atletas" links to `/athletes`, and the second item links to `/invitations`.
  - `ADMIN`: the second item of "Usuarios" links to `/invitations`.
  - "Invitaciones pendientes" becomes "Invitaciones" for both roles.
- **Users screen**: `GET /users` is now admin-only, so a trainer reaching `/users` by URL sees the `FORBIDDEN` error
  instead of `UNEXPECTED`. The spec and its test change; the view code does not.

### Fronts and roles

- backoffice, `TRAINER`:
  - sees both screens, "Invitar atleta" and "Revocar";
  - must NOT see the "Entrenador" column.
- backoffice, `ADMIN`:
  - sees the invitations screen read-only, with the "Entrenador" column;
  - must NOT see "Invitar atleta", the invite modal, "Revocar" or the revoke dialog;
  - has no "Todos los atletas" entry. Opening `/athletes` by URL gets `403` and shows the load error.
- app, `USER`: wiring only (container keys). No screen, hook or copy changes.

Hiding actions per role is a presentation choice. The API enforces every rule (`403`).

### Components (atomic level, reuse)

- Reused atoms: the table atoms, `text-field`, `chip`, `tooltip`, `button`, `icon-button`, `dialog`,
  `dialog-title`, `dialog-content`, `dialog-actions`, `alert`, `typography`.
- Reused molecules: `HomeSkeleton`, `LoadError`.
- New molecules:
  - `invitations/components/molecules/invitation-status-filter.tsx` (status pills);
  - `invitations/components/molecules/invitation-status-chip.tsx` (status label in the table).
- New organisms:
  - `athletes/components/organisms/athletes-table.tsx`;
  - `invitations/components/organisms/invitations-table.tsx`;
  - `invitations/components/organisms/invite-athlete-dialog.tsx`;
  - `invitations/components/organisms/revoke-invitation-dialog.tsx`.
- New views: `athletes/views/athletes-view.tsx`, `invitations/views/invitations-view.tsx`.
- Extended organism: `common/components/organisms/navigation-drawer.tsx`.
- `fullNameOf` moves from `users/filter-users.ts` to `common/full-name.ts`, so three areas share it without importing
  across areas.

### Canonical references

`.stpr/template/README.md` marks the atom, molecule and organism references as `pending`. This change follows the
backoffice shapes of the users screen. UI references come from the backoffice only.

- Aliases and input types -> `core/src/domain/users/user.ts` and `core/src/domain/workouts/workout.ts` (the
  `…Input` types that join an id to a request).
- Repositories -> `core/src/infrastructure/repositories/user-repository.ts`.
- Use cases:
  - no-input lists -> `get-users-use-case.ts`;
  - id-only actions -> `activate-user-use-case.ts`;
  - request forwarding -> `create-user-use-case.ts`;
  - id + body -> `update-workout-exercise-use-case.ts`.
- Invite form rules -> `core/src/application/validation/user-rules.ts`.
- Routes -> `backoffice/src/app/users/page.tsx`.
- Views -> `backoffice/src/presentation/users/views/users-view.tsx`.
- List hooks -> `use-users.ts`. Invite hook -> `use-create-user.ts`. Revoke hook -> `use-change-user-activation.ts`.
- Tables -> `users-table.tsx`.
- `InviteAthleteDialog` -> `create-user-dialog.tsx`.
- `RevokeInvitationDialog` -> `change-user-activation-dialog.tsx`.
- `InvitationStatusFilter` -> `role-filter.tsx`. `InvitationStatusChip` -> `user-status-chip.tsx`.
- Filters -> `filter-users.ts`.
- Tests -> `users-view.test.tsx`, `create-user-dialog.test.tsx`, `change-user-activation-dialog.test.tsx`,
  `filter-users.test.ts`, `user-repository.test.ts`, `user-rules.test.ts` and the user use case tests.
- E2E -> `backoffice/e2e/users.spec.ts`.

### Conflicts and stop cases

- **No stop case.** Every endpoint is served by the running API (verified against its `/openapi/v1.json`), the
  invitation responses already carry the trainer's name, and the front adds no business rule.
- The API work for these endpoints is still uncommitted in `api/`. The snapshot must be refreshed from that running
  API.
- Expiry is not a persisted status. The front shows the status as returned plus the "Caduca" date, and does not
  compute "expired" itself.

## Capabilities

### New Capabilities

- `trainer-athletes`: core operations over the trainer–athlete relationship: list mine, list a trainer's, link and
  end; failures as domain error codes.
- `athlete-invitations`: core operations over athlete invitations: list, invite, revoke, accept and reject; the
  invite form's structural rules; failures as domain error codes.
- `backoffice-athletes`: the trainer's "Todos los atletas" screen: table, search, states, role visibility.
- `backoffice-invitations`: the "Invitaciones" screen per role: tables, search, status pills, invite modal, revoke
  confirmation, states, role visibility.

### Modified Capabilities

- `backoffice-navigation`: the drawer copy reads "Invitaciones" instead of "Invitaciones pendientes"; the trainer's
  "Todos los atletas" and both roles' "Invitaciones" now navigate.
- `backoffice-users`: a trainer opening `/users` by URL now gets `FORBIDDEN`, not `UNEXPECTED`.

## Impact

- `core`:
  - snapshot and generated types;
  - new `domain/trainer-athletes/` and `domain/athlete-invitations/`;
  - two port contracts, two repositories and nine use cases;
  - `validation/` (new `email.ts` and `invitation-rules.ts`; `user-rules.ts` reuses `email.ts`);
  - `dependencies.ts`, barrels and tests.
- `backoffice`:
  - `container.ts` and test support;
  - new `presentation/athletes` and `presentation/invitations` areas and their routes;
  - `presentation/common/full-name.ts` and the drawer;
  - `i18n` (`athletes`, `invitations`, `common.menu`);
  - component tests and two new E2E specs.
- `app`: `container.ts`, test support and the container test only.
- No new dependency, no new error code, no change to the HTTP error mapper or the API.
