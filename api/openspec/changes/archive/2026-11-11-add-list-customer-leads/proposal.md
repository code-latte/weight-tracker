# Proposal

## Why

Leads are collected through the public `POST /customer-leads`, but the only way to see them is to query the
database. Administrators need to read the list from the API so they can contact interested people. A lead is
still not a domain concept, so the read must not introduce a domain type.

## What Changes

- Add `GET /customer-leads`, restricted to `ADMIN`, returning every stored lead with its `email` and
  `registeredAtUtc`, newest first.
- `TRAINER` and `USER` callers get `403`. Missing or invalid tokens get `401`.
- `POST /customer-leads` keeps its current behaviour: public, rate limited, a bearer token (valid or not) has no
  effect.
- The read goes from an Application use case straight to the existing non-domain `CustomerLeads` repository and
  returns an Application-level view record. No domain type, no mapper, no migration.
- **Conflict with the invariants (approved deviation):** `.stpr/template/STPR_INVARIANTS.md` → *Non-domain
  records* says the API never reads such a record back and that gaining a read turns it into an aggregate. This
  change amends that section: a non-domain record MAY also have an admin-only, read-only list returning an
  Application view record (never an EF entity, never a domain type). A business rule, a state change or a
  non-admin read still forces it to become an aggregate.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `customer-leads`: adds the admin-only listing of stored leads. The existing registration requirements are
  unchanged.

## Canonical Template Mapping

- Admin lead listing (use case) -> `src/WeightTracker.Application/UseCases/Users/GetAllUsersUseCase.cs` -> nearest
  admin-only list: takes the caller's `Role`, rejects non-admins before reading.
- Lead view record -> `src/WeightTracker.Application/UseCases/Users/UserWithTrainerView.cs` -> Application view
  next to its use case, standing in for a domain type.
- Repository read -> `src/WeightTracker.Infra/Repositories/CustomerLeadRepository.cs` -> existing non-domain
  repository; the read lives with the write.
- Endpoint -> `src/WeightTracker.Api/Endpoints/Users/UsersController.cs` (`GetAll`) -> `AuthorizedController`
  action with `[RequireRole(Role.ADMIN)]`, catch through `ExceptionMapper.Map`.
- Tests -> `test/WeightTracker.Unit/Application/Users/GetAllUsersUseCaseTests.cs`,
  `test/WeightTracker.Integration/Users/ListUsersEndpointTests.cs`,
  `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs` -> role-branch unit tests, the protected-route
  auth matrix, and the secured-operation document checks.

## Module Classification

- Customer leads -> non-domain record (not catalog-like: written publicly, read by admins only) with an
  admin-only read.
- Admin access -> auth/authorization support through the existing `RequireRole` filter.

## Business Identifiers And Mutability

- `CustomerLead.Id`: immutable technical primary key from `Id.New()`. Unaffected and not exposed.
- `CustomerLead.Email`: unique and immutable after creation. Unaffected.

## Impact

- **API**: `CustomerLeadsController` becomes an `AuthorizedController`; `[AllowAnonymous]` moves from the class to
  the `Register` action; new `GetAll` action and `CustomerLeadResponse` DTO. New OpenAPI operation
  `GET /customer-leads` (secured, `200`/`401`/`403`); the front refreshes its snapshot in its own plan.
- **Application**: `GetAllCustomerLeadsUseCase`, `CustomerLeadView`, `ICustomerLeadRepository.GetAll`.
- **Infra**: `CustomerLeadRepository.GetAll`. No schema change.
- **Domain**: unchanged; `Email` and `Role` are only consumed.
- **Docs**: `STPR_INVARIANTS.md` (*Non-domain records*) and the `ICustomerLeadRepository` summary.
- **Out of scope**: domain model for leads, delete/export/edit, `GET /customer-leads/{id}`, pagination/filter/search,
  any change to `POST /customer-leads`, backoffice or app screens.
