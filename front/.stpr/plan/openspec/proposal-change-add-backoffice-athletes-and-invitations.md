# Change Proposal: Trainer Athletes And Athlete Invitations In Core And The Backoffice

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-athletes-and-invitations/`.

## Change

Refresh the OpenAPI snapshot to pick up the `trainer-athletes` and `athlete-invitations` endpoints. Add their
repositories and use cases to `core`, and wire them in both fronts. In the backoffice, give a `TRAINER` a
"Todos los atletas" screen and an "Invitaciones" screen where they can invite and revoke. Give an `ADMIN` a read-only
"Invitaciones" screen across all trainers.

## Objective

The API now links trainers to their athletes and lets trainers invite people to become their athletes (see
`api/.stpr/plan/openspec/proposal-change-add-trainer-athletes-and-invitations.md`). `core` knows none of these
operations. In the backoffice, the trainer's "Todos los atletas" and "Invitaciones pendientes" drawer entries lead
nowhere. An admin has no way to see the invitations being sent.

## Capabilities

Core (every new endpoint, so both fronts can use them):

- Trainer athletes:
  - list my athletes (`GET /trainer-athletes`);
  - list a trainer's athletes (`GET /trainer-athletes/{trainerId}`);
  - link an athlete (`POST /trainer-athletes/{trainerId}`);
  - end a relationship (`PUT /trainer-athletes/{trainerId}/end`).
- Athlete invitations:
  - list invitations (`GET /athlete-invitations`);
  - invite (`POST /athlete-invitations`);
  - revoke, accept and reject (`PUT /athlete-invitations/{invitationId}/revoke|accept|reject`).
- All of them are wired in `core` and both containers. The backoffice screens below use the trainer list, the
  invitation list, invite and revoke. The rest are wiring only, for later changes (the app's accept/reject, the
  admin's link/end).

Backoffice, `TRAINER`:

- **"Todos los atletas"** (`/athletes`): a table of the trainer's current athletes from `GET /trainer-athletes`. Its
  columns are "Nombre completo" (as in the users screen), "Email" and "Atleta desde" (relationship start date). One
  search field filters the rows client-side by full name or email.
- **"Invitaciones"** (`/invitations`): a table of the trainer's invitations from `GET /athlete-invitations`. Its
  columns are "Email", "Estado", "Enviada" and "Caduca".
  - Filters: one search field (by invited email) and status pills (Pendiente, Aceptada, Rechazada, Revocada).
    Search and pills combine.
  - An "Invitar atleta" button opens a modal with an email field that sends `POST /athlete-invitations`. On success
    the modal closes, a success message shows and the list reloads. On failure the modal stays open with the error.
  - Each `PENDING` row has a "Revocar" action. After a confirmation dialog it sends
    `PUT /athlete-invitations/{id}/revoke`. On success a message shows and the list reloads. Other rows show no
    action.

Backoffice, `ADMIN`:

- **"Invitaciones"** (`/invitations`): the same table, over every invitation (`GET /athlete-invitations` returns all
  of them for an admin). Its columns are "Entrenador" (the trainer's full name), "Email del atleta", "Estado",
  "Enviada" and "Caduca".
  - Filters: one search field (by trainer full name or athlete email) and the same status pills.
  - Read-only: no "Invitar atleta" button, no modal, no "Revocar" action.

Drawer and copy:

- `TRAINER`: "Atletas" > "Todos los atletas" links to `/athletes`, and "Invitaciones" links to `/invitations`.
- `ADMIN`: "Usuarios" > "Todos los usuarios" (unchanged), and "Invitaciones" links to `/invitations`.
- "Invitaciones pendientes" becomes "Invitaciones" for both roles.

Common to every list: loading, empty, no-match and error states, as in the users screen.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER`
  - `TRAINER`:
    - sees "Todos los atletas" and "Invitaciones" (with "Invitar atleta" and "Revocar");
    - must NOT see the "Entrenador" column, which would always be themselves.
  - `ADMIN`:
    - sees "Invitaciones" with the "Entrenador" column and its search;
    - must NOT see "Invitar atleta", the invite modal, "Revocar" or the revoke dialog;
    - has no "Todos los atletas" entry (they keep "Todos los usuarios"). If an admin opens `/athletes` by URL,
      `GET /trainer-athletes` answers `403` and the screen shows the load error.
- app -> lands here: wiring only -> roles: `USER` -> the container registers every new use case. No screen, hook or
  copy changes. Accepting and rejecting from the app is a later change.

The front decides none of these permissions. It hides actions per role as a presentation choice. The API enforces
them (`403`). The role comes from `useCurrentUser().profile.role`.

`/invitations` is one route whose view renders per role, as `/users` already does for the "Añadir usuario" button.

## Classification

Core:

- Aliases over `components["schemas"]` -> entity type:
  - `TrainerAthlete` (`TrainerAthleteListItemResponse`), `TrainerAthleteRelationship` (`TrainerAthleteResponse`)
    and the link/end request -> `core/src/domain/trainer-athletes/trainer-athlete.ts`;
  - `AthleteInvitation` (`InvitationResponse`), `CreatedAthleteInvitation` (`CreatedInvitationResponse`),
    `InviteAthleteRequest` and `InvitationTokenRequest` -> `core/src/domain/athlete-invitations/athlete-invitation.ts`.
- Repository contracts -> `core/src/application/ports/`:
  - `TrainerAthleteRepositoryInterface`: `getMine()`, `getByTrainer(trainerId)`, `link(trainerId, request)`,
    `end(trainerId, request)`;
  - `AthleteInvitationRepositoryInterface`: `getAll()`, `invite(request)`, `revoke(id)`, `accept(id, request)`,
    `reject(id, request)`.
- `TrainerAthleteRepository` and `AthleteInvitationRepository` -> repository implementation ->
  `core/src/infrastructure/repositories/`.
- Use cases -> `core/src/application/use-cases/trainer-athletes/` (`GetMyAthletesUseCase`,
  `GetTrainerAthletesUseCase`, `LinkAthleteToTrainerUseCase`, `EndTrainerAthleteUseCase`) and
  `core/src/application/use-cases/athlete-invitations/` (`GetAthleteInvitationsUseCase`, `InviteAthleteUseCase`,
  `RevokeAthleteInvitationUseCase`, `AcceptAthleteInvitationUseCase`, `RejectAthleteInvitationUseCase`).
- `Dependencies`:
  - repositories: `trainerAthleteRepository`, `athleteInvitationRepository`;
  - use cases: `getMyAthletesUseCase`, `getTrainerAthletesUseCase`, `linkAthleteToTrainerUseCase`,
    `endTrainerAthleteUseCase`, `getAthleteInvitationsUseCase`, `inviteAthleteUseCase`,
    `revokeAthleteInvitationUseCase`, `acceptAthleteInvitationUseCase`, `rejectAthleteInvitationUseCase`;
  - file: `core/src/application/dependencies.ts`.
- Barrels re-export the new aliases, contracts, repositories and use cases.

Backoffice and app wiring:

- Registrations of both repositories and every use case -> composition root ->
  `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`.
- Placeholders for the new keys -> test helper -> `backoffice/test/support/render-with-providers.tsx` and
  `app/test/support/render-with-providers.tsx`.

Backoffice, new `athletes` domain area:

- `/athletes` route -> route -> `backoffice/src/app/athletes/page.tsx`.
- `AthletesView` -> view -> `backoffice/src/presentation/athletes/views/athletes-view.tsx`.
- `useAthletes` -> state hook -> `backoffice/src/presentation/athletes/hooks/use-athletes.ts`.
- `AthletesTable` -> organism -> `backoffice/src/presentation/athletes/components/organisms/athletes-table.tsx`.
- `filterAthletes` -> view filter -> `backoffice/src/presentation/athletes/filter-athletes.ts`.

Backoffice, new `invitations` domain area:

- `/invitations` route -> route -> `backoffice/src/app/invitations/page.tsx`.
- `InvitationsView` (renders per role) -> view -> `backoffice/src/presentation/invitations/views/invitations-view.tsx`.
- State hooks -> `backoffice/src/presentation/invitations/hooks/`:
  - `useInvitations` (list, status, errorCode, reload);
  - `useInviteAthlete` (submitting, errorCode, invite, reset);
  - `useRevokeInvitation` (submitting, errorCode, revoke, reset).
- Organisms -> `backoffice/src/presentation/invitations/components/organisms/`:
  - `InvitationsTable` (columns per role, optional trainer column, optional revoke action);
  - `InviteAthleteDialog` (email form inside a dialog);
  - `RevokeInvitationDialog` (confirmation).
- Molecules -> `backoffice/src/presentation/invitations/components/molecules/`:
  - `InvitationStatusFilter` (status pills);
  - `InvitationStatusChip` (translated status in the table).
- `filterInvitations` -> view filter -> `backoffice/src/presentation/invitations/filter-invitations.ts`.

Shared pieces:

- Full name: reuse `fullNameOf` from `presentation/users/filter-users.ts`. If two areas need it, move it to
  `presentation/common/` instead of importing across areas. Decide at proposal time.
- Atoms: reuse the table atoms, text field, chip, button, icon button, dialog atoms and alert in
  `common/components/atoms/`.
- Loading and error: reuse `HomeSkeleton` and `LoadError`.
- Drawer: extended in place -> `backoffice/src/presentation/common/components/organisms/navigation-drawer.tsx`.
- Copy: new `athletes` and `invitations` namespaces (`backoffice/src/i18n/{es,en}/`), plus a `menu.*` change in
  `common.json`.

## Canonical Template Mapping

- Aliases -> `core/src/domain/users/user.ts` -> aliases over `components["schemas"]` only.
- Repositories -> `core/src/infrastructure/repositories/user-repository.ts` -> `GET`, `POST` and `PUT` (with or
  without body) through `HttpClientInterface`, failures through `mapHttpError`.
- Use cases:
  - no-input lists -> `core/src/application/use-cases/users/get-users-use-case.ts`;
  - id-only actions (revoke) -> `activate-user-use-case.ts`;
  - request-forwarding actions (invite, link) -> `create-user-use-case.ts`;
  - id + body actions (accept, reject, end, list by trainer) -> `update-my-profile-use-case.ts` for the forwarding,
    with a small input object such as `{ id, request }`. Decide the input shape at proposal time.
- Core tests -> the tests of those same files plus `user-repository.test.ts`.
- Routes -> `backoffice/src/app/users/page.tsx` -> default-exports the view, nothing else.
- `AthletesView`, `InvitationsView` -> `backoffice/src/presentation/users/views/users-view.tsx` -> `AppShell`, `h1`,
  search + pills above a table, skeleton / `LoadError`, role-dependent elements.
- `useAthletes`, `useInvitations` -> `backoffice/src/presentation/users/hooks/use-users.ts`.
- `useInviteAthlete` -> `use-create-user.ts`. `useRevokeInvitation` -> `use-change-user-activation.ts`.
- `AthletesTable`, `InvitationsTable` -> `backoffice/src/presentation/users/components/organisms/users-table.tsx` ->
  organism over the table atoms, data by props, optional actions column, empty / no-match text.
- `InviteAthleteDialog` -> `create-user-dialog.tsx` -> form inside a dialog, unmounted on close, error `Alert`.
- `RevokeInvitationDialog` -> `change-user-activation-dialog.tsx` -> confirmation, destructive button, error `Alert`,
  stale-list close.
- `InvitationStatusFilter` -> `users/components/molecules/role-filter.tsx` -> same pill group.
- `InvitationStatusChip` -> `users/components/molecules/user-status-chip.tsx`.
- `filterAthletes`, `filterInvitations` -> `backoffice/src/presentation/users/filter-users.ts` -> case- and
  accent-insensitive substring, API order kept.
- Atom, molecule and organism references are `pending` in `.stpr/template/README.md`. Follow the backoffice shapes
  above.
- View tests -> `backoffice/test/presentation/users/views/users-view.test.tsx`.
- Dialog tests -> `create-user-dialog.test.tsx` and `change-user-activation-dialog.test.tsx`.
- E2E -> `backoffice/e2e/users.spec.ts`.

UI references come from the backoffice only. `.stpr/template/` and its README are not updated here.

## Screens And Navigation

- backoffice -> `/athletes` -> `AthletesView` -> drawer "Atletas" > "Todos los atletas" (`TRAINER`) -> success: table
  or empty state; failure: `LoadError` with retry (including an admin reaching the URL directly, `403`); `401`
  follows the existing session handling.
- backoffice -> `/invitations` -> `InvitationsView` -> drawer "Invitaciones" (`TRAINER` under "Atletas", `ADMIN`
  under "Usuarios") -> success: the role's table or empty state; failure: `LoadError` with retry.
- "Invitar atleta" (`TRAINER` only) -> `InviteAthleteDialog`:
  - success: the dialog closes, "Invitación enviada." shows and the list reloads;
  - failure: the dialog stays open with the error and the typed email;
  - Cancel: closes without a request.
- "Revocar" on a `PENDING` row (`TRAINER` only) -> `RevokeInvitationDialog`:
  - success: the dialog closes, "Invitación revocada." shows and the list reloads;
  - failure: the dialog stays open with the error. Closing after a `CONFLICT` or `NOT_FOUND` reloads the list.

## API Contract

All of these are **not** in `core/openapi/v1.json` yet, so a refresh is needed. They are provided by the API change
`add-trainer-athletes-and-invitations`.

Trainer athletes:

- `GET /trainer-athletes` (`TRAINER`) -> `200` `TrainerAthleteListItemResponse[]` (`id`, `athleteId`, `email`,
  `name`, `surname`, `trainerName`, `trainerSurname`, `startedAtUtc`), ordered by email. Failures: `401`, `403`.
- `GET /trainer-athletes/{trainerId}` (`ADMIN`) -> `200` `TrainerAthleteListItemResponse[]`. Failures: `401`,
  `403`, `404`. Wiring only.
- `POST /trainer-athletes/{trainerId}` (`ADMIN`) -> request `TrainerAthleteRequest`, response `201`
  `TrainerAthleteResponse`. Failures: `400`, `401`, `403`, `404`, `409`. Wiring only.
- `PUT /trainer-athletes/{trainerId}/end` -> request `TrainerAthleteRequest` (`athleteId`), response `200`
  `TrainerAthleteResponse`. Failures: `400`, `401`, `403`, `404`. Wiring only.

Athlete invitations:

- `GET /athlete-invitations` (`ADMIN`: every invitation; `TRAINER`: their own) -> `200` `InvitationResponse[]`
  (`id`, `trainerId`, `trainerName`, `trainerSurname`, `inviteEmail`, `userId`, `userName`, `userSurname`
  (both null for an unregistered email), `status`, `createdAtUtc`, `expiresAtUtc`, `acceptedAtUtc`), newest first.
  Failures: `401`, `403`.
- `POST /athlete-invitations` (`TRAINER`) -> request `InviteAthleteRequest` (`email`), response `201`
  `CreatedInvitationResponse` (the invitation plus its raw `token`). Failures: `400`, `401`, `403`, `409`.
- `PUT /athlete-invitations/{invitationId}/revoke` (the inviting `TRAINER`) -> no body, response `200`
  `InvitationResponse`. Failures: `401`, `403`, `404`, `409`.
- `PUT /athlete-invitations/{invitationId}/accept|reject` (the invited `USER`) -> request `InvitationTokenRequest`
  (`token`), response `200` `InvitationResponse`. Failures: `400`, `401`, `403`, `404`, `409`. Wiring only.

Run `openapi:refresh` + `openapi:generate` once, with the API running from the branch that has these endpoints.
Commit the snapshot and the generated types (Prettier-formatted like the committed file) as a step of their own. No
hand-written type.

## Held State

- `useAthletes` -> the loaded athletes, status and error code -> the hook on mount and on `reload` -> resets on
  screen exit.
- `useInvitations` -> the loaded invitations, status and error code -> the hook on mount and on `reload` (also after an invite, a revoke, or a stale-list close) ->
  resets on screen exit.
- `AthletesView` local state -> search text -> resets on screen exit.
- `InvitationsView` local state -> search text, selected status pill, invite dialog open/closed, revoke target,
  success message -> resets on screen exit.
- `InviteAthleteDialog` form state -> email value and field error -> resets when the dialog closes.
- Nothing is cached across screens or kept after logout.

## Forms And Structural Validation

- Invite athlete -> email -> required, email format (the same check as the create-user form). Trimmed before sending.

Whether the email can be invited (an existing trainer or admin, a duplicate pending invitation, an athlete who
already has a trainer) is an API rule, and comes back as `409`.

## Errors And Copy

- Session expired / missing -> `UNAUTHORIZED` -> existing session handling.
- Caller's role not allowed (admin opening `/athletes`, trainer revoking someone else's invitation) -> `FORBIDDEN` ->
  `LoadError` or dialog error with the existing message.
- Malformed email refused by the API -> `VALIDATION` -> invite dialog error "Introduce un email válido."
- Email cannot be invited -> `CONFLICT` -> invite dialog error "No se puede invitar a este email: ya tiene una
  invitación pendiente, ya entrena con alguien o no es una cuenta de atleta."
- Invitation no longer pending, or expired -> `CONFLICT` -> revoke dialog error "Esta invitación ya no está pendiente.
  Recarga la lista."
- Invitation not found -> `NOT_FOUND` -> revoke dialog error "Esta invitación ya no existe."
- No connection -> `NETWORK`. Anything else -> `UNEXPECTED` -> generic error.
- No new error code. The HTTP error mapper is not touched.

New copy, `common` namespace:

- `menu.athletes.invitations` -> "Invitaciones" -> "Invitations" (replaces `menu.athletes.pendingInvitations` for
  both roles; the old key is removed).

New copy, `athletes` namespace:

- `list.title` -> "Todos los atletas" -> "All athletes"
- `list.search` -> "Buscar por nombre o email" -> "Search by name or email"
- `list.columns.fullName`, `email`, `since` -> "Nombre completo", "Email", "Atleta desde" -> "Full name", "Email",
  "Athlete since"
- `list.empty` -> "Todavía no tienes atletas." -> "You have no athletes yet."
- `list.noMatches` -> "Ningún atleta coincide con la búsqueda." -> "No athletes match your search."

New copy, `invitations` namespace:

- `list.title` -> "Invitaciones" -> "Invitations"
- `list.searchByEmail` (trainer) -> "Buscar por email" -> "Search by email"
- `list.searchByTrainerOrEmail` (admin) -> "Buscar por entrenador o email" -> "Search by trainer or email"
- `list.statusFilter` -> "Estado" -> "Status"
- `list.invite` -> "Invitar atleta" -> "Invite athlete"
- `list.columns.trainer`, `email`, `athleteEmail`, `status`, `sentAt`, `expiresAt`, `actions` -> "Entrenador",
  "Email", "Email del atleta", "Estado", "Enviada", "Caduca", "Acciones" -> "Trainer", "Email", "Athlete email",
  "Status", "Sent", "Expires", "Actions"
- `status.PENDING`, `ACCEPTED`, `REJECTED`, `REVOKED` -> "Pendiente", "Aceptada", "Rechazada", "Revocada" ->
  "Pending", "Accepted", "Rejected", "Revoked"
- `list.revoke` -> "Revocar" -> "Revoke"
- `list.empty` -> "Todavía no hay invitaciones." -> "No invitations yet."
- `list.noMatches` -> "Ninguna invitación coincide con la búsqueda." -> "No invitations match your search."
- `invite.title` -> "Invitar atleta" -> "Invite athlete"
- `invite.fields.email` -> "Email" -> "Email"
- `invite.submit` -> "Enviar invitación" -> "Send invitation"
- `invite.cancel` -> "Cancelar" -> "Cancel"
- `invite.success` -> "Invitación enviada." -> "Invitation sent."
- `invite.validation.required`, `invite.validation.email` -> "Este campo es obligatorio.", "Introduce un email
  válido." -> "This field is required.", "Enter a valid email."
- `invite.errors.VALIDATION`, `invite.errors.CONFLICT` -> as above -> "Enter a valid email.", "This email cannot be
  invited: it already has a pending invitation, already trains with someone, or is not an athlete account."
- `revoke.title` -> "¿Revocar la invitación a {{email}}?" -> "Revoke the invitation to {{email}}?"
- `revoke.body` -> "El enlace dejará de funcionar y no podrá aceptarse." -> "The link will stop working and it can no
  longer be accepted."
- `revoke.confirm` -> "Revocar" -> "Revoke"
- `revoke.cancel` -> "Cancelar" -> "Cancel"
- `revoke.success` -> "Invitación revocada." -> "Invitation revoked."
- `revoke.errors.CONFLICT`, `revoke.errors.NOT_FOUND` -> as above -> "This invitation is no longer pending. Reload
  the list.", "This invitation no longer exists."

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- Showing or sharing the invitation token returned by `POST /athlete-invitations`. The API sends no email yet, so a
  later change decides how the token reaches the athlete.
- Accepting or rejecting invitations (the app's athlete flow).
- Admin screens to link an athlete to a trainer, end a relationship, or browse a given trainer's athletes.
- Ending a relationship from the trainer's athletes table.
- Server-side search, sorting and pagination.
- Showing expired invitations with their own status. The API keeps them `PENDING` and expires them by time (see
  Notes).
- Must not change: the API, the users screen (except the copy and spec note below), `/users/me`, signup and the
  other drawer entries.

## Notes

- **No stop case,** provided the API change is running when the snapshot is refreshed. Every endpoint exists in
  `api/src/WeightTracker.Api/Endpoints`, and the front adds no business rule.
- **Trainer names for the admin (decided).** `InvitationResponse` carries `trainerName` and `trainerSurname`, so the
  "Entrenador" column and its search use those fields directly. No `GET /users` join and no API change.
- **Expired invitations.** Expiry is not a persisted status, so an expired invitation still reads `PENDING`. This
  change shows the status as the API returns it, plus the "Caduca" date, and does not compute "expired" in the front
  (that would be a business rule). Revoking an expired one answers `409` and shows the "no longer pending" message.
  If the API later returns an effective status, the pills follow it.
- **Revoke confirmation (decided).** A confirmation dialog with a destructive button, as when deactivating a user.
- **Status pills.** One pill per status, single selection, pressing it again clears it, as in `RoleFilter`. The
  status cell uses `InvitationStatusChip` with the translated label.
- **Dates.** "Atleta desde", "Enviada" and "Caduca" use `dd/mm/yyyy` in the active language, as the deactivation date
  does.
- **Search.** Client-side, case- and accent-insensitive substring:
  - athletes: on full name and email;
  - trainer invitations: on `inviteEmail`;
  - admin invitations: on the trainer full name (`trainerName` + `trainerSurname`) and `inviteEmail`.
- **Drawer.** `peopleSection` keeps its role switch. The second item becomes `menu.athletes.invitations` with
  `href: "/invitations"` for both roles. The trainer's "Todos los atletas" gets `href: "/athletes"`.
  `backoffice-navigation` changes accordingly: "Items without a screen do nothing" loses these entries, and the
  drawer copy requirement swaps "Invitaciones pendientes" for "Invitaciones".
- **Users screen.** `GET /users` is now admin-only (`403` for a trainer, no longer `501`). The `backoffice-users`
  scenario "Trainer opens the URL directly" changes from `UNEXPECTED` to `FORBIDDEN`. No code change is expected
  there: `LoadError` already renders any code.
- **Use case input shape.** Operations that need an id plus a body (`accept`, `reject`, `end`) or an id only
  (`getByTrainer`) take a single input object. The name and shape are decided at proposal time, following the
  existing workout exercise use cases if they already do this.
- **Verification tiers:**
  - core unit:
    - each use case: input forwarded, success returned, repository failure returned unchanged;
    - each repository method: path, method and body checked; success; every declared failure status; `500` ->
      `UNEXPECTED`; network -> `NETWORK`.
  - container tests in both fronts gain every new key.
  - backoffice component tests:
    - `athletes-view.test.tsx`: rows, full name dash, search by name and by email, empty, no-match, `LoadError`
      with retry, an admin's `FORBIDDEN`.
    - `invitations-view.test.tsx`, as a trainer: rows, status chips, search by email, status pills, both combined,
      "Invitar atleta" shown, "Revocar" only on `PENDING` rows, revoke success reloads, no "Entrenador" column.
    - `invitations-view.test.tsx`, as an admin: the "Entrenador" column from `trainerName` + `trainerSurname`, search by trainer and
      by email, pills; "Invitar atleta", "Revocar" and both dialogs absent (assert absence).
    - `invite-athlete-dialog.test.tsx`: required and format errors block sending, email trimmed, success,
      `VALIDATION` and `CONFLICT` messages with the email kept, cancel.
    - `revoke-invitation-dialog.test.tsx`: copy, destructive button, cancel sends nothing, success, `CONFLICT`,
      `NOT_FOUND`, stale-list close.
    - drawer test: trainer links to `/athletes` and `/invitations`, admin links to `/invitations`, "Invitaciones"
      copy for both, no "Invitaciones pendientes".
  - backoffice E2E (`e2e/athletes.spec.ts`, `e2e/invitations.spec.ts`):
    - a trainer opens "Todos los atletas" from the drawer and searches;
    - a trainer invites an athlete and revokes a pending invitation;
    - an admin opens "Invitaciones", filters by status and trainer, and sees no invite or revoke control.
    - All API calls are intercepted with `page.route`.
  - translation parity passes with the new namespaces and keys.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed.
