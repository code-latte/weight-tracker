# Manual review checklist

Use this checklist during the `Review` phase, after code generation and test execution, to manually verify that the generated project stays aligned with the template and conventions.

This checklist is a guide for human review. AI may help inspect the codebase, compare files, and summarize findings, but final validation remains manual.

**Reference:** Compare against **`.stpr/template/`** as the source of truth.

---

## Architecture

- [ ] The Dependency Rule holds: no `ProjectReference` points outward, and Domain references nothing.
- [ ] Only aggregate roots have repositories and mappers.
- [ ] No aggregate holds a reference to another aggregate root; cross-aggregate links are `Id` values.
- [ ] `AddDomainEvent` was not called (no dispatcher exists yet), unless this change added one.

## Structure

- [ ] Module folders exist per layer: `WeightTracker.Domain/<Module>`, `WeightTracker.Application/UseCases/<Module>`, `WeightTracker.Infra/Repositories`, and `WeightTracker.Api/Endpoints/<Resource>`.
- [ ] Repository contracts live in `WeightTracker.Application/Interfaces`; implementations live in `WeightTracker.Infra/Repositories`.
- [ ] Read projection contracts (`I<Projection>Query`) live in `WeightTracker.Application/Interfaces`; implementations live in `WeightTracker.Infra/Queries`.
- [ ] Tests live in `test/WeightTracker.Unit` (no infrastructure) and `test/WeightTracker.Integration` (real Postgres in a disposable Testcontainers container, never the compose database).

## Domain

- [ ] Each piece matches its declared classification: aggregate roots expose the mutation methods the capability needs,
      entities are reachable only through their root, and value objects are immutable `record` types.
- [ ] `Create` sets `IsNew = true` and `Rehydrate` sets `IsNew = false`; constructors stay private.
- [ ] Value objects are `record` types and have unit tests.
- [ ] `Id` is used for every identifier; no per-entity identifier type was introduced.
- [ ] No `Guid.CreateVersion7()` or `Guid.NewGuid()` outside `Id.New()`.
- [ ] Value object semantics shared by two or more modules live in `Domain/Common` rather than duplicated.
- [ ] Aggregates reference other aggregates by `Id`, with no navigation property crossing an aggregate boundary.
- [ ] Business-action inputs are validated before mutating aggregate state.
- [ ] Aggregate methods that model actions validate the action semantics, not only the resulting state.
- [ ] The implementation does not rely only on HTTP DTO validation for domain-significant action inputs.

## Persistence

- [ ] Every new table has a FluentMigrator migration named `ActionObject_YYYYMMDDHHmm`, and `Down()` reverses it.
- [ ] No EF Core migrations or model snapshot were generated.
- [ ] EF configuration matches the migrations: table names, column names, and lengths.
- [ ] String column lengths match the limits declared on the matching value object or aggregate constant.
- [ ] Keys are configured `.ValueGeneratedNever()`.
- [ ] Each aggregate root has a mapper exposing `MapToEntity` and `MapToDomain`, and mapping happens inside repositories.
- [ ] Repositories do not call `SaveChanges`; the use case calls `IUnitOfWork.SaveChangesAsync` exactly once.
- [ ] Repositories take and return domain types only; no EF entity leaks past the repository.
- [ ] Every I/O method takes a `CancellationToken` as its last parameter and passes it through to EF.
- [ ] `DbContext` is reached directly only from repositories, read projections in `Infra/Queries`, seed code and the integration test bootstrap.
- [ ] Every read projection follows `STPR_INVARIANTS.md` ("Read projections"): own interface, no-tracking, returns an Application view, never used before a write, and named in the plan with its reason.

## API

- [ ] Architecture invariants from `.stpr/template/STPR_INVARIANTS.md` are respected, including controller/use-case separation.
- [ ] Controllers inject use cases only, and every handler catch delegates to `ExceptionMapper.Map(exception)`.
- [ ] Every custom exception introduced by the change has an explicit status mapping in `ExceptionMapper`.
- [ ] New use cases, repositories and services are registered in `Program.cs`.
- [ ] Endpoint visibility matches the matrix approved in the plan.
- [ ] Every use case that edits workout content enforces the workout edit rule (`STPR_INVARIANTS.md`, "Business authorization rules") through the shared Application service, never inline. Every use case that changes a workout's status enforces the status rule through its own shared service.
- [ ] No domain object crosses the HTTP boundary; responses are DTOs from `Endpoints/<Resource>/Dtos`.
- [ ] Every new or changed action declares its responses with `[ProducesResponseType]`, including the failure codes.
- [ ] Protected endpoints appear in the OpenAPI document with a resolved `Bearer` security requirement.

## Reference parity

- [ ] Repository and mapper class naming matches the template.
- [ ] `.stpr/template/` is still identical to its repository counterparts:
      `for f in $(cd .stpr/template && find src test \( -name '*.cs' -o -name '*.csproj' \) -not -path '*/bin/*' -not -path '*/obj/*'); do cmp -s ".stpr/template/$f" "$f" || echo "DRIFT: $f"; done`
- [ ] Deviations from `.stpr/template/` were either corrected in the project or captured back into the template or planning assets for future runs.

## Tests

- [ ] Unit tests exist for new aggregates, value objects, use cases and mappers, under `test/WeightTracker.Unit`
      mirroring the `src` folder structure, with no database or HTTP involved.
- [ ] Integration tests cover the affected endpoints against the real database, under `test/WeightTracker.Integration`.
- [ ] Tests authenticate through `POST /auth/login`; no test mints a token directly or writes rows to fake auth.
- [ ] Every protected route added or changed covers `401` without token and `401` with an invalid token.
- [ ] Role or ownership scoping is covered on every branch, asserting what is excluded as well as what is returned.
- [ ] `403`, `404` and `400` cases exist where the endpoint can produce them.
- [ ] Public routes carry no missing-token or invalid-token cases.
- [ ] New tests replicate the structure of the closest existing test rather than inventing a new shape.
- [ ] Tests are order independent and start from the truncate-and-reseed baseline.
- [ ] Every new test body is labelled `// Arrange`, `// Act`, `// Assert` (or `// Arrange & Act`), phases separated by a blank line.
- [ ] Each test has exactly one Act; a test needing two was split.
- [ ] The behaviour named in the test name is invoked in the Act phase, not inside an assertion.

## Verification

- [ ] `dotnet build WeightTracker.slnx` passes with no warnings.
- [ ] `dotnet test WeightTracker.slnx` passes with Docker running (the suite starts its own Testcontainers Postgres).

---

**Resources:** `.stpr/README.md`, `AGENTS.md`, `.stpr/template/STPR_INVARIANTS.md`, `.stpr/template/README.md`, `.stpr/plan/prompts/`.
