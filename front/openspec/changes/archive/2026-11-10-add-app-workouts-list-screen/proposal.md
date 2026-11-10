# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-app-workouts-list-screen.md`.

## Why

The Entrenamientos tab only shows its heading. The athlete already has workouts, prescribed by their trainer or
created by themselves, and core can already list them (`GetWorkoutsUseCase`), but the app shows none. This tab is the
athlete's main entry to their training: what is planned this week, what is done and what is left.

## What Changes

- The Entrenamientos tab becomes a week view with three parts: a fixed week-progress card at the top, a scrollable
  list of the viewed week's workouts, and a fixed week navigator at the bottom, just above the tab bar.
- **Week-progress card.** Left block (up to 70% of the width): title "Esta semana" / "Próxima semana" / the week range,
  and subtitle the week range (for this and next week) or the year (for any other week). Right block: `completed /
total` and a progress bar at that ratio. Completed means `COMPLETED` or `REVIEWED`.
- **Workout card**, one per workout: a status icon (completed: filled tick in secondary colour; reviewed: filled tick
  in primary colour; started: play in secondary colour; published or own draft: empty outlined circle), the workout
  name, the author (the trainer's name and surname, or "Creado por mí" for the athlete's own workouts) and a pill with
  the short weekday of `workoutDate` for done workouts, `--` otherwise. Ordered by `workoutDate`.
- **Week navigator.** Left caret to the previous week, right caret to the next week, the week range always in the
  middle. No limit either way.
- **Pull to refresh.** Pulling the list down reads the workouts again with the platform's spinner, shown for at least
  800 ms; the viewed week is kept.
- Weeks run Monday to Sunday in local time; the screen opens on the current week. Dates follow the active language.
- An empty week shows an empty-state line and `0 / 0`. The first fetch shows a loading state; a failure shows the
  error state with a retry.
- **BREAKING (spec):** the Entrenamientos tab screen no longer shows the "Entrenamientos" heading; the tab label stays.

Fronts and roles: app only, role `USER`, no role difference. The athlete sees exactly what the API returns to them;
the front only filters it by the viewed week. The backoffice does not change.

API: `GET /workouts` (in `core/openapi/v1.json`), response `WorkoutListItemResponse[]`, called without `standalone`.
It has no date filter, so the app fetches once per mount (and on retry or pull to refresh) and filters by
`workoutDate`. No snapshot refresh.

## Capabilities

### New Capabilities

- `app-workouts-list`: the athlete's Entrenamientos week view: the week-progress card, the workout cards (status,
  author, day), the week navigator, which workouts belong to a week, and the loading, empty and failure states.

### Modified Capabilities

- `app-navigation`: tapping Entrenamientos shows the workouts week view instead of a screen headed "Entrenamientos".

No core capability changes: `workout-management` already covers listing the caller's workouts.

## Canonical References

- Route -> `app/src/app/(app)/(tabs)/workouts.tsx` -> already the two-statement route; unchanged.
- `WorkoutsView` -> `app/src/presentation/profile/views/profile-view.tsx` -> closest app view loading data through a
  hook with loading and error states.
- `useWeekWorkouts` state hook -> `app/src/presentation/profile/hooks/use-profile.ts` -> app hook calling a use case
  through the container and mapping `Result` to state.
- `Icon`, `Chip` and `RefreshControl` atoms (new) -> `app/src/presentation/common/components/atoms/progress-bar.tsx` -> thin Paper
  wrappers, props only.
- `WeekProgressCard`, `WorkoutStatusIcon`, `WorkoutCard`, `WeekNavigator` molecules (new, `workouts` area) ->
  `app/src/presentation/profile/components/molecules/trainer-card.tsx` -> a few atoms, no use cases.
- Week helpers -> `app/src/presentation/common/format/initials.ts` -> pure presentation formatter.
- View test -> `app/test/presentation/profile/views/profile-view.test.tsx`.
- Reused: atoms `Card`, `Text`, `ProgressBar`, `IconButton`, `SafeArea`; molecules `LoadingState`, `ErrorState`.

The backoffice is not a UI reference. No organism is needed. `.stpr/template/` and its README are not updated.

## Decisions Carried From The Brief

- A `DRAFT` the athlete owns is shown as pending (empty circle).
- The subtitle for weeks other than this or next is the year.
- The right caret goes to the next week (the request said "previous" for both).
- The pill's weekday comes from `workoutDate`; the contract has no completion timestamp.
- Filtering by week happens in the front over the API's list; it adds no business rule.

## Impact

- App: `workouts-view.tsx` rewritten; new hook, format helper, two atoms, four molecules, `workouts` i18n namespace
  registered in `resources.ts`; `workouts-view.test.tsx` replaced; new `week.test.ts`.
- Core, API, backoffice, `container.ts`: no change.
- Dependencies: none (Paper `Icon` and `Chip`, `Intl.DateTimeFormat` in Hermes). No development build.
- Out of scope: opening a workout, creating one here, an API date filter, refetch on tab focus, mesocycle grouping.
