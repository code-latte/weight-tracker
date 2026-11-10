# Tasks

Core: no change (the three use cases are already in core and registered in the app container).

## 1. App translations

- [x] 1.1 Add the `workout.*` keys of `design.md` to `app/src/i18n/es/workouts.json` and `app/src/i18n/en/workouts.json`; verify the app translation parity test passes

## 2. App atoms

- [x] 2.1 Extend `app/src/presentation/common/components/atoms/icon.tsx` with the `success` and `onPrimary` tones, keeping the existing tones; verify `pnpm --filter app typecheck` passes and existing app tests still pass
- [x] 2.2 Add `app/src/presentation/common/components/atoms/text-link.tsx` (Paper text `Button`, `label` / `onPress` / `accessibilityLabel` by props), modelled on `progress-bar.tsx`; verify typecheck and lint pass

## 3. App state hook

- [x] 3.1 Add `app/src/presentation/workouts/hooks/use-workout.ts` modelled on `use-week-workouts.ts`: parallel workout and catalog reads, mesocycle read only with `mesocycleId` (failure -> no subtitle), `retry`, exported `exerciseProgressOf`, rows with name / type / video / progress, `done` / `total`; verify typecheck passes (behaviour is covered by the view test in 6.2)

## 4. App molecules

- [x] 4.1 Add `ExerciseProgressIcon` (`exercise-progress-icon.tsx`) modelled on `workout-status-icon.tsx`; verify typecheck and lint pass
- [x] 4.2 Add `ExerciseTypeIcon` (`exercise-type-icon.tsx`), rendering nothing for an unknown type; verify typecheck and lint pass
- [x] 4.3 Add `WorkoutHeroPanel` (`workout-hero-panel.tsx`) modelled on `week-progress-card.tsx`: primary background with top inset, back `IconButton`, name, optional subtitle, progress label and bar in `onPrimary`; verify typecheck and lint pass
- [x] 4.4 Add `WorkoutExerciseRow` (`workout-exercise-row.tsx`) modelled on `workout-card.tsx`: progress icon, type icon, name, optional `TextLink`, trailing `chevron-right` labelled; verify typecheck and lint pass
- [x] 4.5 Give `WorkoutCard` an optional `onPress` passed to `Card`; verify existing `workouts-view.test.tsx` still passes

## 5. App routes and list navigation

- [x] 5.1 Move `app/src/app/(app)/(tabs)/workouts.tsx` to `workouts/index.tsx` unchanged and add `workouts/_layout.tsx` (`WorkoutsLayout`, `Stack` with `index` and `[id]`, both `headerShown: false`) modelled on `profile/_layout.tsx`; verify the Entrenamientos tab still opens the list and typecheck passes
- [x] 5.2 In `WorkoutsView`, push `{ pathname: "/workouts/[id]", params: { id } }` from each card's `onPress`; add the "tapping a card opens its workout" case to `app/test/presentation/workouts/views/workouts-view.test.tsx` (router mocked as in `profile-view.test.tsx`); verify it passes

## 6. App workout screen

- [x] 6.1 Add `app/src/presentation/workouts/views/workout-view.tsx` modelled on `workouts-view.tsx` (`useLocalSearchParams`, `router.back`, `Linking.openURL`, fixed panel over a `FlatList`, loading / error with back) and `app/src/app/(app)/(tabs)/workouts/[id].tsx` as the two-statement route; verify typecheck and lint pass
- [x] 6.2 Add `app/test/presentation/workouts/views/workout-view.test.tsx` modelled on `workouts-view.test.tsx`, with the three use cases faked through the container, covering: mesocycle subtitle and `3 / 6 ejercicios` with bar at 50%; standalone without subtitle and no mesocycle read; English subtitle; mesocycle failure without subtitle; progress icons (done, more records than sets, sets summed over several prescriptions, in progress, not started); type icons and a catalog-missing exercise ("Ejercicio", no type icon, no video); name fallback to the first translation; "Ver vídeo" only with `videoUrl` and opening it; "Ver detalle" caret; API order; `0 / 0 ejercicios` for no exercises; back button calls `router.back`, also on failure; loading; failure and retry. Each test in Arrange / Act / Assert with one Act; verify `pnpm --filter app test` passes

## 7. Review and verification

- [x] 7.1 Review the app against its mapped references (`profile/_layout.tsx`, `profile/settings.tsx`, `workouts-view.tsx`, `use-week-workouts.ts`, `progress-bar.tsx`, `week-progress-card.tsx`, `workout-status-icon.tsx`, `workout-card.tsx`, `workouts-view.test.tsx`) and the `AGENTS.md` quality checklist; verify no deviation remains and core and the backoffice are untouched (`git diff --stat`)
- [x] 7.2 Check `.stpr/template/` has not drifted with the loop in `AGENTS.md`; verify it prints no `DRIFT` line
- [x] 7.3 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`; verify it passes
- [x] 7.4 Manual check in Expo Go: panel fixed while the list scrolls, primary colour under the status bar, back keeps the viewed week, "Ver vídeo" opens the browser, light and dark themes

## 8. Single-read workout detail (follows API change `enrich-get-workout-by-id-response`)

- [x] 8.1 Refresh `core/openapi/v1.json` and regenerate `core/src/contract/generated` from the API with `videoUrl` on `WorkoutByIdExerciseResponse` and the embedded `WorkoutMesocycleResponse`; verify the snapshot diff only touches the workout-by-id schemas
- [x] 8.2 Add the `WorkoutByIdExercise` and `WorkoutMesocycle` domain aliases, update `GetWorkoutByIdUseCase`'s doc and the core fixtures; verify `pnpm --filter @weight-tracker/core test` passes
- [x] 8.3 App: `useWorkout` reads only `getWorkoutByIdUseCase`; names, type, video and mesocycle name come from the workout; update `workout-view.test.tsx` (one read, no catalog or mesocycle fakes, no-names exercise reads "Ejercicio"); verify `pnpm --filter app test` passes
- [x] 8.4 Backoffice: the workout strip takes the mesocycle from `workout.mesocycle`, `use-workout-mesocycle.ts` is removed, the catalog read stays for the add-exercise dialog; update `workout-view.test.tsx` (no mesocycle read) and the E2E workout mock to embed the mesocycle; verify the backoffice workout view test passes
- [x] 8.5 Run `pnpm typecheck && pnpm lint && pnpm test` and `pnpm test:e2e` from `front/`; verify both pass
