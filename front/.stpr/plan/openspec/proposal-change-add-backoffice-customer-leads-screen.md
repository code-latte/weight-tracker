# Change Proposal: Add Backoffice Customer Leads Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-customer-leads-screen/`.

## Change

Refresh the OpenAPI snapshot to pick up `GET /customer-leads`, add `GetCustomerLeadsUseCase` to `core`, and add a
backoffice "Leads" screen under a new admin-only "Marketing" drawer section. The screen lists every registered lead.

## Objective

Leads are collected by the public website through `POST /customer-leads`, but nobody can read them without querying
the database. The API now provides `GET /customer-leads`, admin only (see
`api/.stpr/plan/openspec/proposal-change-add-list-customer-leads.md`). The front snapshot knows neither customer-leads
operation, `core` has no lead type or use case, and the backoffice has no screen or menu entry for them.

## Capabilities

- List customer leads (`GET /customer-leads`): the API returns every lead, newest first, to an `ADMIN` and `403` to
  anyone else. This is wired in `core` and both containers.
- New drawer section "Marketing" with one entry, "Leads", linking to `/customer-leads`. It is rendered for `ADMIN`
  only and shows as selected while on the screen.
- Leads table with two columns: "Email" and "Fecha de registro" (`registeredAtUtc`, formatted in the user's locale and
  time zone). Rows keep the order the API returns.
- Loading, empty and error states for the list.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER`
  - `ADMIN`: sees the "Marketing" section and its "Leads" entry, and the leads table.
  - `TRAINER`: must NOT see the "Marketing" section, its title or the "Leads" entry. If a trainer opens
    `/customer-leads` by URL, the API answers `403` and the screen shows `LoadError` (no table, no rows).
- app -> lands here: wiring only -> roles: `USER` -> the container registers `getCustomerLeadsUseCase`. No screen,
  hook or copy changes.

The front does not decide who may read leads. Hiding the section for non-admins is a presentation choice; the API
enforces the rule with `403`. The role comes from `useCurrentUser().profile.role`, as the people section already does.

## Classification

Core:

- `CustomerLead` (alias over `components["schemas"]["CustomerLeadResponse"]`) -> entity type ->
  `core/src/domain/customer-leads/customer-lead.ts`, plus its barrel and the re-export in `core/src/domain/index.ts`
- `getAll()` -> repository contract method -> a new customer leads repository contract in
  `core/src/application/ports/`
- `CustomerLeadRepository.getAll` -> repository implementation ->
  `core/src/infrastructure/repositories/customer-lead-repository.ts`
- `GetCustomerLeadsUseCase` -> use case ->
  `core/src/application/use-cases/customer-leads/get-customer-leads-use-case.ts`
- `Dependencies` key `getCustomerLeadsUseCase` -> composition contract -> `core/src/application/dependencies.ts`
- Barrels re-export the new alias, contract, repository and use case

Backoffice and app wiring:

- Registration of `CustomerLeadRepository` and `getCustomerLeadsUseCase` -> composition root ->
  `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`
- Placeholder for the new key -> test helper -> `backoffice/test/support/render-with-providers.tsx` and
  `app/test/support/render-with-providers.tsx`

Backoffice screen (new `customer-leads` domain area):

- `/customer-leads` route -> route -> `backoffice/src/app/customer-leads/page.tsx`
- `CustomerLeadsView` -> view -> `backoffice/src/presentation/customer-leads/views/customer-leads-view.tsx`
- `useCustomerLeads` (load, status, errorCode, reload) -> state hook ->
  `backoffice/src/presentation/customer-leads/hooks/use-customer-leads.ts`
- `CustomerLeadsTable` (columns, empty state) -> organism ->
  `backoffice/src/presentation/customer-leads/components/organisms/customer-leads-table.tsx`
- Table atoms, skeleton and `LoadError` -> reused from `common/components/`
- Drawer -> extended in place -> `backoffice/src/presentation/common/components/organisms/navigation-drawer.tsx`: a
  "Marketing" section appended for `ADMIN` only, after the existing sections
- Copy -> new `customerLeads` namespace (`backoffice/src/i18n/{es,en}/customer-leads.json`, registered like the
  others) plus new `menu.marketing.*` keys in `common.json`

## Canonical Template Mapping

- `CustomerLead` alias -> `core/src/domain/users/user.ts` -> alias over `components["schemas"]` only, new domain
  area folder with barrel.
- Repository contract and `CustomerLeadRepository.getAll` -> `UserRepository.getAll` -> `GET` with no body through
  `HttpClientInterface`, failures through `mapHttpError`.
- `GetCustomerLeadsUseCase` -> `core/src/application/use-cases/users/get-users-use-case.ts` -> no-input
  `UseCaseInterface<void, …>` delegating to a repository.
- Core tests -> `get-users-use-case.test.ts` and `user-repository.test.ts` (`getAll` block).
- Route -> `backoffice/src/app/users/page.tsx` -> default-exports the view, nothing else.
- `CustomerLeadsView` -> `backoffice/src/presentation/trainer-assignments/views/trainer-assignments-view.tsx` ->
  nearest admin-only list: `AppShell`, `h1`, skeleton / `LoadError` with retry, table. Without search, filters or
  dialog.
- `useCustomerLeads` -> `backoffice/src/presentation/users/hooks/use-users.ts` -> same `status` / `errorCode` /
  `reload` shape, local to the screen.
- `CustomerLeadsTable` -> `backoffice/src/presentation/invitations/components/organisms/invitations-table.tsx` ->
  organism over the table atoms, data by props, dates through `Intl.DateTimeFormat(i18n.language, …)`.
- Drawer section -> the existing `peopleSection(isAdmin)` in `navigation-drawer.tsx` -> role-dependent section built
  from `profile?.role === "ADMIN"`.
- View test -> `backoffice/test/presentation/trainer-assignments/views/trainer-assignments-view.test.tsx` -> fakes
  through the container.
- E2E -> `backoffice/e2e/users.spec.ts` -> `page.route` interception, admin session setup.
- Atom/molecule/organism references are `pending` in `.stpr/template/README.md`; follow the backoffice shapes above.

UI references come from the backoffice only. `.stpr/template/` and its README are not updated here.

## Screens And Navigation

- backoffice -> `/customer-leads` -> `CustomerLeadsView` -> drawer "Marketing" > "Leads" (`ADMIN` only), inside the
  session guard -> success: table, or empty state; failure: `LoadError` with retry (including a `TRAINER` reaching
  the URL directly, `403`); `401` follows the existing session handling.

## API Contract

- `GET /customer-leads` -> in `core/openapi/v1.json`: **no, refresh needed** -> no body, response `200`
  `CustomerLeadResponse[]` (`email`, `registeredAtUtc`), ordered newest first. Failures: `401`, `403` (`TRAINER`,
  `USER`). Provided by the API change `add-list-customer-leads`.
- `POST /customer-leads` -> also enters the snapshot with the refresh -> not used by this change (the website calls
  it directly and imports no workspace).

This change runs `openapi:refresh` + `openapi:generate` once, with the API running the `add-list-customer-leads`
change, and commits the snapshot and the generated types as a step of their own. No hand-written type.

## Held State

- `useCustomerLeads` -> the loaded lead list, status and error code -> the hook on mount and on `reload` -> resets on
  screen exit. Nothing is cached across screens or kept after logout.

## Forms And Structural Validation

None. The screen is read-only.

## Errors And Copy

- Session expired / missing -> `UNAUTHORIZED` -> existing session handling.
- Caller is not an admin -> `FORBIDDEN` -> `LoadError` with its existing message.
- No connection -> `NETWORK`. Anything else -> `UNEXPECTED` -> `LoadError` with retry.
- No new error code. The HTTP error mapper is not touched.

New copy, `common` namespace:

- `menu.marketing.title` -> "Marketing" -> "Marketing"
- `menu.marketing.leads` -> "Leads" -> "Leads"

New copy, `customerLeads` namespace:

- `list.title` -> "Leads" -> "Leads"
- `list.columns.email` -> "Email" -> "Email"
- `list.columns.registeredAt` -> "Fecha de registro" -> "Registered on"
- `list.empty` -> "Todavía no hay leads." -> "No leads yet."

## Platform Capabilities And Dependencies

None.

## Out Of Scope

- Search, filters, sorting, pagination or export of leads.
- Deleting, editing or contacting leads from the backoffice; lead detail screen.
- Any other entry in the "Marketing" section.
- Any app screen, and any `website` change.
- Must not change: the API, `POST /customer-leads` behaviour, the other drawer sections and their order.

## Notes

- **No stop case.** The endpoint exists in the API, and no business rule is added to the front.
- **Order.** The API already sorts newest first; the front renders rows as received and does not re-sort.
- **Date column.** `registeredAtUtc` is a UTC timestamp. Show date and time in the user's locale and local time zone
  (decide at proposal time whether time is shown; date + hours:minutes is the default).
- **Drawer while the profile loads.** The section is absent until the role is known to be `ADMIN`, so there is no
  flash of the admin entry for a trainer. The collapsed rail shows the "Leads" icon with its tooltip, like other
  entries.
- **Icon.** Pick an existing `@mui/icons-material` outlined icon (e.g. `CampaignOutlined` for "Leads"); no new
  dependency.
- **Snapshot refresh** is its own commit: `core/openapi/v1.json` plus `core/src/contract/generated`, never edited by
  hand.
- **Verification tiers:**
  - core unit: `get-customer-leads-use-case.test.ts` (success returned, repository failure returned unchanged).
    `customer-lead-repository.test.ts` for `getAll` (`GET /customer-leads`): `200`, `401`, `403`, `500` ->
    `UNEXPECTED`, network -> `NETWORK`.
  - container tests in both fronts gain the new key.
  - backoffice component tests:
    - `customer-leads-view.test.tsx`: rows with email and formatted date in API order; empty state; `LoadError` with
      retry; a `FORBIDDEN` failure shows `LoadError` and no table.
    - drawer test: `ADMIN` sees "Marketing" / "Leads" linking to `/customer-leads`; `TRAINER` sees neither the
      section title nor the entry.
  - backoffice E2E `e2e/customer-leads.spec.ts`: admin opens "Leads" from the drawer and sees the intercepted leads
    (`GET /customer-leads` intercepted).
  - translation parity passes with the new namespace and keys.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed.
