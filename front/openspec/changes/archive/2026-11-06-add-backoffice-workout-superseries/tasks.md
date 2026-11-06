# Tasks

## 1. Core domain, port and repository

- [x] 1.1 Add `CreateSuperSeriesRequest` and `SuperSeries` (aliases of the generated schemas), `SuperSeriesId`, `CreateSuperSeriesInput` and `BreakSuperSeriesInput` to `core/src/domain/workouts/workout.ts` and re-export them; verify `core/src/contract/generated` is untouched and `pnpm --filter @weight-tracker/core typecheck` passes
- [x] 1.2 Add `createSuperSeries(workoutId, request): Promise<Result<SuperSeries, DomainError>>` and `breakSuperSeries(workoutId, superSeriesId): Promise<Result<void, DomainError>>` to `core/src/application/ports/workout-repository-interface.ts`; verify with the core typecheck
- [x] 1.3 Implement both in `core/src/infrastructure/repositories/workout-repository.ts`: `createSuperSeries` following `reorderExercises` (`POST /workouts/{workoutId}/superseries`, JSON body), `breakSuperSeries` following `removeExercise` (`DELETE /workouts/{workoutId}/superseries/{superSeriesId}`, no body, `ok(undefined)`), both through `mapHttpError`; verify with the core typecheck
- [x] 1.4 Extend `core/test/infrastructure/repositories/workout-repository.test.ts` for both methods: method, path and body; success result; `400` → `VALIDATION`, `401` → `UNAUTHORIZED`, `403` → `FORBIDDEN`, `404` → `NOT_FOUND`, `409` → `CONFLICT`, `500` → `UNEXPECTED`, rejected request → `NETWORK`; AAA with one Act each; verify with `pnpm --filter @weight-tracker/core test`

## 2. Core use cases

- [x] 2.1 Create `create-super-series-use-case.ts` and `break-super-series-use-case.ts` in `core/src/application/use-cases/workouts/` following `reorder-workout-exercises-use-case.ts` (split `workoutId` from the rest, return the repository `Result`, no validation) and export them from `index.ts`; verify with the core typecheck
- [x] 2.2 Add `createSuperSeriesUseCase` and `breakSuperSeriesUseCase` to `core/src/application/dependencies.ts`; verify with the core typecheck
- [x] 2.3 Add one test file per use case in `core/test/application/use-cases/workouts/` following `reorder-workout-exercises-use-case.test.ts`: inputs forwarded unchanged and in order, success and failure returned unchanged, repository called once; AAA; verify with the core tests
- [x] 2.4 Review core against `reorder-workout-exercises-use-case.ts`, `WorkoutRepository.reorderExercises` / `removeExercise` and their tests; verify the shape matches with no undocumented differences

## 3. Wiring in both fronts

- [x] 3.1 Register both use cases in `backoffice/src/infrastructure/container.ts` and `app/src/infrastructure/container.ts`, add their placeholders to both `test/support/render-with-providers.tsx`, and add the keys to both `test/infrastructure/container.test.ts`; verify `pnpm typecheck` and both container tests pass

## 4. Backoffice state hook and copy

- [x] 4.1 Create `backoffice/src/presentation/workouts/hooks/use-super-series.ts` following `use-reorder-workout-exercises.ts` (`pending`, `errorCode` cleared on start, `create` resolving to the superseries id or `null`, `breakUp` resolving to `true` on success, both through `runWithSession`); verify with `pnpm --filter backoffice typecheck`
- [x] 4.2 Add the `workout.superSeries.*` and `workout.exercises.dnd.groupName` keys from the design to `backoffice/src/i18n/es/workouts.json` and `en/workouts.json`; verify the translation parity test passes

## 5. Backoffice components

- [x] 5.1 Create the `Checkbox` atom in `backoffice/src/presentation/common/components/atoms/checkbox.tsx` following `switch.tsx`; verify with the backoffice typecheck
- [x] 5.2 Extend `SortableExerciseListItem` (molecule) with the optional `selection` prop rendering the `Checkbox` atom after the handle, labelled "Seleccionar para superserie"; verify the existing view tests still pass
- [x] 5.3 Create `SuperSeriesCard` (molecule) in `backoffice/src/presentation/workouts/components/molecules/super-series-card.tsx`: `useSortable` on the `Card` atom spanning the full row, the "Romper superserie" `IconButton` with the `Link` icon in the top-left corner, the "Reordenar superserie" handle beside it, and an inner `EXERCISE_GRID_COLUMNS` grid for its children; verify with the backoffice typecheck and lint (no MUI widget import outside atoms)
- [x] 5.4 Create `BreakSuperSeriesDialog` (organism) in `backoffice/src/presentation/workouts/components/organisms/break-super-series-dialog.tsx` with the layout of `end-trainer-athlete-dialog.tsx`, presentational (`open`, `pending`, `onCancel`, `onConfirm`), confirm disabled while `pending`; verify with the backoffice typecheck
- [x] 5.5 Extend `WorkoutExercisesList` (organism): build units from consecutive items sharing a `superSeriesId` (compared with `String()`, a run of one is ungrouped), sort over unit ids (`group:<first entry id>` for runs), render ungrouped units with `selection` and runs as `SuperSeriesCard` with plain `ExerciseListItem`s, keep continuous numbering, add the "Agrupar en superserie" `Button` with the `Link` icon when two or more are selected (disabled while `grouping`), the superseries error `Alert`, the new props from the design, and group names in the dnd-kit announcements; verify with the backoffice typecheck

## 6. Backoffice view

- [x] 6.1 In `LoadedWorkout` (`workout-view.tsx`) add `selectedIds`, toggled by the checkboxes and pruned when an entry is removed or joins a group; verify with the backoffice typecheck
- [x] 6.2 Add `handleGroup`: create with the selected entry ids in list order; on success set the returned id on the members, compute the contiguous order, clear the selection and, only if the order changed, send one reorder with the catalog ids, restoring the previous order with `inOrderOf` if it fails; on create failure change nothing; verify with the backoffice typecheck
- [x] 6.3 Change `handleReorder` to take unit ids and move the active unit's span of entries to the target unit's place, keeping the existing send and restore; verify with the backoffice typecheck
- [x] 6.4 Add the break flow: the group's link icon sets `breakTarget` and opens `BreakSuperSeriesDialog`; confirm calls `breakUp`, closes the dialog and, on success, sets `superSeriesId: null` on every entry holding that id; cancel clears `breakTarget`; verify with the backoffice typecheck

## 7. Backoffice tests and review

- [x] 7.1 Extend `backoffice/test/presentation/workouts/views/workout-view.test.tsx` with faked `createSuperSeriesUseCase` and `breakSuperSeriesUseCase` and one test per scenario of the `backoffice-workout-editor` delta: checkboxes only on ungrouped exercises and on a lone member; the button hidden with zero or one checked and shown with two; grouping sends the entry ids, then one reorder with the contiguous order, and shows the group card renumbered; no reorder when members are already together; a refused creation and a refused reorder; group card with "Romper superserie" and members without handle or checkbox; members apart give two cards; dialog opens, cancel sends nothing, confirm breaks and restores the checkboxes; a refused break; buttons disabled while in progress; a group card dragged with the keyboard moves all its members and sends one reorder. AAA with one Act each; verify with `pnpm --filter backoffice test`
- [x] 7.2 Extend `backoffice/e2e/workout.spec.ts` with one flow intercepted with `page.route`: check two exercises, click "Agrupar en superserie" and assert the `POST .../superseries` and `PUT .../exercises/order` bodies, then break the group and assert the `DELETE .../superseries/{id}` path; verify with `pnpm test:e2e`
- [x] 7.3 Review the backoffice against `use-reorder-workout-exercises.ts`, `switch.tsx`, `sortable-exercise-list-item.tsx`, `end-trainer-athlete-dialog.tsx` and the existing view test; verify the only differences are the presentational break dialog and the unit-based sortable list documented in the design

## 8. Verification

- [x] 8.1 Check `.stpr/template/` is still byte-identical to its counterparts with the drift loop in `AGENTS.md`
- [x] 8.2 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`, then `pnpm test:e2e`; verify all pass
