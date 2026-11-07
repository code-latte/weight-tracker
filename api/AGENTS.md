# Development Guidelines

This file is the operational guide for working in this repository. Keep it focused on precedence, workflow, verification, and maintenance. This project is maintained with the STPR workflow. Architectural rules live in `.stpr/template/STPR_INVARIANTS.md`.

## Project Overview

This repository follows **Clean Architecture** and uses **STPR** as its development workflow. All modified code must align with:

1. `.stpr/template/STPR_INVARIANTS.md`
2. `AGENTS.md`

## Rule Precedence

Use these sources with this precedence when there is overlap or tension:

1. **`.stpr/template/STPR_INVARIANTS.md`**: non-negotiable technical contract
2. **`AGENTS.md`**: operational guidance for maintaining and extending the project

## Scope Of This Guide

This file is intentionally concise. It should capture project-level rules, workflow, testing, and maintenance guidance. Detailed architectural rules, module classification, and reference examples belong in `.stpr/template/`.

## Development Guidelines

### Adding a New Entity

1. Start from the canonical reference selected through `.stpr/template/README.md`.
2. Mirror the chosen reference structure and naming unless the repo already has an approved local variation.
3. Respect all invariants from `.stpr/template/STPR_INVARIANTS.md`.

### Adding a New Endpoint

1. Add the use case in `WeightTracker.Application/UseCases/` for orchestration.
2. Check whether it is needed new logic in the domain. Aggregates should be the gate to those rules
3. Follow the template and invariants instead of introducing ad hoc wiring.
4. Add or reuse DTOs and response mapping following the template.

### Changing Business Validations

- **Domain**: Change value objects or entity and keep validation rules in domain code.
- **API**: Keep API validation structural and align behavior with the template and invariants.
- Update unit tests for value objects and entity.

### Fixing Bugs

- Identify the correct layer: API (controller/DTO), domain (entity, value object, use case), or data (mapping, queries).
- Fix the issue in the layer where it belongs, following template and invariants.
- Run affected tests and, when possible, full suites.

### Environment and Configuration

- Add new keys to **`appsettings.json`**, with development-only values in `appsettings.Development.json`.
- Never hardcode ports, connection strings, or JWT settings in code. Read them from configuration.
- Secrets must not be committed. The only exceptions are the development-only `ConnectionStrings:Default`, `Jwt:SigningKey` and `AdminUser:Email`/`AdminUser:Password`; every other environment supplies them through `ConnectionStrings__Default`, `Jwt__SigningKey`, `AdminUser__Email` and `AdminUser__Password`.

## Testing Guidelines

- **Unit tests** (`test/WeightTracker.Unit`): cover aggregates, value objects, use cases and mappers.
  Mirror the `src` folder structure (`Domain/<Module>`, `Domain/Common`, `Application/<Module>`, `Infra/Mappers`).
  No database, no HTTP, no Docker. Repositories and services are mocked.
- **Integration tests** (`test/WeightTracker.Integration`): run against a real Postgres 17 that
  Testcontainers starts fresh for each run, migrates once, and removes when the run ends, driving the
  API through `WebApplicationFactory`. This is the only tier
  that touches the database, so it carries both the happy-path coverage and the auth/error matrix below.
- **Authenticate only through the API.** Any test that calls an endpoint obtains its bearer token with
  `AuthenticatedClientFor(email)`, which calls `POST /auth/login`. Never mint a token with
  `JwtTokenService`, and never write rows directly to get a caller authenticated. Seeded credentials live
  in `SeedData`. (Unit tests may of course mock `ITokenService`; the rule is about tests that go over HTTP.)
- **Protected endpoint auth matrix.** Every `[Authorize]` route needs at least two cases: `401` with no
  token and `401` with an invalid token. Add `403` for a caller whose role or ownership forbids the
  operation, `404` for a missing resource, and `400` for structural request errors when they apply.
- **Per route, not per module.** A covered `GET /workouts` does not satisfy the matrix for
  `GET /workouts/{id}`, `POST`, `PUT` or `DELETE`. Each protected route carries its own cases.
- **Public endpoints.** For `[AllowAnonymous]` routes do not add missing-token or invalid-token cases.
  Cover success plus the business failures that belong to the endpoint, such as `401` for wrong credentials
  on `POST /auth/login`.
- **Scoped reads.** When a response depends on the caller's role or ownership, cover every branch and
  assert what is *excluded*, not only what is returned.
- **Determinism.** Integration tests share one database, so they run serially in a single xUnit collection
  and every test starts from a truncate-and-reseed baseline. Never let a test depend on another's writes.
- **Template parity.** Before adding a test, open the closest existing test for that kind of work and
  replicate its structure. Do not reduce coverage relative to that pattern unless the plan says why.
- **Arrange / Act / Assert.** Every test body is split into labelled phases. See
  `.stpr/template/STPR_INVARIANTS.md` for the exact convention.
- **Verification.** Any change touching API, domain, persistence, mappers, seed or auth runs the full
  `dotnet test WeightTracker.slnx` with Docker running before it is considered done.
  Integration tests require Docker; do not skip them to make a run go green.

## Running And Verifying Locally

Requires .NET SDK 10 and Docker.

```bash
docker compose -f ../infra/docker-compose.yml up -d   # Postgres 17 on 5432 (development only)
dotnet build WeightTracker.slnx                       # warnings are errors
dotnet test WeightTracker.slnx                        # unit + integration
```

- `dotnet test` needs Docker running but not the compose container. The integration suite
  starts its own throwaway Postgres container through Testcontainers, migrates it, and removes
  it at the end, so a fresh clone needs no manual database setup, every run starts from a clean
  database, and running it never touches the development data in `weight-tracker`.
- `dotnet run --project src/WeightTracker.Api` in Development applies migrations and seeds
  the development database on startup.
- **API reference**: in Development, Scalar is served at `http://localhost:5199/scalar` and the raw
  document at `/openapi/v1.json`. To call a protected endpoint: run `POST /auth/login` with a seeded
  user, copy `accessToken` from the response, and paste it into the authentication box. The token is
  kept across page refreshes and expires after 15 minutes, after which you log in again. Neither the
  UI nor the document is served in production.
- To apply migrations without starting the API, run the migrator directly. It takes the connection
  string as its first argument or from `ConnectionStrings__Default`, and the admin user it inserts from
  `AdminUser__Email` and `AdminUser__Password`. It exits non-zero on failure:
  ```bash
  dotnet run --project src/WeightTracker.Migrations -- "Host=localhost;Port=5432;Database=weight-tracker;Username=admin;Password=admin"
  ```
  Add `--down` to roll the whole schema back. That is destructive.
- In VS Code, open the `api` folder and use the **Debug API** launch configuration. Its `prelaunch`
  task runs clean, restore, build, `docker compose up -d`, then the migrator, stopping at the first
  failure. `Debug API (no prelaunch)` skips straight to build when you just want a fast restart.
- Seeded credentials for development and tests live in `src/WeightTracker.Infra/Seed/SeedData.cs`.
- **LAN access in Development**: CORS allows any origin and the API listens on all interfaces over
  HTTP, so browsers and devices on the local network (Expo web, Expo Go) can call
  `http://<machine-lan-ip>:5199` (VS Code debug configurations) or `:5000` (`http` profile). Do not use
  the `https` profile from a LAN device, since it redirects to `https://localhost:5001`. Allow the macOS
  firewall prompt the first time, or the device will time out. The development API and its seeded
  credentials are then reachable by anyone on the network. Scalar stays at `http://localhost:5199/scalar`.
  Nothing of this applies outside Development.
- Outside Development, `Jwt:SigningKey` must be supplied (for example via the
  `Jwt__SigningKey` environment variable) or startup fails.

`.stpr/template/src` and `.stpr/template/test` are curated copies of canonical files from `src/` and `test/`. Check they have not drifted:

```bash
for f in $(cd .stpr/template && find src test \( -name '*.cs' -o -name '*.csproj' \) -not -path '*/bin/*' -not -path '*/obj/*'); do
  cmp -s ".stpr/template/$f" "$f" || echo "DRIFT: $f"
done
```

## Best Practices & Maintenance Guidelines

### Working process (for non-trivial changes)

When adding entities, endpoints, or making architectural changes:

1. **Understand the request** and how it fits the existing structure (see `.stpr/template/` for reference).
2. **Plan** where new code goes (which module, api/data/domain) and what tests are needed.
3. **Implement** by following the STPR references and invariants first; use this document only for project-level guidance.
4. **Review before considering done**: use the checklist below; optionally run the manual review checklist in `.stpr/review/CHECKLIST_REVISION.md` if the change is large.

### Quality checklist (before considering a task complete)

- [ ] The change follows `.stpr/template/STPR_INVARIANTS.md` without local exceptions.
- [ ] The module structure, naming, and wiring match `.stpr/template/`.
- [ ] No mutable business state was modeled as a read-only catalog.
- [ ] Domain-significant primitives were not left as ad hoc validated numbers or strings in use cases.
- [ ] Every business-action input was validated before mutating aggregate state.
- [ ] Aggregate action methods protect the action semantics, not only the resulting state.
- [ ] Identifiers come from `Id.New()`; no `Guid.CreateVersion7()` or `Guid.NewGuid()` was introduced elsewhere.
- [ ] New tables have a FluentMigrator migration whose `Down()` reverses it, and no EF Core migration was generated.
- [ ] EF configuration matches the migration: table names, column names, lengths, and keys marked `.ValueGeneratedNever()`.
- [ ] Aggregate mappers round-trip, and mapping stays inside repositories.
- [ ] New use cases, repositories and services are registered in `Program.cs`.
- [ ] Every controller catch delegates to `ExceptionMapper.Map(exception)`, and new custom exceptions have explicit status mappings.
- [ ] Every use case that edits workout content (exercises, sets, effort, superseries) enforces the workout edit rule from `.stpr/template/STPR_INVARIANTS.md` (admin unless the workout is reviewed; prescribing trainer if any, otherwise the owner; only drafts for non-admins). Status changes and reviews are not edits and enforce the status and review rules instead.
- [ ] `.stpr/template/src` is still byte-identical to its `src` counterparts (see the parity check below).
- [ ] New or changed behavior is covered by the relevant tests and the affected suites pass.
- [ ] If the change touched endpoints, persistence, auth, mappers or seed, the full `dotnet test WeightTracker.slnx` was run with Docker running.
- [ ] Every protected route added or changed covers `401` without token and `401` with an invalid token.
- [ ] Every new test is split into labelled `// Arrange` / `// Act` / `// Assert` phases, with exactly one Act.
- [ ] No duplicated code that should be shared; naming and structure match the template.

### When this guide applies

For **trivial changes** (typos, single-line fixes, config tweaks), you can edit directly without following a full change workflow. Use this guide for anything that touches architecture, new entities, new endpoints, or test strategy.

## Resources

| What you need                                  | Where to look                                                 |
| ---------------------------------------------- | ------------------------------------------------------------- |
| STPR method entry point                        | `.stpr/README.md`                                             |
| Code structure and patterns                    | `.stpr/template/src/` and `.stpr/template/test/`              |
| STPR invariants and non-negotiable rules       | `.stpr/template/STPR_INVARIANTS.md`                           |
| Module type decision and reference selection   | `.stpr/template/README.md`                                    |
| Domain errors                                  | `src/WeightTracker.Domain/Exceptions`                         |
| Application errors                             | `src/WeightTracker.Application/Exceptions`                    |
| Domain-to-HTTP error mapping                   | `src/WeightTracker.Api/Common/ExceptionMapper.cs`             |
| Manual review checklist                        | `.stpr/review/CHECKLIST_REVISION.md`                          |

Following this guide keeps the project consistent with the template and its invariants.
