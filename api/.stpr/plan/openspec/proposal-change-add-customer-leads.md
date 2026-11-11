# Change Proposal: Register Customer Leads

## Change

Add a public, rate-limited `POST /customer-leads` endpoint that stores a lead's email and the UTC time it was registered.

## Objective

Collect contact emails from people interested in the product before they have an account. A lead is not a domain concept today, so the change is a plain write to the database. It reuses the `Email` value object so that lead emails follow the same rules as user emails.

## Capabilities

- An anonymous caller can register a lead by email.
- The registration time is set by the server in UTC. The client never sends it.
- The endpoint is rate limited per client so it can't be used to flood the table.

## Module Classification

- Customer leads -> transactional record, but **not** a domain aggregate (no domain entity, mapper or invariants) -> skeleton-backed, `src/WeightTracker.Application/UseCases/` + `src/WeightTracker.Infra/Repositories/`
- Email -> shared value object, reused unchanged -> `.stpr/template/src/WeightTracker.Domain/Common/Email.cs`
- Public access + rate limiting -> auth/authorization support -> `src/WeightTracker.Api/Endpoints/Auth/AuthController.cs` (the `[AllowAnonymous]` pattern)

Leads are written only. They aren't catalog-like and aren't publicly readable.

## Canonical Template Mapping

- Use case -> `src/WeightTracker.Application/UseCases/Auth/SignupUseCase.cs` -> nearest anonymous write flow: `Email.From` on input, `IClock.UtcNow` for the timestamp, `IUnitOfWork` commit.
- Repository contract and implementation -> `src/WeightTracker.Application/Interfaces/` + `src/WeightTracker.Infra/Repositories/` -> same placement and `CancellationToken` shape as the existing repositories.
- EF entity + configuration -> `src/WeightTracker.Infra/Entities/ExerciseFavoriteEntity.cs`, `WeightTrackerDbContext` -> a small flat table with `CreatedAtUtc`-style timestamp.
- Migration -> `src/WeightTracker.Migrations/Migrations/202611031642_CreateFavoriteExercisesTable.cs` -> PascalCase table/columns, `timestamptz`, reversible `Down()`.
- Endpoint -> `src/WeightTracker.Api/Endpoints/Auth/AuthController.cs` -> `[AllowAnonymous]` controller, `ExceptionMapper.Map` in catch.
- Integration tests -> `.stpr/template/test/WeightTracker.Integration/Auth/AuthEndpointsTests.cs` -> public-endpoint test shape (no 401 matrix).
- Unit tests -> `test/WeightTracker.Unit/Application/` -> use-case branches with mocked repository, clock and unit of work.

## State And Mutability

- Each registration inserts at most one row into `CustomerLeads` (`Id`, `Email`, `RegisteredAtUtc`).
- Rows are never updated or deleted by this change.
- No read endpoint is exposed.

## Business Rules

- The email is validated and normalized through `Email.From` (trim, lowercase, max 254 characters). An invalid email throws `InvalidEmailException`, which already maps to 400.
- A missing or null `email` in the body is a structural 400 at the API.
- `RegisteredAtUtc` comes from `IClock.UtcNow`.
- **Duplicates:** the email is unique in `CustomerLeads`. Registering an email that already exists succeeds without inserting a second row, and the original `RegisteredAtUtc` is kept. The response is the same either way, so the public endpoint can't be used to check whether an email is on the list. A unique-constraint violation from a concurrent insert is treated the same way. *(Alternative: 409. Rejected because it reveals which emails are registered.)*
- Success returns `204 No Content`.

## Roles And Visibility

- Public (`[AllowAnonymous]`). No authentication, role or ownership checks.
- No caller can read leads through the API in this change.

## Business Identifiers And Mutability

- `CustomerLead.Id` is the immutable technical primary key, generated with `Id.New()`.
- `CustomerLead.Email` is unique and immutable after creation.

## Out Of Scope

- Any domain model for leads (aggregate, domain events, lifecycle).
- Listing, exporting or deleting leads (admin or otherwise).
- Extra fields (name, source, consent flags, locale).
- Confirmation emails, double opt-in, CAPTCHA.
- Turning a lead into a user, or linking leads to users.
- Forwarded-headers / reverse-proxy client IP resolution.
- App or backoffice changes.

## Notes

- **Layers:** Application and Infra hold the logic. The **API layer still has to change**, though: a new `CustomerLeadsController`, a request DTO, rate-limiter setup and use-case registration in `Program.cs`. The Domain project is not modified. Its `Email` and `Id` are only consumed.
- **Approved deviation (repository without aggregate):** the invariants say only aggregate roots get repositories and repositories take domain types. Here `ICustomerLeadRepository.Add(Id id, Email email, DateTime registeredAtUtc, CancellationToken)` (plus an existence check, or an insert-if-absent) takes value objects and primitives instead of an aggregate, and there is no mapper. The EF entity is built inside the repository. Repositories still never call `SaveChanges`. The use case commits through `IUnitOfWork`. Document this deviation in `STPR_INVARIANTS.md` as the rule for non-domain records.
- **Persistence:** FluentMigrator migration `CreateCustomerLeadsTable` with `Id` (uuid PK `PK_CustomerLeads`), `Email` (`Email.MaxLength`, unique index), `RegisteredAtUtc` (`timestamptz`). `Down()` drops the table. The EF configuration mirrors it, with `Id` marked `.ValueGeneratedNever()`. No EF Core migration.
- **Rate limiting:** use ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting`. Add a named fixed-window policy (e.g. `customer-leads`) partitioned by `RemoteIpAddress` and apply it with `[EnableRateLimiting("customer-leads")]` on this action only. Rejected requests get `429 Too Many Requests`. Limits come from configuration (`RateLimiting:CustomerLeads:PermitLimit`, `RateLimiting:CustomerLeads:WindowSeconds`) in `appsettings.json`, not hardcoded.
- **Tests:**
  - Unit: valid email registers with the clock's time and commits. Invalid email throws before any repository call. A duplicate email doesn't insert and still succeeds.
  - Integration (`POST /customer-leads`): 204 and the row exists with the normalized email and a UTC timestamp. 400 for a missing email. 400 for a malformed email. Duplicate (including different casing) returns 204 and leaves one row with the original timestamp. 429 after the limit is exceeded.
  - Rate-limiter state lives in memory for the shared `WebApplicationFactory`, so it isn't reset by truncate-and-reseed. The test config must set a limit high enough for the other tests, and the 429 test needs either its own factory with a low limit or a configurable test override. It must not depend on other tests' requests.
  - `SeedData` truncation must include `CustomerLeads`.
- **Deviations:** only the repository-without-aggregate deviation above.
