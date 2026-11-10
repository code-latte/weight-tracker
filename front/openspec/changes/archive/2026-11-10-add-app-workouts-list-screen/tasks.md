# Tasks

Core, the API contract, the backoffice and `container.ts` do not change: `getWorkoutsUseCase` is already registered
in the app. Work starts in the app.

## 1. Translations

- [x] 1.1 Add `app/src/i18n/es/workouts.json` and `app/src/i18n/en/workouts.json` with the keys of design.md
      ("Errors and copy") and register the `workouts` namespace in `app/src/i18n/resources.ts`. Verify the
      translation parity test passes.

## 2. Week helpers

- [x] 2.1 Create `app/src/presentation/common/format/week.ts` with `weekStartOf`, `addWeeks`, `isInWeek`,
      `formatWeekRange`, `formatShortWeekday` and `weekYear` as in design.md: local-time dates only, never
      `new Date("YYYY-MM-DD")`; `es` abbreviations without trailing dots; range sides capitalised, weekday lowercase.
- [x] 2.2 Add `app/test/presentation/common/format/week.test.ts` (Arrange / Act / Assert): Monday stays Monday;
      Sunday 15 Nov 2026 -> Monday 9 Nov; a week crossing the year (29 Dec 2025 - 4 Jan 2026); `addWeeks` by ±1 and
      by -260; `isInWeek` on both bounds and outside; `es` range "Lun, 9 nov - Dom, 15 nov"; `en` range
      "Mon, Nov 9 - Sun, Nov 15"; `es` weekday "mié". Verify it passes.

## 3. Atoms

- [x] 3.1 Create `Icon` (`app/src/presentation/common/components/atoms/icon.tsx`): Paper `Icon`, `source`, `size`,
      `tone` (`primary` / `secondary` / `default`, colour from the Paper theme) and `accessibilityLabel` by props.
- [x] 3.2 Create `Chip` (`app/src/presentation/common/components/atoms/chip.tsx`): Paper `Chip`, compact, text by
      props, not pressable. Verify `pnpm typecheck` passes.

## 4. Molecules (`app/src/presentation/workouts/components/molecules/`)

- [x] 4.1 `WorkoutStatusIcon`: maps `WorkoutStatus` to icon, tone and label (labels by props): `COMPLETED` ->
      `check-circle` secondary; `REVIEWED` -> `check-circle` primary; `STARTED` -> `play-circle` secondary;
      `PUBLISHED` and `DRAFT` -> `circle-outline` default.
- [x] 4.2 `WorkoutCard`: `Card` with `WorkoutStatusIcon` on the left, name and author `Text`s in the widest middle
      column, `Chip` with the day label on the right; all strings by props.
- [x] 4.3 `WeekProgressCard`: `Card` with title and subtitle on the left (`maxWidth: "70%"`), `progress` text and
      `ProgressBar` (ratio, 0 when total is 0) on the right; strings and numbers by props.
- [x] 4.4 `WeekNavigator`: left and right `IconButton` carets (`chevron-left` / `chevron-right`, labels by props)
      around the range `Text`; `onPrevious` / `onNext` by props. Verify `pnpm typecheck` passes.

## 5. State hook

- [x] 5.1 Create `useWeekWorkouts` (`app/src/presentation/workouts/hooks/use-week-workouts.ts`) on the shape of
      `use-profile.ts`: fetches `getWorkoutsUseCase.execute()` on mount with the `active` guard; `retry`; holds
      `weekStart` from `weekStartOf(new Date())`; derives the week's workouts (filtered, stable-sorted by
      `workoutDate`), `completed` (`COMPLETED` or `REVIEWED`), `total`, `isCurrentWeek`, `isNextWeek`; exposes
      `previousWeek` / `nextWeek`, which never fetch.

## 6. View

- [x] 6.1 Rewrite `WorkoutsView` (`app/src/presentation/workouts/views/workouts-view.tsx`): `SafeArea` column with
      `WeekProgressCard` (title "Esta semana" / "Próxima semana" / range; subtitle range or year), a `FlatList` of
      `WorkoutCard`s with `flex: 1` (author: trainer name and surname, else `author.me`; day: short weekday for
      `COMPLETED` / `REVIEWED`, else `day.pending`), `LoadingState` / `ErrorState` (`errors:<code>`,
      `workouts:retry`) / the `empty` line in the list's place, and `WeekNavigator` at the bottom. Remove the
      "Entrenamientos" heading. The route file stays unchanged.
- [x] 6.2 Replace `app/test/presentation/workouts/views/workouts-view.test.tsx`, modelled on `profile-view.test.tsx`
      (`setup`, fakes through `renderWithProviders`, `expo-font` mock, Arrange / Act / Assert, one Act), with today
      frozen to Monday 9 Nov 2026. Cases: loading state while the fetch is pending. Current week: "Esta semana",
      "Lun, 9 nov - Dom, 15 nov", "2 / 5", only that week's workouts, in date order. Right caret: "Próxima semana"
      with "Lun, 16 nov - Dom, 22 nov"; right twice: title is the range and subtitle "2026"; left caret from next week
      returns to "Esta semana". Navigator shows the range on the current week. Status icons by label, including
      `DRAFT` as "Pendiente". Author: "Carlos Ruiz" with a trainer; "Creado por mí" without, and the athlete's name
      absent. Pill: "mié" for a `COMPLETED` workout on 11 Nov, "--" for `STARTED` and `PUBLISHED`. Empty week: empty
      line and "0 / 0". Failure shows the error state; retry calls the use case again and shows the week. Changing
      week does not call the use case again. Verify it passes.

## 7. Pull to refresh

- [x] 7.1 Create `RefreshControl` (`app/src/presentation/common/components/atoms/refresh-control.tsx`): React
      Native's `RefreshControl` (the platform spinner) in the Paper theme's primary colour, other props passed through.
- [x] 7.2 In `useWeekWorkouts`, add `refreshing` and `refresh`: reads the workouts again together with a wait of
      `MIN_REFRESH_MS` (800 ms), keeps the current list and the viewed week meanwhile, then applies the result (the
      error state on failure) and ends `refreshing`. Pass them to the `FlatList` through the atom in `WorkoutsView`.
- [x] 7.3 Add a `pull to refresh` block to `workouts-view.test.tsx` with all timers faked: a refresh reads again and
      shows the new list on the same week, spinner off; the spinner and the old list stay until the delay passes; a
      failed refresh shows the error and retry. Verify it passes.

## 8. Review

- [x] 8.1 Review the app against the mapped references (proposal "Canonical References"): atoms against
      `progress-bar.tsx`, molecules against `trainer-card.tsx`, the hook against `use-profile.ts`, the view against
      `profile-view.tsx`, the helpers against `initials.ts`, the test against `profile-view.test.tsx`. Confirm only
      atoms import Paper widgets, imports flow downward, presentation imports no `@weight-tracker/core/infrastructure`,
      and every string is in `es` and `en`. Record any finding and fix it.
- [x] 8.2 Confirm core, backoffice and `container.ts` are unchanged: this command prints nothing,
      `git diff --stat main -- core backoffice app/src/infrastructure/container.ts`.
- [x] 8.3 Confirm `.stpr/template/` is byte-identical to its counterparts with the drift check in `AGENTS.md`.
- [x] 8.6 Show the weekday of `completedAt` (local time) in the day pill instead of `workoutDate`, `--` when a done
      workout has none: `formatShortWeekday` takes an ISO instant, `week.test.ts` covers local-time reading, and
      `workouts-view.test.tsx` covers the completion day differing from the planned one and the missing case. Verify
      with `pnpm --filter app test`.
- [x] 8.4 Manual check in Expo Go: card and navigator stay fixed while the list scrolls; pulling the list down shows the spinner and refreshes; carets move weeks; icon
      colours in light and dark; Hermes renders the `es` and `en` ranges as specified.
- [x] 8.5 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes. No E2E is planned.
