# Tasks

## 1. API contract snapshot

- [x] 1.1 Run the API from the current `api/` working tree (change `add-list-customer-leads`) on `http://localhost:5199`. Then run `pnpm --filter @weight-tracker/core openapi:refresh` followed by `openapi:generate`.
  - Verify `core/openapi/v1.json` lists `GET /customer-leads` with `200`, `401` and `403`.
  - Verify the regenerated types include `CustomerLeadResponse` with `email` and `registeredAtUtc`.
  - Review the diff and commit the snapshot and generated types alone.
  - Never hand-edit `core/src/contract/generated`.

## 2. Core: customer leads list

- [x] 2.1 Create `core/src/domain/customer-leads/customer-lead.ts` with the `CustomerLead` alias over the generated schema, and re-export it from `core/src/domain/index.ts`. Verify with `pnpm --filter @weight-tracker/core typecheck`.
- [x] 2.2 Create `core/src/application/ports/customer-lead-repository-interface.ts` (`getAll()`), exported from the ports barrel. Then implement `CustomerLeadRepository` in `core/src/infrastructure/repositories/customer-lead-repository.ts` (`GET /customer-leads`, no body, failures through `mapHttpError`), exported from the repositories barrel. Add `core/test/infrastructure/repositories/customer-lead-repository.test.ts` (template: the `getAll` block of `user-repository.test.ts`), covering:
  - `200` with two leads, returned unchanged and in order;
  - `200` with an empty list;
  - `401` → `UNAUTHORIZED`, `403` → `FORBIDDEN`, `500` → `UNEXPECTED`;
  - network → `NETWORK`.

  Use AAA with one Act each. Verify with `pnpm --filter @weight-tracker/core test`.

- [x] 2.3 Create `GetCustomerLeadsUseCase` in `core/src/application/use-cases/customer-leads/get-customer-leads-use-case.ts` (template `get-users-use-case.ts`), with a `customer-leads/index.ts` barrel re-exported from `use-cases/index.ts`. Add `get-customer-leads-use-case.test.ts` (success returned, repository failure returned unchanged). Verify with the core tests.
- [x] 2.4 Add `customerLeadRepository` and `getCustomerLeadsUseCase` to `Dependencies`. Verify that the typecheck now fails only in the two containers, which group 3 fixes.
- [x] 2.5 Review core against its references (`user.ts`, `user-repository-interface.ts`, `user-repository.ts`, `get-users-use-case.ts`) and the invariants: aliases only, no throw, `Result` everywhere. Verify the drift loop from `AGENTS.md` prints nothing.

## 3. Wiring in both fronts

- [x] 3.1 Register `customerLeadRepository` and `getCustomerLeadsUseCase` in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`. Add the `getCustomerLeadsUseCase` placeholder to both `test/support/render-with-providers.tsx`, and extend each container test with the new keys. Verify with `pnpm typecheck` and the container tests of both fronts.

## 4. Backoffice: copy

- [x] 4.1 Create `backoffice/src/i18n/{es,en}/customer-leads.json` with `list.title`, `list.columns.email`, `list.columns.registeredAt` and `list.empty` (design → Translations), and register the `customerLeads` namespace in `resources.ts`. Add `menu.marketing.title` and `menu.marketing.leads` to both `common.json`. Verify the translation parity test passes.

## 5. Backoffice: state hook

- [x] 5.1 Create `src/presentation/customer-leads/hooks/use-customer-leads.ts` as a copy of `users/hooks/use-users.ts` over `getCustomerLeadsUseCase`, returning `{ status, leads, errorCode, reload }`. Verify with the backoffice typecheck.

## 6. Backoffice: components

- [x] 6.1 Create the `CustomerLeadsTable` organism (`components/organisms/customer-leads-table.tsx`), shaped like `invitations-table.tsx`:
  - columns "Email" and "Fecha de registro";
  - date and hours:minutes through `Intl.DateTimeFormat(i18n.language, …)`;
  - rows in the order received, keyed by email;
  - "Todavía no hay leads." when the list is empty.

  Verify with the backoffice typecheck and lint (no MUI widget outside atoms).

## 7. Backoffice: view and route

- [x] 7.1 Create `views/customer-leads-view.tsx` (template `trainer-assignments-view.tsx`): `AppShell`, `h1` "Leads", `LoadError` with retry, `HomeSkeleton` while loading, then `CustomerLeadsTable`. Create `backoffice/src/app/customer-leads/page.tsx` that only default-exports the view. Verify with the backoffice typecheck.
- [x] 7.2 Add `test/presentation/customer-leads/views/customer-leads-view.test.tsx` (template `trainer-assignments-view.test.tsx`). Cover every `backoffice-customer-leads` scenario:
  - rows in API order, with email and formatted date (expected text built with the same `Intl.DateTimeFormat` options);
  - loading placeholder with no table;
  - empty state;
  - failure with retry requesting the leads again;
  - `FORBIDDEN` shows `LoadError` and no table or row;
  - English heading and columns.

  Fake `getCustomerLeadsUseCase` through the container, use AAA, and render in `es` unless the test is about language. Verify with `pnpm --filter backoffice test`.

## 8. Backoffice: drawer

- [x] 8.1 Add the `marketingSection` (`menu.marketing.title`, one item `menu.marketing.leads` with `CampaignOutlined` and `href: "/customer-leads"`) to `navigation-drawer.tsx`, appended after the existing sections only when `profile?.role === "ADMIN"`. Extend `navigation-drawer.test.tsx`:
  - an admin sees "Marketing" with "Leads" after "Seguimiento";
  - "Leads" links to `/customer-leads` and is selected there, with Inicio not selected;
  - a trainer sees neither "Marketing" nor "Leads";
  - neither shows while the profile loads;
  - the English copy.

  Verify with the backoffice tests.

- [x] 8.2 Review the backoffice against its references (`users/page.tsx`, `trainer-assignments-view.tsx`, `use-users.ts`, `invitations-table.tsx`, `peopleSection`) and the quality checklist in `AGENTS.md`:
  - atomic levels and downward imports;
  - routes only in `src/app`;
  - no `core/infrastructure` import in presentation;
  - every string in `es` and `en`.

  Verify by reading the diff against each reference.

## 9. Backoffice E2E

- [x] 9.1 Add `backoffice/e2e/customer-leads.spec.ts` (template `users.spec.ts`). Flow: an admin session opens "Leads" from the drawer, ends on `/customer-leads`, and sees the intercepted leads in order. Intercept `GET /users/me` and `GET /customer-leads` with `page.route`. Verify with `pnpm test:e2e`.

## 10. Final verification

- [x] 10.1 From `front/`, run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` and verify that all pass. Then run the template drift loop from `AGENTS.md` and verify it prints nothing.
