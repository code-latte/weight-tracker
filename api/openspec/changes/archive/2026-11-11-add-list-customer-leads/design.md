# Design

## Context

`CustomerLeads` is the repository's only non-domain record (see `STPR_INVARIANTS.md` → *Non-domain records*): a
flat table (`Id`, `Email`, `RegisteredAtUtc`) written by `RegisterCustomerLeadUseCase` through
`ICustomerLeadRepository.Add`, with no domain type and no mapper. `CustomerLeadsController` derives from
`ControllerBase` and carries `[AllowAnonymous]` on the class. Admin-only reads elsewhere (`GET /users`) use an
`AuthorizedController` action with `[RequireRole(Role.ADMIN)]` plus a role check inside the use case.

Motivation and scope: see proposal.md. Required behaviour: see `specs/customer-leads/spec.md`.

## Goals / Non-Goals

**Goals:**
- Read leads from Application without adding anything to Domain.
- Reuse the existing admin authorization approach unchanged.
- Keep `POST /customer-leads` byte-for-byte equivalent in behaviour (anonymous, rate limited, token ignored).

**Non-Goals:**
- Turning leads into an aggregate.
- Any query capability beyond "all, ordered".

## Layer Mapping

- **Domain**: no change. `Email` and `Role` are consumed.
- **Application**:
  - `UseCases/CustomerLeads/CustomerLeadView.cs`: `record CustomerLeadView(Email Email, DateTime RegisteredAtUtc)`.
  - `UseCases/CustomerLeads/GetAllCustomerLeadsUseCase.cs`: `Execute(Role callerRole, CancellationToken)`; throws
    `UnauthorizedOperationException("GET /customer-leads")` when the role is not `ADMIN`, before any repository call;
    otherwise returns `_leads.GetAll(...)`. No unit of work (read only).
  - `Interfaces/ICustomerLeadRepository.cs`: add `Task<IReadOnlyList<CustomerLeadView>> GetAll(CancellationToken)`;
    rewrite the summary so it no longer says the record is never read back.
- **Infrastructure**: `CustomerLeadRepository.GetAll` reads `_context.CustomerLeads` with `AsNoTracking`, orders by
  `RegisteredAtUtc` descending then `Email` ascending, and projects to `CustomerLeadView` via `Email.From(entity.Email)`.
  No migration, no EF configuration change.
- **Api**:
  - `CustomerLeadsController : AuthorizedController`, constructor adds `GetAuthenticatedUserUseCase` for the base and
    `GetAllCustomerLeadsUseCase`.
  - `[AllowAnonymous]` moves from the class to `Register`; `[EnableRateLimiting]` stays on `Register` only.
  - New `GetAll` action: `[HttpGet]`, `[RequireRole(Role.ADMIN)]`, `[ProducesResponseType]` for `200`
    (`IReadOnlyList<CustomerLeadResponse>`), `401`, `403` (`ProblemDetails`); passes `CurrentUser!.Role`; catch
    through `ExceptionMapper.Map`.
  - `Dtos/CustomerLeadResponse.cs`: `Email` (string), `RegisteredAtUtc` (DateTime), static `From(CustomerLeadView)`.
  - `Program.cs`: register `GetAllCustomerLeadsUseCase`.
- **Docs**: amend `.stpr/template/STPR_INVARIANTS.md` → *Non-domain records* (see Decision 1). This file is not
  part of the `.stpr/template/src` parity copies, so no drift check is affected.

Cross-module dependencies: `CustomerLeadsController` now depends on `GetAuthenticatedUserUseCase` (Auth) through
the base class. Nothing else crosses areas.

## Decisions

1. **Keep leads non-domain and amend the invariant.** The user explicitly asked for no Domain change. The
   invariant currently says a read turns the record into an aggregate; we narrow that to "a rule, a state change or
   a non-admin read". *Alternative:* model a `CustomerLead` aggregate with a mapper. Rejected: it has no rules or
   lifecycle, so it would be an empty shell.
2. **Application view record instead of a domain type.** The repository returns `CustomerLeadView`, which lives in
   Application, so repositories still never leak EF entities and Domain stays untouched. Its `Email` is the value
   object for consistency with `Add(Id, Email, ...)`. *Alternative:* `string Email`. Rejected for consistency; the
   cost is one `Email.From` per row on data that was validated on write.
3. **Role checked twice (filter + use case).** Mirrors `GET /users`: `[RequireRole]` gives `401` before `403`, and
   the use case keeps the rule out of HTTP-only code.
4. **Controller becomes `AuthorizedController` with action-level `[AllowAnonymous]`.** `AuthorizedController`
   skips caller resolution for `[AllowAnonymous]` actions, so `Register` keeps ignoring any token (existing scenario
   "Invalid token is ignored"). *Alternative:* a second controller on the same route. Rejected: splits one resource
   across two classes for no gain.
5. **Ordering in the repository query**, newest first with email tie-break, so the result is deterministic.

## Risks / Trade-offs

- [Moving `[AllowAnonymous]` could accidentally protect `POST`] → the existing anonymous and invalid-token
  `POST /customer-leads` integration tests must stay green unchanged.
- [Ordering test depends on distinct registration times] → register leads sequentially in Arrange; Postgres
  `timestamptz` keeps microsecond precision, so sequential requests get distinct times. The equal-timestamp
  tie-break is covered in `test/WeightTracker.Integration/Repositories/CustomerLeadRepositoryTests.cs`, where the
  registration times are controlled, rather than through HTTP.
- [Arrange registrations hit the rate limiter] → the shared `ApiFactory` sets `RateLimiting:CustomerLeads:PermitLimit`
  to `10000`, so a handful of `POST`s is safe and no test depends on another's requests.
- [Unbounded list] → accepted: leads are a small pre-launch list; pagination is out of scope.

## Migration Plan

No schema change. Deploy normally; rollback is reverting the code. The front refreshes its OpenAPI snapshot in a
separate plan.
