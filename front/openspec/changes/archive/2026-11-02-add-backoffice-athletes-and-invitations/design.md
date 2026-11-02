# Design

## Context

See proposal.md (Why) and the specs for behaviour. Current state this design builds on:

- **API.** The running API serves `/trainer-athletes` (`GET`, `GET/POST {trainerId}`, `PUT {trainerId}/end`) and
  `/athlete-invitations` (`GET`, `POST`, `PUT {id}/revoke|accept|reject`). `core/openapi/v1.json` has none of them
  yet. The API code is uncommitted in `api/`.
- **Response fields.**
  - `InvitationResponse` and `CreatedInvitationResponse` carry `trainerName` / `trainerSurname` and nullable
    `userName` / `userSurname`. `CreatedInvitationResponse` also carries the raw `token`.
  - `TrainerAthleteListItemResponse` carries the athlete's profile plus the trainer's name.
  - `TrainerAthleteResponse` carries both names and the status.
- **Users screen patterns this change copies.**
  - `UsersView`: search + pills + table, `isAdmin` from `useCurrentUser`.
  - `useUsers`: `status` / `errorCode` / `reload`, with a generation guard.
  - `CreateUserDialog` + `useCreateUser`: a form inside a dialog, unmounted on close.
  - `ChangeUserActivationDialog` + `useChangeUserActivation`: confirmation, stale-list close, `change` returns the
    error code or `null`.
  - `RoleFilter`, `UserStatusChip`, `filterUsers`.
- **Shared helpers.**
  - `fullNameOf` lives in `presentation/users/filter-users.ts`.
  - `EMAIL_PATTERN` is private to `core/src/application/validation/user-rules.ts`.
- **Drawer.** `peopleSection(isAdmin)` builds the second drawer section. Only "Todos los usuarios" has an `href`.

## Goals / Non-Goals

**Goals:**

- Map every new operation into `core` with the shapes already used for users and workouts.
- Build both screens from the users screen's organisms and hooks, with one route per screen and role-dependent
  rendering inside the view.

**Non-Goals:**

- No shared generic "data table" or "confirmation dialog" in `common`. Each area keeps its own organisms, like the
  users area.
- No use of the invitation token in the backoffice.
- No computed "expired" status.

## Decisions

### Core

- **Aliases.**
  - `core/src/domain/trainer-athletes/trainer-athlete.ts`:
    - `TrainerAthlete` = `TrainerAthleteListItemResponse`;
    - `TrainerAthleteRelationship` = `TrainerAthleteResponse`;
    - `TrainerAthleteRequest`;
    - `TrainerAthleteInput = TrainerAthleteRequest & { trainerId: string }`, used by link and end.
  - `core/src/domain/athlete-invitations/athlete-invitation.ts`:
    - `AthleteInvitation` = `InvitationResponse`;
    - `CreatedAthleteInvitation` = `CreatedInvitationResponse`;
    - `InviteAthleteRequest`;
    - `InvitationTokenRequest`;
    - `InvitationResponseInput = InvitationTokenRequest & { invitationId: string }`, used by accept and reject.
  - All are re-exported from the domain barrel.
  - The `…Input` types follow `UpdateWorkoutExerciseInput`: the id joined to the generated request, never
    hand-written fields.
- **Ports.**
  - `TrainerAthleteRepositoryInterface`:
    - `getMine(): Result<TrainerAthlete[]>`;
    - `getByTrainer(trainerId): Result<TrainerAthlete[]>`;
    - `link(trainerId, request): Result<TrainerAthleteRelationship>`;
    - `end(trainerId, request): Result<TrainerAthleteRelationship>`.
  - `AthleteInvitationRepositoryInterface`:
    - `getAll(): Result<AthleteInvitation[]>`;
    - `invite(request): Result<CreatedAthleteInvitation>`;
    - `revoke(id): Result<AthleteInvitation>`;
    - `accept(id, request)` and `reject(id, request)`: `Result<AthleteInvitation>`.
- **Repositories.** `TrainerAthleteRepository` and `AthleteInvitationRepository` in
  `core/src/infrastructure/repositories/`. Each method is one `httpClient.request`, failures go through
  `mapHttpError`, and the body is sent only where the contract declares one. They are modelled on `UserRepository`.
- **Use cases.** One per operation, each forwarding to the repository and returning its `Result` unchanged:
  - `use-cases/trainer-athletes/`:
    - `GetMyAthletesUseCase` (`void`);
    - `GetTrainerAthletesUseCase` (`string` trainerId);
    - `LinkAthleteToTrainerUseCase` and `EndTrainerAthleteUseCase` (`TrainerAthleteInput`).
  - `use-cases/athlete-invitations/`:
    - `GetAthleteInvitationsUseCase` (`void`);
    - `InviteAthleteUseCase` (`InviteAthleteRequest`);
    - `RevokeAthleteInvitationUseCase` (`string` invitationId);
    - `AcceptAthleteInvitationUseCase` and `RejectAthleteInvitationUseCase` (`InvitationResponseInput`).
  - The alternative, one use case per area with an action argument, was rejected. It breaks "one operation, one use
    case" and hides failure meanings.
- **Validation.**
  - New `core/src/application/validation/email.ts` exports `isEmail(value)`, holding the pattern now private to
    `user-rules.ts`, which imports it. Behaviour is unchanged and the existing `user-rules` tests stay green.
  - New `invitation-rules.ts` exports `InviteAthleteFormValues` (`{ email }`), `InviteAthleteFormViolations` and
    `validateInviteAthleteForm` (violations `required` / `email` on the trimmed value).
  - Both are exported from the validation barrel.
- **Dependencies** (`core/src/application/dependencies.ts`):
  - `trainerAthleteRepository`, `athleteInvitationRepository`;
  - `getMyAthletesUseCase`, `getTrainerAthletesUseCase`, `linkAthleteToTrainerUseCase`, `endTrainerAthleteUseCase`;
  - `getAthleteInvitationsUseCase`, `inviteAthleteUseCase`, `revokeAthleteInvitationUseCase`,
    `acceptAthleteInvitationUseCase`, `rejectAthleteInvitationUseCase`.

  All are registered in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`
  (repositories built over `httpClient`, use cases over their repository). Placeholders go in both
  `test/support/render-with-providers.tsx`.

### Backoffice

Imports flow downward, and only atoms import MUI widgets. Layout `Box` / `Container` stay as in the users screen.

- **Shared full name.** `fullNameOf` moves to `backoffice/src/presentation/common/full-name.ts`, and
  `users/filter-users.ts` re-imports it there. Its test moves to `test/presentation/common/full-name.test.ts`.
  - The alternative was importing `users/filter-users` from `athletes` and `invitations`. It was rejected because one
    domain area would depend on another.
- **Athletes area** (`presentation/athletes/`):
  - `filter-athletes.ts`: `filterAthletes(athletes, search)`, matching the full name and the email with the same
    normalisation as `filterUsers`.
  - `hooks/use-athletes.ts`: a copy of the `useUsers` shape over `getMyAthletesUseCase`.
  - `components/organisms/athletes-table.tsx`: three columns (full name or "—", email, `dd/mm/yyyy` start date),
    with the empty and no-match text, like `UsersTable`.
  - `views/athletes-view.tsx`: `AppShell`, `h1`, the search `TextField`, the table, `HomeSkeleton` / `LoadError`.
  - The route is `src/app/athletes/page.tsx`.
- **Invitations area** (`presentation/invitations/`):
  - `statuses.ts`: the ordered status codes (`PENDING`, `ACCEPTED`, `REJECTED`, `REVOKED`), the counterpart of
    `users/roles.ts`.
  - `filter-invitations.ts`: `filterInvitations(invitations, search, status, { byTrainer })`.
    - The text matches `inviteEmail`, plus the trainer's full name when `byTrainer` is true.
    - The status matches `invitation.status`.
    - The API order is kept.
  - Hooks:
    - `hooks/use-invitations.ts`: the `useUsers` shape over `getAthleteInvitationsUseCase`;
    - `hooks/use-invite-athlete.ts`: the `useCreateUser` shape (`inviting`, `errorCode`, `invite`, `reset`);
    - `hooks/use-revoke-invitation.ts`: the `useChangeUserActivation` shape (`submitting`, `errorCode`,
      `revoke(invitation): Promise<DomainErrorCode | null>`, `reset`).
  - Molecules:
    - `components/molecules/invitation-status-filter.tsx`: the `RoleFilter` shape (label, options, single
      selection, toggle to clear);
    - `components/molecules/invitation-status-chip.tsx`: a small chip with the translated status. `PENDING` uses the
      primary colour outlined, the others default outlined. No tooltip.
  - Organisms:
    - `components/organisms/invitations-table.tsx`:
      - props: `invitations`, `filtered`, `showTrainer: boolean`, `onRevoke?: (invitation) => void`;
      - "Entrenador" renders only with `showTrainer`, and the email header switches between "Email" and "Email del
        atleta" with it;
      - the actions column (visually hidden header "Acciones") renders only with `onRevoke`, and holds a
        text-button "Revocar" on `PENDING` rows only. A visible button rather than a ⋮ menu: it is the only action
        and only appears on some rows.
    - `components/organisms/invite-athlete-dialog.tsx`: the `CreateUserDialog` shape with one email field,
      `validateInviteAthleteForm`, the trimmed email, and errors as
      `t("invite.errors.<code>", { defaultValue: tc("errors:<code>") })`.
    - `components/organisms/revoke-invitation-dialog.tsx`: the `ChangeUserActivationDialog` shape.
      - `invitation: AthleteInvitation | null`, `onClose(stale)`, `onRevoked()`;
      - a destructive confirm button;
      - `CONFLICT` and `NOT_FOUND` mark the list as stale.
  - `views/invitations-view.tsx`:
    - `isAdmin` / `isTrainer` from `useCurrentUser`;
    - local `search`, `status`, `inviting`, `target`, and a `notice` for the success message (`"invited"`,
      `"revoked"` or `null`);
    - the search label depends on the role;
    - `InvitationsTable` gets `showTrainer={isAdmin}` and `onRevoke={isTrainer ? setTarget : undefined}`;
    - "Invitar atleta", `InviteAthleteDialog` and `RevokeInvitationDialog` render only for `isTrainer`;
    - `filtered` is `search.trim() !== "" || status !== null`.
  - The route is `src/app/invitations/page.tsx`.
- **Drawer.** `peopleSection(isAdmin)` returns:
  - for an admin: `menu.users.all` (`/users`) and `menu.athletes.invitations` (`/invitations`);
  - otherwise: `menu.athletes.all` (`/athletes`) and `menu.athletes.invitations` (`/invitations`).

  `menu.athletes.pendingInvitations` is removed. While the profile loads, the drawer shows the trainer copy with
  links, as today.

- **Dates.** One `Intl.DateTimeFormat(i18n.language, { day: "2-digit", month: "2-digit", year: "numeric" })` per
  table, as in `UsersTable`.

### Translations

The new keys and their Spanish and English texts are exactly those listed in the brief's Errors And Copy:

- `backoffice/src/i18n/{es,en}/athletes.json`;
- `backoffice/src/i18n/{es,en}/invitations.json` (including `status.*`, `invite.*`, `revoke.*`);
- `common.json`: `menu.athletes.invitations` replaces `menu.athletes.pendingInvitations`.

Both new namespaces are registered in `backoffice/src/i18n/resources.ts`. The parity test covers them. The app gets
no copy.

### Errors

- Invite:
  - `CONFLICT` -> the "cannot invite" text;
  - `VALIDATION` -> "Introduce un email válido." / "Enter a valid email.".
- Revoke:
  - `CONFLICT` -> "Esta invitación ya no está pendiente. Recarga la lista." / "This invitation is no longer pending.
    Reload the list.";
  - `NOT_FOUND` -> "Esta invitación ya no existe." / "This invitation no longer exists.".
- Lists: `FORBIDDEN` (an admin on `/athletes`, a trainer on `/users`), `NETWORK` and `UNEXPECTED` -> the existing
  `errors:<code>` through `LoadError`.
- `UNAUTHORIZED` -> `runWithSession`.
- No new `DomainErrorCode`. `http-error-mapper.ts` is untouched.

### State

- `useAthletes`, `useInvitations`: the list, status and error code. They are set on mount and on `reload`, and reset
  on screen exit.
- `AthletesView`: `search`.
- `InvitationsView`: `search`, `status`, `inviting`, `target`, `notice`. All reset on screen exit.
- `useInviteAthlete`, `useRevokeInvitation`: submitting state and error code, reset when their dialog closes.

### Tests

They follow the existing tier templates. The test helpers change only for the new placeholders.

- **core.**
  - `trainer-athlete-repository.test.ts` and `athlete-invitation-repository.test.ts`: per method, the path, method
    and body; success; each declared failure status; `500` -> `UNEXPECTED`; network -> `NETWORK`.
  - Nine use case tests (template `create-user-use-case.test.ts`).
  - `invitation-rules.test.ts`.
  - The existing `user-rules.test.ts` unchanged and green.
- **Containers.** Both fronts' container tests gain every key.
- **backoffice.**
  - `full-name.test.ts` (moved), `filter-athletes.test.ts`, `filter-invitations.test.ts`.
  - `athletes-view.test.tsx`, `invitations-view.test.tsx` (trainer and admin, including the absence assertions),
    `invite-athlete-dialog.test.tsx`, `revoke-invitation-dialog.test.tsx`.
  - Drawer test: new links, "Invitaciones" copy, "Calendario" as the placeholder.
  - `users-view.test.tsx`: the trainer case returns `FORBIDDEN`.
- **E2E.**
  - `e2e/athletes.spec.ts`: a trainer opens "Todos los atletas" from the drawer and searches.
  - `e2e/invitations.spec.ts`: a trainer invites and revokes; an admin filters by status and trainer, with no
    controls.
  - The API is intercepted with `page.route`. `mockUsersMe` is reused.

## Risks / Trade-offs

- [The API code is uncommitted] → The snapshot is taken from the running API. If the API changes before it is
  committed, refresh again. Task 1.1 checks the paths and fields.
- [Expired invitations read `PENDING` and show "Revocar"] → Revoking one answers `409`, and the dialog explains it
  and reloads on close. The "Caduca" date lets the trainer see it coming.
- [Moving `fullNameOf` touches the users area] → It is a pure move with the same signature, covered by the existing
  users tests and the moved unit test.
- [`menu.athletes.pendingInvitations` is removed] → Only the drawer uses it, and the parity test and the drawer test
  catch a leftover.

## Migration Plan

1. Refresh the snapshot with the API running and commit it alone. Format the generated types with Prettier to match
   the committed style.
2. Rollback is reverting the change's commits. No data or persisted state is involved.
