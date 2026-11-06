# Change Proposal: Attach Workouts To Mesocycle Weeks

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/attach-workouts-to-mesocycle-weeks/`.

## Change

Let a trainer or admin create workouts inside a mesocycle week from the mesocycle screen, list each week's workouts in a
collapsible row, and show on the workout screen who it is for and, when placed, which mesocycle week it belongs to, with
"Volver" returning to wherever the workout lives.

## Objective

Mesocycles and their weeks can be planned (change `add-backoffice-mesocycle-screens`), but a week is still an empty
label: workouts can only be created standalone, and the API's placement pair (`mesocycleId` + `mesocycleWeekNumber`) is
never sent. A training block is not usable until its weeks hold workouts.

After this change, every defined week in "Planificación semanal" offers "Nuevo entrenamiento"; a week with workouts
expands to list them; a placed workout is created with only name and description; and the workout screen tells the user
whose workout it is and where it sits.

## Capabilities

- Week rows in `MesocycleWeeksCard`:
  - week **with** workouts -> collapsible row (collapsed by default); its content lists that week's workouts and a
    "Nuevo entrenamiento" button;
  - week **without** workouts -> plain row, not collapsible, with a "Nuevo entrenamiento" button.
- Create a workout in a week: dialog asking only name and description; the request carries `mesocycleId`,
  `mesocycleWeekNumber` and the mesocycle's athlete as `userId` (and its trainer as `trainerId` for `ADMIN`), and no
  `workoutDate`.
- Create a standalone workout: unchanged (same dialog and fields as today, from "Entrenamientos asignados").
- Workout list items in a week navigate to `/workout?id=<id>`, as the standalone table already does.
- Workout screen context strip, between "Volver" and the workout properties card:
  - athlete full name (always);
  - trainer full name (`ADMIN` only);
  - mesocycle name + "Semana N" (only when the workout is placed), linking to the mesocycle.
- Workout screen "Volver" goes to `/mesocycle?id=<mesocycleId>` for a placed workout and to `/assigned-workouts` for a
  standalone one.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both create and open workouts in mesocycle weeks and see
  the context strip. `ADMIN` sees the trainer full name in the strip; `TRAINER` must NOT see it (no label, no value).
  `TRAINER` never sends `trainerId`, as today.
- app -> lands here: no -> no screen, hook, copy or wiring changes.

Role branches are presentation only, read from `useCurrentUser().profile.role`.

## Classification

Mesocycles domain area:

- `MesocycleWeeksCard` -> organism, extended in place -> `backoffice/src/presentation/mesocycles/components/organisms/mesocycle-weeks-card.tsx`
  (receives the workouts grouped by week number and the create callback)
- `MesocycleWeekListItem` -> molecule, extended in place -> `.../mesocycles/components/molecules/mesocycle-week-list-item.tsx`
  (collapsible only when it has workouts; holds the "Nuevo entrenamiento" button)
- `MesocycleWeekWorkoutItem` (name, status, link to the workout) -> molecule -> `.../mesocycles/components/molecules/mesocycle-week-workout-item.tsx`
- Collapse / accordion primitive -> atom, new if none exists -> `backoffice/src/presentation/common/components/atoms/`
- `CreateWeekWorkoutDialog` (name + description) -> organism -> `.../mesocycles/components/organisms/create-week-workout-dialog.tsx`
- `useMesocycleWorkouts` (workouts of one mesocycle, grouped by week) -> state hook -> `.../mesocycles/hooks/use-mesocycle-workouts.ts`
- `useCreateWeekWorkout` -> state hook -> `.../mesocycles/hooks/use-create-week-workout.ts`, over the existing
  `CreateWorkoutUseCase`

Workouts domain area:

- `WorkoutView` -> view, extended in place -> `backoffice/src/presentation/workouts/views/workout-view.tsx`
  (back link target and the context strip)
- `WorkoutContextStrip` -> molecule -> `.../workouts/components/molecules/workout-context-strip.tsx`
- `useWorkoutContext` (the mesocycle name for a placed workout; athlete and trainer come with the workout) -> state hook -> `.../workouts/hooks/use-workout-context.ts`
- Update builder in `workout-view.tsx` -> keeps sending `mesocycleId` + `mesocycleWeekNumber` as read, so editing a
  placed workout never unplaces it

Core:

- `Workout` domain aliases -> regenerated from the refreshed snapshot (no hand-written type)
- `WorkoutById` -> entity type (alias of the generated `WorkoutByIdResponse`) -> `core/src/domain/workouts/workout.ts`;
  `GetWorkoutByIdUseCase` and `WorkoutRepository.getById` return it instead of `WorkoutDetail`
- `GetMesocycleWorkoutsUseCase` -> use case -> `core/src/application/use-cases/mesocycles/`, over a new
  `getWorkouts(mesocycleId)` method on the mesocycle repository contract and implementation; registered in the
  backoffice `container.ts` (and the app's, as the other mesocycle use cases are)
- `CreateWorkoutUseCase` / `UpdateWorkoutUseCase` -> unchanged signature; the generated request type already makes
  `workoutDate` nullable

## Canonical Template Mapping

- `MesocycleWeeksCard` / `MesocycleWeekListItem` -> their current versions -> extended in place, same card and row shape.
- `MesocycleWeekWorkoutItem` -> `workouts/components/organisms/workouts-table.tsx` row -> same name, status chip and
  navigation to `/workout?id=`.
- `CreateWeekWorkoutDialog` + `useCreateWeekWorkout` -> `create-workout-dialog.tsx` + `use-create-workout.ts` -> same
  modal, fields, structural validation, inline error and `router.push` to the workout on success; without athlete,
  trainer and date fields. Duplicate rather than add mode flags to the workouts dialog (no cross-area import).
- `useMesocycleWorkouts` -> `workouts/hooks/use-workouts.ts` -> same status / errorCode / reload shape.
- `WorkoutContextStrip` + `useWorkoutContext` -> the mesocycle screen header and `use-mesocycle.ts` -> read-only text,
  skeleton while loading, hidden on failure (it never blocks the workout card).
- `GetMesocycleWorkoutsUseCase` + repository method -> `get-mesocycle-by-id-use-case.ts` and its method in
  `core/src/infrastructure/repositories/mesocycle-repository.ts` -> same id-scoped GET and error mapping.
- Tests -> `backoffice/test/presentation/mesocycles/views/mesocycle-view.test.tsx`,
  `backoffice/test/presentation/workouts/views/workout-view.test.tsx`, `core/test/.../workout-repository.test.ts`.
- E2E -> `backoffice/e2e/mesocycles.spec.ts` and `backoffice/e2e/workout.spec.ts` -> `page.route` interception.

Atom/molecule/organism references are `pending` in `.stpr/template/README.md`; follow the shapes in the backoffice
mesocycles and workouts areas. `.stpr/template/` is not updated here.

## Screens And Navigation

- backoffice -> `/mesocycle?id=<id>` -> `MesocycleView` -> "Planes asignados" row -> week rows as described; "Nuevo
  entrenamiento" opens `CreateWeekWorkoutDialog` for that week -> on success: `router.push("/workout?id=<newId>")`; on
  failure: inline error in the dialog, form kept. Workouts list failure: inline `LoadError` with retry inside the
  weeks card; weeks are still shown and editable.
- backoffice -> `/mesocycle` (create mode) -> unchanged: weeks card disabled, so no workout can be added before saving.
- backoffice -> `/workout?id=<id>` -> `WorkoutView` -> standalone table row, week workout item or after create ->
  context strip above the properties card; "Volver" (and the not-found link) -> `/mesocycle?id=<mesocycleId>` when
  placed, `/assigned-workouts` otherwise.

The back target is derived from the loaded workout's `mesocycleId`, not from where the user came from, so it holds on
refresh and deep links. On not-found the workout is unknown, so the link goes to `/assigned-workouts`.

## API Contract

- `POST /workouts` -> in `core/openapi/v1.json`: yes -> `CreateWorkoutRequest`; only `name` is required, `workoutDate`
  is nullable. A placed workout sends `mesocycleId` + `mesocycleWeekNumber` and no date; the API derives it.
- `PUT /workouts/{id}` -> yes -> `UpdateWorkoutRequest`; `name` and `userId` required, `workoutDate` nullable. The
  update keeps sending the placement it read.
- `GET /mesocycles/{id}/workouts` -> yes -> new `GetMesocycleWorkoutsUseCase` -> `WorkoutListItemResponse[]`, every
  workout of the mesocycle regardless of week. Grouping by `mesocycleWeekNumber` is presentation.
- `GET /workouts?standalone=true` -> yes -> unchanged; `WorkoutListItemResponse` now also carries `mesocycleId` +
  `mesocycleWeekNumber`, next to `athlete` and `trainer`.
- `GET /workouts/{id}` -> yes -> now answers `WorkoutByIdResponse`: the detail fields plus `athlete` and `trainer`
  profiles. `PUT /workouts/{id}`, `PUT .../exercises/order` and `PUT .../status` still answer `WorkoutDetailResponse`
  (no profiles).
- `GET /mesocycles/{id}` -> yes -> mesocycle name, athlete and trainer for the create dialog and the strip.

The snapshot was refreshed (`openapi:refresh` + `openapi:generate`) ahead of this change; it is committed as a step of
its own. No hand-written type.

## Held State

- `useMesocycleWorkouts` -> the mesocycle's workouts grouped by week, status, error code -> the hook on mount, `reload`
  after a failed load -> resets on screen exit.
- Expanded week rows -> local to `MesocycleWeeksCard` -> the user -> resets on screen exit.
- `CreateWeekWorkoutDialog` form -> local -> the user -> resets on save, cancel or close.
- `useWorkoutContext` -> athlete, trainer and mesocycle names -> the hook after the workout loads -> resets on screen
  exit.

Nothing is cached across screens or kept after logout.

## Forms And Structural Validation

- Week workout -> name -> required, trimmed, same max length as the standalone dialog.
- Week workout -> description -> optional, same max length as the standalone dialog.
- Week workout -> athlete, trainer, date -> not rendered; athlete and trainer come from the loaded mesocycle, the date
  from the API.
- Standalone workout -> unchanged (athlete, `ADMIN`-only trainer, name, description, date all as today).

Whether the workout fits the week (athlete, trainer, defined week) is the API's and comes back as an error.

## Errors And Copy

- Session expired -> `UNAUTHORIZED` -> existing session handling.
- Not the mesocycle's trainer / no active relationship -> `FORBIDDEN` -> inline error in the dialog.
- Invalid field -> `VALIDATION` -> inline error.
- Mesocycle or week gone -> `NOT_FOUND` -> inline error in the dialog.
- Placement refused (athlete/trainer mismatch, incomplete placement) -> `CONFLICT` or `VALIDATION`, whichever the API
  change maps -> inline error, form kept.
- No connection / anything else -> `NETWORK` / `UNEXPECTED` -> `LoadError` with retry on load, inline error on save.

No new error code expected; confirm against the API changes at proposal time. If one appears, it goes into the HTTP
error mapper and both languages.

New copy (main keys; final set fixed at proposal time):

- `mesocycle.weeks.newWorkout` -> "Nuevo entrenamiento" -> "New workout"
- `mesocycle.weeks.workoutCount` -> "{{count}} entrenamiento(s)" (plural forms) -> "{{count}} workout(s)"
- `mesocycle.weeks.workoutsLoadError` -> "No se pudieron cargar los entrenamientos." -> "Workouts could not be loaded."
- `mesocycle.weekWorkout.title` -> "Nuevo entrenamiento · Semana {{number}}" -> "New workout · Week {{number}}"
- `workout.context.athlete` -> "Atleta" -> "Athlete"
- `workout.context.trainer` -> "Entrenador" -> "Trainer"
- `workout.context.mesocycle` -> "{{name}} · Semana {{number}}" -> "{{name}} · Week {{number}}"
- `workout.backToMesocycle` -> "Volver al mesociclo" -> "Back to mesocycle"

## Platform Capabilities And Dependencies

None. A collapsible row is built from MUI (`Accordion` or `Collapse`), already a dependency.

## Out Of Scope

- Moving a workout between weeks or mesocycles, or turning a standalone workout into a placed one and back.
- Deleting workouts; reordering workouts inside a week.
- Showing or editing the workout date on the create dialog for placed workouts.
- Week removal (`DELETE /mesocycles/{id}/weeks/{weekNumber}`) in the UI, even though its API change adds it.
- Any app screen.
- Must not change: the standalone create dialog and the "Entrenamientos asignados" table (still `standalone=true`).

## Notes

- **Assumption to confirm:** a week with workouts also shows "Nuevo entrenamiento", inside its expanded content, so a
  second workout can be added. The brief only stated it for empty weeks.
- **Collapsed row summary.** The row header keeps chip, "Semana N" and objective, plus a workout count; the edit week
  action stays reachable without expanding.
- **Week order.** Workouts inside a week sorted by date, then name, as received; no client-side business ordering.
- **Placed date.** The workout screen shows the date the API derived. Whether it stays editable for a placed workout is
  decided at proposal time, after the API change fixes that any sent date is ignored.
- **Strip names.** Athlete and trainer full names come from the `athlete` / `trainer` profiles of `GET /workouts/{id}`
  for every workout. Only the mesocycle name needs `GET /mesocycles/{id}`, and only when the workout is placed.
- **Profiles survive saves.** Update, reorder and status calls answer without profiles. When the view replaces its
  workout with one of those responses, it keeps the profiles it loaded (they cannot change from this screen).
- **No stop case**: every endpoint and field is in the snapshot and no business rule is added.
- **Verification tiers:**
  - core unit: `GetMesocycleWorkoutsUseCase` and the repository method (success and every error code); create sends
    the placement without `workoutDate`.
  - backoffice component:
    - `mesocycle-view.test.tsx`: week without workouts is not collapsible and has the button; week with workouts
      expands to its workouts; create sends name, description, `mesocycleId`, `mesocycleWeekNumber`, athlete
      `userId`, no date, `trainerId` only for `ADMIN`; navigation on success; inline error on failure; workouts load
      failure + retry.
    - `workout-view.test.tsx`: strip with athlete always; trainer for `ADMIN`, absent for `TRAINER`; mesocycle + week
      only when placed; "Volver" target per placement; update keeps the placement.
  - backoffice E2E: `mesocycles.spec.ts` (mesocycle -> week -> "Nuevo entrenamiento" -> workout screen -> "Volver" to the
    mesocycle -> week expanded shows it), API intercepted with `page.route`.
  - translation parity passes.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed beyond the ones stated above.
