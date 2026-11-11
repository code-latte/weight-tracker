# Proposal

## Why

The product needs a way to collect contact emails from people interested in it before they have an account. Today the only public writes are signup and login, and both create or authenticate users. A lead is not a domain concept, so the change is a plain write to the database. It reuses the `Email` value object so a lead's email follows the same rules as a user's.

Source brief: `.stpr/plan/openspec/proposal-change-add-customer-leads.md`.

## What Changes

- New public endpoint `POST /customer-leads` (`[AllowAnonymous]`). It takes `{ "email": "..." }` and responds `204 No Content`.
- The email is validated and normalized through `Email.From`. A malformed email returns `400`, and so does a missing one.
- The server sets the registration time in UTC from `IClock.UtcNow`. The client never sends it.
- Each email is stored once. Registering an email that is already stored also returns `204`, inserts nothing and keeps the original time, so the endpoint can't be used to find out which emails are registered.
- Per-client fixed-window rate limiting on this endpoint only, with limits read from configuration. Requests over the limit get `429 Too Many Requests`.
- New `CustomerLeads` table (`Id`, `Email` unique, `RegisteredAtUtc`) created by a reversible FluentMigrator migration.

## Capabilities

### New Capabilities

- `customer-leads`: anonymous registration of interested people's emails, including validation, deduplication, server-side UTC timestamp and rate limiting.

### Modified Capabilities

None. No existing spec covers public lead capture or rate limiting.

## Canonical Template References

| Capability part | Kind | Canonical reference | Why |
| --- | --- | --- | --- |
| Use case | skeleton-backed | `src/WeightTracker.Application/UseCases/Auth/SignupUseCase.cs` | Nearest anonymous write: `Email.From`, `IClock.UtcNow`, existence check, `IUnitOfWork` commit |
| Repository contract and implementation | skeleton-backed | `src/WeightTracker.Application/Interfaces/IExerciseFavoriteRepository.cs`, `src/WeightTracker.Infra/Repositories/ExerciseFavoriteRepository.cs` | Smallest repository; stages writes without committing |
| EF entity and configuration | skeleton-backed | `src/WeightTracker.Infra/Entities/ExerciseFavoriteEntity.cs`, `WeightTrackerDbContext` | Flat table with a UTC timestamp |
| Migration | skeleton-backed | `src/WeightTracker.Migrations/Migrations/202611031642_CreateFavoriteExercisesTable.cs` | PascalCase table and columns, `timestamptz`, reversible `Down()` |
| Email | template-backed | `.stpr/template/src/WeightTracker.Domain/Common/Email.cs` | Reused unchanged |
| Endpoint | skeleton-backed | `src/WeightTracker.Api/Endpoints/Auth/AuthController.cs` | `[AllowAnonymous]` controller, `ExceptionMapper.Map` in catch |
| Integration tests | template-backed | `.stpr/template/test/WeightTracker.Integration/Auth/AuthEndpointsTests.cs` | Public endpoint shape, no missing-token or invalid-token cases |
| Unit tests | skeleton-backed | `test/WeightTracker.Unit/Application/` (signup use-case tests) | Use-case branches with mocked repository, clock and unit of work |

## Classification, State And Identifiers

- Customer leads are transactional records, not a domain aggregate and not catalog-like. Rows are inserted and never updated or deleted. Nothing reads them through the API.
- `CustomerLead.Id`: immutable technical primary key, generated with `Id.New()`.
- `CustomerLead.Email`: unique and **immutable** after creation.

## Conflicts With Template Or Invariants

- **Repository without an aggregate.** The invariants say only aggregate roots get repositories and that repositories take domain types. Leads have no aggregate, as requested. `ICustomerLeadRepository` takes value objects (`Id`, `Email`) and a `DateTime`, has no mapper, and builds the EF entity inside the repository. Repositories still never commit. This is the only allowed deviation, and it is added to `STPR_INVARIANTS.md` as the rule for non-domain records.
- **Layers.** The request names Application and Infra as the layers to modify. The Domain project stays untouched, since `Email` and `Id` are only consumed. The API layer still has to change because the endpoint needs a controller, a request DTO, rate-limiter setup and registration in `Program.cs`.

## Acceptance Criteria

- A valid email returns `204` and stores exactly one row with the normalized email and a UTC `RegisteredAtUtc`.
- A duplicate email, in any casing, returns `204` and leaves the single original row unchanged.
- A missing or malformed email returns `400` and stores nothing.
- Requests beyond the configured limit return `429`.
- The endpoint is public: no token is needed and a bearer token is ignored.
- The full `dotnet test WeightTracker.slnx` passes with Docker running.

## Out Of Scope

- A domain model for leads, and reading, exporting or deleting them.
- Extra fields (name, source, consent, locale), confirmation email, double opt-in, CAPTCHA.
- Turning a lead into a user, and resolving the client IP from forwarded headers behind a proxy.
- App or backoffice changes.

## Impact

- **Api**: new `Endpoints/CustomerLeads/CustomerLeadsController.cs` and request DTO. `Program.cs` gets rate-limiter services and middleware plus the use-case and repository registrations. `appsettings.json` gets `RateLimiting:CustomerLeads`.
- **Application**: `UseCases/CustomerLeads/RegisterCustomerLeadUseCase.cs`, `Interfaces/ICustomerLeadRepository.cs`, and `Exceptions/CustomerLeadAlreadyExistsException.cs` for the concurrent-insert race.
- **Infra**: `Entities/CustomerLeadEntity.cs`, a DbSet and its configuration, `Repositories/CustomerLeadRepository.cs`, and unique-violation translation in `UnitOfWork`.
- **Migrations**: `CreateCustomerLeadsTable`.
- **Tests**: unit tests for the use case, integration tests for the endpoint, and `CustomerLeads` added to `ApiFactory.ResetDatabase`.
- **Docs**: `.stpr/template/STPR_INVARIANTS.md` documents the non-domain record rule.
- **Dependencies**: none. Rate limiting ships with ASP.NET Core.
