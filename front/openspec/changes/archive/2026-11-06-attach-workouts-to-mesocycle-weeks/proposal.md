# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-attach-workouts-to-mesocycle-weeks.md`.

## Why

Mesocycles and their weeks can be planned in the backoffice, but a week is still an empty label: workouts can only be
created standalone and the placement pair (`mesocycleId` + `mesocycleWeekNumber`) is never sent. Worse, saving a placed
workout on the workout screen today sends no placement, and the API reads that as "leave it standalone", so the workout
is silently unplaced. The API now accepts placed workouts without a date, lists a mesocycle's workouts and returns the
athlete and trainer with a workout, so the backoffice can close the loop.

## What Changes

- **Weekly planning holds workouts.** In edit mode, every defined week of "Planificación semanal" offers "Nuevo
  entrenamiento". A week with workouts becomes a collapsible row, collapsed by default, whose content lists its
  workouts and the same button; a week without workouts stays a plain row with the button.
- **Create a workout in a week.** The button opens a dialog asking only Nombre and Descripción. The creation carries
  the mesocycle id, the week number, the mesocycle's athlete as `userId`, the mesocycle's trainer as `trainerId` for an
  `ADMIN` only, and no date. On success the visitor lands on the new workout's screen.
- **Workout items open the workout.** Each listed workout links to `/workout?id=<id>`.
- **Workout screen context.** Between "Volver" and the properties card, the screen shows the athlete's full name
  (always), the trainer's full name (`ADMIN` only) and, for a placed workout, the mesocycle name and "Semana N".
- **Workout screen back link.** "Volver" leads to `/mesocycle?id=<mesocycleId>` for a placed workout and to
  `/assigned-workouts` for a standalone one.
- **Placed workouts keep their placement.** Guardar sends the placement it read. For a placed workout the week field is
  shown read-only (the week dates it) and no date is sent; for a standalone one, the date is sent as today.
- **Core.** New `GetMesocycleWorkoutsUseCase` over `GET /mesocycles/{id}/workouts`. `getWorkoutByIdUseCase` returns the
  new `WorkoutById` alias (detail plus `athlete` and `trainer`).
- The standalone create dialog and the "Entrenamientos asignados" table are unchanged.

## Fronts, roles and references

- **backoffice** -> `ADMIN` and `TRAINER` create and open workouts in weeks and see the context strip. A `TRAINER`
  must NOT see the trainer's name in the strip and never sends `trainerId`. Role branches read
  `useCurrentUser().profile.role`.
- **app** -> nothing changes on screen. Its `container.ts` registers the new use case, as it registers every mesocycle
  use case.
- **Canonical references.** UI references come from the backoffice only; atom, molecule and organism references are
  `pending` in `.stpr/template/README.md`, so the shapes of the mesocycles and workouts areas are followed:
  - weeks card and week row -> their current versions, extended in place;
  - collapsible row -> new `collapse` atom in `common/components/atoms` (only atoms import MUI widgets);
  - week workout item (molecule) -> the row navigation of `workouts-table.tsx`;
  - week workout dialog (organism) and its hook -> `create-workout-dialog.tsx` + `use-create-workout.ts`, duplicated in
    the mesocycles area, not imported across areas;
  - workouts-of-a-mesocycle hook -> `use-workouts.ts`;
  - context strip (molecule) -> the read-only names of `mesocycle-details-card.tsx`;
  - `GetMesocycleWorkoutsUseCase` and its repository method -> `get-mesocycle-by-id-use-case.ts` and
    `MesocycleRepository.getById`.
- **Reused:** `Button`, `Card`, `List`, `ListItem`, `IconButton`, `Typography`, `Link`, `Dialog*`, `TextField` atoms;
  `TrainingWeekTypeChip`, `LoadError`, `HomeSkeleton` molecules; `fullNameOf`.

## API contract

All endpoints are in `core/openapi/v1.json` (refreshed before this change):

- `GET /mesocycles/{id}/workouts` -> `WorkoutListItemResponse[]` with `mesocycleWeekNumber`.
- `POST /workouts` -> `CreateWorkoutRequest`, only `name` required; `workoutDate` nullable.
- `PUT /workouts/{id}` -> `UpdateWorkoutRequest`, `name` and `userId` required; answers `WorkoutDetailResponse`.
- `GET /workouts/{id}` -> `WorkoutByIdResponse` (detail plus `athlete`, `trainer`).
- `GET /mesocycles/{id}` -> mesocycle name, athlete and trainer.

No business rule enters the front: which workouts belong to a mesocycle, whether a placement fits and a placed
workout's date are the API's. Grouping by week number is presentation. Validation is structural only (name required,
lengths as in the standalone dialog).

## Conflicts and prerequisites

- **Prerequisite:** change `add-backoffice-mesocycle-screens` is fully implemented but not archived. Its specs
  (`backoffice-mesocycle-editor`, the `backoffice-workout-editor` header and states) must be archived first, because
  this change modifies them.
- No conflict with the template, the invariants or a stop case.

## Capabilities

### New Capabilities

- `backoffice-workout-context`: the context strip on the workout screen (athlete, admin-only trainer, mesocycle and
  week).

### Modified Capabilities

- `backoffice-mesocycle-editor`: defined weeks list their workouts in collapsible rows and offer "Nuevo entrenamiento"
  with a name-and-description dialog.
- `backoffice-workout-editor`: the header's "Volver" follows the placement (the not-found link stays on
  `/assigned-workouts`, since no workout was read); Guardar keeps the placement and, for a placed workout, shows the week
  read-only and sends no date.
- `mesocycle-management`: reading a mesocycle's workouts.
- `workout-management`: reading a workout by id returns its athlete and trainer; creating a placed workout needs no date.

## Impact

- **core:** `domain/workouts/workout.ts` (new `WorkoutById` alias, `GetMesocycleWorkoutsInput`), mesocycle repository
  contract and implementation, workout repository `getById`, `GetWorkoutByIdUseCase`, new `GetMesocycleWorkoutsUseCase`,
  `dependencies.ts`, use case index; tests for each.
- **backoffice:** `container.ts`; mesocycles area (weeks card, week row, new week workout item, new dialog, two hooks,
  `mesocycle-view.tsx`); workouts area (`workout-view.tsx`, `use-workout.ts`, `workout-details-card.tsx`, new context
  strip); new collapse atom; `es` / `en` copy; component tests and `mesocycles.spec.ts` E2E.
- **app:** `container.ts` registration and its container test only.
- **API:** none; no snapshot refresh in this change. No new error code.
