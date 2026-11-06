# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-backoffice-mesocycle-screens.md`.

## Why

`core` already offers every mesocycle operation and both containers register them, but no screen uses them: trainers
and admins can only create loose workouts and cannot plan a training block. The "Mesociclos" entry also shows
workouts, which is the wrong name now that plans (mesocycles) exist.

## What Changes

- New drawer entry **"Entrenamientos"** (`/assigned-workouts`), placed after "Mesociclos" and before
  "Calendario". It shows today's workouts table and "Nuevo entrenamiento" button, now limited to the workouts not placed in a
  mesocycle (`GET /workouts?standalone=true`).
- **BREAKING (route meaning)**: `/assigned-plans` stops listing workouts and lists **mesocycles** (`GET /mesocycles`),
  with a "Nuevo mesociclo" button and an edit action per row. The workout screen's "Volver" and not-found links move to
  `/assigned-workouts`.
- New create/edit mesocycle screen at `/mesocycle` (create) and `/mesocycle?id=<id>` (edit), built on the workout
  screen's layout (page header with "Volver", stacked full-width cards, save inside the card), loosely inspired by
  `panel_mesocycle.png`:
  - "Información general" card: athlete, trainer (`ADMIN` only, create only), name, start date, duration in weeks
    (stepper), general objective.
  - "Planificación semanal" card: defined weeks sorted by number (type chip, "Semana N", objective, edit action) and
    "Añadir semana"; a dialog defines or redefines a week (type + objective). Disabled in create mode until saved.
- Structural rules for the mesocycle and week forms (required, trimmed lengths, date, 1–52 weeks), mirroring the API's
  limits.
- `PersonPicker` and the assignees hook move from `presentation/workouts` to `presentation/common`, since a second
  area now needs them.

### Role behaviour (backoffice only)

- `ADMIN` and `TRAINER` both list, create and edit mesocycles and weeks.
- `ADMIN` sees the trainer column in the mesocycles table and picks a trainer on create.
- `TRAINER` must NOT see the trainer column (header nor cells) nor the trainer picker, and never sends `trainerId`.
- Rows are not filtered by role; the API decides what each role gets.

The app does not change.

### Conflicts with the template and invariants

- **Validation location.** `STPR_INVARIANTS.md` says structural validation "lives in presentation", but every existing
  form keeps its rules in `core/src/application/validation/` (`workout-rules.ts`, `exercise-rules.ts`, ...). This
  change follows that established practice with `mesocycle-rules.ts`; it adds no new deviation. The brief said "Core:
  none"; this file is the only `core` addition, and no use case, port or repository changes.
- **Image vs. API.** Deleting a week, reordering weeks and the per-week workout count shown in the image are not
  offered by the API and are out of scope. No stop case: every endpoint used is in `core/openapi/v1.json`, and no
  business rule is added.
- **Image vs. template.** The image's sticky "Cancelar / Guardar mesociclo" footer is not adopted; the workout screen's
  in-card save and "Volver" link are used instead.

### Canonical references

| Capability                     | Reference (backoffice only)                                                                                                                                                                                    |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `backoffice-assigned-workouts` | current `workouts/views/assigned-plans-view.tsx` + `workouts-table.tsx`, moved                                                                                                                                 |
| `backoffice-assigned-plans`    | same view and table shape; `use-workouts.ts` for the list hook                                                                                                                                                 |
| `backoffice-mesocycle-editor`  | `workouts/views/workout-view.tsx`, `workout-details-card.tsx`, `workout-exercises-card.tsx`, `exercise-configuration-dialog.tsx`, `create-workout-dialog.tsx`; rules in `core/.../validation/workout-rules.ts` |
| `backoffice-navigation`        | `navigation-drawer.tsx`, extended in place                                                                                                                                                                     |
| `backoffice-workout-editor`    | `workout-view.tsx`, links only                                                                                                                                                                                 |

Atom/molecule/organism references are `pending` in `.stpr/template/README.md`; the shapes of the workouts area apply.

### Components and atomic levels

- Reused atoms: `button`, `icon-button`, `card`, `card-content`, `chip`, `dialog*`, `table*`, `text-field`,
  `typography`, `autocomplete`, `link`, `skeleton`. Reused molecules: `load-error`, `home-skeleton`, `person-picker`
  (moved to `common`). No new atom.
- New molecules (mesocycles): `TrainingWeekTypeChip`, `DurationField`, `MesocycleWeekListItem`.
- New organisms (mesocycles): `MesocyclesTable`, `MesocycleDetailsCard`, `MesocycleWeeksCard`, `MesocycleWeekDialog`.
- Workouts area: `AssignedPlansView` renamed `AssignedWorkoutsView`; `WorkoutsTable` copy keys move to
  `assignedWorkouts.*`.

## Capabilities

### New Capabilities

- `backoffice-assigned-workouts`: the workouts table at `/assigned-workouts`, with its states, role column, edit action
  and "Nuevo entrenamiento" button (requirements moved from `backoffice-assigned-plans`).
- `backoffice-mesocycle-editor`: the create/edit mesocycle screen, its general information form, weekly planning and
  week dialog.

### Modified Capabilities

- `backoffice-assigned-plans`: the screen now lists mesocycles instead of workouts, with "Nuevo mesociclo" and an
  edit action per row.
- `backoffice-navigation`: the drawer gains "Entrenamientos", which links to `/assigned-workouts`.
- `backoffice-workout-editor`: "Volver" and the not-found link lead to `/assigned-workouts`.

## Impact

- **core**: `core/src/application/validation/mesocycle-rules.ts` (+ test, barrel export). `GetWorkoutsUseCase`,
  `WorkoutRepositoryInterface.getAll` and `WorkoutRepository` take an optional `GetWorkoutsInput` (`{ standalone?: boolean }`, an input
  type like `GetWorkoutByIdInput`), sent as `GET /workouts?standalone=true`. No new `Dependencies` key.
- **backoffice**: routes `app/assigned-workouts/`, `app/assigned-plans/` (rewired), `app/mesocycle/`; new
  `presentation/mesocycles/` area (views, hooks, molecules, organisms); `presentation/workouts/` view rename and link
  updates; `PersonPicker` and the assignees hook moved to `presentation/common/`; drawer entry; new `mesocycles` i18n
  namespace in `es` and `en`, `common` drawer key, `workouts` keys renamed.
- **Tests**: backoffice component tests (both list views, mesocycle view, drawer, moved picker imports), core
  validation test, E2E `assigned-workouts.spec.ts` (replaces the workout part of `assigned-plans.spec.ts`) and
  `mesocycles.spec.ts`, translation parity.
- **API contract**: `GET /mesocycles`, `GET /mesocycles/{id}`, `POST /mesocycles`, `PUT /mesocycles/{id}`,
  `POST /mesocycles/{id}/weeks`, `PUT /mesocycles/{id}/weeks/{weekNumber}`, plus the people endpoints the pickers
  already use, and `GET /workouts?standalone=true`. The `standalone` query parameter comes from the API change
  `list-standalone-workouts` (`api/openspec/changes/`) (`GetAllWorkoutsUseCase` keeps only workouts with no mesocycle, after the role scoping) and the snapshot is
  refreshed for it.
- **app**, HTTP error mapper: unchanged. No new dependency.
