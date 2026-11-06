# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. Current state that shapes the approach:

- `core/openapi/v1.json` is already refreshed: `CreateWorkoutRequest.workoutDate` and `UpdateWorkoutRequest.workoutDate`
  are nullable, `GET /mesocycles/{id}/workouts` answers `WorkoutListItemResponse[]` (with `mesocycleId`,
  `mesocycleWeekNumber`, `athlete`, `trainer`), and `GET /workouts/{id}` answers the new `WorkoutByIdResponse`
  (`WorkoutDetailResponse` plus `athlete`, `trainer`). `PUT /workouts/{id}`, `PUT .../exercises/order` and
  `PUT .../status` still answer `WorkoutDetailResponse`.
- The API's `UpdateWorkoutUseCase` treats a missing placement pair as "make it standalone" and ignores the date of a
  placed workout. `workout-view.tsx`'s `toUpdate` sends no placement today, so every save unplaces a placed workout.
- `validateWorkoutDetailsForm` (`core/src/application/validation/workout-rules.ts`) requires `workoutDate`; the week
  dialog has no date.
- `mesocycle-view.tsx` holds the loaded mesocycle in `useMesocycle` and its open dialog as a discriminated union;
  `MesocycleWeeksCard` renders `MesocycleWeekListItem` rows.
- The mesocycles area must not import from `presentation/workouts` (and vice versa); shared pieces live in `common`.
- Prerequisite: `add-backoffice-mesocycle-screens` is archived before this change, so its specs exist in
  `openspec/specs/`.

## Goals / Non-Goals

**Goals:**

- Every placement-related decision (which workouts belong, the placed date, whether a placement fits) stays in the API.
- The workout screen never drops a placement it read.
- Existing atoms and molecules are reused; new UI is the minimum: a collapse atom, two molecules, one organism.

**Non-Goals:**

- No change to the standalone create dialog, `useWorkouts` or the "Entrenamientos asignados" table.
- No change to the HTTP error mapper (no new code).
- No app screen; the app only registers the new use case.
- No change to test helpers or test patterns beyond adding the new key to the existing container placeholders.

## Decisions

### 1. Core: a new alias for the read, a new use case for the list

```
core/src/domain/workouts/workout.ts
  + export type WorkoutById = components["schemas"]["WorkoutByIdResponse"];
core/src/domain/mesocycles/mesocycle.ts
  + export type GetMesocycleWorkoutsInput = { mesocycleId: string };
core/src/application/ports/workout-repository-interface.ts
  getById(id): Promise<Result<WorkoutById, DomainError>>           (was WorkoutDetail)
core/src/application/ports/mesocycle-repository-interface.ts
  + getWorkouts(mesocycleId): Promise<Result<WorkoutListItem[], DomainError>>
core/src/infrastructure/repositories/mesocycle-repository.ts
  + getWorkouts -> GET /mesocycles/{id}/workouts, ok / fail(mapHttpError), as getById
core/src/application/use-cases/workouts/get-workout-by-id-use-case.ts   returns WorkoutById
core/src/application/use-cases/mesocycles/get-mesocycle-workouts-use-case.ts   new, delegates to getWorkouts
core/src/application/dependencies.ts
  getWorkoutByIdUseCase: UseCaseInterface<GetWorkoutByIdInput, WorkoutById>
  + getMesocycleWorkoutsUseCase: UseCaseInterface<GetMesocycleWorkoutsInput, WorkoutListItem[]>
core/src/application/validation/workout-rules.ts
  + WeekWorkoutFormValues { name; description } and validateWeekWorkoutForm (name required ≤ 256 trimmed,
    description ≤ 1024), reusing the existing limits; validateWorkoutDetailsForm is unchanged
```

`getWorkouts` sits on the mesocycle repository because the resource is `/mesocycles/{id}/...`, like `getByAthlete`
sits where its path's subject is. _Alternative_: a `getByMesocycle` on the workout repository. Rejected: the
repositories in `core` follow the route's resource, and the reference (`getById`) is in the mesocycle repository.

`WorkoutById` is a separate alias rather than widening `WorkoutDetail`, since the update, reorder and status responses
really are `WorkoutDetailResponse`. `WorkoutById` is assignable to `WorkoutDetail`, so existing callers keep compiling.

### 2. Wiring

- `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`: register
  `getMesocycleWorkoutsUseCase: new GetMesocycleWorkoutsUseCase(mesocycleRepository)`.
- Both `test/infrastructure/container.test.ts` cover the new key; both `test/support/render-with-providers.tsx` get the
  new key in their existing placeholder map (no new helper).

### 3. Mesocycle screen: workouts held beside the mesocycle

```
backoffice/src/presentation/common/components/atoms/collapse.tsx             MUI Collapse
backoffice/src/presentation/mesocycles/hooks/use-mesocycle-workouts.ts       status, workoutsByWeek, errorCode, reload
backoffice/src/presentation/mesocycles/hooks/use-create-week-workout.ts      creating, errorCode, create, reset
backoffice/src/presentation/mesocycles/components/molecules/mesocycle-week-workout-item.tsx   link to /workout?id=
backoffice/src/presentation/mesocycles/components/molecules/mesocycle-week-list-item.tsx      extended
backoffice/src/presentation/mesocycles/components/organisms/mesocycle-weeks-card.tsx          extended
backoffice/src/presentation/mesocycles/components/organisms/create-week-workout-dialog.tsx    new
```

- `useMesocycleWorkouts(id)` follows `use-workouts.ts` (generation guard, `runWithSession`, reload). It exposes the
  workouts grouped into a `Map<number, WorkoutListItem[]>` keyed by `mesocycleWeekNumber`, keeping the API's order
  inside each week. It is called from `LoadedMesocycle`, so it starts only once the mesocycle is shown and never runs in
  create mode.
- `MesocycleWeeksCard` receives `workouts: { status; byWeek; errorCode; onRetry }` and `onCreateWorkout(weekNumber)`.
  While the workouts load, rows render as today with no count and no button. On failure the card shows `LoadError`
  under its header; rows stay plain.
- `MesocycleWeekListItem` gains `workouts?: { id; name; href }[]`, `countLabel`, `toggleLabel`, `createLabel`,
  `onCreate`. With workouts it keeps the current row content, adds the count and an expand `IconButton` with
  `aria-expanded` beside the edit button, and renders the workouts and the create button below in a `Collapse`
  (collapsed on mount). Without workouts it renders the current row plus the create button. The expanded flag is local
  state of the row, so it resets on screen exit.
  _Alternative_: MUI `Accordion`. Rejected during implementation: its summary renders a `<button>` inside an `<h3>`, so
  the row's edit button would be a button nested in a button.
- `OpenDialog` in `mesocycle-view.tsx` gains `{ kind: "workout"; weekNumber: number }`. `CreateWeekWorkoutDialog`
  mirrors `create-workout-dialog.tsx` (form state, `validateWeekWorkoutForm`, inline error, Cancelar / Crear) without
  pickers or week field. On save the view builds the request from the loaded mesocycle:
  `{ name, description?, mesocycleId, mesocycleWeekNumber, userId: mesocycle.athleteId, trainerId? }` with `trainerId:
mesocycle.trainerId` only when `role === "ADMIN"`, and no `workoutDate`. On success it `router.push`es to
  `/workout?id=<id>`, as the standalone create flow does.
- `useCreateWeekWorkout` duplicates `use-create-workout.ts` in the mesocycles area. _Alternative_: move the hook to
  `common`. Rejected for now: it is ten lines, and a move would touch the workouts area beyond this change's scope.

### 4. Workout screen: placement-aware header, strip and save

```
backoffice/src/presentation/workouts/hooks/use-workout.ts                    holds WorkoutById
backoffice/src/presentation/workouts/hooks/use-workout-mesocycle.ts          the mesocycle of a placed workout
backoffice/src/presentation/workouts/components/molecules/workout-context-strip.tsx
backoffice/src/presentation/workouts/components/organisms/workout-details-card.tsx   weekLocked prop
backoffice/src/presentation/workouts/views/workout-view.tsx
```

- `WorkoutHeader` takes `backHref`: `/mesocycle?id=<mesocycleId>` when `workout.mesocycleId` is set, else
  `/assigned-workouts`. Derived from the loaded workout, not from navigation history, so it holds on refresh and deep
  links. `WorkoutNotFound` keeps `/assigned-workouts` (nothing was read).
- `WorkoutContextStrip` (molecule over `Typography` and `Link` atoms) receives already-resolved texts: athlete name,
  trainer name or `null`, and `mesocycle: { label; href } | "loading" | null`. Names use `fullNameOf(person) ??
person.email`. The view passes the trainer only when `role === "ADMIN"`.
- `useWorkoutMesocycle(mesocycleId | null)` reads `getMesocycleByIdUseCase` once when the id is set; it returns
  `loading`, the mesocycle, or `failed` (the strip then drops the entry). No retry: the entry is informative only.
- The view's loaded workout becomes `WorkoutById`. The view never replaces it with a save answer (the update answer
  only raises the saved flag, a reorder answers success, and this screen changes no status), so the profiles read on
  open stay shown without any merge. _Found during implementation_: the planned merge of profiles into
  `WorkoutDetail` answers was dropped as unneeded.
- `toUpdate` always sends `mesocycleId` and `mesocycleWeekNumber` as read. For a placed workout it sends no
  `workoutDate`; for a standalone one it sends `weekMiddleOf(values.workoutDate)` as today. The form still holds the
  loaded week for a placed workout, so `validateWorkoutDetailsForm` keeps passing unchanged.
- `WorkoutDetailsCard` gets `weekLocked?: boolean`, passed to `WeekField` as `disabled` for a placed workout.

### 5. Copy

`backoffice/src/i18n/{es,en}/mesocycles.json`:

| Key                                        | es                                                 | en                                     |
| ------------------------------------------ | -------------------------------------------------- | -------------------------------------- |
| `weeks.newWorkout`                         | Nuevo entrenamiento                                | New workout                            |
| `weeks.workoutCount_one` / `_other`        | {{count}} entrenamiento / {{count}} entrenamientos | {{count}} workout / {{count}} workouts |
| `weekWorkout.title`                        | Nuevo entrenamiento · Semana {{number}}            | New workout · Week {{number}}          |
| `weekWorkout.fields.name` / `.description` | Nombre / Descripción                               | Name / Description                     |
| `weekWorkout.actions.cancel` / `.create`   | Cancelar / Crear                                   | Cancel / Create                        |

`backoffice/src/i18n/{es,en}/workouts.json`:

| Key                             | es                           | en                         |
| ------------------------------- | ---------------------------- | -------------------------- |
| `workout.context.athlete`       | Atleta                       | Athlete                    |
| `workout.context.trainer`       | Entrenador                   | Trainer                    |
| `workout.context.mesocycle`     | Mesociclo                    | Mesocycle                  |
| `workout.context.mesocycleWeek` | {{name}} · Semana {{number}} | {{name}} · Week {{number}} |

Validation messages reuse the existing per-violation keys of each namespace; error messages reuse the existing per-code
messages. Translation parity covers every key.

### 6. Errors

| Failure                                          | Code                     | Where                                             |
| ------------------------------------------------ | ------------------------ | ------------------------------------------------- |
| Session expired                                  | `UNAUTHORIZED`           | existing session handling                         |
| Not the mesocycle's trainer, no relationship     | `FORBIDDEN`              | week workout dialog, inline                       |
| Invalid field, half-set placement                | `VALIDATION`             | dialog, inline                                    |
| Mesocycle gone                                   | `NOT_FOUND`              | dialog inline; workouts list `LoadError`          |
| Athlete / trainer mismatch, week beyond duration | `CONFLICT`               | dialog, inline                                    |
| No response / anything else                      | `NETWORK` / `UNEXPECTED` | `LoadError` with retry on reads, inline on create |

All codes already exist in the mapper and are translated in `es` and `en`.

### 7. Tests

- core: `get-mesocycle-workouts-use-case.test.ts`, `mesocycle-repository.test.ts` (`getWorkouts`: success, 403, 404,
  401, 500, network), `workout-repository.test.ts` and `get-workout-by-id-use-case.test.ts` fixtures carry the profiles,
  `create-workout-use-case.test.ts` placed-without-date case, `workout-rules.test.ts` for `validateWeekWorkoutForm`.
- backoffice component: `mesocycle-view.test.tsx` and `workout-view.test.tsx`, with fakes through the container.
- backoffice E2E: `mesocycles.spec.ts`, with `e2e/support/mesocycles-api.ts` serving `GET /mesocycles/{id}/workouts`
  and `POST /workouts`, and the workout read answering with profiles.

## Risks / Trade-offs

- [Expand and edit sit side by side in the row] → each is its own button with its own accessible name; tests assert
  that clicking edit opens the week dialog without expanding.
- [The strip's mesocycle read is an extra request per placed workout] → one read, only when placed; it never blocks the
  editor and is dropped on failure.
- [Profiles shown are those read on open] → this screen sends athlete and trainer as read, so they cannot change
  through it; a reassignment elsewhere shows on the next load.
- [Duplicated create hook in two areas] → small and mirrored from the same reference; a later change can move it to
  `common` if a third caller appears.
- [Archive order] → archiving this change before `add-backoffice-mesocycle-screens` would fail on the modified
  `backoffice-workout-editor` header; archive that one first.
