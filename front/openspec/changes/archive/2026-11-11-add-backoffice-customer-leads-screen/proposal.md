# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-customer-leads-screen.md`.

## Why

The public website collects leads through `POST /customer-leads`, but the only way to read them is to query the
database. The API now offers `GET /customer-leads`, admin only (API change `add-list-customer-leads`). The front
snapshot knows neither customer-leads operation, `core` has no lead type or use case, and the backoffice has no screen
or menu entry for leads.

## What Changes

- **Snapshot refresh**: `openapi:refresh` + `openapi:generate` pick up `GET /customer-leads` (and the already public
  `POST /customer-leads`, which nothing in the front uses). They go in a commit of their own. No hand-written type.
- **core**: a new `customer-leads` domain area with the `CustomerLead` alias, a customer lead repository contract and
  implementation (`getAll`), `GetCustomerLeadsUseCase`, and the keys `customerLeadRepository` /
  `getCustomerLeadsUseCase` in `Dependencies`. Both fronts register them. The app only gets the wiring.
- **backoffice `/customer-leads` screen** ("Leads"):
  - a read-only table with "Email" and "Fecha de registro" (`registeredAtUtc`, shown in the user's locale and local
    time zone), in the order the API returns (newest first);
  - loading, empty and error states.
- **Drawer**: a new "Marketing" section with one entry, "Leads", linking to `/customer-leads`. It is rendered for an
  `ADMIN` only, after the existing sections.

### Fronts and roles

- backoffice, `ADMIN`: sees the "Marketing" section, its "Leads" entry and the leads table.
- backoffice, `TRAINER`: must NOT see the "Marketing" title or the "Leads" entry. Neither do visitors whose profile
  has not loaded yet. If a trainer opens `/customer-leads` by URL, the API answers `403` and the screen shows
  `LoadError`, with no table and no rows.
- app, `USER`: wiring only (container keys). No screen, hook or copy changes.

Hiding the section is a presentation choice. The API enforces who may read leads (`403`).

### Components (atomic level, reuse)

- Atoms reused, none added: `table*`, `typography`, `skeleton`.
- Molecules reused, none added: `LoadError`, `HomeSkeleton`.
- Organisms: new `CustomerLeadsTable`; `navigation-drawer` and `AppShell` extended or reused.

### Canonical references

All UI references come from the backoffice. Atom, molecule and organism references are `pending` in
`.stpr/template/README.md`, so this change follows the backoffice shapes already in place.

- Alias -> `core/src/domain/users/user.ts`, re-exported from `core/src/domain/index.ts`.
- Repository contract and implementation -> `UserRepositoryInterface.getAll` / `UserRepository.getAll`.
- Use case -> `core/src/application/use-cases/users/get-users-use-case.ts`.
- Route -> `backoffice/src/app/users/page.tsx`.
- View -> `trainer-assignments-view.tsx`, the closest admin-only list. List hook -> `users/hooks/use-users.ts`.
- Table -> `invitations/components/organisms/invitations-table.tsx` (`Intl.DateTimeFormat` over `i18n.language`).
- Drawer -> the existing role-dependent `peopleSection(isAdmin)`.
- Tests -> `get-users-use-case.test.ts`, `user-repository.test.ts`, `trainer-assignments-view.test.tsx`,
  `navigation-drawer.test.tsx`, `e2e/users.spec.ts`.

### Conflicts and stop cases

None. The endpoint exists in the API, and no business rule enters the front. The order comes from the API and the
front does not re-sort it. `403` is already mapped to `FORBIDDEN`, so no new error code is needed. The API keeps leads
as a non-domain record with an admin-only read. On the front they are still an alias over the generated contract,
like every other type.

## Capabilities

### New Capabilities

- `customer-leads`: `core` lists customer leads through the API and returns every failure as a `DomainError` code.
- `backoffice-customer-leads`: the backoffice leads screen, with its read-only table and its states.

### Modified Capabilities

- `backoffice-navigation`: a "Marketing" section with "Leads" is added for admins only, linking to `/customer-leads`
  and shown as selected there.

## Impact

- `core/openapi/v1.json`, `core/src/contract/generated` (regenerated).
- `core/src/domain/customer-leads`, `core/src/domain/index.ts`, `core/src/application/{ports,use-cases/customer-leads,dependencies.ts}`,
  `core/src/infrastructure/repositories/customer-lead-repository.ts`, barrels, `core/test`.
- `backoffice/src/infrastructure/container.ts`, `app/src/infrastructure/container.ts`, both
  `test/support/render-with-providers.tsx`, both container tests.
- `backoffice/src/app/customer-leads`, `backoffice/src/presentation/customer-leads`, `navigation-drawer.tsx`,
  `backoffice/src/i18n/{es,en}` (new `customerLeads` namespace, new `menu.marketing.*` keys), `backoffice/test`,
  `backoffice/e2e/customer-leads.spec.ts`.
- No new dependency, no API change, no error mapper change.
