# Tasks

Core: no change (`GetExercisesUseCase` and `CreateExerciseUseCase` are already registered in the backoffice container).
App: no change.

## 1. Backoffice session catalog provider

- [x] 1.1 Add `backoffice/src/presentation/common/context/exercise-catalog-context.ts` (value `{ status, exercises, errorCode, load, reload, refresh, add, clear }`) modelled on `current-user-context.ts`; verify `pnpm --filter backoffice typecheck` passes
- [x] 1.2 Add `backoffice/src/presentation/common/components/exercise-catalog-provider.tsx` modelled on `current-user-provider.tsx`: idle until `load`, single flight, generation ref, `refresh` that keeps the list and queues one read when one is running, `add`, `clear` on `onEnd`; verify typecheck and lint pass
- [x] 1.3 Add `backoffice/src/presentation/common/hooks/use-exercise-catalog.ts` modelled on `use-current-user.ts` (throws outside the provider); verify typecheck passes
- [x] 1.4 Mount `ExerciseCatalogProvider` in `app-providers.tsx` inside `SessionProvider`, beside `CurrentUserProvider`, and in `test/support/render-with-providers.tsx` the same way (the documented test-helper exception); verify `backoffice/test/presentation/common/components/app-providers.test.tsx` and the existing view tests still pass
- [x] 1.5 Add `backoffice/test/presentation/common/components/exercise-catalog-provider.test.tsx` modelled on `theme-mode-provider.test.tsx` (probe component, `getExercisesUseCase` faked), covering: idle until `load`; `load` twice and two consumers -> one read; `reload` after a failure; `refresh` keeps the list while reading and replaces it on success; failed `refresh` keeps the list and status; `refresh` during a read sends exactly one more read after it; `clear` on session end empties it and ignores a late answer. Arrange / Act / Assert with one Act; verify the test passes

## 2. Backoffice background read and screens

- [x] 2.1 In `app-shell.tsx` (`SignedInContent`), call the catalog's `load()` in the effect that loads the current user; verify typecheck and lint pass (the view tests that render `AppShell` get an empty catalog from the helper's default; the read after sign-in is verified by the E2E in 3.1)
- [x] 2.2 Make `backoffice/src/presentation/workouts/hooks/use-exercises.ts` read the provider: same return shape (status mapped `idle`/`loading` -> `loading`), `load()` on mount, `reload` -> provider `reload`, plus `refresh`; update its comment; verify typecheck passes and `workout-view.test.tsx`, `workout-template-view.test.tsx` and `exercise-catalog-view.test.tsx` still pass unchanged
- [x] 2.3 In `exercise-catalog-view.tsx`, call `catalog.refresh()` after `catalog.add(exercise)` in `onCreated`; update the creation case of `exercise-catalog-view.test.tsx` to expect exactly one more `GET /exercises` and the created exercise still listed while it runs; verify the test passes
- [x] 2.4 Add a case to `workout-view.test.tsx` and to `workout-template-view.test.tsx` that renders the view under a provider whose catalog is already loaded (a probe that loads it first) and asserts no catalog read when the view opens; verify both pass

## 3. Backoffice E2E

- [x] 3.1 Add `backoffice/e2e/exercise-catalog-cache.spec.ts` modelled on `backoffice/e2e/workout.spec.ts`: sign in as a trainer, open `/exercises`, a workout and a template, and assert one intercepted `GET /exercises`; create an exercise and assert one more; verify `pnpm test:e2e` passes for this spec

## 4. Section counts (added on request during apply)

- [x] 4.1 Show each section's count in `exercise-catalog-browser.tsx` ("Todos (n)", "Mis ejercicios (n)", "Favoritos (n)"), counted before the text and pill filters, Favoritos uncounted until the favorites are read; add `workout.catalog.sections.withCount` in `es` and `en`; verify typecheck and lint pass
- [x] 4.2 Update the section lookups in `exercise-catalog-view.test.tsx`, `workout-view.test.tsx` and `e2e/workout.spec.ts` to accept a count, update the label expectations, and add cases for counts unaffected by the filters and for Favoritos without a count when the favorites fail; verify `pnpm --filter backoffice test` passes

## 5. Review and verification

- [x] 5.1 Review the backoffice against its mapped references (`current-user-context.ts`, `current-user-provider.tsx`, `use-current-user.ts`, `app-shell.tsx`, `theme-mode-provider.test.tsx`, `workout.spec.ts`) and the `AGENTS.md` quality checklist; verify no deviation remains beyond the documented test-helper exception, and that core and the app are untouched (`git diff --stat`)
- [x] 5.2 Check `.stpr/template/` has not drifted with the loop in `AGENTS.md`; verify it prints no `DRIFT` line
- [x] 5.3 Run `pnpm typecheck && pnpm lint && pnpm test && pnpm test:e2e` from `front/`; verify it passes
