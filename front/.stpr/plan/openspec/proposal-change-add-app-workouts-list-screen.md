# Change Proposal: Add App Workouts List Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-app-workouts-list-screen/`.

## Change

Turn the Entrenamientos tab into the athlete's week view: a fixed week-progress card at the top, a scrollable list of
the week's workouts (one card each, with status, author and day), and a fixed week navigator at the bottom, just above
the tab bar, to move to the previous or next week.

## Objective

The Entrenamientos tab only shows its title. The athlete already has workouts, prescribed by their trainer or created
by themselves, and core can already list them (`GetWorkoutsUseCase`), but the app shows none. This is the athlete's
main entry to their training: what is planned this week, what is done, and what is left.

## Capabilities

1. **Week-progress card** (fixed at the top, does not scroll):
   - Left block, up to 70% of the width:
     - **Title:** "Esta semana" when the viewed week contains today, "Próxima semana" when it is the week after, and
       otherwise the week range (`Lun, 9 nov - Dom, 15 nov`).
     - **Subtitle:** the week range when the title is "Esta semana" or "Próxima semana"; otherwise the year of the
       week (e.g. `2021`), so a far week is still unambiguous.
   - Right block, two lines:
     - `nCompleted / nWeekTotal` (e.g. `2 / 5`).
     - A progress bar filled at `nCompleted / nWeekTotal` (e.g. 40%); empty when the week has no workouts.
   - A workout counts as completed when its status is `COMPLETED` or `REVIEWED`. The total is every workout the
     athlete sees in that week.
2. **Workout list** (scrollable, between the two fixed items), one card per workout of the viewed week:
   - **Left, status icon:**
     - `COMPLETED` -> filled circle with tick, secondary colour.
     - `REVIEWED` -> filled circle with tick, primary colour.
     - `STARTED` -> circle with play, secondary colour.
     - `PUBLISHED` -> empty outlined circle (not started, pending).
     - `DRAFT` (only the athlete's own workouts; the API hides prescribed drafts) -> same as `PUBLISHED`.
   - **Middle, the widest column:**
     - **Title:** the workout name.
     - **Subtitle:** the author. With `trainerId`, the trainer's name and surname (from `trainer`); without it, the
       literal "Creado por mí".
   - **Right, a small pill:** the short weekday (`lun`, `mar`, `mié`, `jue`, `vie`, `sáb`, `dom`) of the workout's
     `workoutDate` when it is `COMPLETED` or `REVIEWED`; `--` otherwise (pending or in progress).
   - Order: by `workoutDate` ascending; same date keeps the API order.
   - An empty week shows an empty-state line in the list area ("No tienes entrenamientos esta semana"); the card shows
     `0 / 0` and the navigator still works.
3. **Week navigator** (fixed at the bottom, just above the tab bar):
   - Left caret -> previous week. Right caret -> next week.
   - Middle: the viewed week range, always (`Lun, 9 nov - Dom, 15 nov`), whatever the week.
   - No limit in either direction.
4. **Weeks** run Monday to Sunday in the device's local time. The screen opens on the current week every time the tab
   is mounted. Dates are formatted in the active language (`es`: `Lun, 9 nov`; `en`: `Mon, Nov 9`).
5. **Pull to refresh:** pulling the list down reads the workouts again, with the platform's spinner shown for at
   least 800 ms; the viewed week is kept; a failure shows the error state with retry.
6. **Loading and failure:** a loading state while the workouts are fetched the first time; on failure, the existing
   error state with a retry. Changing week does not refetch (see Notes).

## Fronts And Roles

- backoffice -> lands here: no -> unchanged.
- app -> lands here: yes -> roles: `USER` -> the athlete browses weeks and sees their workouts and weekly progress.

No role difference: only `USER` signs in to the app. The athlete sees exactly what the API returns to them; the front
does not hide or add workouts beyond filtering by the viewed week.

## Classification

Core: none. `GetWorkoutsUseCase` (`core/src/application/use-cases/workouts/get-workouts-use-case.ts`) and the
`WorkoutListItem` alias exist and are already registered in `app/src/infrastructure/container.ts`.

App:

- Workouts route -> route -> `app/src/app/(app)/(tabs)/workouts.tsx` (unchanged, still default-exports the view)
- `WorkoutsView` (fixed card, scrollable list, fixed navigator; loading / error / empty) -> view ->
  `app/src/presentation/workouts/views/workouts-view.tsx`
- `useWeekWorkouts` (fetches the workouts once; holds the viewed week start; exposes the week's workouts sorted,
  `completed`, `total`, `isCurrentWeek`, `isNextWeek`, `previousWeek`, `nextWeek`, `retry`, loading and error) ->
  state hook -> `app/src/presentation/workouts/hooks/use-week-workouts.ts`
- Week helpers (week start of a date, add weeks, range and weekday formatting through `Intl` with the active
  language) -> presentation format helper -> `app/src/presentation/common/format/week.ts`
- `Icon` (Paper `Icon`, name, colour and size by props) -> atom ->
  `app/src/presentation/common/components/atoms/icon.tsx`
- `Chip` (Paper `Chip`, compact, text by props) -> atom -> `app/src/presentation/common/components/atoms/chip.tsx`
- `WeekProgressCard` (title, subtitle, `n / total`, `ProgressBar`) -> molecule ->
  `app/src/presentation/workouts/components/molecules/week-progress-card.tsx`
- `WorkoutStatusIcon` (status -> icon and colour) -> molecule ->
  `app/src/presentation/workouts/components/molecules/workout-status-icon.tsx`
- `WorkoutCard` (status icon, name, author, day pill) -> molecule ->
  `app/src/presentation/workouts/components/molecules/workout-card.tsx`
- `WeekNavigator` (two caret `IconButton`s and the range) -> molecule ->
  `app/src/presentation/workouts/components/molecules/week-navigator.tsx`
- `workouts.*` keys -> i18n -> new `workouts` namespace in `app/src/i18n/{es,en}/workouts.json`, registered in
  `app/src/i18n/resources.ts`

Existing atoms reused: `Card`, `Text`, `ProgressBar`, `IconButton`, `SafeArea`; molecules `LoadingState`,
`ErrorState`.

## Canonical Template Mapping

- Route -> `app/src/app/(app)/(tabs)/workouts.tsx` -> already the two-statement route.
- `WorkoutsView` -> `app/src/presentation/profile/views/profile-view.tsx` -> closest app view that loads data through
  a hook and shows loading / error states.
- `useWeekWorkouts` -> `app/src/presentation/profile/hooks/use-profile.ts` -> app hook calling a use case through the
  container and mapping `Result` to state.
- Atoms `Icon`, `Chip` -> `app/src/presentation/common/components/atoms/progress-bar.tsx` -> thin Paper wrappers,
  props only.
- Molecules -> `app/src/presentation/profile/components/molecules/trainer-card.tsx` -> a few atoms in a card, no use
  cases.
- Week helpers -> `app/src/presentation/common/format/initials.ts` -> pure presentation formatter.
- Tests -> `app/test/presentation/workouts/views/workouts-view.test.tsx` (replaced) modelled on
  `app/test/presentation/profile/views/profile-view.test.tsx`.

The backoffice is not a UI reference. `.stpr/template/` and its README are not updated.

## Screens And Navigation

- app -> `/workouts` -> `WorkoutsView` -> tapping the "Entrenamientos" tab -> stays on the screen; the carets change
  the viewed week in place, no navigation.

Tapping a workout card does nothing in this change (see Out Of Scope).

## API Contract

- `GET /workouts` -> in `core/openapi/v1.json`: yes -> response `WorkoutListItemResponse[]` (`WorkoutListItem`); uses
  `name`, `workoutDate`, `trainerId`, `trainer.name`, `trainer.surname`, `status`. Called without `standalone`, so
  mesocycle workouts are included.

No snapshot refresh. The endpoint has no date filter; filtering by week is done in the front (see Notes).

## Held State

- `useWeekWorkouts` -> the athlete's workouts (as fetched), the viewed week start (Monday), loading and error ->
  changed by the carets and by retry -> resets when the screen unmounts (the tab opens again on the current week and
  refetches); logout unmounts it.

## Forms And Structural Validation

None.

## Errors And Copy

- Listing fails (network, server) -> `UNEXPECTED` / `NETWORK` (whatever the mapper already returns) -> error state with
  retry, using the existing `errors` copy.

No new error code.

New `workouts` namespace (Spanish from the request; English proposed):

- `week.current` -> "Esta semana" -> "This week"
- `week.next` -> "Próxima semana" -> "Next week"
- `week.previous` -> "Semana anterior" -> "Previous week" (accessible label of the left caret)
- `week.following` -> "Semana siguiente" -> "Next week" (accessible label of the right caret)
- `progress` -> "{{completed}} / {{total}}" -> "{{completed}} / {{total}}"
- `author.me` -> "Creado por mí" -> "Created by me"
- `day.pending` -> "--" -> "--"
- `empty` -> "No tienes entrenamientos esta semana" -> "No workouts this week"
- `status.completed` / `status.reviewed` / `status.started` / `status.pending` -> "Completado" / "Revisado" /
  "En curso" / "Pendiente" -> "Completed" / "Reviewed" / "In progress" / "Pending" (accessible labels of the status
  icon)

Weekday and month names come from `Intl` in the active language, not from keys. The range is capitalised as in the
request (`Lun, 9 nov - Dom, 15 nov`); the pill stays lowercase (`lun`).

## Platform Capabilities And Dependencies

None. Paper already provides `Icon` and `Chip`; icons are Material Community Icons (`check-circle`, `play-circle`,
`circle-outline`). `Intl.DateTimeFormat` is available in Hermes. No development build needed.

## Out Of Scope

- Opening a workout (detail, start, record sets) from a card. A later change.
- Creating a workout from this screen.
- A date filter on `GET /workouts`. If the list grows too large, it is an API change planned under `api/.stpr/`.
- Refetch on tab focus.
- Grouping by mesocycle or showing the mesocycle week.
- A real completion date: the pill uses `workoutDate`, since the contract has no completion timestamp.
- Must not change: core, the API, the backoffice, the tab bar, other tabs.

## Notes

- **Week filtering (decided).** The front fetches `GET /workouts` once per mount and filters by `workoutDate` inside
  the viewed Monday–Sunday range. This is presentation filtering over what the API already returned, not a business
  rule. `workoutDate` is a date without time: compare as `YYYY-MM-DD` strings, never through `new Date()` with a UTC
  shift.
- **Completion (decided).** Completed = `COMPLETED` or `REVIEWED`; it is a display grouping of statuses, not a rule.
- **Pill day (decided).** The weekday shown for a done workout is the weekday of `workoutDate`. `resultsUpdatedAt` is
  not used: it changes with any later edit.
- **Draft (decided).** A `DRAFT` the athlete owns is shown as pending (empty circle). Not in the request; confirm
  before `/opsx:propose` if a different icon is wanted.
- **Subtitle for other weeks (decided).** The year. The request leaves it open.
- **Right caret.** The request says "previous week" for both carets; this brief takes the right caret as next week.
- **Layout.** The card and the navigator sit outside the `ScrollView`/`FlatList`; only the list scrolls. The navigator
  sits above the tab bar, so it needs no bottom inset of its own.
- **Dates in tests.** Freeze "today" (Jest fake timers with `setSystemTime`) so "Esta semana" / "Próxima semana" are
  deterministic.
- **Specs.** New `app-workouts-list` capability (week card, list, navigator, week rules).
- **Verification tiers:**
  - core unit: none (no core change).
  - app component: `workouts-view.test.tsx` (replaces the current one), with `getWorkoutsUseCase` faked through the
    container and today frozen to a Monday:
    - current week: title "Esta semana", subtitle range, `2 / 5`, bar at 40%, only that week's workouts listed, in
      date order;
    - right caret: "Próxima semana" with its range; right again: title is the range, subtitle the year; left caret
      goes back;
    - status icons per status (by accessible label), including `DRAFT` as pending;
    - author: trainer name and surname with `trainerId`; "Creado por mí" without it, and no own name shown;
    - pill: weekday for `COMPLETED` / `REVIEWED`, `--` for `PUBLISHED` / `STARTED`;
    - empty week: empty line and `0 / 0`;
    - failure: error state, and retry calls the use case again;
    - the navigator always shows the range, also on the current week.
  - core-free unit: `week.test.ts` for the week helpers (Monday start for a Sunday, year boundary, `es` and `en`
    formats).
  - Translation parity covers the `workouts` namespace.
  - No backoffice tests, no E2E (app E2E is a known gap).
  - Manual check in Expo Go: card and navigator stay fixed while the list scrolls; carets move weeks; theme colours of
    the icons in light and dark.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.
- No deviations from the mapped references are allowed.
