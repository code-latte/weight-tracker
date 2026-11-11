# Change Proposal: List Customer Leads

## Change

Add `GET /customer-leads` so an administrator can list every registered lead (email and registration time).

## Objective

Leads are collected through the public `POST /customer-leads`, but nobody can see them without querying the
database. Administrators need to read the list so they can contact interested people. A lead is still not a
domain concept, so the read goes straight from the Application layer to the repository, with no aggregate,
no domain type and no mapper.

## Capabilities

- An `ADMIN` gets every stored lead with its `email` and `registeredAtUtc`.
- A `TRAINER` or `USER` is forbidden.
- `POST /customer-leads` stays public and rate limited, unchanged.

## Module Classification

- Customer leads -> non-domain record, now with an admin-only read (no aggregate, no domain type, no mapper) ->
  `src/WeightTracker.Application/UseCases/CustomerLeads/` + `src/WeightTracker.Infra/Repositories/CustomerLeadRepository.cs`
- Admin-only access -> auth/authorization support -> `src/WeightTracker.Api/Endpoints/RequireRoleAttribute.cs`
  on an `AuthorizedController` action

Leads are not catalog-like. They are written by the public endpoint and now read by administrators only.

## Canonical Template Mapping

- `GetAllCustomerLeadsUseCase` -> `src/WeightTracker.Application/UseCases/Users/GetAllUsersUseCase.cs` -> nearest
  admin-only list: receives the caller's `Role` and throws `UnauthorizedOperationException` for anyone but `ADMIN`
  before touching the repository.
- Read model -> `src/WeightTracker.Application/UseCases/Users/UserWithTrainerView.cs` -> an Application-level
  view record placed next to its use case, used here instead of a domain type.
- `ICustomerLeadRepository.GetAll` / `CustomerLeadRepository.GetAll` -> existing `CustomerLeadRepository` ->
  same contract and placement; projects EF entities to the view inside the repository.
- `CustomerLeadsController.GetAll` -> `src/WeightTracker.Api/Endpoints/Users/UsersController.cs` (`GetAll`) ->
  `[RequireRole(Role.ADMIN)]` on an `AuthorizedController` action, catch through `ExceptionMapper.Map`.
- Response DTO -> `src/WeightTracker.Api/Endpoints/Users/Dtos/UserListItemResponse.cs` -> list item built with
  a static `From(view)` factory.
- Tests -> `test/WeightTracker.Unit/Application/Users/` (GetAllUsers tests) and
  `test/WeightTracker.Integration/CustomerLeads/CustomerLeadEndpointTests.cs` plus the `GET /users` integration
  tests -> role-branch unit tests with a mocked repository, and the protected-route matrix.

## State And Mutability

- Nothing changes state. `GET /customer-leads` only reads `CustomerLeads`.
- No new tables or columns, so no migration.

## Business Rules

- `ADMIN`: receives every stored lead.
- Any other role: `403`, from `[RequireRole]` and again from the use case (`UnauthorizedOperationException`), so
  the rule does not depend only on the HTTP attribute.
- Order: `RegisteredAtUtc` descending (newest first), then `Email` ascending as tie-break.
- An empty table returns `200` with `[]`.
- No filtering, search or pagination.

## Roles And Visibility

- `ADMIN`: `200` with all leads.
- `TRAINER`, `USER`: `403`.
- No token or invalid token: `401`.
- Each item exposes exactly `email` and `registeredAtUtc` (UTC). The technical `Id` is not exposed.

## Business Identifiers And Mutability

- `CustomerLead.Id` is the immutable technical primary key generated with `Id.New()`. Not affected, not exposed.
- `CustomerLead.Email` is unique and immutable after creation. Not affected.

If a future change wants any listed identifier to become mutable, exact persistence semantics must be defined before coding starts.

## Out Of Scope

- Any domain model for leads (aggregate, domain type, mapper, lifecycle).
- Deleting, exporting or editing leads; `GET /customer-leads/{id}`.
- Pagination, filtering and search.
- Any change to `POST /customer-leads` behaviour, its rate limiting or its response.
- Backoffice or app screens (separate front plan after the OpenAPI snapshot refresh).

## Notes

- **Approved deviation (read of a non-domain record):** `STPR_INVARIANTS.md` → *Non-domain records* says the API
  never reads such a record back and that gaining a read turns it into an aggregate. This change keeps leads
  non-domain on purpose. Update that section so a non-domain record may also have an **admin-only, read-only list**
  that returns an Application-level view record (never an EF entity, never a domain type). Any rule, state change
  or non-admin read still forces it to become an aggregate. Update the `ICustomerLeadRepository` summary
  ("never read back by the API") accordingly.
- Read model: `CustomerLeadView(Email Email, DateTime RegisteredAtUtc)` in
  `Application/UseCases/CustomerLeads/` (or `string Email` if the repository should not rebuild value objects;
  confirm at proposal time, `Email` is preferred for consistency with `Add`).
- Repository: add `GetAll(CancellationToken)` returning `IReadOnlyList<CustomerLeadView>`, ordered as above,
  read with `AsNoTracking`.
- Use case: `GetAllCustomerLeadsUseCase.Execute(Role callerRole, CancellationToken)`; register it in `Program.cs`.
- Controller: `CustomerLeadsController` currently is `ControllerBase` with class-level `[AllowAnonymous]`. Make it
  inherit `AuthorizedController`, move `[AllowAnonymous]` to the `Register` action only, and add `GetAll` with
  `[HttpGet]`, `[RequireRole(Role.ADMIN)]`. Pass `CurrentUser!.Role` to the use case.
- Response DTO: `CustomerLeadResponse` (`Email`, `RegisteredAtUtc`) in `Endpoints/CustomerLeads/Dtos` with
  `From(CustomerLeadView)`.
- `[ProducesResponseType]`: `200` (list of `CustomerLeadResponse`), `401`, `403` (`ProblemDetails`).
- Tests:
  - Unit `GetAllCustomerLeadsUseCase`: `ADMIN` returns what the repository returns; `TRAINER` and `USER` throw
    `UnauthorizedOperationException` and never call the repository.
  - Integration `GET /customer-leads`: `401` without token; `401` with invalid token; `403` for seeded `USER`;
    `403` for seeded `TRAINER`; `200` with `[]` for `ADMIN` when no leads exist; `200` for `ADMIN` after
    registering several leads through `POST /customer-leads` in Arrange, asserting order, normalized emails,
    UTC timestamps and that each item has exactly `email` and `registeredAtUtc` (no `id`).
  - Existing `POST /customer-leads` tests must still pass anonymously, proving the moved `[AllowAnonymous]`.
  - Arrange registrations go through the rate-limited endpoint; keep them under the test limit and do not depend
    on other tests' requests.
  - OpenAPI document test (if one covers secured operations): `GET /customer-leads` is secured and declares
    `200`, `401`, `403`; `POST /customer-leads` stays unsecured.
- OpenAPI snapshot changes (new operation on `/customer-leads`); the front refreshes it in its own plan.
- No other deviations from the mapped template references are allowed.
