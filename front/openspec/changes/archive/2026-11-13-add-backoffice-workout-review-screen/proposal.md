# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-workout-review-screen.md`. Visual reference:
`brand/workout_review.png`.

## Why

Once the athlete starts a workout, the backoffice shows the trainer a locked editor they can no longer use, and the set
records the athlete logs are shown nowhere. Trainers need one read-only screen to follow and review a session: how far
the athlete got, and what they did against what was planned.

## What Changes

- `/workout?id=<id>` keeps one route and one workout read.
  - `DRAFT` and `PUBLISHED` workouts open the editor, unchanged.
  - `STARTED`, `COMPLETED` and `REVIEWED` workouts open a new **workout review** screen instead of the locked editor.
  - Any other status opens the editor, as today.
- **The review screen** (read-only, apart from saving a template) has:
  - **Header:** the back link (same rule as the editor), "Revisión del entrenamiento", a subtitle "<name> · <mesocycle>
    · Semana <n>" and the status chip.
  - **Person strip:** the athlete, with the trainer for `ADMIN` only, and "Última actualización" from
    `resultsUpdatedAt`.
  - **Summary cards:** "Series completadas", "Ejercicios terminados" and "Volumen registrado".
  - **Session progress:** a card with a percentage, a progress bar, the sets done and the sets pending.
  - **Tabs:** "Ejercicios", "Resumen y métricas" and "Feedback". The last two are empty. There is no "Expandir todo"
    button and no ⋮ menu.
  - **One expandable card per exercise:** its state ("Completado", "En progreso", "Pendiente"), its chips, an
    "Esfuerzo" line at exercise scope, and a plan-versus-done table ("Serie", "Objetivo (plan)", "Realizado",
    "Descanso"). The plan shows no weight.
- **Counting rule, as the app does:**
  - only an exercise's first records, up to its planned sets, are counted;
  - any extra record is discarded everywhere on the screen;
  - an exercise is finished when its counted records equal its planned sets.
- **Volume:** `reps × weight` over the counted records that have both. Timed and weightless records add nothing, and
  there is no `perSide` doubling.
- **Placeholders** ("—") for data the API will provide later:
  - "Inicio";
  - "Tiempo transcurrido".

  The screen reads once and does not poll.

- **Live status chip:** for `STARTED`, the status chip is outlined red with a live icon and fades slowly in and out. It
  stands still for visitors who prefer reduced motion.

- **"Guardar como plantilla"** stays available on the review, in the header, with the same behaviour as in the editor
  (one template creation, "Plantilla creada." and "Ver plantilla").
- **BREAKING (behaviour):** a started, completed or reviewed workout no longer opens the locked editor. Those workouts
  lose the editing of exercises it offered. The brief scopes that out of the review.
- New backoffice atoms: `Tabs`, `Tab`, `LinearProgress`.

## Capabilities

### New Capabilities

- `backoffice-workout-review`: the read-only review of a started, completed or reviewed workout in the backoffice. It
  covers the header, person strip, placeholders, summary cards, progress, tabs, exercise cards, the plan-versus-done
  table, the counting rule and the translations.

### Modified Capabilities

- `backoffice-workout-editor`: the workout screen chooses editor or review by the loaded workout's status.
  - The header requirement drops its "started workout" scenario, because the editor is only shown for `DRAFT` and
    `PUBLISHED`.
  - The review does not wait for the exercise catalog.
- `backoffice-workout-status`: "A started workout's details cannot be edited" is removed, because those statuses now
  open the review. Its purpose still holds, because started workouts still cannot be edited.

## Fronts, Roles And References

- **backoffice** (`ADMIN`, `TRAINER`): both open the review of any workout the API returns. The only difference is in
  the person strip: `ADMIN` sees the trainer's name and `TRAINER` must not.
- **app:** no change. **core:** no change (`getWorkoutByIdUseCase` is reused).
- **Canonical references, all in the backoffice:**
  - `presentation/workouts/views/workout-view.tsx`: the screen, loading, errors and back link;
  - `components/organisms/workout-context-strip.tsx` and `common/components/molecules/person-card.tsx`: the person
    strip;
  - `components/molecules/exercise-list-item.tsx` and `hooks/use-exercise-labels.ts`: names, chips and effort labels;
  - `components/organisms/workouts-table.tsx` and the `table*` atoms: the sets table;
  - `presentation/common/workout-status.ts`: the status chip;
  - any existing atom in `common/components/atoms`: the new atoms.
- No reference is `pending`. The counting rule mirrors the app's rule without importing it, because the fronts share
  nothing.
- **Component levels:**
  - atoms (new): `Tabs`, `Tab`, `LinearProgress`;
  - molecules: `ReviewStatCard`, `SessionProgressCard`, `ReviewSetsTable`;
  - organisms: `WorkoutReviewHeader`, `WorkoutReviewPersonStrip`, `WorkoutReviewTabs`, `ReviewExerciseCard`;
  - reused atoms: `Card`, `Chip`, `Avatar`, `Collapse`, `IconButton`, the `table*` atoms, `Typography`, `Link`;
  - reused molecules: `HomeSkeleton`, `LoadError`.
- **Business rules:** none are added. The derivations are display arithmetic over data the API returns, with the same
  counting rule as the app.
- **Conflicts with the existing behaviour:** the BREAKING item above. No conflict with the invariants or a stop case.

## Impact

- **Backoffice code:**
  - `presentation/workouts/views/workout-view.tsx`: the status branch;
  - new `views/workout-review-view.tsx`;
  - new molecules and organisms under `presentation/workouts/components`;
  - new `presentation/workouts/workout-review.ts` (pure helpers);
  - three new atoms;
  - `src/i18n/{es,en}/workouts.json` (`review.*` keys).
- **Tests:**
  - backoffice component tests for the review and the status branch;
  - unit tests for the helpers;
  - the editor tests that open a `STARTED` workout move to the review.

  No new E2E flow.

- **API:** `GET /workouts/{id}` only, already in `core/openapi/v1.json`. No snapshot refresh. The start time, elapsed
  time and live state are a later API change planned in `api/`.
- **No new dependency.** The relative time uses `Intl.RelativeTimeFormat`.
