# Design

## Context

See `proposal.md` for motivation and `specs/customer-leads/spec.md` for the behavior contract.

Observed today:

- `Email.From` (`src/WeightTracker.Domain/Common/Email.cs`) trims, lowercases and rejects malformed or >254-character addresses with `InvalidEmailException`, which `ExceptionMapper` already maps to `400`.
- `SignupUseCase` is the existing anonymous write: it parses `Email`, checks existence through the repository, uses `IClock.UtcNow` and commits with `IUnitOfWork`.
- `UnitOfWork.SaveChangesAsync` is the only place that calls `SaveChanges`. It translates `DbUpdateConcurrencyException` into `WorkoutConcurrencyConflictException`. Nothing in the codebase handles unique-constraint violations yet.
- `AuthController` is the `[AllowAnonymous]` controller pattern: `[ApiController]`, DTOs under `Dtos/`, `ExceptionMapper.Map` in every catch.
- `Program.cs` has no rate limiting. The pipeline runs `UseCors` (Development) → … → `UseAuthentication` → `UseAuthorization` → `MapControllers`.
- `ApiFactory` sets configuration through `ConfigureAppConfiguration` with an in-memory collection, and `ResetDatabase` truncates an explicit table list.

## Goals / Non-Goals

**Goals:**
- One small write path from controller to table that respects the repository + unit-of-work boundary without inventing a domain aggregate.
- Deduplication that holds under concurrent requests and still always answers `204`.
- A rate limit that is configurable per environment and testable without making the other integration tests flaky.

**Non-Goals:**
- A reusable rate-limiting framework for other endpoints. Only one named policy is added.
- Distributed rate limiting across instances. The limiter is in-memory per process.

## Decisions

### Layer mapping

- **Api**
  - `Endpoints/CustomerLeads/CustomerLeadsController.cs`: `[ApiController]`, `[Route("customer-leads")]`, `[AllowAnonymous]`. `[HttpPost]` with `[EnableRateLimiting(CustomerLeadsRateLimit.PolicyName)]`, response types `204` / `400` (`ValidationProblemDetails`) / `429`. It calls the use case with `request.Email` and returns `NoContent()`. The catch delegates to `ExceptionMapper.Map`.
  - `Endpoints/CustomerLeads/Dtos/RegisterCustomerLeadRequest.cs`: `[Required] string Email`. A missing or null email is a structural `400` produced by `[ApiController]` model validation. Unknown fields such as `registeredAtUtc` are ignored by the binder.
  - `Program.cs`: rate-limiter registration and middleware (below), `ICustomerLeadRepository` and `RegisterCustomerLeadUseCase` registrations.
- **Application**
  - `Interfaces/ICustomerLeadRepository.cs`:
    - `Task<bool> ExistsByEmail(Email email, CancellationToken)`
    - `Task Add(Id id, Email email, DateTime registeredAtUtc, CancellationToken)`, which stages the insert and does not commit.
  - `UseCases/CustomerLeads/RegisterCustomerLeadUseCase.Execute(string email, CancellationToken)`:
    1. `Email.From(email)` (throws `InvalidEmailException` → 400)
    2. if `ExistsByEmail` returns, without writing
    3. `Add(Id.New(), parsed, _clock.UtcNow, ct)`
    4. `_unitOfWork.SaveChangesAsync(ct)`. It catches `CustomerLeadAlreadyExistsException` and returns normally.
  - `Exceptions/CustomerLeadAlreadyExistsException.cs` (`ApplicationLayerException`). It signals that a concurrent request inserted the same email between the existence check and the commit.
- **Domain**: no changes. `Email` and `Id` are consumed as they are.
- **Infra**
  - `Entities/CustomerLeadEntity.cs` (`Id`, `Email`, `RegisteredAtUtc`).
  - `WeightTrackerDbContext`: `DbSet<CustomerLeadEntity> CustomerLeads`. Configuration: `ToTable("CustomerLeads")`, `Id` `.ValueGeneratedNever()`, `Email` `HasMaxLength(Email.MaxLength)` required with a unique constraint `UQ_CustomerLeads_Email` (mirroring `UQ_Users_Email`), `RegisteredAtUtc` `HasColumnType("timestamptz")`.
  - `Repositories/CustomerLeadRepository.cs`: `ExistsByEmail` uses `AnyAsync` on the normalized value. `Add` builds the entity from the value objects and calls `Add` on the DbSet.
  - `Persistence/UnitOfWork.cs`: also catches `DbUpdateException` whose inner `PostgresException` has `SqlState == PostgresErrorCodes.UniqueViolation` and `ConstraintName == "UQ_CustomerLeads_Email"`, and throws `CustomerLeadAlreadyExistsException`. Any other unique violation keeps propagating as today.
- **Migrations**: `CreateCustomerLeadsTable`: `Id` uuid PK `PK_CustomerLeads`, `Email` `AsString(254)` not null, `RegisteredAtUtc` `timestamptz` not null, unique constraint `UQ_CustomerLeads_Email`. `Down()` drops the table.

### Repository without an aggregate (documented deviation)

Leads have no invariants beyond the email format, which `Email` already owns, and they are never loaded back. A domain aggregate would add a type, a mapper and tests that protect nothing. The repository therefore takes value objects instead of an aggregate. It still lives in `Application/Interfaces` + `Infra/Repositories`, keeps EF entities inside, and never commits. This is added to `STPR_INVARIANTS.md` as a "Non-domain records" rule: allowed only for write-only records with no lifecycle, the contract takes value objects and never EF entities, and no mapper is needed. *Alternative considered:* writing through `DbContext` from the use case, rejected because the invariants forbid `DbContext` outside repositories.

### Deduplication: existence check plus constraint as the backstop

The existence check covers the common case cheaply. The unique index guarantees one row even under a race. Translating the violation in `UnitOfWork` keeps EF and Npgsql types out of Application, the same way the concurrency conflict is already handled. *Alternatives considered:*
- `INSERT … ON CONFLICT DO NOTHING` through raw SQL in the repository. Rejected because it runs immediately instead of at commit, which breaks the unit-of-work boundary.
- Answering `409` on duplicates. Rejected because the spec requires duplicates to be indistinguishable from new emails.

`CustomerLeadAlreadyExistsException` is always caught by the use case and never reaches a controller, so it gets **no** `ExceptionMapper` entry. If it ever escaped, the mapper's default `500` would surface the bug rather than hide it. The exception's XML doc records this.

### Rate limiting

ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting`, so no new package is needed.

- `AddRateLimiter` sets `RejectionStatusCode = 429` and adds a policy named `customer-leads`, a `RateLimitPartition.GetFixedWindowLimiter` keyed by `HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"`. `TestServer` has no remote IP, so every test request shares the `"unknown"` partition. Options: `PermitLimit` and `Window` from configuration, `QueueLimit = 0` so excess requests are rejected rather than delayed.
- Configuration is bound from `RateLimiting:CustomerLeads` (`PermitLimit`, `WindowSeconds`) into a small options class (`CustomerLeadsRateLimitOptions`, in the Api project next to the controller) through `AddOptions().Bind().Validate().ValidateOnStart()` (both > 0). The policy resolves `IOptions<CustomerLeadsRateLimitOptions>` per request instead of reading the section at the top of `Program`, because configuration read there only sees environment variables, not `WebApplicationFactory` overrides. This differs from the eager read-and-throw used for `TrainerInvitationOptions`, and it is what lets the test factories set their own limits. `appsettings.json` uses `PermitLimit: 5` and `WindowSeconds: 60`. Development uses the same values unless overridden.
- `app.UseRateLimiter()` goes after `UseCors` and before `UseAuthentication`, so rejected requests skip token validation and the Development CORS headers are still present on a `429`. The endpoint-specific policy is resolved from endpoint metadata, so only `POST /customer-leads` is limited.
- *Alternative considered:* a sliding window or token bucket. Fixed window is the simplest to configure and test, and it is enough for abuse protection on a write-only form.

### Tests

- **Unit** (`test/WeightTracker.Unit/Application/CustomerLeads/RegisterCustomerLeadUseCaseTests.cs`, mirroring the signup use-case tests):
  - a valid email adds with `Id`, normalized email and the clock's time, then commits
  - an invalid email throws `InvalidEmailException` with no repository or commit call
  - an existing email neither adds nor commits
  - `CustomerLeadAlreadyExistsException` on commit is swallowed
- **Integration** (`test/WeightTracker.Integration/CustomerLeads/CustomerLeadEndpointTests.cs`, mirroring `AuthEndpointsTests`):
  - `204` with the row asserted through `ApiFactory.CountRows<CustomerLeadEntity>` and a scoped read of `Email`/`RegisteredAtUtc` (`DateTimeKind.Utc`, within the request window)
  - normalization
  - `400` for missing, malformed and too-long emails, each with zero rows
  - duplicates in any casing, with one row and an unchanged time
  - client-supplied `registeredAtUtc` ignored
  - an invalid bearer token ignored
  - No missing-token cases, since the route is public.
- **Rate limit tests and determinism.** `ApiFactory` adds `RateLimiting:CustomerLeads:PermitLimit = 10000` to its in-memory configuration, so the limiter state, which survives `ResetDatabase`, can never reject the other tests' requests. The `429` test creates a derived factory with `Factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration(c => c.AddInMemoryCollection(PermitLimit = 2, WindowSeconds = 3600)))`. That gives it its own host and limiter on the same database. It sends three requests and asserts `204, 204, 429` and two rows. It then asserts `POST /auth/login` on the same derived client is not `429`. The derived factory is disposed in the test. This is the one documented exception to the shared-factory helper pattern.
- `ApiFactory.ResetDatabase` adds `"CustomerLeads"` to its `TRUNCATE` list.

## Risks / Trade-offs

- [Behind a reverse proxy every client shares the proxy's IP, so one partition throttles everybody] → Out of scope here. Forwarded-headers configuration must be in place before deploying behind a proxy. Tracked in the proposal's Out Of Scope.
- [The in-memory limiter is per instance, so N instances allow N× the limit] → Acceptable for abuse protection. A distributed limiter can replace the policy later without changing the endpoint.
- [Lead emails are personal data stored with no retention or deletion path] → No API exposes them. Retention and removal are a later change.
- [Unique-violation translation in `UnitOfWork` is matched by constraint name] → The constraint name is defined once in the migration and once in the EF configuration, and an integration test exercises the duplicate path.

## Migration Plan

- Deploy runs the FluentMigrator `CreateCustomerLeadsTable` through the existing migrator. It is additive, so there is no data migration.
- Rollback: `--down` past this migration drops `CustomerLeads` and its rows. The endpoint must be rolled back with it.
