# Design

## Context

See proposal.md for the motivation. Current state:

- `app/src/presentation/workouts/views/workouts-view.tsx` shows only the "Entrenamientos" heading inside `SafeArea`;
  its test asserts that heading. The route `app/src/app/(app)/(tabs)/workouts.tsx` already default-exports the view.
- `GetWorkoutsUseCase` (`core`) returns `Result<WorkoutListItem[], DomainError>` and is registered as
  `getWorkoutsUseCase` in `app/src/infrastructure/container.ts`. `GET /workouts` has no date filter; for a `USER`
  the API returns the athlete's own workouts and the published prescribed ones (prescribed drafts are hidden).
- `WorkoutListItem` carries `name`, `workoutDate` (`YYYY-MM-DD`), `trainerId`, `trainer` (`UserProfile | null`, with
  `name` and `surname`), `status` (`DRAFT | PUBLISHED | STARTED | COMPLETED | REVIEWED`) and `completedAt` (ISO
  date-time or `null`), which the refreshed contract added.
- Reusable app pieces: atoms `Card`, `Text`, `ProgressBar`, `IconButton`, `SafeArea`; molecules `LoadingState`,
  `ErrorState` (message, retry label, `onRetry`). `useProfile` is the hook shape to follow (discriminated `state`,
  `active` guard on mount, `retry`).

## Goals / Non-Goals

**Goals:**

- One fetch per mount, week navigation in memory.
- Date logic in one pure, tested helper; no timezone drift on `workoutDate`.
- Only atoms import React Native Paper.

**Non-Goals:**

- No change to core, the container, the API contract or the backoffice.
- No new organism: the view composes molecules directly, as `ProfileView` does.
- No change to test helpers (`app/test/support/render-with-providers.tsx`) or test patterns.

## Decisions

### Layering and files (app only)

| Piece               | Level      | File                                                                                                                     |
| ------------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------ |
| `Icon`              | atom       | `app/src/presentation/common/components/atoms/icon.tsx` (Paper `Icon`)                                                   |
| `Chip`              | atom       | `app/src/presentation/common/components/atoms/chip.tsx` (Paper `Chip`, compact)                                          |
| `WeekProgressCard`  | molecule   | `app/src/presentation/workouts/components/molecules/week-progress-card.tsx`                                              |
| `WorkoutStatusIcon` | molecule   | `app/src/presentation/workouts/components/molecules/workout-status-icon.tsx`                                             |
| `WorkoutCard`       | molecule   | `app/src/presentation/workouts/components/molecules/workout-card.tsx`                                                    |
| `RefreshControl`    | atom       | `app/src/presentation/common/components/atoms/refresh-control.tsx` (React Native `RefreshControl`, Paper primary colour) |
| `WeekNavigator`     | molecule   | `app/src/presentation/workouts/components/molecules/week-navigator.tsx`                                                  |
| `useWeekWorkouts`   | state hook | `app/src/presentation/workouts/hooks/use-week-workouts.ts`                                                               |
| week helpers        | format     | `app/src/presentation/common/format/week.ts`                                                                             |
| `WorkoutsView`      | view       | `app/src/presentation/workouts/views/workouts-view.tsx` (rewritten)                                                      |

Imports flow downward: view -> molecules -> atoms -> Paper. Molecules take ready-to-show strings and values by props
(title, subtitle, author, day label, accessible labels); the view resolves them with `useTranslation("workouts")` and
the week helpers. `WorkoutStatusIcon` maps status to icon name and tone. `Icon` accepts
`tone: "primary" | "secondary" | "default"` and resolves the colour from the Paper theme itself, so no molecule
imports Paper. Icons: `check-circle` (completed, secondary; reviewed, primary),
`play-circle` (started, secondary), `circle-outline` (published, draft; default tone).

### Week helpers (`common/format/week.ts`)

Pure functions over `YYYY-MM-DD` strings, built with local-time `Date(year, month, day)` and never `new Date("YYYY-MM-DD")`
(which parses as UTC and can shift a day):

- `weekStartOf(date: Date): string` — the Monday of the week containing `date`.
- `addWeeks(weekStart: string, n: number): string`.
- `isInWeek(day: string, weekStart: string): boolean` — `weekStart <= day <= weekStart + 6` by string comparison.
- `formatWeekRange(weekStart: string, language: string): string` — `Intl.DateTimeFormat(language, { weekday: "short",
day: "numeric", month: "short" })` for Monday and Sunday, first letter of each side capitalised, joined with
  `" - "`. Trailing dots that `es` adds to abbreviations (`nov.`) are removed so it reads `Lun, 9 nov`.
- `formatShortWeekday(instant: string, language: string): string` — the weekday of an ISO date-time such as
  `completedAt`, read with `new Date` (the string carries its offset) in local time; lowercase, no trailing dot
  (`mié`).
- `weekYear(weekStart: string): string`.

Alternative rejected: a date library (`date-fns`, `dayjs`). `Intl` covers both languages in Hermes and avoids a new
dependency.

### State (`useWeekWorkouts`)

- Holds `state: { status: "loading" } | { status: "ready"; workouts: WorkoutListItem[] } | { status: "error";
errorCode: DomainErrorCode }` and `weekStart` (initially `weekStartOf(new Date())`).
- Fetches with `getWorkoutsUseCase.execute()` (no `standalone`) on mount, with the `active` guard of `useProfile`;
  `retry` sets loading and fetches again.
- Derives, with `useMemo`, the viewed week's workouts (filtered by `isInWeek`, stable-sorted by `workoutDate`),
  `completed` (`COMPLETED` or `REVIEWED`), `total`, `isCurrentWeek` and `isNextWeek` (compared to
  `weekStartOf(new Date())` at render).
- Exposes `previousWeek` / `nextWeek`. Changing week never fetches.
- Resets when the screen unmounts; logout unmounts it. No provider: the state is local to the screen.

- `refresh` (pull to refresh) sets `refreshing`, then reads again together with `wait(MIN_REFRESH_MS)` (800 ms) via
  `Promise.all`, so a fast answer still shows the spinner long enough to be seen. The current `state` and `weekStart`
  are kept meanwhile; the result replaces `state` (ready, or error with retry) and `refreshing` ends.

Alternative rejected: refetching per week. The endpoint cannot filter by date, so it would fetch the same list again.

Alternative rejected for the refresh: keeping the old list when a refresh fails. It would hide the failure; the error
state with retry is what the first read already shows.

### View layout

`SafeArea` (no bottom edge; the tab bar owns it) with a column: `WeekProgressCard` on top, a `FlatList` of
`WorkoutCard`s with `flex: 1` (or `LoadingState` / `ErrorState` / the empty line in its place), and `WeekNavigator` at
the bottom. Card and navigator sit outside the list, so only the list scrolls. The card's left block has
`maxWidth: "70%"`. When loading or on error, the card and navigator are still shown for the current week, with the
card at `0 / 0`.

### Dependencies and container

No new `Dependencies` key and no `container.ts` change: `getWorkoutsUseCase` is already registered.

### Errors and copy

- Fetch failure -> whatever `DomainError` code the mapper returns (`UNEXPECTED` for network or server) -> `ErrorState`
  with `t("errors:<code>")` and `t("workouts:retry")`. No new error code.
- New `workouts` namespace in `app/src/i18n/es/workouts.json` and `app/src/i18n/en/workouts.json`, registered in
  `app/src/i18n/resources.ts`:

| Key                | es                                   | en                        |
| ------------------ | ------------------------------------ | ------------------------- |
| `week.current`     | Esta semana                          | This week                 |
| `week.next`        | Próxima semana                       | Next week                 |
| `week.previous`    | Semana anterior                      | Previous week             |
| `week.following`   | Semana siguiente                     | Next week                 |
| `progress`         | {{completed}} / {{total}}            | {{completed}} / {{total}} |
| `author.me`        | Creado por mí                        | Created by me             |
| `day.pending`      | --                                   | --                        |
| `empty`            | No tienes entrenamientos esta semana | No workouts this week     |
| `status.completed` | Completado                           | Completed                 |
| `status.reviewed`  | Revisado                             | Reviewed                  |
| `status.started`   | En curso                             | In progress               |
| `status.pending`   | Pendiente                            | Pending                   |
| `retry`            | Reintentar                           | Retry                     |

The trainer's name is `${trainer.name} ${trainer.surname}`, not translated.

### Tests

- `app/test/presentation/workouts/views/workouts-view.test.tsx` is replaced, following `profile-view.test.tsx`
  (`setup` with fakes through `renderWithProviders`, `expo-font` mock for Paper icons, Arrange / Act / Assert). Today
  is frozen with `jest.useFakeTimers({ now: new Date(2026, 10, 9) })` (Monday) so week titles are deterministic.
- `app/test/presentation/common/format/week.test.ts` covers the helpers (Sunday -> previous Monday, year boundary,
  `es` and `en` range, weekday without trailing dot).

## Risks / Trade-offs

- [The full list grows with years of history] → Accepted for now; a date filter on `GET /workouts` is an API change
  planned under `api/.stpr/` if it matters.
- [`Intl` output for `es` differs between engines (dots, capitalisation)] → The helper normalises dots and case, and
  `week.test.ts` pins the exact strings under Jest; manual check in Expo Go confirms Hermes.
- [Fake timers interfere with `waitFor`] → Use `jest.useFakeTimers({ now, doNotFake: [...] })` or set the system time
  only, as needed; keep it in the test file, not in shared helpers.
- [A done workout completed before the API recorded `completedAt` has none] → Its pill shows `--` rather than falling
  back to the planned date, which would claim a day it may not have been done on.
- [The spec of the Entrenamientos tab heading changes] → `app-navigation` is modified in this change; the old heading
  test goes with the replaced view test.
