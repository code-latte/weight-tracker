# Tasks

Core and app need no change (see design.md, "Mapping to core and the fronts"). All work is in `backoffice/`; paths
below are relative to `backoffice/src/presentation/` unless stated. Tests mock the API as their tier's template does.

## 1. Shared pieces in `common`

- [x] 1.1 Add `common/initials.ts` with `initialsOf({ name, surname, email })` and `test/presentation/common/initials.test.ts` mirroring `full-name.test.ts` (name and surname, name only, blank name falls back to the email, trims and upper-cases); verify with `pnpm --filter backoffice test initials`
- [x] 1.2 Add the `Avatar` atom `common/components/atoms/avatar.tsx` shaped like `chip.tsx`; verify it typechecks
- [x] 1.3 Switch `common/components/organisms/user-menu.tsx` to the `Avatar` atom and `initialsOf`, dropping its local `initials`; verify `user-menu.test.tsx` passes unchanged
- [x] 1.4 Add `common:person.role.athlete` / `trainer` ("Atleta"/"Athlete", "Entrenador"/"Trainer") to `src/i18n/es/common.json` and `src/i18n/en/common.json`; verify the translation parity test passes
- [x] 1.5 Add the `PersonCard` molecule `common/components/molecules/person-card.tsx` (`name`, `initials`, `role`; content-sized outlined card, 32 px avatar, name `body2` semibold, role caption) per design.md; verify it typechecks and lints

## 2. Workout screen context

- [x] 2.1 Move `workouts/components/molecules/workout-context-strip.tsx` to `workouts/components/organisms/` and rewrite it: person cards for athlete and optional trainer, mesocycle card with caption and `Breadcrumbs` ("/", week `aria-current="page"`), no link, skeleton while loading; `WorkoutContextMesocycle` becomes `"loading" | { name; week }`
- [x] 2.2 Update `workouts/views/workout-view.tsx`: import the strip from `organisms`, build person props with `displayNameOf`, `initialsOf` and `common:person.role.*`, keep the `ADMIN`-only trainer rule, and make `mesocycleEntryOf` return `{ name, week }` from `workout.context.week`
- [x] 2.3 In `src/i18n/{es,en}/workouts.json` add `workout.context.week` and `workout.context.breadcrumb`, remove `workout.context.mesocycleWeek`, `workout.context.athlete`, `workout.context.trainer`; verify parity passes
- [x] 2.4 Update the strip block of `test/presentation/workouts/views/workout-view.test.tsx` to the `backoffice-workout-context` delta scenarios (initials, name, role caption; admin trainer card; trainer sees no trainer card; no trainer; breadcrumb with no link; standalone; loading; failed read; English); verify `pnpm --filter backoffice test workout-view` passes
- [x] 2.5 Review the workout strip against `workout-context-strip.tsx` (previous shape) and `load-error.tsx`: section label, wrap and spacing kept, organism level justified, no MUI widget used by two files imported outside an atom

## 3. Mesocycle screen

- [x] 3.1 In `src/i18n/{es,en}/mesocycles.json` add `mesocycle.context.label`, `weeks.workoutsTable.name`, `weeks.workoutsTable.exercises`, `weeks.editWorkout`, and remove `mesocycle.duration.decrease` / `increase`; verify parity passes
- [x] 3.2 Add the `MesocycleContextStrip` organism `mesocycles/components/organisms/mesocycle-context-strip.tsx` (athlete, optional trainer, same section and spacing as the workout strip)
- [x] 3.3 Rework `mesocycles/components/organisms/mesocycle-details-card.tsx`: `people` becomes `pickers | none`, drop `NameLine` and the names branch, first row Nombre / Fecha de inicio / Duración (`TextField type="number"` with min/max/step), second row Objetivo general `minRows={2}`, save bottom right
- [x] 3.4 Delete `mesocycles/components/molecules/duration-field.tsx`; verify nothing imports it (`pnpm typecheck`)
- [x] 3.5 Add the `MesocycleWeekWorkoutsTable` molecule `mesocycles/components/molecules/mesocycle-week-workouts-table.tsx` (Nombre, Ejercicios, edit `IconButton` rendered as a `NextLink` with `aria-label`, empty edit-column header as in `workouts-table.tsx`)
- [x] 3.6 Rework `mesocycles/components/molecules/mesocycle-week-list-item.tsx`: contained small create button, `WeekWorkouts.entries` replaced by `hasEntries`, expanded body renders the button then `children`
- [x] 3.7 Delete `mesocycles/components/molecules/mesocycle-week-workout-item.tsx`; verify nothing imports it
- [x] 3.8 Update `mesocycles/components/organisms/mesocycle-weeks-card.tsx` to build table rows from `WorkoutListItem` (`exerciseCount = exercises.length`, `href`, `editLabel`) and pass the table as the row's `children`
- [x] 3.9 Update `mesocycles/views/mesocycle-view.tsx`: render `MesocycleContextStrip` after `MesocycleHeader` in `LoadedMesocycle` only, pass `people: { kind: "none" }` in edit mode, build person props as the workout view does
- [x] 3.10 Update `test/presentation/mesocycles/views/mesocycle-view.test.tsx` to the `backoffice-mesocycle-editor` delta: stepper tests replaced by typed duration, `range` and required errors; names asserted in person cards, not in the details card; admin trainer card, trainer sees none; create mode has no person card; expanded body shows the button before the table; counts per row; "Editar Pierna" leads to `/workout?id=w-1`; empty week shows the create button; English copy; verify `pnpm --filter backoffice test mesocycle-view` passes
- [x] 3.11 Review the mesocycle pieces against their mapped references (`workout-details-card.tsx`, `workouts-table.tsx`, `workout-exercises-card.tsx`, the workout strip): layout, button variant, atomic levels, downward imports, no molecule-to-molecule import

## 4. End-to-end and integration

- [x] 4.1 Update `backoffice/e2e/mesocycles.spec.ts` (workout context: breadcrumb text and no mesocycle link; week: "Editar Pierna" `href` instead of the name link) and any affected assertion in `backoffice/e2e/workout.spec.ts`, with API calls intercepted by `page.route`; verify `pnpm test:e2e` passes
- [x] 4.2 Run the `.stpr/template/` drift check from `AGENTS.md` and confirm no drift
- [x] 4.3 Run the quality checklist in `AGENTS.md` against the change (atomic levels, translations in `es` and `en`, no business rule, static export intact)
- [x] 4.4 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/` and verify it passes
