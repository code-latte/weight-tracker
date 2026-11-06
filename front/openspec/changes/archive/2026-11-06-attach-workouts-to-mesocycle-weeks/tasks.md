# Tasks

## 0. Prerequisites

- [x] 0.1 Commit the refreshed `core/openapi/v1.json` and `core/src/contract/generated/api.ts` (with the two fixture fixes) as a step of their own; verify `git log -1 --stat` lists only those files
- [x] 0.2 Confirm `add-backoffice-mesocycle-screens` is archived, so `openspec/specs/backoffice-mesocycle-editor/` exists and `backoffice-workout-editor` holds its `/assigned-workouts` header; verify `openspec list --specs`

## 1. Core: domain aliases and structural rule

- [x] 1.1 Add `WorkoutById` to `core/src/domain/workouts/workout.ts` and `GetMesocycleWorkoutsInput` to `core/src/domain/mesocycles/mesocycle.ts`, both exported from the domain barrel; verify `pnpm --filter @weight-tracker/core typecheck`
- [x] 1.2 Add `WeekWorkoutFormValues` and `validateWeekWorkoutForm` to `workout-rules.ts` (name required ≤ 256 trimmed, description ≤ 1024, reusing the existing limits) and export them; extend `workout-rules.test.ts` with required, too-long name and too-long description cases; verify `pnpm --filter @weight-tracker/core test`

## 2. Core: reading a workout with its people

- [x] 2.1 Retype `WorkoutRepositoryInterface.getById`, `WorkoutRepository.getById` and `GetWorkoutByIdUseCase` to `WorkoutById`, and `getWorkoutByIdUseCase` in `dependencies.ts`; verify typecheck across `pnpm typecheck`
- [x] 2.2 Update the `getById` fixtures in `workout-repository.test.ts` and `get-workout-by-id-use-case.test.ts` to carry `athlete` and `trainer`, and assert they are returned unchanged; verify `pnpm --filter @weight-tracker/core test`
- [x] 2.3 Add a placed-without-date case to `create-workout-use-case.test.ts` and `workout-repository.test.ts` (`create` body holds no `workoutDate`); verify `pnpm --filter @weight-tracker/core test`

## 3. Core: listing a mesocycle's workouts

- [x] 3.1 Add `getWorkouts(mesocycleId)` to `MesocycleRepositoryInterface` and `MesocycleRepository` (`GET /mesocycles/{id}/workouts`), shaped like `getById`; cover success, empty list, 401, 403, 404, 500 and no response in `mesocycle-repository.test.ts`; verify `pnpm --filter @weight-tracker/core test`
- [x] 3.2 Add `GetMesocycleWorkoutsUseCase` in `use-cases/mesocycles/`, export it, add `getMesocycleWorkoutsUseCase` to `Dependencies`, and add `get-mesocycle-workouts-use-case.test.ts` mirroring `get-mesocycle-by-id-use-case.test.ts`; verify `pnpm --filter @weight-tracker/core test`
- [x] 3.3 Review core against `get-mesocycle-by-id-use-case.ts`, `MesocycleRepository.getById` and `workout-rules.ts`: no hand-written type, nothing throws, no business rule; verify the template drift check prints nothing

## 4. Wiring in both fronts

- [x] 4.1 Register `getMesocycleWorkoutsUseCase` in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`; add the key to both `container.test.ts` and to the placeholder map of both `test/support/render-with-providers.tsx`; verify `pnpm --filter backoffice test` and `pnpm --filter app test`
- [x] 4.2 Review the app against its current mesocycle wiring: only `container.ts` and its tests changed, no screen, hook or copy

## 5. Backoffice: atoms and copy

- [x] 5.1 Add a `collapse.tsx` atom in `presentation/common/components/atoms`, shaped like the existing wrapper atoms (an accordion was dropped: its summary is a `<button>`, so the row's edit button would nest inside it); verify `pnpm --filter backoffice typecheck`
- [x] 5.2 Add the `mesocycles.json` keys (`weeks.newWorkout`, `weeks.workoutCount_one|_other`, `weekWorkout.*`) and the `workouts.json` keys (`workout.context.*`) in `es` and `en`, as listed in design §5; verify the translation parity test passes

## 6. Backoffice: workouts in mesocycle weeks

- [x] 6.1 Add `use-mesocycle-workouts.ts` (grouped by `mesocycleWeekNumber`, API order kept, reload) and `use-create-week-workout.ts`, mirroring `use-workouts.ts` and `use-create-workout.ts`; verify `pnpm --filter backoffice typecheck`
- [x] 6.2 Add the `mesocycle-week-workout-item.tsx` molecule (workout name as a link to `/workout?id=`) and extend `mesocycle-week-list-item.tsx`: collapsible row with its count and an expand toggle beside the edit button when it has workouts, plain row with "Nuevo entrenamiento" otherwise, edit button not toggling the row; verify `pnpm --filter backoffice typecheck`
- [x] 6.3 Add the `create-week-workout-dialog.tsx` organism (Nombre, Descripción, Cancelar, Crear, `validateWeekWorkoutForm`, inline error) mirroring `create-workout-dialog.tsx` without pickers or week; verify `pnpm --filter backoffice typecheck`
- [x] 6.4 Extend `mesocycle-weeks-card.tsx` (workouts state, `LoadError` with retry, `onCreateWorkout`) and `mesocycle-view.tsx` (`useMesocycleWorkouts` in `LoadedMesocycle`, `{ kind: "workout" }` dialog, request built from the mesocycle with `trainerId` only for `ADMIN` and no date, `router.push` on success); verify `pnpm --filter backoffice typecheck`
- [x] 6.5 Extend `mesocycle-view.test.tsx`: workouts read once in edit mode and never in create mode; week with workouts collapsed with its count, expands to its workouts and the button; edit button does not expand; week without workouts not collapsible with the button; workout link href; dialog has no athlete, trainer or date; blank and too-long name; trainer request without `trainerId` and admin request with it, both without date; navigation on success; inline 409 keeps values; cancel sends nothing; workouts failure + retry with weeks still editable; English copy; verify `pnpm --filter backoffice test`

## 7. Backoffice: workout screen

- [x] 7.1 Change `use-workout.ts` and `LoadedWorkout` to hold `WorkoutById` (no merge needed: the view never replaces its workout with a save answer); verify `pnpm --filter backoffice typecheck`
- [x] 7.2 Make `WorkoutHeader` take `backHref` from the workout's placement; make `toUpdate` always send `mesocycleId` + `mesocycleWeekNumber` as read and send `workoutDate` only for a standalone workout; add `weekLocked` to `workout-details-card.tsx`, passed to `WeekField` as disabled; verify `pnpm --filter backoffice typecheck`
- [x] 7.3 Add `use-workout-mesocycle.ts` (one `getMesocycleByIdUseCase` read when placed, nothing otherwise) and the `workout-context-strip.tsx` molecule, rendered between the header and the cards; trainer passed only for `ADMIN`; verify `pnpm --filter backoffice typecheck`
- [x] 7.4 Extend `workout-view.test.tsx`: athlete always, email fallback; trainer for `ADMIN`, label and name absent for `TRAINER`, absent without trainer; placed workout reads its mesocycle once and links "Hipertrofia · Semana 2"; standalone reads no mesocycle; mesocycle failure leaves the editor working; "Volver" target per placement; placed save carries the placement and no date; standalone save unchanged with no placement; week field disabled when placed; names kept after a save; English strip; verify `pnpm --filter backoffice test`

## 8. Backoffice: E2E and review

- [x] 8.1 Extend `e2e/support/mesocycles-api.ts` to serve `GET /mesocycles/{id}/workouts` and `POST /workouts`, and extend `mesocycles.spec.ts`: open a mesocycle → "Nuevo entrenamiento" in a week → create → workout screen shows athlete and "<name> · Semana N" → "Volver" → the week row expands to the new workout; verify `pnpm test:e2e`
- [x] 8.2 Review the backoffice against its references (`create-workout-dialog.tsx`, `use-workouts.ts`, `workouts-table.tsx`, `mesocycle-details-card.tsx`, the current weeks card and row): atomic levels and folders, imports flowing downward, only atoms importing MUI widgets, no import between `mesocycles` and `workouts` areas, every string in `es` and `en`, static export intact

## 9. Integration

- [x] 9.1 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/` and verify all pass
- [x] 9.2 Run the template drift check from `front/AGENTS.md` and verify it prints nothing

## Workflow follow-up

- Archive this change after `add-backoffice-mesocycle-screens` and after review.
