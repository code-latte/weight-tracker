# Proposal

## Why

The backoffice review shows what the athlete recorded, set by set, but the "Resumen y métricas" tab is an empty
panel. The trainer has no view of how the session went as a whole, or of how each exercise compares with its previous
execution. The API now computes both (`GET /workouts/{workoutId}/summary` and
`GET /users/{userId}/exercises/{exerciseId}/history`, API change `add-workout-summary-and-exercise-history`). This change
fills the tab from them, with the session observations mocked until the API returns them.

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-workout-review-metrics.md`. Visual reference:
`brand/review.png`.

## What Changes

- **Contract sync.** Refresh `core/openapi/v1.json` and regenerate `core/src/contract/generated`, in a commit of its
  own. The refresh adds:
  - the summary types: `WorkoutSummaryResponse`, `ExerciseSummaryResponse`, `SetsInTargetResponse` and
    `PreviousExecutionResponse`;
  - `ExerciseHistorySessionResponse`;
  - the two routes.
- **Core.**
  - Domain aliases for the summary and the history session.
  - `WorkoutRepositoryInterface.getSummary` and `UserRepositoryInterface.getExerciseHistory`, with their
    implementations.
  - `GetWorkoutSummaryUseCase` and `GetUserExerciseHistoryUseCase`.
  - Both keys are registered in both fronts' `container.ts`, because both build `Container<Dependencies>`. Only the
    backoffice reads them.
- **Backoffice, "Resumen y métricas" tab.** The tab reads the summary the first time it is opened. It shows:
  - **"Resultados de la sesión":**
    - "Series completadas", "Ejercicios completados" and "Duración", from the workout the screen already holds and the
      screen's existing rules;
    - "Series en rango objetivo" and "Volumen de fuerza", from the summary;
    - none of the cards shows a change against another workout.
  - **"Evolución de la carga" and "Evolución del volumen":** a line chart of the max weight and a bar chart of the
    volume, over the athlete's last 5 sessions of one weighted exercise. The two charts share one selector.
  - **"Comparativa por ejercicio":** one row per weighted exercise, with:
    - its previous and current top set;
    - the weight change;
    - its volume;
    - its compliance.
  - **"Distribución de series por grupo muscular":** counted records per exercise `muscle`, for trained muscles only.
  - **"Observaciones de la sesión":** a fixed list of three sample observations, marked with a "Vista previa" chip.
- **Backoffice dependency.** `@mui/x-charts` is added to `backoffice` for the two charts. It is installed with
  `pnpm --filter backoffice add @mui/x-charts` (the user asked for `npm install`, but the repo is a pnpm workspace).
  Only two new atoms import it.

No breaking change.

## Capabilities

### New Capabilities

- `workout-summary`: core reads a workout's summary (sets in target, volume, and per exercise the top set and the
  previous execution) through the API, and returns every failure as a `DomainError` code.
  - Lands in `core`, and only the backoffice uses it.
  - Canonical reference: `GetWorkoutByIdUseCase` and `WorkoutRepository.getById`, which have the same id-in-path
    read in the same repository.
- `exercise-history`: core reads one user's per-session history of a catalog exercise through the API, with a limit,
  and returns every failure as a `DomainError` code.
  - Lands in `core`, and only the backoffice uses it.
  - Canonical reference: `GetMyExerciseLastPerformanceUseCase` and `UserRepository.getMyExerciseLastPerformance`,
    which have the same repository and the same per-exercise read.
- `backoffice-workout-review-metrics`: the content of the review's "Resumen y métricas" tab.
  - Lands in the backoffice only, for `ADMIN` and `TRAINER`, with no role branch.
  - Canonical references: the review's own `review-stat-card.tsx`, `review-exercise-card.tsx`,
    `review-sets-table.tsx` and `use-workout.ts`, and the `linear-progress.tsx`, `text-field.tsx` and `menu-item.tsx`
    atoms.
  - The chart atoms have no reference (charts are new to the backoffice), so this change establishes them as thin
    wrappers like the other atoms.

### Modified Capabilities

- `backoffice-workout-review`: the "Resumen y métricas" tab is no longer an empty panel. "Feedback" stays empty.

## Fronts And Roles

- **backoffice:** `ADMIN` and `TRAINER` see the same tab for any workout the API returns to them. There is no role
  difference, and nothing in the tab is hidden per role.
- **app:** two registration lines in `container.ts`, with no screen, copy or behaviour change. `USER` sees nothing
  new.

## Components

- New organisms, in `backoffice/src/presentation/workouts/components/organisms/`:
  - `WorkoutReviewMetrics`, the tab content holding the blocks;
  - `SessionResultsCard`;
  - `ExerciseEvolutionCard`, used twice: line and bar;
  - `ExerciseComparisonCard`;
  - `MuscleDistributionCard`;
  - `SessionObservationsCard`.
- New molecules, in `components/molecules/`:
  - `WeightChangeChip`;
  - `ComplianceChip`;
  - `MuscleSetsRow`;
  - `ObservationItem`.
- `ReviewStatCard` -> molecule (existing) -> gains an optional caption line.
- New atoms, in `common/components/atoms/`: `LineChart` and `BarChart`, the only importers of `@mui/x-charts`.
- Reused atoms: `card`, `typography`, `chip`, `linear-progress`, `text-field` (select), `menu-item`, `table*`,
  `skeleton` and `alert`.

## Conflicts With The Invariants

- **No business rule** enters the front:
  - the API computes compliance, volume, top sets and the previous execution;
  - the front only counts records with the review's existing counting rule, groups by the `muscle` the API returns,
    and formats numbers;
  - "weighted" means the API returned a non-null `volume`.
- **No hand-written type.** Every type comes from the refreshed snapshot. The mocked observations are a presentation
  fixture with their own local shape, not a domain type. They will be replaced when the contract carries them.
- **Shared wiring only.** Registering the backoffice-only keys in the app's container follows the precedent of
  `show-exercise-last-performance` (D2). It shares no UI, copy or presentation code.
- **History anchoring.** The endpoint is not anchored to the reviewed workout's date, so reviewing an older workout can
  show later sessions. This is accepted, and an `until` parameter is out of scope.

## Acceptance Criteria

- The refreshed snapshot and regenerated types are committed on their own, and `core/src/contract/generated` is not
  hand-edited.
- Core covers both use cases and both repository methods, for `200`, `401`, `403`, `404` and a network failure.
- The tab reads the summary once, on its first opening.
- The five cards, the comparison rows, the muscle rows and the observations match the spec. That includes:
  - the "—" cases;
  - non-weighted exercises hidden;
  - untrained muscles absent.
- Changing the chart exercise reads the history again. An empty history shows "Sin datos de este ejercicio".
- A failed summary shows a load error with retry in the tab. A failed history shows one in the evolution cards only.
- Every new string exists in `es` and `en`, and translation parity passes.
- `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. No E2E flow is planned.

## Impact

- `core`:
  - `openapi/v1.json` and `src/contract/generated`;
  - `src/domain/{workouts,users}`, `src/application/ports`, `src/application/use-cases/{workouts,users}` and
    `src/application/dependencies.ts`;
  - `src/infrastructure/repositories/{workout,user}-repository.ts`;
  - the matching tests.
- `backoffice`:
  - `package.json` (`@mui/x-charts`) and `src/infrastructure/container.ts`;
  - `src/presentation/workouts/{hooks,components,views}` and `workout-review.ts`;
  - two chart atoms, `src/i18n/{es,en}/workouts.json` and `test/presentation/workouts`.
- `app`: `src/infrastructure/container.ts` registers the two keys.
- API: none from this change. It depends on `add-workout-summary-and-exercise-history`.
