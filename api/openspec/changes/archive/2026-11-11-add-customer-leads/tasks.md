# Tasks

## 1. Persistence

- [x] 1.1 Add FluentMigrator migration `CreateCustomerLeadsTable` in `src/WeightTracker.Migrations/Migrations/` mirroring `CreateFavoriteExercisesTable`: `Id` uuid PK `PK_CustomerLeads`, `Email` `AsString(254)` not null, `RegisteredAtUtc` `timestamptz` not null, unique constraint `UQ_CustomerLeads_Email`. `Down()` drops the table. Verify the migrator runs up and `--down` against a scratch database without errors.
- [x] 1.2 Add `CustomerLeadEntity` and its `DbSet`/configuration in `WeightTrackerDbContext` (`ToTable("CustomerLeads")`, `Id` `.ValueGeneratedNever()`, `Email` `HasMaxLength(Email.MaxLength)` required with unique constraint `UQ_CustomerLeads_Email`, `RegisteredAtUtc` `timestamptz`). Add `"CustomerLeads"` to the `TRUNCATE` list in `ApiFactory.ResetDatabase`. Verify `dotnet build WeightTracker.slnx` passes.
- [x] 1.3 Add `ICustomerLeadRepository` (`ExistsByEmail`, `Add(Id, Email, DateTime, ct)`, which stages without committing) in `Application/Interfaces` and implement `CustomerLeadRepository` in `Infra/Repositories`, keeping the EF entity inside. Register it in `Program.cs`. Verify the build passes.
- [x] 1.4 Add `CustomerLeadAlreadyExistsException` in `Application/Exceptions`, documenting that it never leaves the use case and has no `ExceptionMapper` entry. In `UnitOfWork.SaveChangesAsync`, translate a `DbUpdateException` with a Postgres unique violation on `UQ_CustomerLeads_Email` into it and leave other violations untouched. Verify the build passes.
- [x] 1.5 Add repository integration tests under `test/WeightTracker.Integration/Repositories/`, following the existing repository tests. Cover: `Add` + commit stores one row with the given values; `ExistsByEmail` true/false; a second `Add` of the same email committed in another scope raises `CustomerLeadAlreadyExistsException`. Verify the tests pass with Docker running.

## 2. Use case

- [x] 2.1 Add `RegisterCustomerLeadUseCase` in `Application/UseCases/CustomerLeads/` mirroring `SignupUseCase`'s shape (`Email.From` → `ExistsByEmail` returns early → `Add(Id.New(), email, _clock.UtcNow)` → commit, swallowing `CustomerLeadAlreadyExistsException`) and register it in `Program.cs`. Verify the build passes.
- [x] 2.2 Add `test/WeightTracker.Unit/Application/CustomerLeads/RegisterCustomerLeadUseCaseTests.cs` mirroring the signup use-case tests, using Arrange/Act/Assert with one Act each. Cover: valid email adds the normalized email with the clock's time and commits; invalid email throws `InvalidEmailException` with no repository or commit call; existing email neither adds nor commits; `CustomerLeadAlreadyExistsException` from commit is swallowed. Verify the unit tests pass.

## 3. Endpoint and rate limiting

- [x] 3.1 Add `CustomerLeadsRateLimitOptions` (`PermitLimit`, `WindowSeconds`, both validated > 0 on start) bound from `RateLimiting:CustomerLeads`, with `PermitLimit: 5` and `WindowSeconds: 60` in `appsettings.json`. In `Program.cs`, register `AddRateLimiter` with `RejectionStatusCode = 429` and the `customer-leads` fixed-window policy partitioned by remote IP (`"unknown"` fallback, `QueueLimit = 0`), and call `app.UseRateLimiter()` after `UseCors` and before `UseAuthentication`. Add `RateLimiting:CustomerLeads:PermitLimit = 10000` to `ApiFactory`'s in-memory configuration. Verify the build passes and the existing integration suite still passes.
- [x] 3.2 Add `CustomerLeadsController` (`[ApiController]`, `[Route("customer-leads")]`, `[AllowAnonymous]`, `[HttpPost]` with `[EnableRateLimiting("customer-leads")]`, `204`/`400`/`429` response types, `ExceptionMapper.Map` in the catch) and `Dtos/RegisterCustomerLeadRequest` with `[Required] Email`, mirroring `AuthController`. Verify `POST /customer-leads` appears in `/openapi/v1.json` in Development.
- [x] 3.3 Add `test/WeightTracker.Integration/CustomerLeads/CustomerLeadEndpointTests.cs` following `AuthEndpointsTests`, with no missing-token cases since the route is public. Cover:
  - `204` stores one row with a UTC `RegisteredAtUtc` inside the request window
  - the email is stored normalized
  - `400` with zero rows for a missing, a malformed and a too-long email
  - a duplicate in different casing returns `204` and leaves one row with the original time
  - a client-supplied `registeredAtUtc` is ignored
  - an invalid bearer token still returns `204`

  Verify the tests pass with Docker running.
- [x] 3.4 Add the rate-limit test in the same class: create a derived factory with `WithWebHostBuilder` overriding `PermitLimit = 2` and `WindowSeconds = 3600`, send three posts and assert `204, 204, 429` with two rows, then assert `POST /auth/login` on the same client is not `429`. Dispose the derived factory. Verify the test passes and the rest of the suite is unaffected when run in the same run.

## 4. Documentation

- [x] 4.1 Add a "Non-domain records" rule to `.stpr/template/STPR_INVARIANTS.md`. It applies only to write-only records with no lifecycle; the repository contract takes value objects instead of an aggregate and never exposes EF entities; there is no mapper; repositories still never commit. Name `CustomerLeads` as the example. Verify the rule sits next to the repository contract rules.

## 5. Review and verification

- [x] 5.1 Review `customer-leads` against its mapped references (`SignupUseCase`, `ExerciseFavoriteRepository`, `CreateFavoriteExercisesTable`, `AuthController`, `AuthEndpointsTests`) and the AGENTS.md quality checklist. Record any deviation beyond the repository-without-aggregate rule and the derived test factory in `design.md`.
  - Deviations recorded in design.md: repository without aggregate (`Add` instead of `Save`); `CustomerLeadAlreadyExistsException` has no `ExceptionMapper` entry because it never leaves the use case; derived test factory for the limit tests; limiter options bound through `IOptions` + `ValidateOnStart` instead of the eager read used for `TrainerInvitationOptions`.
  - Naming aligned to the local precedent: unique constraint `UQ_CustomerLeads_Email`, mirroring `UQ_Users_Email`, instead of the planned `UX_` index.
- [x] 5.2 Run the template parity check from AGENTS.md and confirm there is no `DRIFT:` output.
- [x] 5.3 Run `dotnet build WeightTracker.slnx` and the full `dotnet test WeightTracker.slnx` with Docker running. Verify both succeed with no warnings.
