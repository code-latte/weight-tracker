# Tasks

## 1. API contract snapshot

- [x] 1.1 With the API running on `:5199`, run `pnpm --filter @weight-tracker/core openapi:refresh` then `openapi:generate`; review the diff of `core/openapi/v1.json` and `core/src/contract/generated` (the `/workout-templates` paths, `POST /workouts/{workoutId}/templates`, and the template schemas) and commit both alone; verify `pnpm --filter @weight-tracker/core typecheck` passes and no generated file was edited by hand

## 2. Core domain, port and repository

- [x] 2.1 Create `core/src/domain/workout-templates/workout-template.ts` with the aliases and inputs from the design (aliases follow the generated schema names) and re-export it from `core/src/domain/index.ts`; verify with the core typecheck
- [x] 2.2 Add `WORKOUT_TEMPLATE_NAME_MAX_LENGTH` (256) and `WORKOUT_TEMPLATE_DESCRIPTION_MAX_LENGTH` (1024) in `core/src/application/validation/workout-template-rules.ts`, exported like `workout-rules.ts`, with a test modelled on `workout-rules.test.ts`; verify with `pnpm --filter @weight-tracker/core test`
- [x] 2.3 Create `core/src/application/ports/workout-template-repository-interface.ts` with one method per endpoint (list, get, create, update, delete, add / update / remove exercise, reorder, create / break superseries, create workout from template, create from workout), each returning `Result`; export it; verify with the core typecheck
- [x] 2.4 Implement `core/src/infrastructure/repositories/workout-template-repository.ts` modelled method by method on `workout-repository.ts` (`?trainerId=` only when given, `ok(undefined)` on 204, every error through `mapHttpError`) and export it; verify with the core typecheck
- [x] 2.5 Add `core/test/infrastructure/repositories/workout-template-repository.test.ts` modelled on `workout-repository.test.ts`: for every method the method, path, query and body; the success result; `400`→`VALIDATION`, `401`→`UNAUTHORIZED`, `403`→`FORBIDDEN`, `404`→`NOT_FOUND`, `409`→`CONFLICT`, `500`→`UNEXPECTED`, rejected request→`NETWORK`; AAA with one Act each; verify with the core tests

## 3. Core use cases

- [x] 3.1 Create the thirteen use cases from the design in `core/src/application/use-cases/workout-templates/`, each modelled on its workout twin (split the path id from the body, return the repository `Result`, no validation), with an `index.ts` re-exported from `use-cases/index.ts`; verify with the core typecheck
- [x] 3.2 Add `workoutTemplateRepository` and the thirteen use case keys to `core/src/application/dependencies.ts`; verify with the core typecheck
- [x] 3.3 Add one test per use case in `core/test/application/use-cases/workout-templates/` modelled on `reorder-workout-exercises-use-case.test.ts` (inputs forwarded unchanged, success and failure returned unchanged, repository called once); AAA; verify with the core tests
- [x] 3.4 Review core against `WorkoutRepository`, the workout use cases and their tests; verify the shape matches with no undocumented difference

## 4. Wiring in both fronts

- [x] 4.1 Register the repository and every new use case in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts` after the workout keys, add the placeholders to both `test/support/render-with-providers.tsx` and the keys to both `test/infrastructure/container.test.ts`; verify `pnpm typecheck` and both container tests pass

## 5. Backoffice: shared editor refactor (no behaviour change)

- [x] 5.1 Give `useSaveWorkoutExercise`, `useRemoveWorkoutExercise`, `useReorderWorkoutExercises` and `useSuperSeries` an `owner: "workout" | "template"` parameter (default `"workout"`) that picks the use case key; verify the existing workout view tests pass unchanged
- [x] 5.2 Extract the exercise part of `LoadedWorkout` into `presentation/workouts/hooks/use-exercises-editor.ts` (entries, dialog union, selection, break target, add-flow focus and every exercise handler) and make `workout-view.tsx` use it; verify `workout-view.test.tsx` and `pnpm --filter backoffice exec playwright test workout.spec.ts` pass unchanged
- [x] 5.3 Extend `WorkoutDetailsCard` (organism) with `title`, optional `showWeek` (default `true`) and optional `secondaryAction` rendered right before "Guardar"; verify the existing workout view tests pass
- [x] 5.4 Move `exercise-section-list.tsx` to `presentation/common/components/molecules/section-list.tsx`, generic over the section value, and update `ExerciseCatalogBrowser`; verify the exercise catalog and workout view tests pass and lint finds no MUI widget import outside atoms

## 6. Backoffice: copy

- [x] 6.1 Add the `workouts.json` and `common.json` keys from the design to `backoffice/src/i18n/es` and `en` (plural forms for the exercise count); verify the translation parity test passes

## 7. Backoffice: templates list and editor

- [x] 7.1 Create the hooks `useWorkoutTemplates`, `useWorkoutTemplate`, `useCreateWorkoutTemplate`, `useUpdateWorkoutTemplate` and `useDeleteWorkoutTemplate` in `presentation/workouts/hooks/`, modelled on `use-workouts.ts`, `use-workout.ts`, `use-create-workout.ts` and `use-update-workout.ts`; verify with the backoffice typecheck
- [x] 7.2 Create `WorkoutTemplatesTable` (organism) modelled on `workouts-table.tsx`, with `showTrainer` and an "Editar <name>" row action; verify with the backoffice typecheck
- [x] 7.3 Create `CreateWorkoutTemplateDialog` (organism) modelled on `create-workout-dialog.tsx`, reduced to Nombre, Descripción and the admin-only Entrenador `PersonPicker`, with the structural checks of the `backoffice-workout-templates` spec; verify with the backoffice typecheck
- [x] 7.4 Create `DeleteWorkoutTemplateDialog` (organism) modelled on `break-super-series-dialog.tsx`; verify with the backoffice typecheck
- [x] 7.5 Create `workouts/views/workout-templates-view.tsx` (heading, "Nueva plantilla", table, loading / empty / `LoadError`, admin trainer names from `useAssignees("ADMIN")`) and the route `src/app/workout-templates/page.tsx`; verify with the backoffice typecheck
- [x] 7.6 Create `workouts/views/workout-template-view.tsx` (`?id=` in `Suspense`, header with "Volver" and "Eliminar plantilla", `WorkoutDetailsCard` with `showWeek={false}`, the exercise organisms fed by `useExercisesEditor("template", …)`, not-found and failure states, delete flow) and the route `src/app/workout-template/page.tsx`; verify with the backoffice typecheck
- [x] 7.7 Give the drawer's `menu.library.workoutTemplates` the `href` `/workout-templates`; verify with the drawer test from 10.1

## 8. Backoffice: create a workout from a template

- [x] 8.1 Create `useWorkoutTemplateOptions` and `useCreateWorkoutFromTemplate` in `presentation/common/hooks/`, modelled on `use-workouts.ts` and `use-create-workout.ts`; verify with the backoffice typecheck
- [x] 8.2 Create `WorkoutTemplatePicker` (organism) in `presentation/common/components/organisms/` with selectable rows (name, description, exercise count), loading, empty, `LoadError` and `hint` states; verify with the backoffice typecheck and lint
- [x] 8.3 Extend `CreateWorkoutDialog` with `SectionList` ("Desde cero" default, "Desde plantilla"), keeping the scratch form unchanged and, under "Desde plantilla", Semana, Atleta, admin Entrenador (which drives `trainerId` and clears the selection) and the picker; on "Crear" send one creation from the template with the athlete and the Wednesday of the week; verify with the backoffice typecheck
- [x] 8.4 Extend `CreateWeekWorkoutDialog` the same way, with only the picker under "Desde plantilla" (admin: the mesocycle's trainer id) and a creation carrying the athlete, mesocycle id and week number; verify with the backoffice typecheck

## 9. Backoffice: save a workout as a template

- [x] 9.1 Create `useSaveWorkoutAsTemplate` in `presentation/workouts/hooks/`, modelled on `use-update-workout.ts`, resolving to the new template id; verify with the backoffice typecheck
- [x] 9.2 In `workout-view.tsx`, pass the outlined "Guardar como plantilla" `Button` as `secondaryAction`, disabled while the form differs from the saved values (with the hint) or the request is pending, and show "Plantilla creada." with the "Ver plantilla" link on success and the error message on failure; verify with the backoffice typecheck

## 10. Backoffice tests and review

- [x] 10.1 Extend the drawer test: "Plantillas de entrenamiento" links to `/workout-templates`, is selected there, and no other entry is; AAA; verify with `pnpm --filter backoffice test`
- [x] 10.2 Add `backoffice/test/presentation/workouts/views/workout-templates-view.test.tsx` covering every scenario of the `backoffice-workout-templates` spec, including that a trainer gets no "Entrenador" header or cell and no users request, and the create dialog per role; AAA; verify with the backoffice tests
- [x] 10.3 Add `workout-template-view.test.tsx` covering every scenario of the `backoffice-workout-template-editor` spec, asserting each exercise request goes to a template use case and none to a workout one; AAA; verify with the backoffice tests
- [x] 10.4 Extend `assigned-workouts-view.test.tsx` (or the dialog's own test, whichever holds the create dialog cases today) with every scenario of the `backoffice-workout-creation` delta, both roles; AAA; verify with the backoffice tests
- [x] 10.5 Extend `mesocycle-view.test.tsx` with every scenario of the `backoffice-mesocycle-editor` delta, both roles; AAA; verify with the backoffice tests
- [x] 10.6 Extend `workout-view.test.tsx` with every scenario of the `backoffice-workout-editor` delta; AAA; verify with the backoffice tests
- [x] 10.7 Add `backoffice/e2e/workout-templates.spec.ts` (drawer → list → create → add an exercise), a "Desde plantilla" flow in `assigned-workouts.spec.ts` and a "Guardar como plantilla" flow in `workout.spec.ts`, all with `page.route`, asserting request paths and bodies; verify with `pnpm test:e2e`
- [x] 10.8 Review the backoffice against `assigned-workouts-view.tsx`, `workouts-table.tsx`, `create-workout-dialog.tsx`, `workout-view.tsx`, `break-super-series-dialog.tsx`, `use-update-workout.ts` and their tests; verify the only differences are those documented in the design (owner-aware hooks, `useExercisesEditor`, generic `SectionList`, templates in the `workouts` area)

## 11. App review

- [x] 11.1 Review the app: only `container.ts`, its test-support placeholders and its container test changed, with no screen, hook or copy; verify with `git diff --stat app/`

## 12. Verification

- [x] 12.1 Check `.stpr/template/` is still byte-identical to its counterparts with the drift loop in `AGENTS.md`
- [x] 12.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`, then `pnpm test:e2e`; verify all pass
