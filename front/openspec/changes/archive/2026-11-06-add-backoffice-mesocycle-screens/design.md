# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. Current state that shapes the approach:

- `core` already exposes `Mesocycle`, `MesocycleWeek`, `TrainingWeekType` (generated enum) and the seven mesocycle use
  cases; both `container.ts` register them, and `render-with-providers.tsx` already has placeholders for the keys.
- The backoffice workouts area is the nearest reference: `assigned-plans-view.tsx` + `workouts-table.tsx` +
  `use-workouts.ts` (list), `workout-view.tsx` + `workout-details-card.tsx` + `workout-exercises-card.tsx` (editor),
  `exercise-configuration-dialog.tsx` (modal form), `create-workout-dialog.tsx` + `use-workout-assignees.ts` +
  `person-picker.tsx` (who the item is for), and `core/src/application/validation/workout-rules.ts` (structural rules).
- The drawer selects an item by `pathname === href`.
- Copy is split by domain area namespace and registered in `backoffice/src/i18n/resources.ts`.

## Goals / Non-Goals

**Goals:**

- Reuse every existing atom and molecule; add only area-level molecules and organisms.
- Keep the workouts list byte-for-byte in behaviour, only relocated.
- One editor view serving both create and edit, chosen by the presence of `id`.

**Non-Goals:**

- No change to `core` use cases, ports, repositories, `Dependencies` or containers.
- No change to the app, the API or the HTTP error mapper.
- No week deletion, reordering or workout counts (not in the API).

## Decisions

### 1. Workouts list moves, the route meaning changes

`presentation/workouts/views/assigned-plans-view.tsx` is renamed `assigned-workouts-view.tsx`
(`AssignedWorkoutsView`) and served by a new `app/assigned-workouts/page.tsx`. Its keys in `workouts.json` move from
`assignedPlans.*` to `assignedWorkouts.*` (title "Entrenamientos" / "Workouts"). `workout-view.tsx`
links and `workout.backToList` point to `/assigned-workouts` ("Volver a entrenamientos").
`app/assigned-plans/page.tsx` then default-exports the new mesocycles `AssignedPlansView`.

_Alternative_: keep workouts at `/assigned-plans` and put mesocycles at `/assigned-mesocycles`. Rejected: the drawer
label "Mesociclos" is what the user wants for mesocycles, and route names mirror drawer labels.

### 2. New `mesocycles` domain area in the backoffice

```
backoffice/src/app/assigned-plans/page.tsx              route (rewired)
backoffice/src/app/mesocycle/page.tsx                   route
backoffice/src/presentation/mesocycles/
  views/assigned-plans-view.tsx                         AssignedPlansView
  views/mesocycle-view.tsx                              MesocycleView (create + edit)
  hooks/use-mesocycles.ts                               list: status, mesocycles, errorCode, reload
  hooks/use-mesocycle.ts                                one by id: status, mesocycle, errorCode, reload, replace
  hooks/use-create-mesocycle.ts                         create: status, errorCode, create(values)
  hooks/use-update-mesocycle.ts                         update: status, errorCode, saved, update(values)
  hooks/use-save-mesocycle-week.ts                      define or redefine: status, errorCode, save(input)
  hooks/use-mesocycle-date.ts                           start date as text in the active language
  components/molecules/training-week-type-chip.tsx      chip atom + theme colour per type
  components/molecules/duration-field.tsx               icon-button atoms − / text-field atom / +
  components/molecules/mesocycle-week-list-item.tsx     chip, "Semana N", objective, edit icon button
  components/organisms/mesocycles-table.tsx             table atoms, role column, empty state, edit action
  components/organisms/mesocycle-details-card.tsx       card with fields, pickers (create), read-only people (edit)
  components/organisms/mesocycle-weeks-card.tsx         card, "Añadir semana", list or empty/locked text
  components/organisms/mesocycle-week-dialog.tsx        dialog atoms, type select, objective, cancel/save
```

Atomic levels and imports: molecules import atoms only; organisms import atoms and molecules; views import organisms,
molecules and hooks. Only atoms import MUI widgets; MUI icons and layout primitives (`Box`, `Stack`, `Container`) are
allowed at any level, as today. The type select uses the existing `text-field` atom with `select` and the `menu-item`
atom, as other forms do; no new atom.

`useSaveMesocycleWeek` covers both define and redefine (the dialog knows which), mirroring `useSaveWorkoutExercise`.
Every mutation hook returns the `Mesocycle` the API answered, and the view passes it to `useMesocycle().replace`, so the
screen never re-reads after a write.

### 3. One view, two modes

`MesocycleView` reads `id` with `useSearchParams` inside `Suspense` (as `WorkoutView`). Without `id` it renders create
mode: `useMesocycle` is not started, the details card shows the pickers and "Crear mesociclo", the weeks card is
locked. After a successful creation it calls `router.replace("/mesocycle?id=<id>")`; the view re-renders in edit mode
and reads the mesocycle once.

_Alternative_: a create dialog like `CreateWorkoutDialog`. Rejected: the creation requires every general field (name,
objective, start date, duration), so the dialog would duplicate the whole card.

_Alternative_: allow planning weeks before the first save and send them in a batch. Rejected: weeks are keyed by the
mesocycle id and the batch would not be atomic.

### 4. Pickers move to `common`

`PersonPicker` moves to `presentation/common/components/molecules/person-picker.tsx` and `useWorkoutAssignees` to
`presentation/common/hooks/use-assignees.ts` (`useAssignees`, type `Assignee`), per the invariant that a molecule moves
to `common` when a second area needs it. `create-workout-dialog.tsx` and its tests update their imports only; behaviour
is unchanged. The hook keeps its role handling: trainer's current athletes, or for an admin one users read split into
active athletes and trainers.

### 5. Structural rules in `core/src/application/validation/mesocycle-rules.ts`

Mirrors `workout-rules.ts`: `MESOCYCLE_NAME_MAX_LENGTH = 256`, `MESOCYCLE_OBJECTIVE_MAX_LENGTH = 4000`,
`MESOCYCLE_DURATION_MIN = 1`, `MESOCYCLE_DURATION_MAX = 52`; `MesocycleDetailsFormValues` (`name`, `objective`,
`startDate`, `durationInWeeks` as strings), `CreateMesocycleFormValues` adds `athleteId`, `trainerId`;
`validateMesocycleDetailsForm`, `validateCreateMesocycleForm(values, { withTrainer })`; `MesocycleWeekFormValues`
(`trainingWeekType`, `objective`) and `validateMesocycleWeekForm`. Violations reuse `required`, `maxLength`, `date`,
`uuid`, plus `range` for the duration. Values are trimmed before measuring; `isCalendarDate` is reused.

The limits come from the API domain (`MesocycleName`, `MesocycleObjective`, `DurationInWeeks`) because the snapshot
declares none. This follows existing practice in `core` (see the proposal's conflicts section).

### 6. Next free week number is presentation

`nextFreeWeekOf(weeks, duration)` in `presentation/mesocycles/weeks.ts` returns the lowest number in `1..duration` with
no week, or `null`. It mirrors the loaded duration only; the API still rejects duplicates and weeks beyond the duration
with `CONFLICT`. Weeks are sorted by `weekNumber` for display without mutating the loaded mesocycle.

### 7. Dates and numbers

- The start date is shown with `Intl.DateTimeFormat(language, { day: "numeric", month: "short", year: "numeric",
timeZone: "UTC" })` over `YYYY-MM-DDT00:00:00Z`, the same technique as `useWeekLabel`, so it never shifts a day.
- The start date field is the `text-field` atom with `type="date"`; its value stays `YYYY-MM-DD`.
- `durationInWeeks` is typed `number | string` by the generator; it is read with `Number(...)` and sent as a number.
- Duration and week counts use i18next plurals (`duration_one` / `duration_other`).

### 8. Role branches

`useCurrentUser().profile.role` decides: the trainer column in `MesocyclesTable` (`showTrainer`), the trainer picker
and `withTrainer` in create mode, and the read-only trainer line in edit mode. The list waits for the role before
rendering, as `AssignedPlansList` does today. The create request omits `trainerId` for a `TRAINER`.

### 9. Training week type colours

`TrainingWeekTypeChip` maps each `TrainingWeekType` to a theme palette colour (for example `info` for ADAPTATION,
`success` for ACCUMULATION, `warning` for INTENSIFICATION, `error` for REALIZATION, `secondary` for DELOAD, `default`
for RECOVERY) with the chip's soft variant; no hex values. Labels come from `mesocycles:trainingWeekType.<VALUE>`.

### 10. Held state

| Hook                   | Holds                                   | Changed by                            | Resets               |
| ---------------------- | --------------------------------------- | ------------------------------------- | -------------------- |
| `useMesocycles`        | list, status, errorCode                 | mount, `reload`                       | screen exit          |
| `useMesocycle`         | mesocycle, status, errorCode            | mount, `reload`, `replace`            | screen exit          |
| `useCreateMesocycle`   | status, errorCode                       | `create`                              | screen exit          |
| `useUpdateMesocycle`   | status, errorCode, `saved` flag         | `update`; a field edit clears `saved` | screen exit          |
| `useSaveMesocycleWeek` | status, errorCode                       | `save`                                | dialog close         |
| `useAssignees`         | athletes, trainers, status, errorCode   | mount (create mode), `reload`         | screen exit          |
| View-local form state  | details values/violations, dialog state | the user                              | save / cancel / exit |

Every hook ignores answers that arrive after unmount (generation counter, as `useWorkoutAssignees`). Nothing is kept
after logout; `UNAUTHORIZED` follows the existing session handling through `runWithSession`.

### 11. Errors and copy

Error codes involved: `UNAUTHORIZED`, `FORBIDDEN`, `VALIDATION`, `NOT_FOUND`, `CONFLICT`, `NETWORK`, `UNEXPECTED`; all
already mapped and translated in `errors.json` (`es` and `en`). `NOT_FOUND` on the mesocycle read renders the
not-found state instead of `LoadError`. No new code, no mapper change.

New namespace `mesocycles` (`es` / `en`), registered in `resources.ts`:

- `assignedPlans`: `title`, `newMesocycle`, `columns.{name,athlete,trainer,startDate,duration,weeks}`, `empty`, `edit`,
  `duration_one|other` ("{{count}} semana(s)" / "{{count}} week(s)"), `definedWeeks` ("{{defined}} / {{total}}").
- `mesocycle`: `createTitle`, `editTitle`, `subtitle`, `back`, `notFound`, `backToList`, `saved`,
  `details.{title,create,save}`, `fields.{athlete,trainer,name,startDate,duration,objective}`,
  `duration.{decrease,increase}`, `validation.{required,maxLength,date,range,athleteRequired,trainerRequired}`,
  `noAthletes`, `noTrainers`, `retry`.
- `weeks`: `title`, `subtitle`, `add`, `week` ("Semana {{number}}"), `edit` ("Editar semana {{number}}"), `empty`,
  `saveFirst`, `dialog.{type,objective,cancel,save}`.
- `trainingWeekType.{ADAPTATION,ACCUMULATION,INTENSIFICATION,REALIZATION,DELOAD,RECOVERY}`.

`common.json`: `menu.programming.assignedWorkouts` ("Entrenamientos" / "Workouts").
`workouts.json`: `assignedPlans.*` → `assignedWorkouts.*`; `workout.backToList` reworded.

### 12. Drawer

`navigation-drawer.tsx` adds `{ label: "menu.programming.assignedWorkouts", icon: ListAltOutlined, href:
"/assigned-workouts" }` after "Mesociclos". Exact-path selection already keeps the two entries apart.

### 13. Workouts list outside mesocycles

The workouts screen asks `GET /workouts?standalone=true`; the API applies the filter after its role scoping, so it can
only narrow what the caller already sees. The front does not filter rows: `WorkoutListItemResponse` carries no
placement, and the API stays the source of truth for which workouts belong to a plan.

_Alternative_: expose `mesocycleId` on the list item and hide rows in the front. Rejected: the front would start
filtering rows and still download every placed workout.

### 14. Tests

Following the closest test of each tier, Arrange / Act / Assert, API mocked:

- core: `core/test/application/validation/mesocycle-rules.test.ts`, shaped like `workout-rules.test.ts`.
- backoffice component: `test/presentation/workouts/views/assigned-workouts-view.test.tsx` (moved from
  `assigned-plans-view.test.tsx`); `test/presentation/mesocycles/views/assigned-plans-view.test.tsx`;
  `test/presentation/mesocycles/views/mesocycle-view.test.tsx` (create per role, validation, navigation, failure; edit
  load, update, saved, 409; weeks list, next free week, define, redefine, failures, cancel; not-found; retry);
  `mesocycles/weeks.test.ts`; drawer test; existing create workout dialog tests still pass after the move.
- backoffice E2E (`page.route`): `e2e/assigned-workouts.spec.ts` (from the workout part of `assigned-plans.spec.ts`,
  which is rewritten for mesocycles) and `e2e/mesocycles.spec.ts` (drawer → list → create → add a week → edit it).
  `e2e/workout.spec.ts` updated if it asserts the back link.
- Translation parity covers the new namespace.

## Risks / Trade-offs

- [Bookmarks or tests pointing at `/assigned-plans` for workouts break] → All in-repo links and tests are updated in
  this change; the drawer offers the new entry.
- [Duplicated limits drift from the API] → Constants are named after the API value objects; a `VALIDATION` answer is
  still shown if they drift.
- [A user lowers the duration below a defined week] → The API answers `CONFLICT`; the screen shows it and keeps the
  typed value (spec scenario).
- [Moving `PersonPicker` touches the workouts area] → Imports only; existing create workout tests guard behaviour.
- [`router.replace` after create re-mounts data hooks] → One extra read of the new mesocycle; acceptable and keeps edit
  mode identical whether reached from the table or after creation.

## Migration Plan

Front-only, shipped as one static export. Rollback is reverting the change; no data or contract migration.
