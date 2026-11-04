# Template Reference Map

This file is the entrypoint for using `.stpr/template/` with minimal context.

Use it together with:

- `STPR_INVARIANTS.md`
- the specific reference files listed below
- the reference tests only when the task changes behavior or coverage

## Goal

Use this file to choose the right reference quickly. The goal is not to load the whole template, but to open the smallest useful set of files for the task.

Default reading order:

1. classify the module
2. pick the canonical reference
3. open only the `minimal set`
4. open `optional files` only if the task needs them
5. open tests last, not first

When selecting a reference, first decide whether it is:

- `template-backed`: the implementation reference exists inside `.stpr/template/`
- `skeleton-backed`: the implementation reference lives in the repository skeleton under `src/`

## Canonical Items Classification

| Item                    | Classification                                | Why                                                                       |
| ----------------------- | --------------------------------------------- | ------------------------------------------------------------------------- |
| `Workouts`              | `Aggregate root with value objects`           | Create / Rehydrate / IsNew, inner entity (WorkoutExercise), value objects, multi-table aggregate |
| `ExerciseUseCase`       | `Caller-scoped read use case`                 | Reads scoped by the caller (anonymous, owner, admin, visible workouts); the same aggregate also has owner-checked writes |
| `Id` / `Email`          | `Shared value objects`                        | Shared identifier and business identifier used across modules             |
| `WorkoutRepository`     | `Repository implementation`                   | Multi-table aggregate load, cross-aggregate reference by id               |
| `WorkoutMapper`         | `Aggregate mapper`                            | MapToEntity / MapToDomain across three tables, discriminated value object  |
| `WorkoutsController`    | `REST controller`                             | Authenticated endpoint, `CurrentUser` to use case, ExceptionMapper in catch |
| `AuthorizedController`  | `Base controller`                             | Resolves the caller through a use case, 401 when the user no longer exists |
| `ExceptionMapper`       | `Domain-to-HTTP error mapping`                | Single mapping point for custom exceptions                                |
| `WorkoutEditAuthorizer` | `Business authorization service`              | The workout edit rule (content, plus the lock once started), shared by every use case that edits a workout |
| `WorkoutStatusAuthorizer` | `Business authorization service`            | The status rule: who may attempt each lifecycle move. Status changes are not edits |
| `Auth`                  | `Aggregate root plus use cases`               | JWT issuing plus persisted, rotating refresh tokens                       |
| `CreateWorkoutsTable`   | `FluentMigrator migration`                    | Naming, FK, index and rollback shape                                      |
| `DatabaseSeeder`        | `Seed`                                        | Deterministic ids, aggregates built through the domain                    |
| `ApiFactory`            | `Integration test bootstrap`                  | Real Postgres, migrate, truncate, reseed per test                         |
| `IntegrationTest`       | `Integration test base`                       | Login through the API, authenticated client per seeded user               |
| `AuthEndpointsTests`    | `Endpoint auth matrix`                        | 401 without token, 401 with invalid token, rotation and replay            |
| `WorkoutScopingTests`   | `Scoped read endpoint`                        | Every role branch, asserting exclusions as well as inclusions             |
| `WorkoutTests`          | `Aggregate unit test`                         | Arrange/Act/Assert phases, invariants, factory and rehydrate behaviour    |

## Template Contents

`.stpr/template/src/` and `.stpr/template/test/` are curated copies of canonical reference files from the
repository `src/` and `test/` trees. They must stay byte-identical to their counterparts. Check parity with:

```bash
for f in $(cd .stpr/template && find src test \( -name '*.cs' -o -name '*.csproj' \) -not -path '*/bin/*' -not -path '*/obj/*'); do
  cmp -s ".stpr/template/$f" "$f" || echo "DRIFT: $f"
done
```

Everything not copied there is skeleton-backed and must be read from `src/` or `test/`.

## Reference Origin

Use this split to decide where the canonical implementation reference actually lives.

### Template-backed references

These have implementation references inside `.stpr/template/`:

- `Workouts`
- `Id` / `Email` -> `.stpr/template/src/WeightTracker.Domain/Common`
- `ApiFactory` / `IntegrationTest` -> `.stpr/template/test/WeightTracker.Integration/Bootstrap`
- `AuthEndpointsTests` -> `.stpr/template/test/WeightTracker.Integration/Auth`
- `WorkoutScopingTests` -> `.stpr/template/test/WeightTracker.Integration/Workouts`
- `WorkoutTests` -> `.stpr/template/test/WeightTracker.Unit/Domain/Workouts`

### Skeleton-backed references

These are provided by the base project skeleton and should be read from the repository `src/` tree:

- `ExerciseUseCase` -> `src/WeightTracker.Application/UseCases/Exercises`
- `WorkoutRepository` -> `src/WeightTracker.Infra/Repositories/WorkoutRepository.cs`
- `WorkoutMapper` -> `src/WeightTracker.Infra/Mappers/WorkoutMapper.cs`
- `WeightTrackerDbContext` -> `src/WeightTracker.Infra/Persistence/WeightTrackerDbContext.cs`
- `WorkoutsController` -> `src/WeightTracker.Api/Endpoints/Workouts`
- `AuthorizedController` -> `src/WeightTracker.Api/Endpoints/AuthorizedController.cs`
- `ExceptionMapper` -> `src/WeightTracker.Api/Common/ExceptionMapper.cs`
- `WorkoutEditAuthorizer` -> `src/WeightTracker.Application/Services/WorkoutEditAuthorizer.cs`
- `Auth` -> `src/WeightTracker.Domain/Auth`, `src/WeightTracker.Application/UseCases/Auth`, `src/WeightTracker.Infra/Services`
- `CreateWorkoutsTable` -> `src/WeightTracker.Migrations/Migrations`
- `DatabaseSeeder` -> `src/WeightTracker.Infra/Seed`

Note on repository contracts: repository interfaces live in `src/WeightTracker.Application/Interfaces`,
as fixed by `STPR_INVARIANTS.md` section 4.

Rule:

- Do not describe a skeleton-backed capability as if its canonical files existed inside `.stpr/template/`.
- When a capability is skeleton-backed, use the skeleton as the implementation reference and use `.stpr/template/STPR_INVARIANTS.md` as the architectural contract.
- When a capability is skeleton-backed, preserve the local structural and typing patterns already used by that skeleton reference unless the plan explicitly approves a deviation.

## Quick Selection Rules

- If the requested piece is an aggregate root with value objects, replicate `Workouts`. Its state is **mutable**: do not use it as evidence that anything is read-only.
- If you need a new read use case whose result depends on the caller, replicate the `ExerciseUseCase` (`GetAllExercisesUseCase`); Exercises are a mutable aggregate, so do not use them as evidence that anything is read-only
- If you need a repository implementation or an aggregate mapper spanning several tables, replicate `WorkoutRepository` and `WorkoutMapper`
- If you need a new endpoint, replicate `WorkoutsController`, and route every catch through `ExceptionMapper`
- If a controller has protected routes, inherit `AuthorizedController` and read the caller from `CurrentUser`; never parse claims in the controller
- If a use case edits the content of an existing workout, call `WorkoutEditAuthorizer.EnsureCanEdit` before touching the aggregate, as `CreateSuperSeriesUseCase` does
- If a use case changes the status of a workout, call `WorkoutStatusAuthorizer.EnsureCanChange` instead, as `ChangeWorkoutStatusUseCase` does; a status change is not an edit
- If you need a new identifier or business identifier, reuse `Id` and `Email` from `Domain/Common` instead of adding a per-module value object
- If you need a new table, replicate the migration shape in `src/WeightTracker.Migrations/Migrations`
- If you need a test for a protected endpoint, replicate `AuthEndpointsTests`; it carries the `401` matrix every protected route must have
- If the endpoint returns different data per role or owner, replicate `WorkoutScopingTests`; it asserts exclusions, not only inclusions

## Minimal Context Loading

Do not start by reading all files of a reference module.

Open the smallest useful set first:

- `STPR_INVARIANTS.md`
- one reference module from this file
- one test file only if behavior must be copied exactly

Use these defaults:

- New endpoint in existing module:
  open the controller, the target use case, the DI registration in `Program.cs`, and one matching endpoint test
- New entity or module:
  open the aggregate, the repository contract in `Application/Interfaces`, the repository implementation and mapper in
  `Infra`, the FluentMigrator migration, the EF configuration in `WeightTrackerDbContext`, and one happy-path test
- Validation change:
  open the value object or aggregate first, then only the affected tests
- New or changed endpoint test:
  open `AuthEndpointsTests` for the auth matrix and `WorkoutScopingTests` for a scoped read, plus
  `ApiFactory` and `IntegrationTest` for the bootstrap
- New aggregate unit test:
  open `WorkoutTests` for the Arrange/Act/Assert shape and invariant coverage

There are no module wiring files in this stack. Dependency registration happens in `src/WeightTracker.Api/Program.cs`.
