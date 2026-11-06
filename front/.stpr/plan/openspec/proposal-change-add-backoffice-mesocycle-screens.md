# Change Proposal: Add Backoffice Mesocycle Screens

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-mesocycle-screens/`.

## Change

Split the backoffice "Programación" section in two: a new "Entrenamientos asignados" drawer entry takes over the current
workouts table, and "Planes asignados" now lists mesocycles. Add a create/edit mesocycle screen, inspired by
`panel_mesocycle.png` (repository root) but built with the layout, cards and styles of the existing workout screen.

## Objective

`core` already offers every mesocycle operation (change `add-mesocycle-usecases`) and both containers register them, but
no screen uses them. Trainers and admins cannot plan a training block: they can only create loose workouts. The
"Planes asignados" entry shows workouts, which is the wrong name for them now that plans (mesocycles) exist.

After this change a trainer or admin can list the mesocycles they can see, create one for an athlete, edit its general
information and define or redefine its weeks. Workouts stay reachable, unchanged, from their own drawer entry.

## Capabilities

- New drawer entry "Entrenamientos asignados" showing the current workouts table and the "Nuevo entrenamiento" button,
  exactly as today, at a new route.
- "Planes asignados" lists mesocycles (`GET /mesocycles`) in a table, with a "Nuevo mesociclo" button.
- Create a mesocycle: athlete, trainer (`ADMIN` only), name, start date, duration in weeks and general objective.
- Edit a mesocycle's name, start date, duration and objective.
- Weekly planning: list the defined weeks (type chip, "Semana N", objective), add a week (type + objective) and edit a
  week (type + objective).
- Loading, empty, not-found and error states for the list and the screen.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER` -> both list, create and edit mesocycles and weeks. `ADMIN`
  sees the trainer column in the mesocycles table and picks the trainer on create; `TRAINER` must NOT see the trainer
  column (header nor cells) nor the trainer picker, and never sends `trainerId`.
- app -> lands here: no -> no screen, hook, copy or wiring changes.

The front does not filter rows by role: it renders what `GET /mesocycles` returns. Role branches are presentation only,
read from `useCurrentUser().profile.role`, as the workout screens already do.

## Classification

Navigation and workouts (`workouts` domain area, moved, not rewritten):

- Drawer -> organism, extended in place -> `backoffice/src/presentation/common/components/organisms/navigation-drawer.tsx`:
  new item `menu.programming.assignedWorkouts` -> `/assigned-workouts`, after "Planes asignados" and before "Calendario"
- `/assigned-workouts` route -> route -> `backoffice/src/app/assigned-workouts/page.tsx`
- Current `AssignedPlansView` -> view, renamed `AssignedWorkoutsView` -> `backoffice/src/presentation/workouts/views/assigned-workouts-view.tsx`
  (copy keys move from `assignedPlans.*` to `assignedWorkouts.*` in the `workouts` namespace)
- Back links in `workout-view.tsx` -> point to `/assigned-workouts`

Mesocycles (new `mesocycles` domain area in the backoffice):

- `/assigned-plans` route -> route, now rendering the mesocycles list -> `backoffice/src/app/assigned-plans/page.tsx`
- `AssignedPlansView` -> view -> `backoffice/src/presentation/mesocycles/views/assigned-plans-view.tsx`
- `/mesocycle` route (create without `id`, edit with `?id=`) -> route -> `backoffice/src/app/mesocycle/page.tsx`
- `MesocycleView` -> view -> `backoffice/src/presentation/mesocycles/views/mesocycle-view.tsx`
- `MesocyclesTable` -> organism -> `backoffice/src/presentation/mesocycles/components/organisms/mesocycles-table.tsx`
- `MesocycleDetailsCard` ("Información general") -> organism -> `.../mesocycles/components/organisms/mesocycle-details-card.tsx`
- `MesocycleWeeksCard` ("Planificación semanal") -> organism -> `.../mesocycles/components/organisms/mesocycle-weeks-card.tsx`
- `MesocycleWeekDialog` (add / edit a week) -> organism -> `.../mesocycles/components/organisms/mesocycle-week-dialog.tsx`
- `MesocycleWeekListItem` (chip, "Semana N", objective, edit action) -> molecule -> `.../mesocycles/components/molecules/mesocycle-week-list-item.tsx`
- `TrainingWeekTypeChip` -> molecule over the existing chip atom (or a new `chip` atom if none exists) -> `.../mesocycles/components/molecules/`
- `DurationField` (− / value / + stepper) -> molecule -> `.../mesocycles/components/molecules/duration-field.tsx`
- Athlete / trainer pickers -> reuse `workouts/components/molecules/person-picker.tsx` and `useWorkoutAssignees`; if
  reused across areas, move them to `common` rather than importing across domain areas (decide at proposal time)
- State hooks -> `useMesocycles`, `useMesocycle`, `useCreateMesocycle`, `useUpdateMesocycle`, `useDefineMesocycleWeek`,
  `useRedefineMesocycleWeek` -> `backoffice/src/presentation/mesocycles/hooks/`
- Copy -> new `mesocycles` namespace -> `backoffice/src/i18n/es/mesocycles.json`, `backoffice/src/i18n/en/mesocycles.json`,
  registered where the other namespaces are; drawer key in `common.json`

Core: none. The use cases exist and are registered.

## Canonical Template Mapping

- Routes -> `backoffice/src/app/workout/page.tsx` -> default-exports the view, nothing else.
- `AssignedWorkoutsView` -> the current `assigned-plans-view.tsx` -> moved as is.
- Mesocycles `AssignedPlansView` + `MesocyclesTable` + `useMesocycles` -> current `assigned-plans-view.tsx`,
  `workouts-table.tsx`, `use-workouts.ts` -> same table, role-dependent trainer column, skeleton, empty state and
  `LoadError` with retry.
- `MesocycleView` -> `workouts/views/workout-view.tsx` -> same header (`h1` + "Volver" back link), card stack, `?id=`
  read through `useSearchParams` inside `Suspense`, not-found state, dialog state as one discriminated union.
- `MesocycleDetailsCard` -> `workout-details-card.tsx` -> titled card, form fields, in-card save button.
- `MesocycleWeeksCard` + `MesocycleWeekListItem` -> `workout-exercises-card.tsx` + `exercise-list-item.tsx` -> titled
  card with an action button ("Añadir semana") and a row list with an edit action.
- `MesocycleWeekDialog` -> `exercise-configuration-dialog.tsx` -> modal form with cancel / save and inline error.
- Create flow -> `create-workout-dialog.tsx` + `use-create-workout.ts` -> athlete and `ADMIN`-only trainer pickers, then
  `router.push` to the edit route with the new id.
- Mutation hooks -> `use-update-workout.ts`, `use-save-workout-exercise.ts` -> status / errorCode shape.
- View tests -> `backoffice/test/presentation/workouts/views/` -> fakes through the container.
- E2E -> `backoffice/e2e/assigned-plans.spec.ts` and the workout E2E -> `page.route` interception, same session setup.

UI references come from the backoffice only; the image is a visual hint, the workout screen is the reference.
Atom/molecule/organism references are `pending` in `.stpr/template/README.md`; follow the shapes in the workouts area.
`.stpr/template/` and its README are not updated here.

## Screens And Navigation

- backoffice -> `/assigned-workouts` -> `AssignedWorkoutsView` -> drawer "Entrenamientos asignados" -> unchanged
  behaviour of today's workouts table; "Nuevo entrenamiento" still opens the create workout dialog.
- backoffice -> `/assigned-plans` -> `AssignedPlansView` (mesocycles) -> drawer "Planes asignados" -> success: table or
  empty state; row -> `/mesocycle?id=<id>`; "Nuevo mesociclo" -> `/mesocycle`; failure: `LoadError` with retry.
- backoffice -> `/mesocycle` -> `MesocycleView` (create mode) -> "Nuevo mesociclo" -> title "Crear mesociclo"; general
  information card with athlete (and trainer for `ADMIN`) pickers; the weekly planning card is shown disabled with a hint
  that the mesocycle must be saved first -> on create success: `router.replace` to `/mesocycle?id=<id>`; on failure:
  inline error, form kept.
- backoffice -> `/mesocycle?id=<id>` -> `MesocycleView` (edit mode) -> table row or after create -> title "Editar
  mesociclo"; athlete/trainer shown read-only; save updates the general info; weeks can be added and edited -> on unknown
  id: not-found state with a link back to `/assigned-plans`; on failure: inline error per card / dialog.
- "Volver" -> `/assigned-plans`. Workout screen "Volver" -> `/assigned-workouts`.

## API Contract

- `GET /mesocycles` -> in `core/openapi/v1.json`: yes -> `GetMesocyclesUseCase` -> `Mesocycle[]`.
- `GET /mesocycles/{id}` -> yes -> `GetMesocycleByIdUseCase` -> `Mesocycle` (weeks, trainer and athlete profiles).
- `POST /mesocycles` -> yes -> `CreateMesocycleUseCase` -> `CreateMesocycleRequest` (`trainerId` only for `ADMIN`).
- `PUT /mesocycles/{id}` -> yes -> `UpdateMesocycleUseCase` -> `UpdateMesocycleRequest`.
- `POST /mesocycles/{id}/weeks` -> yes -> `DefineMesocycleWeekUseCase` -> `DefineMesocycleWeekRequest`.
- `PUT /mesocycles/{id}/weeks/{weekNumber}` -> yes -> `RedefineMesocycleWeekUseCase` -> `RedefineMesocycleWeekRequest`.
- Athlete / trainer options -> the endpoints already used by `useWorkoutAssignees`.

No snapshot refresh. No hand-written type: `TrainingWeekType` values come from the generated enum (`ADAPTATION`,
`ACCUMULATION`, `INTENSIFICATION`, `REALIZATION`, `DELOAD`, `RECOVERY`).

## Held State

- `useMesocycles` -> loaded list, status, error code -> the hook on mount and `reload` -> resets on screen exit.
- `useMesocycle` -> the loaded mesocycle -> the hook on mount; replaced by the response of every update, define or
  redefine -> resets on screen exit.
- Form state of the details card and the week dialog -> local -> the user -> resets on save, cancel or screen exit.

Nothing is cached across screens or kept after logout.

## Forms And Structural Validation

- Mesocycle -> athlete -> required (create only).
- Mesocycle -> trainer -> required for `ADMIN` (create only); not rendered for `TRAINER`.
- Mesocycle -> name -> required, trimmed, max 256 (API `MesocycleName.MaxLength`), with a character counter.
- Mesocycle -> objective -> required, trimmed, max 4000 (API `MesocycleObjective.MaxLength`), multiline, counter.
- Mesocycle -> start date -> required, valid date (`yyyy-MM-dd`), sent without time-zone shift.
- Mesocycle -> duration in weeks -> required integer, stepper bounded 1–52 (API `DurationInWeeks`).
- Week -> training week type -> required, one of the generated enum values.
- Week -> objective -> required, trimmed, max 4000 (same value object as the mesocycle objective).
- Week -> week number -> not typed by the user on add: the lowest week number in `1..durationInWeeks` not yet defined.

The snapshot declares no length limits, so these mirror the API domain constants. Everything else (duration shorter
than a defined week, duplicate week, week beyond duration, trainer–athlete relationship) is the API's and comes back as
an error.

## Errors And Copy

- Session expired -> `UNAUTHORIZED` -> existing session handling.
- Role not allowed / no active relationship -> `FORBIDDEN` -> inline error with the existing message.
- Invalid field -> `VALIDATION` -> inline error.
- Unknown mesocycle -> `NOT_FOUND` -> not-found state on load, inline error on save.
- Duplicate week, week beyond duration, duration shorter than a defined week -> `CONFLICT` -> inline error, form kept.
- No connection / anything else -> `NETWORK` / `UNEXPECTED` -> `LoadError` with retry on load, inline error on save.

No new error code; the HTTP error mapper is not touched. Error copy reuses the existing per-code messages; if the
`mesocycles` namespace needs its own wording for `CONFLICT`, it is added in both languages.

New copy (main keys; final set fixed at proposal time):

- `menu.programming.assignedWorkouts` -> "Entrenamientos asignados" -> "Assigned workouts"
- `assignedPlans.title` -> "Planes asignados" -> "Assigned plans"
- `assignedPlans.newMesocycle` -> "Nuevo mesociclo" -> "New mesocycle"
- `assignedPlans.columns.name|athlete|trainer|startDate|duration|weeks` -> "Nombre", "Atleta", "Entrenador", "Fecha de
  inicio", "Duración", "Semanas definidas" -> "Name", "Athlete", "Trainer", "Start date", "Duration", "Defined weeks"
- `assignedPlans.empty` -> "Todavía no hay planes asignados." -> "No plans assigned yet."
- `mesocycle.createTitle` / `editTitle` -> "Crear mesociclo" / "Editar mesociclo" -> "Create mesocycle" / "Edit mesocycle"
- `mesocycle.subtitle` -> "Define el objetivo del mesociclo y planifica sus semanas" -> "Define the mesocycle's goal and
  plan its weeks"
- `mesocycle.details.title` -> "Información general" -> "General information"
- `mesocycle.fields.name|objective|startDate|duration|athlete|trainer` -> "Nombre", "Objetivo general", "Fecha de
  inicio", "Duración (semanas)", "Atleta", "Entrenador" -> to be proposed
- `mesocycle.weeks.title` -> "Planificación semanal" -> "Weekly planning"
- `mesocycle.weeks.subtitle` -> "Define el objetivo de cada semana" -> "Define each week's goal"
- `mesocycle.weeks.add` -> "Añadir semana" -> "Add week"
- `mesocycle.weeks.week` -> "Semana {{number}}" -> "Week {{number}}"
- `mesocycle.weeks.saveFirst` -> "Guarda el mesociclo para planificar sus semanas." -> "Save the mesocycle to plan its
  weeks."
- `trainingWeekType.ADAPTATION|ACCUMULATION|INTENSIFICATION|REALIZATION|DELOAD|RECOVERY` -> "Adaptación",
  "Acumulación", "Intensificación", "Realización", "Descarga", "Recuperación" -> "Adaptation", "Accumulation",
  "Intensification", "Realization", "Deload", "Recovery"
- `assignedPlans.*` keys in `workouts.json` -> renamed to `assignedWorkouts.*`, title "Entrenamientos asignados".

## Platform Capabilities And Dependencies

None. No new port and no new package (pickers, dialogs and MUI icons already exist).

## Out Of Scope

- From the image, not supported by the API: deleting a week, reordering weeks (drag handle) and the per-week workout
  count ("4 entrenamientos"). They need an API change first.
- Deleting a mesocycle; changing its athlete or trainer after creation.
- Placing workouts in a mesocycle week from either screen (the `mesocycleId` + `mesocycleWeekNumber` pair).
- The header, search, notifications and other chrome shown in the image; the existing `AppShell` stays.
- Sorting, filtering, search and pagination in both tables.
- Any app screen.
- Must not change: the API, `core`, the workout screen apart from its back links, the other drawer entries.

## Notes

- **Layout.** Follow the workout screen, not the image, wherever they differ: same header, card padding, typography,
  buttons and in-card save. The image's sticky "Cancelar / Guardar mesociclo" footer is not adopted unless decided at
  proposal time; "Volver" covers cancel.
- **Week chip colours.** One colour per `TrainingWeekType`, taken from the theme palette (not hard-coded hex values).
- **Weeks order.** Rows sorted by `weekNumber`. The card shows only defined weeks; "Añadir semana" is disabled when every
  week in `1..durationInWeeks` is defined (structural mirror of the duration, the API still decides).
- **Edit sends everything.** `PUT /mesocycles/{id}` replaces name, objective, start date and duration; the form sends
  the four values it holds.
- **Duration display.** `durationInWeeks` is typed `integer | string` in the snapshot; normalise to a number when
  reading.
- **Cross-area reuse.** The mesocycles area must not import from `presentation/workouts`. Shared pickers move to
  `presentation/common` as part of this change, or are duplicated; decide at proposal time (moving is preferred).
- **Route rename.** `/assigned-plans` changes meaning (workouts -> mesocycles). Any link or test that relied on it
  for workouts moves to `/assigned-workouts`.
- **Menu label casing.** Written in sentence case ("Entrenamientos asignados") to match the existing entries.
- **No stop case**: every endpoint is in the snapshot and no business rule is added.
- **Verification tiers:**
  - backoffice component tests:
    - `assigned-workouts-view.test.tsx`: the existing assigned-plans workout tests, moved.
    - `assigned-plans-view.test.tsx` (mesocycles): rows with every column; `ADMIN` sees the trainer column, `TRAINER`
      does not (header and cells absent); empty state; failure + retry; "Nuevo mesociclo" links to `/mesocycle`.
    - `mesocycle-view.test.tsx`: create mode (pickers per role, `TRAINER` has no trainer picker and sends no
      `trainerId`, required fields, length limits, duration bounds, weeks card disabled, navigation on success, inline
      error on failure); edit mode (values loaded, update sent with all four fields, not-found state); add week (lowest
      free number sent, `CONFLICT` shown); edit week (redefine sent with its week number).
    - drawer test: "Entrenamientos asignados" links to `/assigned-workouts`, "Planes asignados" to `/assigned-plans`,
      each selected on its route.
  - backoffice E2E: `assigned-workouts.spec.ts` (current workouts flow at its new route) and `mesocycles.spec.ts`
    (drawer -> list -> create -> add a week -> edit it), API intercepted with `page.route`.
  - translation parity passes with the new namespace.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed beyond the ones stated above.
