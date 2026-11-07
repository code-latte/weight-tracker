# Change Proposal: Add Backoffice Workout Templates

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-backoffice-workout-templates/`.

## Change

Bring workout templates to the front: refresh the API snapshot, add the template use cases in `core`, give the
"Plantillas de entrenamiento" drawer entry a list and an editor that mirror "Entrenamientos asignados", and connect
templates and workouts both ways: create a workout from a template, and save a workout as a template.

## Objective

The API now offers workout templates (`/workout-templates`, `POST /workouts/{id}/templates`,
`POST /workout-templates/{id}/workouts`), but the snapshot in `core/openapi/v1.json` predates them and the
"Plantillas de entrenamiento" drawer entry leads nowhere. Trainers rebuild the same session by hand for every athlete
and every week.

After this change a trainer (or an admin on behalf of a trainer) keeps a library of reusable workouts, edits them with
the same editor as a workout, starts any new workout (standalone or in a mesocycle week) from one of them, and turns
an existing workout into a template in one click.

## Capabilities

1. **Contract and core** (first task, its own step):
   - Refresh the snapshot (`openapi:refresh` + `openapi:generate`) and commit the snapshot and generated types alone.
   - Domain aliases, a `WorkoutTemplateRepository` contract and HTTP implementation, and one use case per operation:
     list, get by id, create, update, delete, add / update / remove exercise, reorder exercises, create / break
     super-series, create a workout from a template, and create a template from a workout.
   - Register them in the backoffice `container.ts` (the app does not use them).
2. **Templates list**: "Plantillas de entrenamiento" opens a table of templates (`GET /workout-templates`): the
   trainer's own, or every trainer's for `ADMIN`; with a "Nueva plantilla" button.
3. **Create a template**: a dialog with name, description and, for `ADMIN` only, trainer. No date, mesocycle, week or
   athlete. On success, go to the template editor.
4. **Edit a template**: the same screen as editing a workout: details card (name, description), exercises card with
   add / configure / remove / reorder, super-series create / break, exercise catalog dialog. Without the athlete, date,
   mesocycle and week context the workout screen shows.
5. **Delete a template** from its editor, with confirmation (the API offers `DELETE`; decide at proposal time whether
   it also appears as a row action in the table).
6. **Create a workout from a template**: the create workout dialog, both the standalone one (`create-workout-dialog`)
   and the mesocycle week one (`create-week-workout-dialog`), gets two sections on its left, laid out like the
   exercise catalog's section list: "Desde cero" (today's form, unchanged) and "Desde plantilla" (a list of the
   trainer's templates showing name, description and number of exercises). Picking a template and confirming creates
   the workout and opens it.
7. **Save a workout as a template**: on the workout screen, an outlined "Guardar como plantilla" button just to the
   left of the save button. It creates a template from the workout's saved state and confirms it to the user.

## Fronts And Roles

- backoffice -> lands here: yes -> roles: `ADMIN`, `TRAINER`:
  - `TRAINER`: lists, creates, edits and deletes their own templates; creates workouts from them; saves their workouts
    as templates. Must NOT see the trainer column in the templates table nor the trainer picker on create, and never
    sends `trainerId`.
  - `ADMIN`: sees every trainer's templates with a trainer column; picks the owning trainer on create (required: an
    admin never owns a template). In the standalone "Desde plantilla" section the list shows the templates of the
    trainer picked in the dialog (`GET /workout-templates?trainerId=`), and is empty until one is picked.
- app -> lands here: no -> no screen, hook, copy or wiring changes. The `core` use cases are not registered there.

The front does not filter by ownership: it renders what the API returns and lets it refuse. Role branches are
presentation only, read from `useCurrentUser().profile.role`, as the workout screens do.

## Classification

Core (`workout-templates` domain area):

- `WorkoutTemplate`, `WorkoutTemplateSummary` (if the list returns a different shape), `CreateWorkoutTemplateRequest`,
  `UpdateWorkoutTemplateRequest`, `CreateWorkoutFromTemplateRequest` -> entity type (aliases of generated types) ->
  `core/src/domain/...` next to the workout aliases
- `WorkoutTemplateRepositoryInterface` -> repository contract -> `core/src/application/...` next to the workout one
- `GetWorkoutTemplatesUseCase`, `GetWorkoutTemplateByIdUseCase`, `CreateWorkoutTemplateUseCase`,
  `UpdateWorkoutTemplateUseCase`, `DeleteWorkoutTemplateUseCase`, `AddExerciseToWorkoutTemplateUseCase`,
  `UpdateWorkoutTemplateExerciseUseCase`, `RemoveExerciseFromWorkoutTemplateUseCase`,
  `ReorderWorkoutTemplateExercisesUseCase`, `CreateWorkoutTemplateSuperSeriesUseCase`,
  `BreakWorkoutTemplateSuperSeriesUseCase`, `CreateWorkoutFromTemplateUseCase` -> use case ->
  `core/src/application/use-cases/workout-templates/`
- `CreateWorkoutTemplateFromWorkoutUseCase` -> use case -> `core/src/application/use-cases/workout-templates/` (it
  calls `POST /workouts/{id}/templates`, but its result is a template)
- `HttpWorkoutTemplateRepository` -> repository implementation -> `core/src/infrastructure/...` next to the workout one
- Validation constants (name / description max length) -> `core/src/application/validation`, mirroring the workout ones
- Wiring -> `backoffice/src/infrastructure/.../container.ts`

Backoffice (new `workout-templates` domain area):

- Drawer item `menu.library.workoutTemplates` -> organism, extended in place (add its `href`) ->
  `presentation/common/components/organisms/navigation-drawer.tsx`
- `/workout-templates` route -> route -> `backoffice/src/app/workout-templates/page.tsx`
- `/workout-template` route (`?id=`) -> route -> `backoffice/src/app/workout-template/page.tsx`
- `WorkoutTemplatesView` -> view -> `presentation/workout-templates/views/workout-templates-view.tsx`
- `WorkoutTemplateView` -> view -> `presentation/workout-templates/views/workout-template-view.tsx`
- `WorkoutTemplatesTable` -> organism -> `presentation/workout-templates/components/organisms/`
- `CreateWorkoutTemplateDialog` -> organism -> `presentation/workout-templates/components/organisms/`
- `DeleteWorkoutTemplateDialog` -> organism -> same folder (confirmation)
- Template editor cards and dialogs -> see "Shared editor" in Notes
- State hooks -> `useWorkoutTemplates`, `useWorkoutTemplate`, `useCreateWorkoutTemplate`, `useUpdateWorkoutTemplate`,
  `useDeleteWorkoutTemplate`, plus the exercise / reorder / super-series hooks for templates ->
  `presentation/workout-templates/hooks/`

Backoffice (existing areas, extended):

- `CreateWorkoutDialog` -> organism, extended -> `workouts/components/organisms/create-workout-dialog.tsx`: section
  list on the left, "Desde cero" keeps today's form
- `CreateWeekWorkoutDialog` -> organism, extended -> `mesocycles/components/organisms/create-week-workout-dialog.tsx`
- `WorkoutTemplatePicker` (list of templates: name, description, exercise count; single selection) -> organism ->
  `presentation/common/components/organisms/` (used by both dialogs, so it cannot live in either area)
- Dialog section list ("Desde cero" / "Desde plantilla") -> molecule, generalised from
  `workouts/components/molecules/exercise-section-list.tsx` or a sibling of it in `common` (decide at proposal time)
- `useWorkoutTemplateOptions` (templates for a trainer) and `useCreateWorkoutFromTemplate` -> state hook ->
  `presentation/common/hooks/`
- "Guardar como plantilla" button -> in `WorkoutDetailsCard` (beside its save button) or the view header, wherever
  the save button lives -> `workouts/components/organisms/workout-details-card.tsx`
- `useSaveWorkoutAsTemplate` -> state hook -> `presentation/workouts/hooks/`
- Copy -> new `workoutTemplates` namespace (`backoffice/src/i18n/es|en/workout-templates.json`, registered with the
  others) plus new keys in `workouts.json` and `mesocycles.json`

## Canonical Template Mapping

- Snapshot refresh -> `core/openapi/v1.json` + `core/src/contract/generated` -> generated only, committed alone.
- Template aliases, repository contract and implementation, use cases -> the workout ones in `core` (and
  `.stpr/template/` reference chosen through its README) -> same resource shape, same sub-resources (exercises,
  order, super-series).
- Core tests -> the workout use case and repository tests in `core/test` -> one per use case, every repository error code.
- Routes -> `backoffice/src/app/workout/page.tsx` -> default-exports the view, nothing else.
- `WorkoutTemplatesView` + `WorkoutTemplatesTable` + `useWorkoutTemplates` -> `assigned-workouts-view.tsx`,
  `workouts-table.tsx`, `use-workouts.ts` -> same table, `ADMIN`-only trainer column, skeleton, empty state, `LoadError`
  with retry, "new" button opening a dialog.
- `CreateWorkoutTemplateDialog` + `useCreateWorkoutTemplate` -> `create-workout-dialog.tsx` + `use-create-workout.ts`
  -> same dialog, fields reduced to name, description and the `ADMIN`-only trainer picker; `router.push` to the editor.
- `WorkoutTemplateView` -> `workouts/views/workout-view.tsx` -> same header, back link (to `/workout-templates`),
  `?id=` through `useSearchParams` inside `Suspense`, not-found state, details card, exercises card, dialog union,
  super-series. No `WorkoutContextStrip` (no athlete, date or mesocycle).
- Template exercise / reorder / super-series hooks -> `use-save-workout-exercise.ts`, `use-remove-workout-exercise.ts`,
  `use-reorder-workout-exercises.ts`, `use-super-series.ts` -> same status / errorCode shape.
- Dialog sections -> `exercise-catalog-browser.tsx` + `exercise-section-list.tsx` -> the left section list the user asked for.
- `WorkoutTemplatePicker` -> the list in `exercise-catalog-browser.tsx` -> `List` atom with selectable rows.
- `useSaveWorkoutAsTemplate` -> `use-update-workout.ts` -> mutation hook with status / errorCode / done flag.
- View tests -> `backoffice/test/presentation/workouts/views/` -> fakes through the container.
- E2E -> the workout and assigned-workouts E2E specs -> `page.route` interception, same session setup.

UI references come from the backoffice only. Atom/molecule/organism references are `pending` in
`.stpr/template/README.md`; follow the shapes in the workouts area. `.stpr/template/` is not updated here.

## Screens And Navigation

- backoffice -> `/workout-templates` -> `WorkoutTemplatesView` -> drawer "Plantillas de entrenamiento" -> success:
  table or empty state; row -> `/workout-template?id=<id>`; "Nueva plantilla" -> create dialog -> on success
  `/workout-template?id=<newId>`; on failure: inline error in the dialog, form kept; load failure: `LoadError` + retry.
- backoffice -> `/workout-template?id=<id>` -> `WorkoutTemplateView` -> table row or after create -> title "Editar
  plantilla"; details and exercises edited as in a workout; delete -> confirmation -> `/workout-templates`; unknown id:
  not-found state with a link back; failures: inline per card / dialog. "Volver" -> `/workout-templates`.
- backoffice -> `/assigned-workouts` -> "Nuevo entrenamiento" -> `CreateWorkoutDialog`: "Desde cero" as today;
  "Desde plantilla": athlete, date and (`ADMIN`) trainer, plus the template list -> on success
  `/workout?id=<newId>`; on failure: inline error, selection kept.
- backoffice -> `/mesocycle?id=<id>` -> add a workout to a week -> `CreateWeekWorkoutDialog`: "Desde cero" as today;
  "Desde plantilla": the template list of the mesocycle's trainer; athlete, mesocycle and week come from the context ->
  on success: same as today's week workout creation; on failure: inline error.
- backoffice -> `/workout?id=<id>` -> "Guardar como plantilla" -> on success: confirmation (snackbar, optionally with
  a link to the new template; decide at proposal time), user stays on the workout; on failure: inline error.

## API Contract

All present in the API, none in the current snapshot; this change refreshes it (`openapi:refresh` + `openapi:generate`).

- `GET /workout-templates` (`?trainerId=` optional) -> list -> `GetWorkoutTemplatesUseCase`
- `GET /workout-templates/{id}` -> `GetWorkoutTemplateByIdUseCase` -> `WorkoutTemplate`
- `POST /workout-templates` -> `CreateWorkoutTemplateUseCase` -> `CreateWorkoutTemplateRequest` (`name`, `description`,
  `trainerId` only for `ADMIN`)
- `PUT /workout-templates/{id}` -> `UpdateWorkoutTemplateUseCase` -> `UpdateWorkoutTemplateRequest`
- `DELETE /workout-templates/{id}` -> `DeleteWorkoutTemplateUseCase`
- `POST /workout-templates/{templateId}/exercises` -> `AddExerciseToWorkoutTemplateUseCase`
- `PUT /workout-templates/{templateId}/exercises/{exerciseId}` -> `UpdateWorkoutTemplateExerciseUseCase`
- `DELETE /workout-templates/{templateId}/exercises/{exerciseId}` -> `RemoveExerciseFromWorkoutTemplateUseCase`
- `PUT /workout-templates/{templateId}/exercises/order` -> `ReorderWorkoutTemplateExercisesUseCase`
- `POST /workout-templates/{templateId}/superseries` -> `CreateWorkoutTemplateSuperSeriesUseCase`
- `DELETE /workout-templates/{templateId}/superseries/{superSeriesId}` -> `BreakWorkoutTemplateSuperSeriesUseCase`
- `POST /workout-templates/{templateId}/workouts` -> `CreateWorkoutFromTemplateUseCase` ->
  `CreateWorkoutFromTemplateRequest` (`userId` required; `workoutDate` for standalone, or `mesocycleId` +
  `mesocycleWeekNumber`; no `trainerId`: the workout's trainer is the template's) -> `Workout`
- `POST /workouts/{workoutId}/templates` (no body) -> `CreateWorkoutTemplateFromWorkoutUseCase` -> `WorkoutTemplate`

No hand-written type. Exact response shapes (list item vs. detail, exercise count field) are taken from the refreshed
snapshot; if the list response carries no exercise count, the picker derives it from the returned exercises.

## Held State

- `useWorkoutTemplates` -> loaded list, status, error code -> the hook on mount and `reload` -> resets on screen exit.
- `useWorkoutTemplate` -> the loaded template -> mount; replaced by every update / exercise / super-series response ->
  resets on screen exit.
- `useWorkoutTemplateOptions` -> templates for the dialog's trainer -> loaded when the "Desde plantilla" section opens
  (and again when an `ADMIN` changes the trainer) -> resets when the dialog closes.
- Create dialogs -> selected section, selected template, form values -> local -> reset on close or success.
- `useSaveWorkoutAsTemplate` -> status, error code -> resets on screen exit.

Nothing is cached across screens or kept after logout.

## Forms And Structural Validation

- Template -> name -> required, trimmed, max 256 (API).
- Template -> description -> optional, trimmed, max 1024 (API).
- Template -> trainer -> required for `ADMIN` on create; not rendered for `TRAINER`.
- Template exercises -> same structural rules as workout exercises (reused, not redefined).
- Workout from template (standalone) -> template -> required; athlete -> required; date -> required; trainer ->
  required for `ADMIN` (only to load its templates, not sent).
- Workout from template (mesocycle week) -> template -> required; the rest comes from the context.

Ownership, trainer–athlete relationship, mesocycle membership and week bounds are the API's and come back as errors.

## Errors And Copy

- Session expired -> `UNAUTHORIZED` -> existing session handling.
- Not the owner / no active relationship -> `FORBIDDEN` -> inline error.
- Invalid field or invalid template trainer -> `VALIDATION` -> inline error.
- Unknown template / workout -> `NOT_FOUND` -> not-found state on load, inline error on save.
- Conflicts -> `CONFLICT` -> inline error, form kept.
- No connection / anything else -> `NETWORK` / `UNEXPECTED` -> `LoadError` with retry on load, inline error on save.

No new error code is expected; confirm at proposal time against the API's `ExceptionMapper` for the template
exceptions. If one is needed, it is mapped in the HTTP error mapper and translated in both languages.

New copy (main keys; final set fixed at proposal time):

- `workoutTemplates.title` -> "Plantillas de entrenamiento" -> "Workout templates"
- `workoutTemplates.new` -> "Nueva plantilla" -> "New template"
- `workoutTemplates.columns.name|description|exercises|trainer` -> "Nombre", "Descripción", "Ejercicios",
  "Entrenador" -> "Name", "Description", "Exercises", "Trainer"
- `workoutTemplates.empty` -> "Todavía no hay plantillas." -> "No templates yet."
- `workoutTemplate.createTitle` / `editTitle` -> "Crear plantilla" / "Editar plantilla" -> "Create template" / "Edit template"
- `workoutTemplate.delete`, `.deleteConfirm` -> "Eliminar plantilla", "¿Eliminar esta plantilla?" -> to be proposed
- `createWorkout.sections.scratch` / `.template` -> "Desde cero" / "Desde plantilla" -> "From scratch" / "From template"
- `createWorkout.template.exerciseCount` -> "{{count}} ejercicios" -> "{{count}} exercises" (plural forms)
- `createWorkout.template.empty` / `.pickTrainer` -> "No hay plantillas para este entrenador." / "Elige un entrenador
  para ver sus plantillas." -> to be proposed
- `workout.saveAsTemplate` -> "Guardar como plantilla" -> "Save as template"
- `workout.savedAsTemplate` -> "Plantilla creada" -> "Template created"

## Platform Capabilities And Dependencies

None. No new port and no new package.

## Out Of Scope

- Any app screen, hook or wiring.
- Sharing templates between trainers, duplicating a template, or versioning templates.
- Search, filtering, sorting and pagination in the templates table and in the dialog's template list.
- Keeping workouts in sync with the template they came from (a created workout is independent).
- Changing a template's trainer after creation.
- Must not change: the API, today's "Desde cero" forms and their behaviour, the other drawer entries.

## Notes

- **Task order.** 1) snapshot refresh (own commit), 2) core aliases, repository, use cases and tests, plus backoffice
  wiring, 3) templates list and editor, 4) create workout from template, 5) save workout as template.
- **Shared editor.** "Edit a template = edit a workout" is the core of the change. The workout editor organisms
  (`workout-details-card`, `workout-exercises-card`, `workout-exercises-list`, `exercise-configuration-dialog`,
  `exercise-catalog-dialog`, `super-series-card`, `break-super-series-dialog`) are bound to workouts through their
  hooks. Preferred: make them take their data and callbacks through props (most already do), move the ones both areas
  need to `presentation/common`, and give each area its own hooks. Not allowed: the `workout-templates` area importing
  from `presentation/workouts`, or copy-pasting the whole editor. Decide the exact split at proposal time.
- **Save as template uses the saved workout.** The API copies the server state. If the details form has unsaved
  changes, either disable the button or say so in its tooltip; decide at proposal time. No business rule is added.
- **Dialog sections.** The section list sits on the left of the dialog as in the exercise catalog; the dialog widens
  accordingly. "Desde cero" is selected by default.
- **Exercise count.** Shown in the table and in the picker; taken from the API response, never computed from business
  rules.
- **Route naming.** `/workout-templates` (list) and `/workout-template?id=` (editor) follow `/assigned-workouts` and
  `/workout?id=`. A template is always created from the dialog, so the editor has no create mode.
- **No stop case**: every endpoint exists in the API; the snapshot refresh is part of this change.
- **Verification tiers:**
  - core unit: every new use case; `HttpWorkoutTemplateRepository` success and every error code per method.
  - backoffice component tests:
    - `workout-templates-view.test.tsx`: rows; `ADMIN` sees the trainer column, `TRAINER` does not (header and cells
      absent); empty; failure + retry; create dialog (`TRAINER` has no trainer picker and sends no `trainerId`,
      `ADMIN` must pick one; required and length limits; navigation on success; inline error on failure).
    - `workout-template-view.test.tsx`: loaded values; update; add / configure / remove / reorder exercise;
      super-series create / break; delete with confirmation and navigation; not-found; no context strip.
    - create workout dialog (standalone and mesocycle week): "Desde cero" unchanged; "Desde plantilla" lists name,
      description and exercise count; `ADMIN` must pick a trainer first; request sent with the right fields per
      dialog; navigation on success; inline error on failure.
    - `workout-view.test.tsx`: "Guardar como plantilla" is outlined, left of save, calls the use case and confirms;
      inline error on failure.
    - drawer test: "Plantillas de entrenamiento" links to `/workout-templates` and is selected there.
  - backoffice E2E: `workout-templates.spec.ts` (drawer -> list -> create -> add an exercise), create a workout from a
    template (standalone), save a workout as a template; API intercepted with `page.route`.
  - translation parity passes with the new namespace.
- Done means `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` passes from `front/`.
- No deviation from the mapped references is allowed beyond the ones stated above.
