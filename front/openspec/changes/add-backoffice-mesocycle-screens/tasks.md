# Tasks

## 1. Core: structural rules

- [x] 1.1 Add `core/src/application/validation/mesocycle-rules.ts` (limits, form value types, `validateMesocycleDetailsForm`, `validateCreateMesocycleForm`, `validateMesocycleWeekForm`) shaped like `workout-rules.ts`, and export it from the validation barrel; verify `pnpm --filter @weight-tracker/core typecheck`
- [x] 1.2 Add `core/test/application/validation/mesocycle-rules.test.ts` mirroring `workout-rules.test.ts`: required, trimmed max lengths (256 / 4000), invalid date, duration 0 / 1 / 52 / 53 / non-integer, athlete uuid, trainer only `withTrainer`, week type and objective; verify `pnpm --filter @weight-tracker/core test`
- [x] 1.3 Review core against `workout-rules.ts`: no use case, port, repository or `Dependencies` change; no hand-written domain type

## 2. Backoffice: move the workouts list to `/assigned-workouts`

- [x] 2.1 Rename `presentation/workouts/views/assigned-plans-view.tsx` to `assigned-workouts-view.tsx` (`AssignedWorkoutsView`), add `app/assigned-workouts/page.tsx`, and move `workouts.json` keys `assignedPlans.*` → `assignedWorkouts.*` in `es` and `en` (title "Entrenamientos" / "Workouts"); update `workouts-table.tsx` keys
- [x] 2.2 Point `workout-view.tsx` "Volver" and not-found links to `/assigned-workouts` and reword `workout.backToList` ("Volver a entrenamientos" / "Back to workouts")
- [x] 2.3 Move `test/presentation/workouts/views/assigned-plans-view.test.tsx` to `assigned-workouts-view.test.tsx` with the new route and copy, and update `workout-view.test.tsx` link assertions; verify `pnpm --filter backoffice test`

## 3. Backoffice: navigation drawer

- [x] 3.1 Add `common.json` key `menu.programming.assignedWorkouts` in `es` and `en`, and the drawer item (`ListAltOutlined`, `/assigned-workouts`) after "Mesociclos" in `navigation-drawer.tsx`
- [x] 3.2 Extend the drawer test: order of the Programación section, "Entrenamientos" links to `/assigned-workouts`, each of the two entries is selected only on its own route, English label; verify `pnpm --filter backoffice test`

## 4. Backoffice: move the pickers to `common`

- [x] 4.1 Move `person-picker.tsx` to `presentation/common/components/molecules/` and `use-workout-assignees.ts` to `presentation/common/hooks/use-assignees.ts` (`useAssignees`, `Assignee`); update `create-workout-dialog.tsx` and test imports only; verify the create workout dialog tests pass unchanged

## 5. Backoffice: mesocycles list at `/assigned-plans`

- [x] 5.1 Create the `mesocycles` namespace (`es`, `en`) with the `assignedPlans.*` and `trainingWeekType.*` keys and register it in `src/i18n/resources.ts`; verify the translation parity test passes
- [x] 5.2 Add `presentation/mesocycles/hooks/use-mesocycles.ts` (shaped like `use-workouts.ts`) and `use-mesocycle-date.ts` (UTC-safe `Intl` formatting as in `useWeekLabel`)
- [x] 5.3 Add organism `mesocycles-table.tsx` (name, athlete, `ADMIN`-only trainer, start date, plural duration, defined / total, edit icon button to `/mesocycle?id=`, empty text) shaped like `workouts-table.tsx`
- [x] 5.4 Add view `presentation/mesocycles/views/assigned-plans-view.tsx` ("Nuevo mesociclo" link button to `/mesocycle`, skeleton, `LoadError`, waits for the role) and rewire `app/assigned-plans/page.tsx` to it
- [x] 5.5 Add `test/presentation/mesocycles/views/assigned-plans-view.test.tsx` covering every `backoffice-assigned-plans` scenario: rows and order, no workout request, every column, email fallback, time-zone safe date, plural, `ADMIN` trainer column, `TRAINER` header and cells absent, empty, failure + retry, button link, edit link, English; verify `pnpm --filter backoffice test`

## 6. Backoffice: mesocycle editor molecules

- [x] 6.1 Add `presentation/mesocycles/weeks.ts` (`nextFreeWeekOf`, sorted weeks) and `test/presentation/mesocycles/weeks.test.ts` (gap first, none free, unsorted input)
- [x] 6.2 Add molecules `training-week-type-chip.tsx` (theme colour per type, translated label), `duration-field.tsx` (− / value / +, disabled at 1 and 52, accessible labels "Reducir duración" / "Aumentar duración") and `mesocycle-week-list-item.tsx` (chip, "Semana N", objective, "Editar semana N")

## 7. Backoffice: mesocycle editor hooks and organisms

- [x] 7.1 Add hooks `use-mesocycle.ts` (with `replace`), `use-create-mesocycle.ts`, `use-update-mesocycle.ts` (`saved` flag) and `use-save-mesocycle-week.ts` (define or redefine), shaped like `use-workout.ts`, `use-create-workout.ts`, `use-update-workout.ts` and `use-save-workout-exercise.ts`, all through `runWithSession` and ignoring late answers
- [x] 7.2 Add organism `mesocycle-details-card.tsx` shaped like `workout-details-card.tsx`: pickers (create; trainer only for `ADMIN`) or read-only athlete / `ADMIN`-only trainer (edit), name and objective with counters, date field, duration field, error / saved alerts, "Crear mesociclo" or "Guardar"
- [x] 7.3 Add organism `mesocycle-weeks-card.tsx` shaped like `workout-exercises-card.tsx`: title, subtitle, "Añadir semana" (disabled when locked or no free week), list, empty text, locked text
- [x] 7.4 Add organism `mesocycle-week-dialog.tsx` shaped like `exercise-configuration-dialog.tsx`: title "Semana N", type select, objective, inline error, "Cancelar" / "Guardar" disabled while saving
- [x] 7.5 Add the `mesocycle.*` and `weeks.*` keys in `es` and `en`; verify translation parity

## 8. Backoffice: mesocycle editor view and route

- [x] 8.1 Add `presentation/mesocycles/views/mesocycle-view.tsx` shaped like `workout-view.tsx` (header, two stacked cards, `useSearchParams` in `Suspense`, dialog state union, not-found and `LoadError`), with create → `router.replace("/mesocycle?id=<id>")`, update and week saves feeding `replace`; add `app/mesocycle/page.tsx`
- [x] 8.2 Add `test/presentation/mesocycles/views/mesocycle-view.test.tsx` covering every `backoffice-mesocycle-editor` scenario: modes, header, card order, fields and counter, stepper bounds, trainer picker per role and no `trainerId` for `TRAINER`, picker loading / failure, validation messages with nothing sent, creation payload and `replace`, creation in progress and failure, locked weeks card, edit-mode data per role, update payload, saved and 409, weeks order and empty, next free week and disabled add, define and redefine payloads, failed week save, cancel, not found, read retry, English; verify `pnpm --filter backoffice test`
- [x] 8.3 Review the backoffice against its references (`workout-view.tsx`, `workout-details-card.tsx`, `workout-exercises-card.tsx`, `exercise-configuration-dialog.tsx`, `workouts-table.tsx`): atomic levels and folders, imports flow downward, only atoms import MUI widgets, no import from `presentation/workouts` in `presentation/mesocycles`, no `@weight-tracker/core/infrastructure` import, every string in `es` and `en`

## 9. Backoffice: E2E

- [x] 9.1 Add `e2e/assigned-workouts.spec.ts` from the workout flow of `e2e/assigned-plans.spec.ts`, rewrite `assigned-plans.spec.ts` for the mesocycles list, and update `e2e/workout.spec.ts` if it asserts the back link
- [x] 9.2 Add `e2e/mesocycles.spec.ts`: drawer → "Mesociclos" → "Nuevo mesociclo" → create → add a week → edit it, every API call intercepted with `page.route`; verify `pnpm test:e2e`

## 10. Integration

- [x] 10.1 Run the `.stpr/template/` drift check from `front/` and confirm no drift
- [x] 10.2 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/` and confirm it passes
