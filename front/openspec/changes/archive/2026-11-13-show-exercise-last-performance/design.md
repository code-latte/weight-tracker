# Design

## Context

See `proposal.md` for the motivation and `specs/` for the behaviour. Current state that shapes the approach:

- `core/openapi/v1.json` does not have the endpoint yet. The API adds it in `add-exercise-last-performance-endpoint`
  (uncommitted in `api/`), so the snapshot refresh needs that API running.
- `UserRepository.getTrainer` already reads a `/users/...` route whose `204` comes back as `ok(null)`:
  `FetchHttpClient` returns `undefined` for an empty body and the repository maps it to `null`.
- `Dependencies` (`core/src/application/dependencies.ts`) is one interface, and both `app` and `backoffice` build
  `new Container<Dependencies>(...)`. Every key has to be registered in both.
- The app's exercise screen (`workout-exercise-view.tsx`) already resolves the workout exercise from the route, opens
  `videoUrl` through `ExerciseHeroPanel`'s `video` prop, and has `exercise.exerciseId`, `exercise.type` and
  `i18n.language`.
- The effort scale per type lives only in the backoffice: `useExerciseLabels.effortLabel` and the
  `efforts` / `effortsByType` keys of `backoffice/src/i18n/{es,en}/workouts.json`. The app has no effort copy.
- The app formats dates with `Intl.DateTimeFormat(language, …)` in `presentation/common/format/week.ts`.

## Goals / Non-Goals

**Goals:**

- One new use case in `core` following the `getTrainer` shape exactly.
- A dialog fed by a state hook that reads on open, with no cache or provider.
- The app owns its effort copy and lookup, with the same texts as the backoffice.

**Non-Goals:**

- No shared effort module in `core`, and no backoffice refactor (stop case, see Decisions).
- No new port, adapter, error code or package.

## Decisions

### D1. Core: users domain area, `getTrainer` shape

- Aliases in `core/src/domain/users/user.ts` (or a sibling file re-exported from the area's index):
  `ExerciseLastPerformance = components["schemas"]["ExerciseLastPerformanceResponse"]` and
  `LastPerformanceSet = components["schemas"]["LastPerformanceSetResponse"]`.
- `UserRepositoryInterface.getMyExerciseLastPerformance(exerciseId: string): Promise<Result<ExerciseLastPerformance | null, DomainError>>`.
- `UserRepository` implementation: `GET /users/me/exercises/${exerciseId}/last-performance`,
  `request<ExerciseLastPerformance | undefined>`, `ok(response ?? null)`, `fail(mapHttpError(error))`.
- `GetMyExerciseLastPerformanceUseCase implements UseCaseInterface<string, ExerciseLastPerformance | null>`, which delegates
  to the repository. It goes in `use-cases/users/` and is exported from its `index.ts`.
- `Dependencies` key `getMyExerciseLastPerformanceUseCase: UseCaseInterface<string, ExerciseLastPerformance | null>`.

_Why the users area:_ the route is under `/users/me`, which `UserRepository` owns. _Alternative:_ `WorkoutRepository`,
since the data comes from workouts. Rejected because repositories here follow the API's route groups.

### D2. Register the use case in both containers (deviation from the brief)

The brief said "app only". Because `Container<Dependencies>` requires every key, the backoffice `container.ts` also
registers `getMyExerciseLastPerformanceUseCase: (c) => new GetMyExerciseLastPerformanceUseCase(c.resolve("userRepository"))`.
This is wiring only: no backoffice presentation code reads it. _Alternative:_ make the key optional or split
`Dependencies` per front. Rejected because it would change the template's composition pattern for one key.

### D3. Effort labels stay per front

The app adds `efforts` and `effortsByType.{STRENGTH,CARDIO,STRETCHING}` to `app/src/i18n/{es,en}/workouts.json`,
copied verbatim from the backoffice. A pure helper `effortKeysOf(code, type?)` goes in
`app/src/presentation/workouts/effort.ts`, beside `set-rows.ts` and `catalog.ts`, and returns the keys to read, most
specific first: `effortsByType.<type>.<code>` when `type` is one of the three known types, then `efforts.<code>`. The
caller passes them to `t` with the code as `defaultValue`, which gives the backoffice's lookup (type scale, generic
scale, the code as it comes). Like `setTargetLabel`, the helper returns keys and never calls `t`.

_Why:_ moving the copy into `core` and reading it from both fronts is a stop case in `STPR_INVARIANTS.md` ("share …
translations"; "Each front owns its own translations"). _Alternative:_ keep effort codes as data in `core` and the texts
per front. Rejected as unnecessary, since the code list is already the generated `Effort` enum. _Trade-off:_ the two
copies can drift. The texts are short, stable and owned by each front's translators.

### D4. State: a hook that reads when the exercise is shown

`useExerciseLastPerformance()` lives in `app/src/presentation/workouts/hooks/use-exercise-last-performance.ts` and
exposes `{ state, load(exerciseId), reset() }`, where `state` is `idle`, `loading`, `ready` (with the performance, or
`null` for a `204`) or `error` (with the code). It resolves the use case from the container like `use-workout.ts` and
never throws. It ignores an answer that arrives after a newer `load` or a `reset`.

`WorkoutExerciseView` calls `load(exercise.exerciseId)` in an effect keyed on the catalog exercise id, and `reset` when
that id changes or the screen exits, so the last session is read once per exercise shown and not when the dialog opens.
The link needs to know whether there is a weight before the athlete taps, which is why the read cannot wait for the tap.
It owns `lastPerformanceOpen`, set by the link and cleared by close, dismiss and choosing another exercise.

- The link is shown when `state` is `ready` and `hasWeightIn(sets)`: any set whose `weight` is not `null` (a weight of 0
  counts). A failed read, a `204`, a read in flight and a session with no weight all show no link and no error.
- While `state` is `idle` or `loading` the sets and the link are a skeleton (D5); `ready` and `error` both settle it.

| State                                 | Holder                       | Changed by                   | Reset                        |
| ------------------------------------- | ---------------------------- | ---------------------------- | ---------------------------- |
| dialog visible                        | `WorkoutExerciseView`        | link, close, dismiss, switch | screen exit                  |
| `state` (performance, loading, error) | `useExerciseLastPerformance` | `load`                       | exercise switch, screen exit |

_Alternative:_ read when the dialog opens and keep the link always visible. Rejected: the link must hide when there is
no weight. _Alternative:_ load inside the dialog component. Rejected because organisms do not resolve use cases; state
hooks do.

### D5. Components

- `ExerciseHeroPanel` (molecule, existing) does not change: the link would make the header's title row taller and push
  the subtitle and chips down, so it does not live there.
- The link is the `text-link` atom with the `history` icon, in a row of `WorkoutExerciseView` between `ExerciseNavigator`
  and the list of sets, padded like the set rows. It shows when the last session has a weight (D4).
- `Skeleton` (new atom, `common/components/atoms/skeleton.tsx`): a block in the theme's muted surface colour with a width,
  height and radius. `ExerciseSetsSkeleton` (molecule, `components/molecules/exercise-sets-skeleton.tsx`) holds the place
  of the link and of the list: a short bar and one placeholder row (number and card) per prescribed set, hidden from
  screen readers. The view shows it in place of both while `state` is `idle` or `loading`, keeping the header, the
  navigator and the rest band, so the screen settles once. It renders inside the flexible area, in the list's place,
  so the rest band stays against the bottom.
- `LastPerformanceStatCard` (molecule, `components/molecules/last-performance-stat-card.tsx`): props `label` (small) and
  `value` (big), on the `card` atom; two of them share a row. It carries an accessibility label `"{label}: {value}"`.
- `LastPerformanceSetLine` (molecule, `components/molecules/last-performance-set-line.tsx`): one row of the table, with
  props `number`, `amount` (already translated: "10 reps" / "45 s" / empty) and `weight` (already translated: "20 kg" /
  "sin peso"): the number on the left (`headlineMedium` in the theme's secondary colour, like `SetRecordRow`'s), the amount
  centred, the weight on the right (`headlineMedium`, shrinking to fit when long). The table is an outlined `card`. The set with the highest weight also gets a primary-colour `star` `icon` before its
  weight and the small primary-colour text `highest` ("Mayor carga") under it. Uses
  `text`.
- `highestWeightIndexOf(sets)` (pure helper, `app/src/presentation/workouts/last-performance.ts`) returns the position of
  the set with the highest weight, the last one when it is repeated, and `null` when no set has a weight above 0. The
  dialog passes the `highest` text to that row only.
- `Divider` (new atom, `common/components/atoms/divider.tsx`) wraps Paper's `Divider`, since only atoms import the UI
  library. The dialog uses it under the date and between the table's rows.
- `LastPerformanceDialog` (organism, `components/organisms/last-performance-dialog.tsx`): props `visible`,
  `performance`, `exerciseType`, `onClose`; it is only rendered when there is a performance with a weight. Built on the `dialog` atom
  (`Dialog`/`DialogTitle`/`DialogContent`/`DialogActions`), like `confirm-dialog.tsx`. It translates with
  `useTranslation` (organisms may), formats the date with `formatDay` (day, short month and year,
  from the `YYYY-MM-DD` string read as a local date), picks the effort with `effortKeysOf`, and renders
  the content, which is, top to bottom: the calendar `icon` and the date, a
  `Divider`, a row of two stat cards ("Series realizadas" with the number of sets, "Esfuerzo" with the effort label, left
  out when there is none), the title "Series registradas", and one `card` holding a `LastPerformanceSetLine` per set with
  a `Divider` between them.
- Set-line text: `reps != null` -> `lastPerformance.reps`; else `durationSeconds != null` -> `lastPerformance.seconds`;
  else empty. `weight != null` -> `lastPerformance.weight` (so `0` gives "0 kg"); else `lastPerformance.noWeight`.

Imports flow downward: view -> organism -> molecules -> atoms. Only atoms import `react-native-paper`.

### D6. Copy

New keys in `app/src/i18n/{es,en}/workouts.json` under `exercise.lastPerformance`: `link`, `title`, `setsDone`,
`effort`, `setsRecorded`, `highest`, `reps`, `seconds`, `weight`, `noWeight`, `close`. `effort` is the card's title only;
the date and the set number are shown bare, so they need no key. Values are in the brief and the spec. Plus the
top-level `efforts` and `effortsByType`. If `common` already has a close label, reuse it instead of `close`.

## Errors

No error is shown for this read. Any `DomainError` (`NOT_FOUND`, `UNAUTHORIZED`, `NETWORK`, `UNEXPECTED`) settles the
skeleton and leaves the link out, since the last session is a convenience and the screen works without it. `204` is not
an error either. No new code in the HTTP error mapper and no new error copy.

## Tests

- Core: `get-my-exercise-last-performance-use-case.test.ts` mirrors `get-user-trainer-use-case.test.ts`;
  `user-repository.test.ts` gains a `getMyExerciseLastPerformance` block mirroring `getTrainer` (`200`, `204`, `401`,
  `404`, network) with a fake `HttpClientInterface`.
- App: `workout-exercise-view.test.tsx` gains the link, skeleton and dialog scenarios from the spec, faking the use case
  through the container. `effort.test.ts` covers the lookup and `last-performance.test.ts` the highest weight and the
  has-a-weight rule. Translation parity covers the keys. Test helpers and patterns stay as
  they are.

## Risks / Trade-offs

- [The refreshed snapshot types `effort` as a plain `string` or as nullable] → `effortKeysOf` takes a `string` and the
  dialog hides the line when it is empty. Check the generated type after the refresh.
- [The refresh also pulls other pending API changes] → Review the `v1.json` diff. If the backoffice typecheck breaks on
  unrelated changes, stop and report instead of fixing ahead.
- [Effort copy drifts between fronts] → Accepted (D3).
- [One extra request each time an exercise is shown] → Accepted: it is a small read, and the skeleton covers its latency.
- [A failed read hides the link with no explanation] → Accepted, since the screen is complete without it.
- [`workoutDate` parsed as UTC shifts a day back in negative offsets] → Parse `YYYY-MM-DD` as a local date.

## Migration Plan

Snapshot refresh and regeneration form their own commit, before the rest. There is no data migration. Rolling back
means reverting the commits; the endpoint is additive.
