# Design

## Context

See `proposal.md` for the motivation, scope and the decisions taken from the brief's open points.

- `core/openapi/v1.json` has no template path today. The API (`WorkoutTemplatesController`, `WorkoutsController`)
  answers every template read and write with `WorkoutTemplateResponse` (`id`, `name`, `description`, `trainerId`,
  `exercises: WorkoutExerciseDetailResponse[]`), and reuses the workout bodies `AddOrUpdateExerciseRequest`,
  `ReorderWorkoutExercisesRequest` and `CreateSuperSeriesRequest`. Creating from a template answers `WorkoutResponse`.
  The list carries no trainer profile, only `trainerId`.
- The API maps template exceptions to 400 / 403 / 404 / 409, all already handled by `mapHttpError`.
- `Container<Dependencies>` takes a registration for every key, so both fronts' `container.ts` must register every new
  key even though only the backoffice uses them (as with `getAthleteMesocyclesUseCase`).
- The workout screen (`workout-view.tsx`, `LoadedWorkout`, ~540 lines) owns the exercise list state, the open dialog,
  the selection and the superseries / reorder / removal handlers, and calls `useSaveWorkoutExercise`,
  `useRemoveWorkoutExercise`, `useReorderWorkoutExercises` and `useSuperSeries`, each taking the workout id as a call
  argument and resolving a workout use case. The organisms it renders are already presentational.
- The exercise catalog's left sections are `ExerciseSectionList` (molecule, `workouts` area), typed to its three
  exercise sections.

## Goals / Non-Goals

**Goals:**

- One exercise editor for workouts and templates: same organisms, same handlers, different use cases.
- No import from one presentation domain area into another; anything two areas need lives in `presentation/common`.
- Keep "Desde cero" in both create dialogs byte-for-byte in behaviour.

**Non-Goals:**

- Caching templates across screens, or re-reading a template after each mutation (local state is updated from each
  answer, as the workout screen does).
- Any app screen or app copy.

## Decisions

### Contract

- `pnpm --filter @weight-tracker/core openapi:refresh` (API running on `:5199`) then `openapi:generate`, committed
  alone. Expected schemas: `WorkoutTemplateResponse`, `CreateWorkoutTemplateRequest`, `UpdateWorkoutTemplateRequest`,
  `CreateWorkoutFromTemplateRequest`. If a name differs, the aliases follow the generated name; nothing is hand-written.

### Core

- `core/src/domain/workout-templates/workout-template.ts` (re-exported from `domain/index.ts`):
  - `WorkoutTemplate`, `CreateWorkoutTemplateRequest`, `UpdateWorkoutTemplateRequest`,
    `CreateWorkoutFromTemplateRequest` -> aliases of the generated schemas.
  - Inputs: `GetWorkoutTemplatesInput = { trainerId?: string }`, `GetWorkoutTemplateByIdInput = { id }`,
    `UpdateWorkoutTemplateInput = UpdateWorkoutTemplateRequest & { id }`, `DeleteWorkoutTemplateInput = { id }`,
    `AddExerciseToWorkoutTemplateInput`, `UpdateWorkoutTemplateExerciseInput`,
    `RemoveExerciseFromWorkoutTemplateInput`, `ReorderWorkoutTemplateExercisesInput`,
    `CreateWorkoutTemplateSuperSeriesInput`, `BreakWorkoutTemplateSuperSeriesInput` (the workout inputs with
    `templateId` instead of `workoutId`), `CreateWorkoutFromTemplateInput = CreateWorkoutFromTemplateRequest &
    { templateId }`, `CreateWorkoutTemplateFromWorkoutInput = { workoutId }`.
  - The exercise and superseries bodies reuse `AddExerciseRequest`, `ReorderWorkoutExercisesRequest`,
    `CreateSuperSeriesRequest`, `SuperSeries`, `SuperSeriesId` and `WorkoutExerciseDetail` from the workout domain.
- `WorkoutTemplateRepositoryInterface` (`core/src/application/ports/workout-template-repository-interface.ts`): one
  method per endpoint, each `Promise<Result<T, DomainError>>`. `createFromWorkout(workoutId)` lives here, not in the
  workout port: its result is a template.
- `WorkoutTemplateRepository` (`core/src/infrastructure/repositories/workout-template-repository.ts`) modelled method
  by method on `WorkoutRepository`; `getAll` adds `?trainerId=` only when given, like `getWorkouts`' query; every error
  through `mapHttpError`. No mapper change.
- Use cases in `core/src/application/use-cases/workout-templates/`, each modelled on its workout twin (split the path
  id from the body, return the repository `Result`, no validation): `GetWorkoutTemplatesUseCase`,
  `GetWorkoutTemplateByIdUseCase`, `CreateWorkoutTemplateUseCase`, `UpdateWorkoutTemplateUseCase`,
  `DeleteWorkoutTemplateUseCase`, `AddExerciseToWorkoutTemplateUseCase`, `UpdateWorkoutTemplateExerciseUseCase`,
  `RemoveExerciseFromWorkoutTemplateUseCase`, `ReorderWorkoutTemplateExercisesUseCase`,
  `CreateWorkoutTemplateSuperSeriesUseCase`, `BreakWorkoutTemplateSuperSeriesUseCase`,
  `CreateWorkoutFromTemplateUseCase`, `CreateWorkoutTemplateFromWorkoutUseCase`.
- Validation constants: `WORKOUT_TEMPLATE_NAME_MAX_LENGTH = 256`, `WORKOUT_TEMPLATE_DESCRIPTION_MAX_LENGTH = 1024` in
  `core/src/application/validation/workout-template-rules.ts`, mirroring `workout-rules.ts` (same values; kept separate
  because they mirror different API value objects).

### Dependencies keys

`workoutTemplateRepository` plus `getWorkoutTemplatesUseCase`, `getWorkoutTemplateByIdUseCase`,
`createWorkoutTemplateUseCase`, `updateWorkoutTemplateUseCase`, `deleteWorkoutTemplateUseCase`,
`addExerciseToWorkoutTemplateUseCase`, `updateWorkoutTemplateExerciseUseCase`,
`removeExerciseFromWorkoutTemplateUseCase`, `reorderWorkoutTemplateExercisesUseCase`,
`createWorkoutTemplateSuperSeriesUseCase`, `breakWorkoutTemplateSuperSeriesUseCase`,
`createWorkoutFromTemplateUseCase`, `createWorkoutTemplateFromWorkoutUseCase`. Registered in
`backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts` after the workout keys; added
to both `test/support/render-with-providers.tsx` placeholders and both `test/infrastructure/container.test.ts`.

### Backoffice: one editor, two owners

- **Owner-aware hooks.** `useSaveWorkoutExercise`, `useRemoveWorkoutExercise`, `useReorderWorkoutExercises` and
  `useSuperSeries` take `owner: "workout" | "template"` (default `"workout"`, so current callers do not change) and
  resolve the matching use case key. The call signatures keep the owner id as first argument. Their return shapes stay
  the same (reorder already returns `boolean`, not the answered body).
- **Shared editor state.** The exercise part of `LoadedWorkout` (saved entries, open dialog union, selection, break
  target, add-flow focus, and the add / edit / remove / reorder / group / break handlers) moves to a state hook
  `useExercisesEditor(owner, ownerId, initialEntries, catalog)` in `presentation/workouts/hooks/`. `WorkoutView` and
  `WorkoutTemplateView` both call it and render the same organisms (`WorkoutExercisesCard`, `WorkoutExercisesList`,
  `ExerciseCatalogDialog`, `ExerciseConfigurationDialog`, `BreakSuperSeriesDialog`). Alternative rejected: an
  organism holding the hooks, which would break "components are presentational, requests live in views and hooks";
  and a second copy of the editor.
- **Properties card.** `WorkoutDetailsCard` (organism) gains `title`, an optional `showWeek` (default `true`) and an
  optional `secondaryAction: ReactNode` rendered right before "Guardar". The template screen passes
  `showWeek={false}`; the workout screen passes the "Guardar como plantilla" `Button` (atom, `variant="outlined"`).

### Backoffice: components and placement

Imports flow downward; only atoms import MUI widgets.

- `SectionList` -> molecule, **moved and generalised** from `workouts/components/molecules/exercise-section-list.tsx`
  to `common/components/molecules/section-list.tsx`, generic over the section value. `ExerciseCatalogBrowser` uses it
  with the exercise sections; both create dialogs with `"SCRATCH" | "TEMPLATE"`.
- `WorkoutTemplatePicker` -> organism, new -> `common/components/organisms/workout-template-picker.tsx`: `List` atom
  of selectable rows (name, description, "{{count}} ejercicios" from `exercises.length`), loading skeleton, empty and
  `LoadError` states, an optional `hint` shown instead of the list (admin without trainer). Presentational.
- `CreateWorkoutDialog` (`workouts`, organism) and `CreateWeekWorkoutDialog` (`mesocycles`, organism) -> extended:
  `SectionList` on the left, today's form under `SCRATCH`, the picker (plus Semana / Atleta / admin Entrenador in the
  standalone dialog) under `TEMPLATE`. The dialog widens to `md` to fit the sidebar.
- `WorkoutTemplatesTable` -> organism, new -> `workouts/components/organisms/workout-templates-table.tsx`, modelled on
  `workouts-table.tsx`, `showTrainer` prop, trainer name resolved by the view from `useAssignees("ADMIN").trainers`.
- `CreateWorkoutTemplateDialog` -> organism, new -> `workouts/components/organisms/create-workout-template-dialog.tsx`,
  modelled on `create-workout-dialog.tsx` reduced to Nombre, Descripción and admin Entrenador (`PersonPicker` molecule).
- `DeleteWorkoutTemplateDialog` -> organism, new -> `workouts/components/organisms/delete-workout-template-dialog.tsx`,
  modelled on `break-super-series-dialog.tsx` (presentational).
- Views: `workouts/views/workout-templates-view.tsx`, `workouts/views/workout-template-view.tsx`. Routes:
  `src/app/workout-templates/page.tsx`, `src/app/workout-template/page.tsx` (default-export the view only).
- Drawer: `navigation-drawer.tsx` gives `menu.library.workoutTemplates` the `href` `/workout-templates`.

### Backoffice: state hooks and held state

| Hook | Folder | Holds | Changed by | Resets |
| --- | --- | --- | --- | --- |
| `useWorkoutTemplates` | `workouts/hooks` | list, status, errorCode | mount, `reload` | screen exit |
| `useWorkoutTemplate` | `workouts/hooks` | template, status, errorCode | mount, `reload` | screen exit |
| `useCreateWorkoutTemplate` | `workouts/hooks` | saving, errorCode | dialog `create` | dialog close |
| `useUpdateWorkoutTemplate` | `workouts/hooks` | saving, saved, errorCode | `save`, `clearSaved` | screen exit |
| `useDeleteWorkoutTemplate` | `workouts/hooks` | pending, errorCode | `remove` | screen exit |
| `useSaveWorkoutAsTemplate` | `workouts/hooks` | pending, created template id, errorCode | `save`, field edit | screen exit |
| `useExercisesEditor` | `workouts/hooks` | entries, dialog, selection, break target | editor handlers | screen exit |
| `useWorkoutTemplateOptions(trainerId?, enabled)` | `common/hooks` | templates, status, errorCode | `enabled` / `trainerId` change, `reload` | dialog close |
| `useCreateWorkoutFromTemplate` | `common/hooks` | saving, errorCode | `create` | dialog close |

All mutation hooks follow `use-update-workout.ts` (status / errorCode cleared on start, `runWithSession`). The dialogs'
section, selected template and form values are local `useState`, reset on close. Nothing outlives its screen or logout.

"Guardar como plantilla" is disabled while the properties form differs from the last saved values (the view already
holds both), which needs no new state.

### Errors and copy

Codes involved: `UNAUTHORIZED` (session handling), `FORBIDDEN`, `VALIDATION`, `NOT_FOUND`, `CONFLICT`, `NETWORK`,
`UNEXPECTED`. No new code; messages reuse the existing per-code copy in `es` and `en`.

New keys (`es` / `en`), in `workouts.json` unless stated:

- `workoutTemplates.title` "Plantillas de entrenamiento" / "Workout templates"; `.new` "Nueva plantilla" / "New
  template"; `.columns.name|description|exercises|trainer` "Nombre", "Descripción", "Ejercicios", "Entrenador" /
  "Name", "Description", "Exercises", "Trainer"; `.edit` "Editar {{name}}" / "Edit {{name}}"; `.empty` "Todavía no hay
  plantillas." / "No workout templates yet."
- `createTemplate.title` "Nueva plantilla" / "New template"; `.create` "Crear" / "Create"; `.cancel` "Cancelar" / "Cancel".
- `workoutTemplate.title` "Editar plantilla" / "Edit template"; `.back` "Volver" / "Back"; `.details.title`
  "Propiedades de la plantilla" / "Template properties"; `.saved` "Plantilla guardada." / "Template saved.";
  `.delete` "Eliminar plantilla" / "Delete template"; `.deleteConfirm.title` "¿Eliminar esta plantilla?" / "Delete this
  template?"; `.deleteConfirm.body` "Los entrenamientos creados desde ella no cambian." / "Workouts created from it do
  not change."; `.notFound` "No hemos encontrado esta plantilla." / "We couldn't find this template."; `.backToList`
  "Volver a plantillas" / "Back to templates".
- `workout.saveAsTemplate` "Guardar como plantilla" / "Save as template"; `.saveAsTemplateHint` "Guarda los cambios
  antes de crear la plantilla." / "Save your changes before creating the template."; `.savedAsTemplate` "Plantilla
  creada." / "Template created."; `.viewTemplate` "Ver plantilla" / "View template".
- `common.json`: `workoutTemplatePicker.sections.scratch|template` "Desde cero" / "From scratch", "Desde plantilla" /
  "From template"; `.exerciseCount_one|_other` "{{count}} ejercicio" / "{{count}} ejercicios", "{{count}} exercise" /
  "{{count}} exercises"; `.empty` "No hay plantillas disponibles." / "No templates available."; `.pickTrainer` "Elige
  un entrenador para ver sus plantillas." / "Choose a trainer to see their templates."; `.required` "Elige una
  plantilla." / "Choose a template."
- `menu.library.workoutTemplates` already exists in both languages.

### Tests

Patterns are unchanged; no test helper changes beyond the new container placeholders.

- Core: one test per use case (modelled on `reorder-workout-exercises-use-case.test.ts`) and
  `workout-template-repository.test.ts` (modelled on `workout-repository.test.ts`: method, path, body, success, every
  error code, `NETWORK`).
  **Documented exception:** the repository test drives the same four checks per method from one table of operations
  (`describe.each`), and each use case test uses a fake that records every call by method name instead of a hand-typed
  fake with an `otherCalls` counter. Coverage and the AAA phases are the same; the shape avoids thirteen copies of a
  thirteen-method fake.
- Backoffice components: `workout-templates-view.test.tsx`, `workout-template-view.test.tsx`, extended
  `assigned-workouts-view.test.tsx` (dialog sections), `mesocycle-view.test.tsx` (week dialog sections),
  `workout-view.test.tsx` (save as template), drawer test. Existing workout-view tests must keep passing untouched after
  the `useExercisesEditor` extraction: they are the regression net for it.
- Backoffice E2E: `workout-templates.spec.ts` (drawer -> list -> create -> add exercise), plus a "Desde plantilla" flow
  in `assigned-workouts.spec.ts` and a "Guardar como plantilla" flow in `workout.spec.ts`.

### Deviations found while implementing

- `WorkoutExercisesEditor` -> organism, new -> `workouts/components/organisms/workout-exercises-editor.tsx`. Renders
  the exercises card, its list and the catalog, configuration and break dialogs from the `useExercisesEditor` state,
  so both screens share the markup as well as the logic. Presentational.
- `ExerciseConfigurationDialog` saves through its own hook, so it also takes the `owner` prop (default `workout`).
- `SectionsLayout` and `CreateWorkoutSections` -> molecule -> `common/components/molecules/create-workout-sections.tsx`,
  shared by both create dialogs, since the mesocycles area may not import from `workouts`.
- `CreateWeekWorkoutDialog` stays presentational for the creation (`onCreateFromTemplate`), but reads the template
  options itself through `useWorkoutTemplateOptions`, once "Desde plantilla" is first shown.
- The three E2E flows live in one `e2e/workout-templates.spec.ts` with its own interceptors, instead of being split
  into `assigned-workouts.spec.ts` and `workout.spec.ts`.
- The admin trainer picker of the create template dialog shows the existing "Selecciona un entrenador." message, as
  the create workout dialog does.

## Risks / Trade-offs

- [Extracting `useExercisesEditor` from a 540-line view can regress the workout screen] → extract it first, with no
  behaviour change, and run the full workout view tests and `workout.spec.ts` before building the template screen.
- [Generated schema names may differ from the expected ones] → the aliases follow the generated names; the design
  names only the endpoints as fixed.
- [Admin trainer names need the users list, and a template's trainer may be missing from it (e.g. deactivated)] →
  `useAssignees("ADMIN")` already loads the users for the pickers; a trainer id with no match shows "—", never the id.
  If the users read fails, the table still shows its rows with "—" in the trainer column.
- [Templates live in the `workouts` area, not their own] → accepted to avoid moving the whole editor to `common`; a
  later split can move them if the area grows.
- [Section list move touches the exercise catalog] → covered by the existing catalog tests.

## Migration Plan

No data migration. Deploy is the usual static build; rollback is reverting the commits. The snapshot commit can stay
even if the screens are reverted.
