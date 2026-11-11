# Design

## Context

See proposal.md (Why). Facts the approach builds on:

- `core/openapi/v1.json` has no `/customer-leads` path. The API change `add-list-customer-leads` declares
  `GET /customer-leads` → `200 CustomerLeadResponse[]` (`email`, `registeredAtUtc`), plus `401` and `403`. It is not
  committed yet, so the refresh must run against an API built from that working tree.
- Domain areas in `core/src/domain/<area>/<area>.ts` have no barrel of their own. `core/src/domain/index.ts`
  re-exports their aliases.
- `Dependencies` holds both repository keys (`userRepository: UserRepositoryInterface`) and use case keys. Each
  front's `container.ts` registers both, and `test/support/render-with-providers.tsx` has a placeholder per use case
  key.
- The HTTP error mapper already maps `401` → `UNAUTHORIZED`, `403` → `FORBIDDEN`, `5xx` → `UNEXPECTED`, and
  network failures → `NETWORK`.
- `useUsers` sets the shape of a list hook: `status` / list / `errorCode` / `reload`, `runWithSession`, a generation
  guard against late answers.
- `trainer-assignments-view.tsx` sets the shape of an admin list screen: `AppShell` around a content component, `h1`,
  `LoadError` on failure, `HomeSkeleton` while loading, the table organism otherwise.
- The drawer prepends `peopleSection(profile?.role === "ADMIN")` to a static `sections` array. Selection compares
  `href` with the pathname.

## Goals / Non-Goals

**Goals:**

- Add the operation in `core` once, as a new domain area, and register it in both containers.
- Build the screen only from existing atoms and molecules. The only new UI piece is a table organism.

**Non-Goals:**

- Changing the error mapper, the website, `POST /customer-leads`, or any other drawer section.
- Client or server search, sorting, pagination, or caching leads across screens.
- Any app UI.

## Decisions

### Core

- **Alias** in `core/src/domain/customer-leads/customer-lead.ts`:
  `CustomerLead = components["schemas"]["CustomerLeadResponse"]`, re-exported from `core/src/domain/index.ts`. The
  public `POST` request type enters the snapshot but gets no alias, because nothing in the front uses it.
  - Why a new area rather than adding to `users`: a lead is not a user, and the API keeps it as a record of its own.
- **Port**: `core/src/application/ports/customer-lead-repository-interface.ts` with
  `getAll(): Promise<Result<CustomerLead[], DomainError>>`, exported from the ports barrel.
- **Repository**: `CustomerLeadRepository.getAll` in
  `core/src/infrastructure/repositories/customer-lead-repository.ts` (`GET /customer-leads`, no body), with the same
  `try` / `ok` / `fail(mapHttpError(error))` shape as `UserRepository.getAll`. It is exported from the repositories
  barrel.
- **Use case**: `GetCustomerLeadsUseCase implements UseCaseInterface<void, CustomerLead[]>` in
  `core/src/application/use-cases/customer-leads/get-customer-leads-use-case.ts`, modelled on `GetUsersUseCase`. A new
  `customer-leads/index.ts` barrel is re-exported from `use-cases/index.ts`.
- **Dependencies**: new keys `customerLeadRepository: CustomerLeadRepositoryInterface` and
  `getCustomerLeadsUseCase: UseCaseInterface<void, CustomerLead[]>`.
  - Both are registered in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`:
    `customerLeadRepository: (c) => new CustomerLeadRepository(c.resolve("httpClient"))` and
    `getCustomerLeadsUseCase: (c) => new GetCustomerLeadsUseCase(c.resolve("customerLeadRepository"))`.
  - A placeholder for `getCustomerLeadsUseCase` goes in both `test/support/render-with-providers.tsx`, the same way
    the other use case keys get one.
  - Why the app gets it too: `Dependencies` is one contract for both fronts. Leaving the key out of the app's
    container would break its typecheck.

### Backoffice

New `customer-leads` domain area:

| Piece                | Level      | Path                                                                            |
| -------------------- | ---------- | ------------------------------------------------------------------------------- |
| `/customer-leads`    | route      | `backoffice/src/app/customer-leads/page.tsx`                                    |
| `CustomerLeadsView`  | view       | `src/presentation/customer-leads/views/customer-leads-view.tsx`                 |
| `useCustomerLeads`   | state hook | `src/presentation/customer-leads/hooks/use-customer-leads.ts`                   |
| `CustomerLeadsTable` | organism   | `src/presentation/customer-leads/components/organisms/customer-leads-table.tsx` |

- **`useCustomerLeads`**: copy of `useUsers` resolving `getCustomerLeadsUseCase`; returns
  `{ status, leads, errorCode, reload }`.
- **`CustomerLeadsView`**: `AppShell` around a content component, as in `trainer-assignments-view.tsx`: `Container`,
  `h1` "Leads", then `LoadError` (failed), `HomeSkeleton` (loading) or `CustomerLeadsTable` (loaded). It has no
  toolbar, search, filters, dialog or role check. The API's `403` is the only gate, and it lands in `LoadError`.
- **`CustomerLeadsTable`**: shaped like `invitations-table.tsx`. It takes `leads` by props and renders the "Email" and
  "Fecha de registro" columns over the table atoms. When `leads` is empty it shows "Todavía no hay leads." with the
  `typography` atom.
  - Dates go through `new Intl.DateTimeFormat(i18n.language, { day: "2-digit", month: "2-digit", year: "numeric",
hour: "2-digit", minute: "2-digit" })` over `new Date(registeredAtUtc)`. The browser applies its local time zone.
  - Rows are keyed by `email`, which the API keeps unique.
  - Rows render as received, with no sorting.
- **Drawer** (`navigation-drawer.tsx`): a `marketingSection` constant `{ title: "menu.marketing.title", items: [{
label: "menu.marketing.leads", icon: CampaignOutlined, href: "/customer-leads" }] }`. The render list becomes
  `[peopleSection(isAdmin), ...sections, ...(isAdmin ? [marketingSection] : [])]`.
  - `isAdmin` is the existing `profile?.role === "ADMIN"`, computed once. A visitor whose profile has not loaded is
    therefore not admin and sees no Marketing section, so the section never flashes for a trainer.
  - Selection works through the existing `href === pathname` comparison.
- **Imports** flow downward: view → organism → atoms. Only atoms import MUI widgets. `Box`, `Container` and the icon
  follow the exceptions the trainer assignments view and the drawer already use.

### Translations

- New `customerLeads` namespace in `backoffice/src/i18n/{es,en}/customer-leads.json`, registered in `resources.ts`:
  `list.title` ("Leads" / "Leads"), `list.columns.email` ("Email" / "Email"), `list.columns.registeredAt` ("Fecha de
  registro" / "Registered on"), `list.empty` ("Todavía no hay leads." / "No leads yet.").
- `common.json` gains `menu.marketing.title` ("Marketing" / "Marketing") and `menu.marketing.leads` ("Leads" /
  "Leads").

### Errors

| Failure       | Code           | Shown                                          |
| ------------- | -------------- | ---------------------------------------------- |
| not admin     | `FORBIDDEN`    | `LoadError` with retry                         |
| no connection | `NETWORK`      | `LoadError` with retry                         |
| any other     | `UNEXPECTED`   | `LoadError` with retry                         |
| session       | `UNAUTHORIZED` | existing session handling via `runWithSession` |

No new code. The mapper is untouched, and `LoadError` already translates each code in `es` and `en`.

### State

| Holder             | Holds                    | Changed by      | Resets      |
| ------------------ | ------------------------ | --------------- | ----------- |
| `useCustomerLeads` | list, status, error code | mount, `reload` | screen exit |

Nothing persists across screens or after logout.

### Tests

The templates are the existing tests of the referenced files. Test helpers change only to add the container
placeholder.

- **core**: `get-customer-leads-use-case.test.ts` (template `get-users-use-case.test.ts`) and
  `customer-lead-repository.test.ts` (template: the `getAll` block of `user-repository.test.ts`): `200`, empty list,
  `401`, `403`, `500` → `UNEXPECTED`, network → `NETWORK`.
- **backoffice component tests**:
  - `customer-leads-view.test.tsx` (template `trainer-assignments-view.test.tsx`): rows in API order, email and
    formatted date, loading, empty, failure with retry, `FORBIDDEN` with no table, English heading and columns;
  - the drawer test: an admin sees "Marketing" / "Leads" last, links to `/customer-leads` and selected there; a
    trainer and a profile still loading see neither;
  - the container test gains the two keys.
- **app**: the container test gains the two keys.
- **E2E**: `e2e/customer-leads.spec.ts` (template `users.spec.ts`) intercepts `GET /users/me` (admin) and
  `GET /customer-leads`, opens "Leads" from the drawer and sees the intercepted rows.
- **Translations**: the parity tests pick up the new namespace and keys.

Date assertions in tests build the expected text with the same `Intl.DateTimeFormat` options, not a hard-coded
string, so they do not depend on the time zone of the machine running them.

## Risks / Trade-offs

- [The snapshot refresh depends on an uncommitted API change] → Run the refresh against the API built from the
  current `api/` working tree. If the API change is altered before merge, refresh again. Task 1 verifies the schema
  name and fields before anything else is built.
- [The whole list loads at once] → Acceptable at current volume. Pagination would be an API change and is out of
  scope.
- [A trainer who types the URL sees a generic `FORBIDDEN` error] → Their drawer has no entry, so they only get there
  deliberately. The API still refuses the data.
- [The generated schema name differs from `CustomerLeadResponse`] → The alias follows whatever name the refresh
  produces. Only the alias line changes.

## Migration Plan

The snapshot refresh goes first, in its own commit, with the API running locally. Core, wiring and the backoffice
follow. There is no data migration. To roll back, revert the commits. The API endpoint stays usable without the front.
