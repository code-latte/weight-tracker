# Design

## Context

See `proposal.md`, Why. What exists today:

- `core` has `UserSummary` (alias over `UserSummaryResponse`), `getUsersUseCase`, `linkAthleteToTrainerUseCase` and
  `endTrainerAthleteUseCase`. The last two take `TrainerAthleteInput` (`{ trainerId, athleteId }`). All three are
  registered in both `container.ts` files, and their repository already maps `400`/`401`/`403`/`404`/`409`.
- The backoffice has the `users` area. It holds the reference view, the list and action hooks, the users table, the
  activation dialog, the create dialog, the role pills and the `UserRowActions` ⋮ menu.
- The `athletes` area has a view, `useAthletes` and an `AthletesTable` with no actions.
- `presentation/common/` already holds shared helpers (`full-name.ts`, `search-text.ts`), atoms and the drawer.
- `add-backoffice-athletes-and-invitations` is implemented but not archived. Its `backoffice-athletes` and
  `backoffice-navigation` deltas must reach the main specs before this change's deltas apply.

## Goals / Non-Goals

**Goals:**

- One end-relationship flow (hook + dialog) used unchanged by the trainer's and the admin's screen.
- The admin screen needs one `GET /users` request; trainer options come from that same response.
- No change in `core` beyond the regenerated contract.

**Non-Goals:**

- No new use case, repository method, port or `Dependencies` key.
- No change to the users or invitations screens' behaviour. The users area only changes an import path.
- No app change.

## Decisions

### Contract refresh only in core

Run `openapi:refresh` + `openapi:generate` against the API branch with `add-trainer-to-users-list`.

- `UserSummary` keeps pointing to the `GET /users` item schema.
- If the API names it differently (for example `UserListItemResponse`), the alias in `core/src/domain/users/user.ts`
  is repointed to it. `UserSummary` stays the name, and the activate/deactivate responses keep their own alias.
- The nested trainer is read as `user.trainer` (`UserProfileResponse | null`).
- No hand-written type.
- Core tests whose fixtures build a `UserSummary` gain `trainer: null`.

Alternative: a new `AthleteAssignment` alias or use case. Rejected, because it adds a type and a use case for data the
existing listing already returns.

### Shared end-relationship flow in `presentation/common/`

Both `athletes` and `trainer-assignments` need it, and areas do not import each other. This is the same rule that put
`fullNameOf` in `common/`. The pieces:

- `common/hooks/use-end-trainer-athlete.ts`:
  - state: `{ submitting, errorCode, end(input), reset }`;
  - `end` runs `endTrainerAthleteUseCase` through `useSession().runWithSession`;
  - it resolves to `null` or the error code, as `useChangeUserActivation` does.
- `common/components/organisms/end-trainer-athlete-dialog.tsx`, modelled on `change-user-activation-dialog.tsx`:
  - props: `target: { trainerId, athleteId, athleteName } | null`, `onClose(stale)` and `onEnded()`;
  - the inner confirmation unmounts on close, the button is destructive, errors show in an `Alert`;
  - `CONFLICT` and `NOT_FOUND` mark the list stale.

Each view builds the `target` itself:

- trainer: `{ trainerId: profile.id, athleteId: row.athleteId }`;
- admin: `{ trainerId: user.trainer.id, athleteId: user.id }`.

The dialog never needs to know the caller's role.

### Row actions menu moves to `common/`

`UserRowActions` is generic (label, action label, `onAction`). It moves unchanged to
`common/components/molecules/row-actions.tsx` as `RowActions`, and `users-table.tsx` updates its import. It has no test of its own; the
users view tests keep covering it.

Alternative: duplicate it in each area, or import across areas. Rejected: the first duplicates code, the second
breaks the area boundary.

### `Autocomplete` atom

`common/components/atoms/autocomplete.tsx` wraps MUI `Autocomplete`. Only atoms import MUI widgets.

- Its input renders through the `TextField` atom (label, error, helper text).
- It takes `options`, `value`, `onChange`, `getOptionLabel`, `filterOptions` (optional), `noOptionsText` and `label`.
- No domain knowledge.

### `trainer-assignments` area

- **Route.** `src/app/trainer-assignments/page.tsx` default-exports `TrainerAssignmentsView`.
- **`useTrainerAssignments`** (modelled on `useUsers`):
  - holds `{ status, athletes, trainers, errorCode, reload }`;
  - one `getUsersUseCase` call, split by `role`: `USER` → athletes, `TRAINER` with `isActive` → trainer options;
  - keeps the API order;
  - splitting is display filtering, like the role pills, not a business rule.
  - Decision: inactive trainers are not offered. The API still answers `400`/`409` if a stale option is used.
- **`useAssignTrainer`** (modelled on `useCreateUser`): holds `{ submitting, errorCode, assign(input), reset }` over
  `linkAthleteToTrainerUseCase`.
- **`filterTrainerAssignments(athletes, search, assignment)`** (modelled on `filterUsers`):
  - matching is a case- and accent-insensitive substring on athlete full name, email and trainer full name, through
    `common/search-text.ts`;
  - `assignment` is `"with" | "without" | null`.
- **`AssignmentFilter`** molecule: a copy of `RoleFilter`'s pill group with the two pills.
- **`TrainerAssignmentsTable`** organism:
  - columns are full name, email, trainer and actions;
  - the actions column uses `RowActions` with "Finalizar relación" when `trainer !== null`, else "Asignar
    entrenador";
  - props are `athletes`, `filtered`, `onAssign(user)` and `onEnd(user)`.
- **`AssignTrainerDialog`** organism (modelled on `create-user-dialog.tsx`):
  - props are `athlete: UserSummary | null`, `trainers`, `onClose(stale)` and `onAssigned()`;
  - the form unmounts on close, and required validation shows on submit;
  - the selection is kept on failure, and `CONFLICT`/`NOT_FOUND` mark the list stale;
  - options are labelled "Full name · email" and filtered on both.
- **`TrainerAssignmentsView`** (modelled on `users-view.tsx`):
  - `AppShell`, `h1`, search + pills, skeleton / `LoadError`, table, both dialogs and a success `Alert`;
  - one message slot for "assigned" or "ended", cleared when a dialog opens.

### `athletes` area changes

- `AthletesTable` gains an optional `onEnd(athlete)`. With it, the table renders an actions column of `RowActions`,
  as `UsersTable` does with its activation prop.
- `AthletesView` adds:
  - the end target and the success message;
  - `EndTrainerAthleteDialog`, with `trainerId` from `useCurrentUser().profile.id`.
- `onEnd` is passed only once the profile is loaded.

### Drawer

In `navigation-drawer.tsx`, the admin branch of `peopleSection` becomes:

1. `menu.users.all` → `/users`
2. `menu.users.trainerAssignments` → `/trainer-assignments` (icon `AssignmentIndOutlined`)
3. `menu.athletes.invitations` → `/invitations`

The trainer branch is unchanged.

### Translations

- `common`:
  - `menu.users.trainerAssignments`;
  - `endRelationship.{title, body, confirm, cancel, success, errors.NOT_FOUND, errors.CONFLICT}`. The shared dialog
    reads its copy from `common`.
- `athletes`: `list.actions.open`, `list.actions.endRelationship` (and `list.columns.actions`, kept for screen
  readers only, as in `users`).
- New `trainerAssignments` namespace, registered in `resources.ts` for `es` and `en`. Its keys are the brief's list,
  assign and copy keys.
- Error codes involved and where each message comes from:

  | Code                         | Message source                                                           |
  | ---------------------------- | ------------------------------------------------------------------------ |
  | `UNAUTHORIZED`               | existing session handling                                                |
  | `FORBIDDEN`                  | `LoadError` or the dialog, from the common `errors` text                 |
  | `NOT_FOUND` (end and assign) | own message                                                              |
  | `CONFLICT` (end and assign)  | own message                                                              |
  | `VALIDATION` (assign)        | own message; elsewhere the common `errors` text                          |
  | `NETWORK`, `UNEXPECTED`      | common `errors` text, through `defaultValue` as in the activation dialog |

  No new code.

### Held state

| Owner                                      | Holds                                            | Changed by            | Reset                  |
| ------------------------------------------ | ------------------------------------------------ | --------------------- | ---------------------- |
| `useTrainerAssignments`                    | users split, status, error                       | mount, `reload`       | screen exit            |
| `useAssignTrainer`, `useEndTrainerAthlete` | submitting, error                                | their action, `reset` | dialog close (unmount) |
| `TrainerAssignmentsView`                   | search, pill, assign target, end target, message | the admin             | screen exit            |
| `AthletesView`                             | search, end target, message                      | the trainer           | screen exit            |

Nothing survives logout.

### Tests

These follow the existing tests of the same tier, with Arrange/Act/Assert and use cases faked through the container.

- **Component tests:**
  - `test/presentation/trainer-assignments/views/trainer-assignments-view.test.tsx`;
  - `.../components/organisms/assign-trainer-dialog.test.tsx`;
  - `test/presentation/trainer-assignments/filter-trainer-assignments.test.ts`;
  - `test/presentation/common/components/organisms/end-trainer-athlete-dialog.test.tsx`;
  - no atom test: the backoffice has no atom tier, so `Autocomplete` is covered through `assign-trainer-dialog.test.tsx`;
  - updates to `athletes-view.test.tsx` and `navigation-drawer.test.tsx`.
- **E2E** with `page.route`: `e2e/trainer-assignments.spec.ts`, plus a case in `e2e/athletes.spec.ts`.

No test helper changes. `render-with-providers` already knows every key used.

## Risks / Trade-offs

- [The API names the list item schema differently from the brief] → the alias is repointed in one place. The spec
  and view code only see `UserSummary`.
- [`add-backoffice-athletes-and-invitations` not archived first] → the navigation MODIFIED blocks would apply over the
  older text. Archive it before archiving this change.
- [Trainer options filtered to active] → a trainer deactivated after load can still be chosen. The API refuses it,
  and the dialog shows the error and marks the list stale.
- [Moving `UserRowActions` touches the users area] → it is an import-only change, covered by the existing users tests.

## Migration Plan

1. Refresh and commit the snapshot and the generated types as a separate step.
2. Ship the front.

No data migration. Rollback is reverting the front commit; the extra `trainer` field is harmless to older fronts.
