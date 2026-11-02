# Change Proposal: Trainer Assignment And Ending Relationships In The Backoffice

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-trainer-assignment/`.

## Change

Refresh the OpenAPI snapshot to pick up the new `trainer` property on `GET /users`. In the backoffice, let a
`TRAINER` end the relationship with one of their athletes from "Todos los atletas", and give an `ADMIN` a new
"Asignación de entrenadores" screen where they assign a trainer to an athlete or end the athlete's current
relationship.

## Objective

`core` already wires `linkAthleteToTrainerUseCase` and `endTrainerAthleteUseCase` (change
`add-backoffice-athletes-and-invitations`), but no screen uses them. A trainer cannot stop training an athlete, and an
admin cannot link athletes to trainers at all. The API change `add-trainer-to-users-list`
(`api/.stpr/plan/openspec/proposal-change-add-trainer-to-users-list.md`) makes `GET /users` return each athlete's
current trainer, so the admin screen needs a single request and no join in the front.

## Capabilities

Backoffice, `TRAINER`:

- **"Todos los atletas"** (`/athletes`) gains an actions column with a "Finalizar relación" action per row.
  - It opens a confirmation dialog naming the athlete. Confirming sends `PUT /trainer-athletes/{trainerId}/end` with
    `{ athleteId }`, where `trainerId` is the logged-in trainer's id (`useCurrentUser().profile.id`) and `athleteId`
    the row's `athleteId`.
  - On success the dialog closes, "Relación finalizada." shows and the list reloads (the athlete disappears).
  - On failure the dialog stays open with the error.

Backoffice, `ADMIN`:

- **"Asignación de entrenadores"** (`/trainer-assignments`): a table of every user with role `USER` from
  `GET /users`. Its columns are "Nombre completo", "Email", "Entrenador" (the trainer's full name, or "Sin
  entrenador") and "Acciones". One search field filters client-side by athlete full name, athlete email or trainer
  full name. Pills "Con entrenador" / "Sin entrenador" filter by assignment; search and pills combine.
  - A row **with** a trainer shows "Finalizar relación": the same confirmation dialog as the trainer's, sending
    `PUT /trainer-athletes/{user.trainer.id}/end` with `{ athleteId: user.id }`.
  - A row **without** a trainer shows "Asignar entrenador": a modal with an MUI `Autocomplete` over the trainers
    (users with role `TRAINER` from the same `GET /users` response), searchable by full name or email. Accepting sends
    `POST /trainer-athletes/{trainerId}` with `{ athleteId: user.id }`. On success the modal closes, "Entrenador
    asignado." shows and the list reloads. On failure the modal stays open with the error and the selection.
  - Exactly one of the two actions shows per row, decided by `user.trainer` being `null` or not.

Drawer and copy:

- `ADMIN`, "Usuarios" section: "Todos los usuarios", **"Asignación de entrenadores"** (`/trainer-assignments`),
  "Invitaciones", in that order.
- `TRAINER` drawer unchanged.

Common to the list: loading, empty, no-match and error states, as in the users screen.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER`
  - `TRAINER`:
    - sees "Finalizar relación" on each row of "Todos los atletas", and its dialog;
    - has no "Asignación de entrenadores" drawer entry. If they open `/trainer-assignments` by URL, `GET /users`
      answers `403` and the screen shows the load error.
  - `ADMIN`:
    - sees "Asignación de entrenadores" with both actions and both dialogs;
    - the athletes screen is unchanged for them (they still get `403` there).
- app -> lands here: no -> no wiring, screen, hook or copy changes. The snapshot refresh regenerates types shared
  through `core`, so the app's typecheck and tests must stay green.

The front decides none of these permissions. It hides actions per role as a presentation choice; the API enforces
them. Which action a row shows comes from the API's `trainer` value, never from a front-side rule.

## Classification

Core:

- `User` alias -> entity type -> `core/src/domain/users/user.ts` -> unchanged source; the regenerated
  `components["schemas"]` gains `trainer`. If the API introduces a list-only item schema, the alias points to it.
- No new repository method, use case or `Dependencies` key: `getUsersUseCase`, `linkAthleteToTrainerUseCase` and
  `endTrainerAthleteUseCase` already exist and are registered in both containers.

Backoffice, shared between the `athletes` and `trainer-assignments` domain areas (in `presentation/common/`, not
imported across areas):

- `useEndTrainerAthlete` (submitting, errorCode, end, reset) -> state hook ->
  `backoffice/src/presentation/common/hooks/use-end-trainer-athlete.ts`.
- `EndTrainerAthleteDialog` (confirmation) -> organism ->
  `backoffice/src/presentation/common/components/organisms/end-trainer-athlete-dialog.tsx`.

Backoffice, `athletes` domain area:

- `AthletesTable` -> organism, extended with an optional actions column ->
  `backoffice/src/presentation/athletes/components/organisms/athletes-table.tsx`.
- `AthletesView` -> view, extended with the dialog, the end target and the success message ->
  `backoffice/src/presentation/athletes/views/athletes-view.tsx`.

Backoffice, new `trainer-assignments` domain area:

- `/trainer-assignments` route -> route -> `backoffice/src/app/trainer-assignments/page.tsx`.
- `TrainerAssignmentsView` -> view -> `backoffice/src/presentation/trainer-assignments/views/trainer-assignments-view.tsx`.
- `useTrainerAssignments` (athletes, trainers, status, errorCode, reload; splits one `GET /users` result by role) ->
  state hook -> `backoffice/src/presentation/trainer-assignments/hooks/use-trainer-assignments.ts`.
- `useAssignTrainer` (submitting, errorCode, assign, reset) -> state hook ->
  `backoffice/src/presentation/trainer-assignments/hooks/use-assign-trainer.ts`.
- `TrainerAssignmentsTable` -> organism -> `.../trainer-assignments/components/organisms/trainer-assignments-table.tsx`.
- `AssignTrainerDialog` (autocomplete inside a dialog) -> organism ->
  `.../trainer-assignments/components/organisms/assign-trainer-dialog.tsx`.
- `AssignmentFilter` ("Con entrenador" / "Sin entrenador" pills) -> molecule ->
  `.../trainer-assignments/components/molecules/assignment-filter.tsx`.
- `filterTrainerAssignments` -> view filter -> `backoffice/src/presentation/trainer-assignments/filter-trainer-assignments.ts`.

Shared pieces:

- `Autocomplete` -> new atom wrapping MUI `Autocomplete` (only atoms import MUI widgets) ->
  `backoffice/src/presentation/common/components/atoms/autocomplete.tsx`.
- Full name: reuse the existing `fullNameOf` helper wherever the athletes change left it.
- Atoms: reuse table, text field, chip, button, dialog atoms and alert. Loading and error: `HomeSkeleton`,
  `LoadError`.
- Drawer: extended in place -> `backoffice/src/presentation/common/components/organisms/navigation-drawer.tsx`.
- Copy: new `trainerAssignments` namespace, plus keys in `athletes` and `common` (`backoffice/src/i18n/{es,en}/`,
  registered in `resources.ts`).

## Canonical Template Mapping

- Route -> `backoffice/src/app/users/page.tsx` -> default-exports the view, nothing else.
- `TrainerAssignmentsView` -> `backoffice/src/presentation/users/views/users-view.tsx` -> `AppShell`, `h1`, search +
  pills above a table, skeleton / `LoadError`, row actions opening dialogs, success message.
- `AthletesView` changes -> the same file's dialog and success-message handling.
- `useTrainerAssignments` -> `backoffice/src/presentation/users/hooks/use-users.ts`.
- `useEndTrainerAthlete`, `useAssignTrainer` -> `backoffice/src/presentation/users/hooks/use-change-user-activation.ts`
  (id-driven action with submitting and errorCode) and `use-create-user.ts` (request forwarding).
- `TrainerAssignmentsTable`, `AthletesTable` actions column -> `backoffice/src/presentation/users/components/organisms/users-table.tsx`
  plus `users/components/molecules/user-row-actions.tsx` -> optional actions column, one action per row state.
- `EndTrainerAthleteDialog` -> `users/components/organisms/change-user-activation-dialog.tsx` -> confirmation,
  destructive button, error `Alert`, stale-list close.
- `AssignTrainerDialog` -> `users/components/organisms/create-user-dialog.tsx` -> form inside a dialog, unmounted on
  close, error `Alert`.
- `AssignmentFilter` -> `users/components/molecules/role-filter.tsx` -> same pill group.
- `filterTrainerAssignments` -> `backoffice/src/presentation/users/filter-users.ts` -> case- and accent-insensitive
  substring, API order kept.
- `Autocomplete` atom -> `backoffice/src/presentation/common/components/atoms/text-field.tsx` -> thin MUI wrapper;
  the input renders through the `TextField` atom.
- Atom, molecule and organism references are `pending` in `.stpr/template/README.md`. Follow the backoffice shapes
  above.
- View tests -> `backoffice/test/presentation/users/views/users-view.test.tsx`. Dialog tests ->
  `create-user-dialog.test.tsx`, `change-user-activation-dialog.test.tsx`. E2E -> `backoffice/e2e/users.spec.ts`.

UI references come from the backoffice only. `.stpr/template/` and its README are not updated here.

## Screens And Navigation

- backoffice -> `/athletes` -> `AthletesView` -> drawer "Atletas" > "Todos los atletas" (`TRAINER`) -> "Finalizar
  relación" -> `EndTrainerAthleteDialog`:
  - success: the dialog closes, "Relación finalizada." shows and the list reloads;
  - failure: the dialog stays open with the error. Closing after a `NOT_FOUND` or `CONFLICT` reloads the list;
  - Cancel: closes without a request.
- backoffice -> `/trainer-assignments` -> `TrainerAssignmentsView` -> drawer "Usuarios" > "Asignación de
  entrenadores" (`ADMIN`) -> success: table or empty state; failure: `LoadError` with retry (a trainer reaching the
  URL gets `403`); `401` follows the existing session handling.
  - "Finalizar relación" -> `EndTrainerAthleteDialog`, as above.
  - "Asignar entrenador" -> `AssignTrainerDialog`:
    - success: the dialog closes, "Entrenador asignado." shows and the list reloads;
    - failure: the dialog stays open with the error and the selected trainer. Closing after a `CONFLICT` reloads
      the list;
    - Cancel: closes without a request.

## API Contract

- `GET /users` (`ADMIN`) -> in `core/openapi/v1.json`: **no, not with `trainer`** -> each item gains `trainer`:
  `null`, or the trainer's `id`, `email`, `name`, `surname`, `role`. Provided by the API change
  `add-trainer-to-users-list`. Failures: `401`, `403`.
- `POST /trainer-athletes/{trainerId}` (`ADMIN`) -> yes -> request `TrainerAthleteRequest` (`athleteId`), response
  `201` `TrainerAthleteResponse`. Failures: `400`, `401`, `403`, `404`, `409`.
- `PUT /trainer-athletes/{trainerId}/end` (`ADMIN`, the trainer, the athlete) -> yes -> request
  `TrainerAthleteRequest` (`athleteId`), response `200` `TrainerAthleteResponse`. Failures: `400`, `401`, `403`,
  `404`, `409`.
- `GET /trainer-athletes` -> yes -> unchanged, still the source of "Todos los atletas". `GET /trainer-athletes/{trainerId}`
  is **not** used.

Run `openapi:refresh` + `openapi:generate` once, with the API running from the branch that has
`add-trainer-to-users-list`. Commit the snapshot and the generated types as a step of their own. No hand-written type.

## Held State

- `useTrainerAssignments` -> the loaded athletes and trainers, status and error code -> the hook on mount and on
  `reload` (after an assign, an end or a stale-list close) -> resets on screen exit.
- `TrainerAssignmentsView` local state -> search text, selected pill, assign target, end target, success message ->
  resets on screen exit.
- `AthletesView` local state -> gains the end target and success message -> resets on screen exit.
- `AssignTrainerDialog` form state -> selected trainer and field error -> resets when the dialog closes.
- Nothing is cached across screens or kept after logout.

## Forms And Structural Validation

- Assign trainer -> trainer -> required (the accept button is disabled, or shows "Selecciona un entrenador.", until
  an option is chosen; decide at proposal time following `create-user-dialog.tsx`).

Whether the trainer can take the athlete (role, an athlete who already has a trainer, an inactive account) is an API
rule and comes back as `400`, `404` or `409`.

## Errors And Copy

- Session expired / missing -> `UNAUTHORIZED` -> existing session handling.
- Caller not allowed (trainer opening `/trainer-assignments`, ending someone else's relationship) -> `FORBIDDEN` ->
  `LoadError` or dialog error with the existing message.
- End: no active relationship any more -> `NOT_FOUND` / `CONFLICT` -> "Esta relación ya no está activa. Recarga la
  lista."
- Assign: athlete already has a trainer, or the pair cannot be linked -> `CONFLICT` -> "No se puede asignar este
  entrenador: el atleta ya tiene uno o la relación no es válida."
- Assign: trainer or athlete not found -> `NOT_FOUND` -> "El entrenador o el atleta ya no existe."
- Assign: request refused -> `VALIDATION` -> "Selecciona un entrenador válido."
- No connection -> `NETWORK`. Anything else -> `UNEXPECTED` -> generic error.
- No new error code. The HTTP error mapper is not touched.

New copy, `common` namespace:

- `menu.users.trainerAssignments` -> "Asignación de entrenadores" -> "Trainer assignment"

New copy, `athletes` namespace:

- `list.columns.actions` -> "Acciones" -> "Actions"
- `list.endRelationship` -> "Finalizar relación" -> "End relationship"

Shared end dialog (keys in `common` or a shared namespace, decided at proposal time):

- `endRelationship.title` -> "¿Finalizar la relación con {{name}}?" -> "End the relationship with {{name}}?"
- `endRelationship.body` -> "El atleta dejará de estar asignado a este entrenador." -> "The athlete will no longer be
  assigned to this trainer."
- `endRelationship.confirm`, `cancel` -> "Finalizar", "Cancelar" -> "End", "Cancel"
- `endRelationship.success` -> "Relación finalizada." -> "Relationship ended."
- `endRelationship.errors.NOT_FOUND`, `CONFLICT` -> as above -> "This relationship is no longer active. Reload the
  list."

New copy, `trainerAssignments` namespace:

- `list.title` -> "Asignación de entrenadores" -> "Trainer assignment"
- `list.search` -> "Buscar por atleta, email o entrenador" -> "Search by athlete, email or trainer"
- `list.filter.withTrainer`, `withoutTrainer` -> "Con entrenador", "Sin entrenador" -> "With trainer", "Without
  trainer"
- `list.columns.fullName`, `email`, `trainer`, `actions` -> "Nombre completo", "Email", "Entrenador", "Acciones" ->
  "Full name", "Email", "Trainer", "Actions"
- `list.noTrainer` -> "Sin entrenador" -> "No trainer"
- `list.assign`, `list.endRelationship` -> "Asignar entrenador", "Finalizar relación" -> "Assign trainer", "End
  relationship"
- `list.empty` -> "Todavía no hay atletas." -> "No athletes yet."
- `list.noMatches` -> "Ningún atleta coincide con la búsqueda." -> "No athletes match your search."
- `assign.title` -> "Asignar entrenador a {{name}}" -> "Assign a trainer to {{name}}"
- `assign.fields.trainer` -> "Entrenador" -> "Trainer"
- `assign.noOptions` -> "Ningún entrenador coincide." -> "No trainers match."
- `assign.submit`, `cancel` -> "Asignar", "Cancelar" -> "Assign", "Cancel"
- `assign.success` -> "Entrenador asignado." -> "Trainer assigned."
- `assign.validation.required` -> "Selecciona un entrenador." -> "Select a trainer."
- `assign.errors.VALIDATION`, `NOT_FOUND`, `CONFLICT` -> as above -> "Select a valid trainer.", "The trainer or the
  athlete no longer exists.", "This trainer cannot be assigned: the athlete already has one or the relationship is
  not valid."

## Platform Capabilities And Dependencies

None. MUI `Autocomplete` ships in `@mui/material`, already a backoffice dependency.

## Out Of Scope

- Changing an athlete's trainer in one step (reassign). The admin ends the relationship, then assigns.
- Relationship history, start dates on the assignment screen, and `GET /trainer-athletes/{trainerId}`.
- Ending a relationship from the app (athlete side).
- Server-side search, sorting and pagination.
- Must not change: the API, the users screen, the invitations screen, the trainer drawer, `/users/me` and the app.

## Notes

- **Stop case.** This change must not start until `add-trainer-to-users-list` is merged and running for the snapshot
  refresh. Every other endpoint is already in the snapshot and in `core`.
- **Trainer options.** The autocomplete lists users with role `TRAINER` from the same `GET /users` response, labelled
  "Full name · email". Recommended: list only active trainers (`isActive`) as a presentation choice, since the users
  screen already shows activation; the API still decides with `400` / `409`. Confirm at proposal time.
- **Inactive athletes.** The table lists every `USER`, active or not, as the API returns them. Whether an inactive
  athlete can be assigned is the API's answer.
- **Role split.** Picking `USER` rows and `TRAINER` options from `GET /users` by `role` is display filtering, like the
  users screen's role pills, not a business rule.
- **Trainer id for the trainer's end.** Taken from `useCurrentUser().profile.id`; the API checks it is the caller.
- **Use case inputs.** `endTrainerAthleteUseCase` and `linkAthleteToTrainerUseCase` take `TrainerAthleteInput`
  (`{ trainerId, athleteId }`), already defined in `core/src/domain/trainer-athletes/trainer-athlete.ts`.
- **Shared dialog location (decided).** The end dialog and its hook serve two domain areas, so they live in
  `presentation/common/` instead of one area importing the other.
- **Verification tiers:**
  - core unit: none new; the existing `GET /users` repository test fixtures gain `trainer` where the regenerated
    types require it.
  - backoffice component tests:
    - `athletes-view.test.tsx`: "Finalizar relación" per row; end success sends the current trainer's id and the
      row's `athleteId`, shows the message and reloads; `NOT_FOUND` keeps the dialog open; stale-list close reloads.
    - `trainer-assignments-view.test.tsx`, as an admin: only `USER` rows; trainer full name or "Sin entrenador";
      "Finalizar relación" only on rows with a trainer and "Asignar entrenador" only on rows without (assert the
      other is absent); search by athlete, email and trainer; pills; empty, no-match, `LoadError` with retry; a
      trainer's `FORBIDDEN`.
    - `end-trainer-athlete-dialog.test.tsx`: copy with the athlete's name, destructive button, cancel sends nothing,
      success, `NOT_FOUND` / `CONFLICT`, stale-list close.
    - `assign-trainer-dialog.test.tsx`: options are trainers only, filtering by name and email, required error
      blocks sending, request carries the chosen `trainerId` and the row's `athleteId`, success, `VALIDATION`,
      `NOT_FOUND`, `CONFLICT` with the selection kept, cancel.
    - drawer test: admin sees "Asignación de entrenadores" between "Todos los usuarios" and "Invitaciones", linking
      to `/trainer-assignments`; a trainer does not see it.
  - backoffice E2E (`e2e/trainer-assignments.spec.ts`, plus a case in `e2e/athletes.spec.ts`):
    - an admin opens "Asignación de entrenadores", assigns a trainer to an athlete without one, and ends the
      relationship of an athlete with one;
    - a trainer ends the relationship with an athlete from "Todos los atletas".
    - All API calls are intercepted with `page.route`.
  - translation parity passes with the new namespace and keys.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed.
