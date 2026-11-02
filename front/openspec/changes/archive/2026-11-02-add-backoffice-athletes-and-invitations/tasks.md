# Tasks

## 1. API contract snapshot

- [x] 1.1 With the API (the branch with `add-trainer-athletes-and-invitations`) running on `http://localhost:5199`,
      run `pnpm --filter @weight-tracker/core openapi:refresh` then `openapi:generate`. Then:
  - format `core/src/contract/generated/api.ts` with Prettier (`--ignore-path /dev/null`) to match the committed
    style;
  - verify `core/openapi/v1.json` lists the 4 `/trainer-athletes` operations and the 5 `/athlete-invitations`
    operations;
  - verify `InvitationResponse` carries `trainerName` / `trainerSurname`.

  Never hand-edit `core/src/contract/generated`.

## 2. Core: trainer athletes

- [x] 2.1 Create `core/src/domain/trainer-athletes/trainer-athlete.ts` (`TrainerAthlete`,
      `TrainerAthleteRelationship`, `TrainerAthleteRequest`, `TrainerAthleteInput`), exported from the domain barrel.
      Verify with `pnpm --filter @weight-tracker/core typecheck`.
- [x] 2.2 Add `TrainerAthleteRepositoryInterface` to `core/src/application/ports/` and implement
      `TrainerAthleteRepository` (template `user-repository.ts`). Create `trainer-athlete-repository.test.ts` covering,
      per method:
  - path, method and body;
  - success;
  - every declared failure status (`400`, `401`, `403`, `404`, `409` where declared);
  - `500` → `UNEXPECTED`;
  - network → `NETWORK`.

  Use AAA with one Act each. Verify with `pnpm --filter @weight-tracker/core test`.

- [x] 2.3 Create `GetMyAthletesUseCase`, `GetTrainerAthletesUseCase`, `LinkAthleteToTrainerUseCase` and
      `EndTrainerAthleteUseCase` in `core/src/application/use-cases/trainer-athletes/` (templates `get-users`,
      `activate-user`, `update-workout-exercise`), exported from the barrels. Add one test each (template
      `create-user-use-case.test.ts`):
  - input forwarded;
  - success returned;
  - failure returned unchanged.

  Verify with the core tests.

## 3. Core: athlete invitations and invite rules

- [x] 3.1 Create `core/src/domain/athlete-invitations/athlete-invitation.ts` (`AthleteInvitation`,
      `CreatedAthleteInvitation`, `InviteAthleteRequest`, `InvitationTokenRequest`, `InvitationResponseInput`), exported
      from the domain barrel. Verify with the core typecheck.
- [x] 3.2 Add `AthleteInvitationRepositoryInterface` and implement `AthleteInvitationRepository`. Create
      `athlete-invitation-repository.test.ts` with the same coverage as 2.2 (for invite, the email is sent unchanged; for
      accept/reject, the token body). Verify with the core tests.
- [x] 3.3 Create `GetAthleteInvitationsUseCase`, `InviteAthleteUseCase`, `RevokeAthleteInvitationUseCase`,
      `AcceptAthleteInvitationUseCase` and `RejectAthleteInvitationUseCase` in
      `core/src/application/use-cases/athlete-invitations/`, with one test each as in 2.3. Verify with the core tests.
- [x] 3.4 Extract the email check into `core/src/application/validation/email.ts` (`isEmail`) and use it from
      `user-rules.ts`. Add `invitation-rules.ts` (`InviteAthleteFormValues`, `InviteAthleteFormViolations`,
      `validateInviteAthleteForm`) and `invitation-rules.test.ts` with every scenario of the `athlete-invitations` form
      requirement. Verify that the core tests pass, `user-rules.test.ts` included and unchanged.
- [x] 3.5 Add both repositories and all nine use cases to `Dependencies`. Verify that the core typecheck now fails
      only in the two containers, which group 4 fixes.
- [x] 3.6 Review core against its references (`user-repository.ts`, the user and workout use cases, `user-rules.ts`)
      and the invariants: aliases only, no throw, `Result` everywhere. Verify that the drift loop from `AGENTS.md` prints
      nothing.

## 4. Wiring in both fronts

- [x] 4.1 Register both repositories and all nine use cases in `backoffice/src/infrastructure/container.ts` and
      `app/src/infrastructure/container.ts`. Add their placeholders to both `test/support/render-with-providers.tsx`,
      and extend both container tests with every key. Verify with `pnpm typecheck` and the container tests of both
      fronts.
- [x] 4.2 Review the app side: only the container, its test and the test support changed. Verify with
      `git diff --stat app/`.

## 5. Backoffice: copy and shared helpers

- [x] 5.1 Create `backoffice/src/i18n/{es,en}/athletes.json` and `invitations.json` with the keys in the brief's
      Errors And Copy, and register both namespaces in `resources.ts`. In both `common.json`, replace
      `menu.athletes.pendingInvitations` with `menu.athletes.invitations`. Verify the translation parity test passes.
- [x] 5.2 Move `fullNameOf` to `backoffice/src/presentation/common/full-name.ts`, update `users/filter-users.ts` and
      `users-table.tsx` (and any other importer), and move its tests to `test/presentation/common/full-name.test.ts`.
      Verify that the users tests and the moved test pass.
- [x] 5.3 Create `presentation/athletes/filter-athletes.ts`, `presentation/invitations/statuses.ts` and
      `presentation/invitations/filter-invitations.ts`, with `filter-athletes.test.ts` and `filter-invitations.test.ts`.
      Cover:
  - full name and email matches;
  - case, accents and surrounding spaces;
  - trainer search only with `byTrainer`;
  - the status filter;
  - status and search combined;
  - API order kept.

  Verify with `pnpm --filter backoffice test`.

## 6. Backoffice: athletes screen

- [x] 6.1 Create `hooks/use-athletes.ts` (template `use-users.ts`) and `components/organisms/athletes-table.tsx`
      (template `users-table.tsx`). Verify with `pnpm --filter backoffice typecheck`.
- [x] 6.2 Create `views/athletes-view.tsx` (template `users-view.tsx`) and the route
      `backoffice/src/app/athletes/page.tsx`. Add `test/presentation/athletes/views/athletes-view.test.tsx` covering
      every `backoffice-athletes` scenario: heading, rows in order, row content and dash, search by name, email and
      cleared, empty, no-match, retry, the admin's `FORBIDDEN`, English. Use AAA with one Act each. Verify with the
      backoffice tests.

## 7. Backoffice: invitations screen

- [x] 7.1 Create the hooks `use-invitations.ts`, `use-invite-athlete.ts` and `use-revoke-invitation.ts` (templates
      `use-users.ts`, `use-create-user.ts`, `use-change-user-activation.ts`). Verify through the tests in 7.3–7.5.
- [x] 7.2 Create the molecules `invitation-status-filter.tsx` and `invitation-status-chip.tsx` (templates
      `role-filter.tsx`, `user-status-chip.tsx`) and the organism `invitations-table.tsx` (template `users-table.tsx`):
  - `showTrainer` adds "Entrenador" and switches the email header;
  - `onRevoke` adds the hidden-header actions column with "Revocar" on `PENDING` rows only.

  Verify with `pnpm --filter backoffice typecheck` and `pnpm lint`.

- [x] 7.3 Create `invite-athlete-dialog.tsx` (template `create-user-dialog.tsx`) and its test (template
      `create-user-dialog.test.tsx`). Cover:
  - an empty email and a malformed email block sending;
  - the email is sent trimmed;
  - success calls back;
  - `CONFLICT` and `VALIDATION` show their messages with the email kept;
  - Cancelar sends nothing.

  Verify with the backoffice tests.

- [x] 7.4 Create `revoke-invitation-dialog.tsx` (template `change-user-activation-dialog.tsx`) and its test (template
      `change-user-activation-dialog.test.tsx`). Cover:
  - title with the email, and the destructive button;
  - nothing sent before confirming, and Cancelar sends nothing;
  - success calls back with the invitation's id revoked;
  - `CONFLICT`, `NOT_FOUND` and `FORBIDDEN` keep it open with their message;
  - a stale close after `CONFLICT`/`NOT_FOUND` (button and Escape), but not after `FORBIDDEN`.

  Verify with the backoffice tests.

- [x] 7.5 Create `views/invitations-view.tsx` and the route `backoffice/src/app/invitations/page.tsx`. Add
      `test/presentation/invitations/views/invitations-view.test.tsx` covering every `backoffice-invitations` scenario:
  - as a trainer: rows, chips, dates, no "Entrenador", search by email, pills (selected, cleared, combined),
    "Invitar atleta", "Revocar" only on pending rows, invite success and revoke success (each reloads with its
    notice), stale close reloads;
  - as an admin: the "Entrenador" column, search by trainer and by email, pills, and no invite button, revoke
    action or dialogs (assert absence);
  - states: empty, no-match, retry;
  - English.

  Use AAA with one Act each. Verify with the backoffice tests.

## 8. Backoffice: drawer and users screen

- [x] 8.1 Update `peopleSection` in `navigation-drawer.tsx`:
  - `TRAINER`: `menu.athletes.all` → `/athletes`, `menu.athletes.invitations` → `/invitations`;
  - `ADMIN`: `menu.users.all` → `/users`, `menu.athletes.invitations` → `/invitations`.

  Update the drawer test:
  - the order test reads "Invitaciones" and has no "Invitaciones pendientes";
  - the trainer's links are `/athletes` and `/invitations`, each selected on its screen;
  - the admin's "Invitaciones" links to `/invitations` and is selected there;
  - "Calendario" is the placeholder that does nothing;
  - the link list test is updated.

  Verify with the backoffice tests.

- [x] 8.2 In `users-view.test.tsx`, change the trainer-by-URL case to a `FORBIDDEN` failure that shows its message
      with retry. Verify with the backoffice tests.
- [x] 8.3 Review the backoffice against its references (`users-view`, `use-users`, `use-create-user`,
      `use-change-user-activation`, `users-table`, `create-user-dialog`, `change-user-activation-dialog`, `role-filter`,
      `user-status-chip`, `filter-users`) and the `AGENTS.md` quality checklist:
  - atomic levels;
  - imports flow downward, and only atoms import MUI widgets;
  - no cross-area imports;
  - `src/app` holds routes only;
  - no `core/infrastructure` import in presentation;
  - every string in `es` and `en`.

  Verify by reading the diff against each reference.

## 9. Backoffice E2E

- [x] 9.1 Add `backoffice/e2e/athletes.spec.ts` (template `users.spec.ts`): a trainer session opens "Todos los
      atletas" from the drawer, sees the athletes and filters by name. Intercept `GET /users/me` and
      `GET /trainer-athletes` with `page.route`. Verify with `pnpm test:e2e`.
- [x] 9.2 Add `backoffice/e2e/invitations.spec.ts` with two flows:
  - a trainer opens "Invitaciones" from the drawer, invites an athlete through the modal (sees "Invitación
    enviada." and the new row), then revokes a pending invitation through the dialog (sees "Invitación revocada."
    and its "Revocada" status);
  - an admin opens "Invitaciones", filters by a status pill and by a trainer's name, and sees no "Invitar atleta"
    or "Revocar".

  Intercept `GET /users/me`, `GET`/`POST /athlete-invitations` and `PUT /athlete-invitations/*/revoke`. Verify
  with `pnpm test:e2e`.

## 10. Final verification

- [x] 10.1 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` and verify that all pass.
      Then run the template drift loop from `AGENTS.md` and verify it prints nothing.

## Workflow follow-up

- Archive the change after review. It creates `trainer-athletes`, `athlete-invitations`, `backoffice-athletes` and
  `backoffice-invitations`, and modifies `backoffice-navigation` and `backoffice-users`.
