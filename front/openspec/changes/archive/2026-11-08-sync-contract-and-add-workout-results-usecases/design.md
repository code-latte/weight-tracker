# Design

## Context

See proposal.md for the motivation and the contract diff. `core` already has a workouts area: aliases in
`core/src/domain/workouts/workout.ts`, the `WorkoutRepositoryInterface` port, `WorkoutRepository`, and ten use cases in
`core/src/application/use-cases/workouts/`. Both fronts register them in `container.ts` and stub them in
`test/support/render-with-providers.tsx`. The HTTP error mapper maps by status only (400/401/403/404/409, everything
else `UNEXPECTED`, no response `NETWORK`). This change extends the area in place. It adds no new area and no new
pattern.

## Goals / Non-Goals

**Goals:**

- The snapshot and generated types match the API exactly.
- The six new operations follow the shape of the existing workout operations, so they read the same way.

**Non-Goals:**

- No new repository class or port. Set records and review belong to the workout aggregate in the API and share its
  routes.
- No change to the error mapper, `DomainErrorCode` or translations.

## Decisions

### 1. Contract refresh first, as its own commit

Run the API (Debug API, port 5199) from `main`, then `pnpm --filter @weight-tracker/core openapi:refresh` and
`openapi:generate`. Review the diff against the list in proposal.md and commit both files alone, before any
hand-written code. Then run `pnpm typecheck` to see which typed fixtures need the new required fields.

- _Alternative:_ fold the refresh into the code commit. Rejected, because AGENTS.md asks for it as a step of its own and
  a separate commit makes contract drift visible.

### 2. Domain aliases (`core/src/domain/workouts/workout.ts`)

Add these, all derived from `components["schemas"]`:

| Alias                         | Source                                                                                                                                                        |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SetRecord`                   | `SetRecordResponse`                                                                                                                                           |
| `AddOrUpdateSetRecordRequest` | `AddOrUpdateSetRecordRequest`                                                                                                                                 |
| `ReorderSetRecordsRequest`    | `ReorderSetRecordsRequest`                                                                                                                                    |
| `ReviewWorkoutRequest`        | `ReviewWorkoutRequest`                                                                                                                                        |
| `ChangeWorkoutStatusRequest`  | `ChangeWorkoutStatusRequest`                                                                                                                                  |
| `WorkoutStatus`               | `WorkoutStatus` (not exported from `domain` today; the backoffice's hook-local `WorkoutStatus` loading type in `use-workout.ts` is unrelated and stays as is) |

Inputs follow the existing comment style ("… travels in the path, the rest in the body"):

- `AddSetRecordInput = AddOrUpdateSetRecordRequest & { workoutId; exerciseId }`
- `UpdateSetRecordInput = AddOrUpdateSetRecordRequest & { workoutId; exerciseId; setRecordId }`
- `RemoveSetRecordInput = { workoutId; exerciseId; setRecordId }`
- `ReorderSetRecordsInput = ReorderSetRecordsRequest & { workoutId; exerciseId }`
- `ReviewWorkoutInput = ReviewWorkoutRequest & { workoutId }`
- `ChangeWorkoutStatusInput = ChangeWorkoutStatusRequest & { workoutId }`

Each comment states that `exerciseId` is the catalog exercise id. `WorkoutDetail` and the other existing aliases pick
up `setRecords` and the review fields from regeneration without being edited.

- _Alternative:_ name the input `Workout*` (for example, `AddWorkoutSetRecordInput`). Rejected, because the API's use
  cases are named `AddSetRecord…` and set records exist only in workouts. The use case names match the API's.

### 3. Repository contract and implementation

The new `WorkoutRepositoryInterface` methods all return `Promise<Result<WorkoutDetail, DomainError>>`:

- `addSetRecord(workoutId, exerciseId, request)` sends `POST …/set-records`.
- `updateSetRecord(workoutId, exerciseId, setRecordId, request)` sends `PUT …/set-records/{setRecordId}`.
- `removeSetRecord(workoutId, exerciseId, setRecordId)` sends `DELETE …/set-records/{setRecordId}` with no body.
- `reorderSetRecords(workoutId, exerciseId, request)` sends `PUT …/set-records/order`.
- `review(workoutId, request)` sends `PUT /workouts/{workoutId}/review`.
- `changeStatus(workoutId, request)` sends `PUT /workouts/{workoutId}/status`.

Each method is a copy of `reorderExercises` (`try` → `httpClient.request<WorkoutDetail>` → `ok`, `catch` →
`fail(mapHttpError(error))`).

`removeSetRecord` returns `WorkoutDetail` rather than `void` because the API answers 200 with the detail. This is unlike
`removeExercise`, which answers 204.

### 4. Use cases (`core/src/application/use-cases/workouts/`)

There are six files, each modelled on `reorder-workout-exercises-use-case.ts`: it implements
`UseCaseInterface<Input, WorkoutDetail>`, destructures the path ids, passes the rest as the body, and returns the
repository `Result` unchanged. Each has a one-line JSDoc saying which rule the API owns, for example "Whether the
workout is started is decided by the API."

| File                                | Class                        | Repository method   |
| ----------------------------------- | ---------------------------- | ------------------- |
| `add-set-record-use-case.ts`        | `AddSetRecordUseCase`        | `addSetRecord`      |
| `update-set-record-use-case.ts`     | `UpdateSetRecordUseCase`     | `updateSetRecord`   |
| `remove-set-record-use-case.ts`     | `RemoveSetRecordUseCase`     | `removeSetRecord`   |
| `reorder-set-records-use-case.ts`   | `ReorderSetRecordsUseCase`   | `reorderSetRecords` |
| `review-workout-use-case.ts`        | `ReviewWorkoutUseCase`       | `review`            |
| `change-workout-status-use-case.ts` | `ChangeWorkoutStatusUseCase` | `changeStatus`      |

All six are re-exported from the workouts barrel and the application barrel.

### 5. `Dependencies` and wiring

New keys in `core/src/application/dependencies.ts`:

- `addSetRecordUseCase`
- `updateSetRecordUseCase`
- `removeSetRecordUseCase`
- `reorderSetRecordsUseCase`
- `reviewWorkoutUseCase`
- `changeWorkoutStatusUseCase`

Each is typed `UseCaseInterface<…Input, WorkoutDetail>`.

Register them in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts` next to the
other workout use cases, each as `(c) => new XUseCase(c.resolve("workoutRepository"))`. Both fronts register all six,
as they do for every workout use case today, so `Dependencies` stays a single shape. The API decides what each role may
call.

Test helpers: both `render-with-providers.tsx` gain a placeholder per key (`() => ({ execute: async () => undefined as never })`),
and both `container.test.ts` add the six keys to their list. This extends the existing pattern without changing it.

### 6. State, components, errors, copy

- **State**: none. No provider or hook.
- **Components**: none. No atom, molecule, organism, view or route.
- **Errors**: `VALIDATION`, `UNAUTHORIZED`, `FORBIDDEN`, `NOT_FOUND`, `CONFLICT`, `NETWORK` and `UNEXPECTED` are all
  already mapped and already translated in `es` and `en` in both fronts.
- **Translations**: no new key.

`backoffice` and `app` stay independent.

### 7. Fixtures for the new required fields

After regeneration, every typed literal of `WorkoutDetail`, `WorkoutById`, `WorkoutListItem`, `Workout` or
`WorkoutExerciseDetail` in tests gains the new fields: `reviewedAt: null`, `reviewedBy: null`,
`resultsUpdatedAt: null`, and `setRecords: []` per exercise. Where a file already has a fixture builder, the fields go in
the builder once. The affected files are listed by `pnpm typecheck`. Backoffice E2E route mocks are untyped JSON, so
they only need the fields if a view reads them, and none does.

- _Alternative:_ loosen the aliases with `Partial`. Rejected, because it would be a hand-shaped domain type.

## Risks / Trade-offs

- [The API on `main` differs from the archived changes] → The diff review in Decision 1 is the gate. Any unexpected
  difference stops the change before hand-written code.
- [Fixture churn across many test files makes the diff noisy] → Keep the fixture updates in their own commit, right
  after the contract commit.
- [`reps` and `weight` are generated as `number | string` (the .NET numeric pattern)] → The aliases keep the generated
  type unchanged, so callers pass numbers. This matches existing numeric fields such as `restMinutes`.
- [`FINISHED` reappears in an untyped place (copy, E2E mocks)] → `grep -r FINISHED` across `front/` before closing.
  Today it returns nothing outside `generated`.

## Migration Plan

No deployment step is needed. The fronts are only rebuilt with the new types. To roll back, revert the commits. The
contract commit can be reverted on its own only together with the use case commits, which depend on the new types.
