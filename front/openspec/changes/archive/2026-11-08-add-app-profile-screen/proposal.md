# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-app-profile-screen.md`.

## Why

The Perfil tab shows only a heading. The athlete needs to see who they are signed in as and who their trainer is, and
a stable place for account options and logout. Logout leaves home, where `add-logout` put it as a development
convenience.

## What Changes

- **Core:** reading a user's current trainer (`GET /users/{userId}/trainer`): a `getTrainer(userId)` repository
  method on the user repository and a `GetUserTrainerUseCase` returning the trainer's `UserProfile`, or `null` when the
  API answers `204` (no active trainer). Registered as `getUserTrainerUseCase` in both fronts' containers.
- **App, Perfil tab** (`/profile`), top to bottom:
  - Title row: "Perfil" with a settings cog icon button on the right (no action yet).
  - Identity row: initials avatar, full name, email as subtitle.
  - Trainer card, only when the athlete has a trainer: initials avatar, "Mi entrenador", trainer's full name. No action.
  - Menu card, design only (no action on tap): Mis Datos, Mis Objetivos, Limitaciones, Notificaciones, Acerca de
    Trainera, each with its icon and, for the first three, a subtitle.
  - Logout button.
  - Loading state while the profile loads; error state with retry when it fails.
- **App, home:** the logout button and its handling are removed.
- **Translations:** new `profile` namespace in `es` and `en`; `common:home.logout` removed.
- The API contract snapshot was already refreshed (only `GET /users/{userId}/trainer` was added) and is committed on
  its own before this change.

Fronts and roles:

- **app** -> `USER` -> sees their own profile and trainer and logs out here. An athlete without a trainer must NOT see
  the trainer card or any empty placeholder for it. A signed-out visitor sees nothing (the access gate hides `(app)`).
- **backoffice** -> `ADMIN`, `TRAINER` -> no screen changes; it only registers the new use case, because every front
  registers every core key.

Components (atomic design), all in the app:

- New atoms in `common/components/atoms`: `avatar` (Paper `Avatar.Text`), `icon-button` (Paper `IconButton`), `card`
  (Paper `Card`), `list-item` (Paper `List.Item` / `List.Icon`). Reused atoms: `text`, `button`, `helper-text`.
- New molecules: `loading-state` and `error-state` in `common/components/molecules` (first app view needing them);
  `profile-summary`, `trainer-card` and `profile-menu` in `profile/components/molecules`.
- No organism: the view composes the molecules and two screen hooks (`use-profile`, `use-logout`).

Canonical references:

- `getTrainer` -> `UserRepository.getMe` (`core/src/infrastructure/repositories/user-repository.ts`); the `204` ->
  `null` branch has no reference: **this change establishes it**.
- `GetUserTrainerUseCase` -> `core/src/application/use-cases/users/get-my-profile-use-case.ts`.
- Core tests -> `core/test/infrastructure/repositories/user-repository.test.ts`,
  `core/test/application/use-cases/users/get-my-profile-use-case.test.ts`.
- Container entries -> the `getMyProfileUseCase` entry in each `container.ts`.
- Atoms -> `app/src/presentation/common/components/atoms/button.tsx`, `text.tsx`.
- Molecules -> none exist in the app: **this change establishes the first ones**, per the invariants' definition.
- Screen hooks and view -> the logout handling of `app/src/presentation/common/views/home-view.tsx` (`add-logout`).
- View test -> `app/test/presentation/common/views/home-view.test.tsx`.

API: `GET /users/me` and `GET /users/{userId}/trainer`, both in `core/openapi/v1.json`. No business rule, no form,
no new error code (`403`, `404` already mapped).

Conflicts with the template, the invariants or a stop case: none. One refinement of the brief: the core trainer read
is specified under `trainer-athletes` (which owns reading trainer–athlete relationships) instead of
`account-profile` (the signed-in user's own profile); the code still lives on the user repository, beside the
endpoint's `/users` route.

## Capabilities

### New Capabilities

- `app-profile`: the app's profile screen: identity, trainer card, menu, loading and error states.

### Modified Capabilities

- `app-logout`: the logout button moves from home to the profile screen; home no longer shows it.
- `trainer-athletes`: adds reading a user's current trainer, with `null` when there is none.

## Impact

- `core/src/application/ports/user-repository-interface.ts`, `core/src/infrastructure/repositories/user-repository.ts`,
  `core/src/application/use-cases/users/` (+ index), the `Dependencies` declaration.
- `app/src/infrastructure/container.ts`, `backoffice/src/infrastructure/container.ts`.
- `app/src/presentation/common/components/{atoms,molecules}/`, `app/src/presentation/common/format/initials.ts`,
  `app/src/presentation/profile/{components,hooks,views}/`, `app/src/presentation/common/views/home-view.tsx`.
- `app/src/i18n/{es,en}/{profile,common}.json`, `app/src/i18n/resources.ts`.
- Tests: core repository and use case tests; app `profile-view`, `home-view`, `container` tests; both fronts'
  `test/support/render-with-providers.tsx` (new no-op stubs, existing pattern); backoffice container test if it lists
  keys.
- Not affected: the API, the access gate, `LogoutUseCase`, login and signup, the tab bar, backoffice screens.

Acceptance: the screen shows the sections in order, with the trainer card only when there is a trainer; nothing in
the menu reacts; logout from the profile shows login; home has no logout; `pnpm typecheck && pnpm lint && pnpm test`
passes from `front/`.
