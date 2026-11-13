# Proposal

## Why

When the athlete records a set on the exercise screen, they cannot see what they lifted last time, so they guess
the weight or keep notes elsewhere. The API now answers that question for the caller
(`GET /users/me/exercises/{exerciseId}/last-performance`, API change `add-exercise-last-performance-endpoint`).
This change brings the answer into the screen where sets are recorded, one tap away.

Source brief: `.stpr/plan/openspec/proposal-change-show-exercise-last-performance.md`.

## What Changes

- **Contract sync.** Refresh `core/openapi/v1.json` and regenerate `core/src/contract/generated`, in a commit of its
  own, against the API working tree of `add-exercise-last-performance-endpoint`. It adds
  `ExerciseLastPerformanceResponse` (`workoutDate`, `effort`, `sets[]`) and `LastPerformanceSetResponse` (`reps?`,
  `durationSeconds?`, `weight?` in kg).
- **Core.** New domain aliases, a `UserRepositoryInterface` method, its `UserRepository` implementation (`204` ->
  `null`) and `GetMyExerciseLastPerformanceUseCase`. Its `Dependencies` key is registered in both fronts'
  `container.ts`, because both are typed against the full `Dependencies`. Only the app uses it.
- **App, exercise screen.** The last performance is read when an exercise is shown. If its last session has at least
  one set with a weight, a "Marcas anteriores" link with a history icon appears between the exercise navigator and the
  list of sets; otherwise there is no link. The header keeps only its video link. A skeleton holds the place of the link
  and the sets while the read is in flight, so neither jumps when it settles.
- **App, "Última sesión" dialog.** Opens on the link with what was already read. Shows the session date, two cards (sets
  done, effort in the scale of the exercise's type) and a table of the recorded sets, with the highest weight marked.
- **App, effort labels.** The app gets its own copy of the backoffice's `efforts` / `effortsByType` keys and an
  `effortKeysOf` helper with the same lookup. The backoffice does not change.

No breaking change.

## Capabilities

### New Capabilities

- `exercise-last-performance`: core reads the signed-in user's last performance of a catalog exercise through the
  API, returns `null` when there is none and returns every failure as a `DomainError` code. Lands in `core`; only the app
  wires it. Canonical reference: `GetUserTrainerUseCase` and `UserRepository.getTrainer` (same repository, same
  `204` -> `null` read).
- `app-exercise-last-performance`: the "Marcas anteriores" link on the app's exercise screen and the "Última sesión"
  dialog it opens. Lands in the app only, for `USER`. The endpoint is always about the caller, so there is no role
  branch and no other user's data can appear. Canonical references: the header's video link
  (`exercise-hero-panel.tsx`), `confirm-dialog.tsx` for the dialog, `set-record-row.tsx` for a set line,
  `use-workout.ts` for the state hook, `workout-exercise-view.test.tsx` for the test. The effort lookup and copy mirror
  `backoffice/.../use-exercise-labels.ts` (`effortLabel`), with texts and logic only and no UI. Atom/molecule/organism
  references are `pending` in `.stpr/template/README.md`, so the app shapes above apply.

### Modified Capabilities

None. The new link sits on the screen between the navigator and the sets, without changing any `app-workout-exercise` requirement.

## Fronts And Roles

- **app**: `USER` opens the dialog from `/workouts/[id]/exercises/[exerciseId]` and sees their own last session.
- **backoffice**: one registration line in `container.ts`, and no screen, copy or behaviour change. `ADMIN` and
  `TRAINER` see nothing new; its effort labels stay where they are.

## Components

- `LastPerformanceDialog` -> organism -> `app/src/presentation/workouts/components/organisms/`, built on the `dialog`
  atom.
- `LastPerformanceSetLine` -> molecule -> `app/src/presentation/workouts/components/molecules/`.
- `LastPerformanceStatCard` -> molecule -> `app/src/presentation/workouts/components/molecules/`.
- `Divider` -> atom (new, wraps Paper's `Divider`) -> `app/src/presentation/common/components/atoms/`.
- `Skeleton` -> atom (new, a muted block) -> `app/src/presentation/common/components/atoms/`.
- `ExerciseSetsSkeleton` -> molecule -> `app/src/presentation/workouts/components/molecules/`.
- `ExerciseHeroPanel` (molecule, existing) does not change.
- Reused: atoms `dialog`, `card`, `text`, `text-link`, `icon`, `button`.

## Conflicts With The Invariants

- **Stop case avoided.** The original request asked to move the effort descriptions into `core` and have the
  backoffice read them from there. That would make `backoffice` and `app` share translations, which is a stop case in
  `STPR_INVARIANTS.md` ("Each front owns its own translations"). The brief resolved it: each front keeps its own copy
  and the backoffice stays unchanged. Moving labels into `core` would first need an amendment to the invariants, as a
  separate change.
- **No business rule** enters the front. The API decides which workouts count as the "last" one. Which scale to show is
  a presentation lookup on the exercise type the API already returns.
- **No hand-written type.** The response types come from the refreshed snapshot.

## Acceptance Criteria

- The refreshed snapshot and regenerated types are committed on their own; `core/src/contract/generated` is not
  hand-edited.
- The core use case and repository are covered for `200`, `204` -> `null`, `401`, `404` and a network failure.
- On the exercise screen, "Marcas anteriores" opens a dialog titled "Última sesión" showing the date, the effort label for
  the exercise's type, and the set table. A `null` weight reads "sin peso" and never "null kg"; `0` reads "0 kg".
- The link is shown only when at least one set of the last session has a weight; it is hidden while reading, on a
  failed read, with no last session and when no set has a weight.
- A skeleton replaces the sets and the link while the last performance is read, so the screen does not flicker.
- Every new string exists in `es` and `en`; translation parity passes.
- `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`. There is no E2E, since app E2E is a known gap.

## Impact

- `core`: `openapi/v1.json`, `src/contract/generated`, `src/domain/users`, `src/application/ports`,
  `src/application/use-cases/users`, `src/infrastructure/repositories/user-repository.ts`, matching tests.
- `app`: `src/infrastructure/container.ts`, `src/presentation/workouts/{hooks,components,views}`, a new effort-label
  helper, `src/i18n/{es,en}/workouts.json`, `test/presentation/workouts`.
- `backoffice`: `src/infrastructure/container.ts` registers the new key. It has to keep compiling after the
  snapshot refresh.
- API: none from this change; it depends on `add-exercise-last-performance-endpoint`.
- Dependencies: none new. Expo Go is enough.
