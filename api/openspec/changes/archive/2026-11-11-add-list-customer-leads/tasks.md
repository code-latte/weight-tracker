# Tasks

## 1. Invariant amendment

- [x] 1.1 Amend `.stpr/template/STPR_INVARIANTS.md` → *Non-domain records* to allow an admin-only, read-only list returning an Application view record (never an EF entity or domain type), keeping "a rule, a state change or a non-admin read" as the trigger to become an aggregate; verify the section reads consistently with the *Repositories* rules above it.

## 2. Application and Infrastructure read

- [x] 2.1 Add `CustomerLeadView(Email Email, DateTime RegisteredAtUtc)` in `src/WeightTracker.Application/UseCases/CustomerLeads/`, mirroring `UserWithTrainerView`; verify `dotnet build WeightTracker.slnx` passes.
- [x] 2.2 Add `GetAll(CancellationToken)` returning `IReadOnlyList<CustomerLeadView>` to `ICustomerLeadRepository` and update its summary so it no longer says leads are never read back; verify build.
- [x] 2.3 Implement `CustomerLeadRepository.GetAll` (`AsNoTracking`, `RegisteredAtUtc` desc then `Email` asc, projected to `CustomerLeadView`); add cases to `test/WeightTracker.Integration/Repositories/CustomerLeadRepositoryTests.cs` for empty result, newest-first order and the equal-timestamp email tie-break, following that file's existing Arrange/Act/Assert pattern; verify they pass.
- [x] 2.4 Add `GetAllCustomerLeadsUseCase.Execute(Role callerRole, CancellationToken)` that throws `UnauthorizedOperationException` for non-admins before any repository call, modelled on `GetAllUsersUseCase`; add `test/WeightTracker.Unit/Application/CustomerLeads/GetAllCustomerLeadsUseCaseTests.cs` (ADMIN returns repository result; TRAINER and USER throw and never call the repository) mirroring `GetAllUsersUseCaseTests`; verify unit tests pass.
- [x] 2.5 Review: compare the use case, view and repository method against `GetAllUsersUseCase`, `UserWithTrainerView` and the existing `CustomerLeadRepository` for naming, placement, `CancellationToken` shape and doc comments; fix any divergence.

## 3. API endpoint

- [x] 3.1 Add `CustomerLeadResponse` (`Email`, `RegisteredAtUtc`, static `From(CustomerLeadView)`) in `src/WeightTracker.Api/Endpoints/CustomerLeads/Dtos/`, mirroring `UserListItemResponse`; verify build.
- [x] 3.2 Make `CustomerLeadsController` inherit `AuthorizedController`, move `[AllowAnonymous]` from the class to `Register` (keep `[EnableRateLimiting]` on `Register` only), and add `GetAll` with `[HttpGet]`, `[RequireRole(Role.ADMIN)]`, `[ProducesResponseType]` for 200/401/403 and catch through `ExceptionMapper.Map`; update the controller summary; verify the existing `POST /customer-leads` integration tests (including invalid-token and 429) still pass unchanged.
- [x] 3.3 Register `GetAllCustomerLeadsUseCase` in `Program.cs`; verify the API starts and `GET /customer-leads` resolves.
- [x] 3.4 Add `GET /customer-leads` integration tests in `test/WeightTracker.Integration/CustomerLeads/` following `test/WeightTracker.Integration/Users/ListUsersEndpointTests.cs`: 401 without token, 401 invalid token, 403 for seeded USER and TRAINER with no lead email in the body, 200 `[]` for ADMIN with no leads, 200 for ADMIN after registering leads through `POST /customer-leads` in Arrange asserting newest-first order, normalized emails, UTC timestamps and exactly `email` + `registeredAtUtc` per item (no `id`); authenticate only with `AuthenticatedClientFor`; verify they pass.
- [x] 3.5 Add OpenAPI document assertions in `test/WeightTracker.Integration/OpenApi/OpenApiDocumentTests.cs` following the `/users` `get` cases: `GET /customer-leads` is secured and declares 200/401/403 with the item schema, and `POST /customer-leads` remains unsecured; verify they pass.
- [x] 3.6 Review: compare `CustomerLeadsController.GetAll`, the DTO and the new tests against `UsersController.GetAll`, `UserListItemResponse` and `ListUsersEndpointTests`; fix any divergence and confirm every new test has labelled Arrange/Act/Assert with exactly one Act.

## 4. Verification

- [x] 4.1 Run the `.stpr/template` parity check from `AGENTS.md` and confirm no `DRIFT` lines.
- [x] 4.2 Run `dotnet build WeightTracker.slnx` (warnings as errors) and the full `dotnet test WeightTracker.slnx` with Docker running; confirm everything passes.
- [x] 4.3 Walk the `AGENTS.md` quality checklist for this change and confirm each applicable item.
