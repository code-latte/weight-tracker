# Change Proposal: Add App Profile Screen

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-app-profile-screen/`.

## Change

Replace the placeholder Perfil tab with the athlete's profile screen: identity row, trainer card, a settings menu
(design only) and the logout button, which moves here from home.

## Objective

The Perfil tab shows only a heading. The athlete needs to see who they are signed in as and who their trainer is,
and a stable place for account options and logout. The menu items are laid out now so later changes only wire each
one. Logout leaves home, where `add-logout` put it as a development convenience.

## Capabilities

From top to bottom:

- **Title row:** "Perfil" on the left; a cog icon button on the right (settings). The cog does nothing yet.
- **Identity row:** an avatar with the athlete's initials (first letter of name and of surname) on the left; next to
  it the full name (`name surname`) and, under it as a subtitle, the email.
- **Trainer card** (only when the athlete has a trainer): one list item with the trainer's initials avatar on the left,
  title "Mi entrenador" and the trainer's full name as subtitle. Not wired (no action on tap).
- **Menu card** (design only, nothing wired, no action on tap), one list item each, in this order:
  - dumbbell icon -> "Mis Datos" -> "Medidas, pesos, edad..."
  - target icon -> "Mis Objetivos" -> "Grasa, Músculo, Rendimiento..."
  - heart icon -> "Limitaciones" -> "Lesiones, molestias, observaciones..."
  - bell icon -> "Notificaciones" -> no subtitle
  - info icon -> "Acerca de Trainera" -> no subtitle
- **Logout button** at the bottom: logs out; the access gate then shows login. It is removed from home.
- While the profile loads, a loading state; if it fails, an error message with a retry button.

Avatars show initials only; the picture comes in a later change.

## Fronts And Roles

- backoffice -> lands here: no -> it only registers the new use case in its container, since every front registers
  every core key. No screen changes.
- app -> lands here: yes -> roles: `USER` -> the athlete sees their own profile and trainer and can log out.

No role difference inside the app (only `USER` signs in). An athlete without a trainer must NOT see the trainer card,
nor an empty card or placeholder in its place. A signed-out visitor sees nothing of this screen (the access gate
already hides `(app)`).

## Classification

Core:

- `UserRepositoryInterface.getTrainer(userId)` -> repository contract -> `core/src/application/ports/user-repository-interface.ts`
- `UserRepository.getTrainer` (`GET /users/{userId}/trainer`; `204` -> `null`) -> repository implementation -> `core/src/infrastructure/repositories/user-repository.ts`
- `GetUserTrainerUseCase` (`string` -> `UserProfile | null`) -> use case -> `core/src/application/use-cases/users/get-user-trainer-use-case.ts`
- `getUserTrainerUseCase` -> `Dependencies` key -> `core/src/application` (where `getMyProfileUseCase` is declared)

No new domain alias: the trainer is a `UserProfile`, already aliased in `core/src/domain/users/user.ts`.

App:

- `getUserTrainerUseCase` registration -> composition root -> `app/src/infrastructure/container.ts`
- `Avatar` (Paper `Avatar.Text`, label by props) -> atom -> `app/src/presentation/common/components/atoms/avatar.tsx`
- `IconButton` (Paper `IconButton`) -> atom -> `app/src/presentation/common/components/atoms/icon-button.tsx`
- `Card` (Paper `Card`) -> atom -> `app/src/presentation/common/components/atoms/card.tsx`
- `ListItem` (Paper `List.Item` with an optional `List.Icon` or custom left element, title and optional description
  by props) -> atom -> `app/src/presentation/common/components/atoms/list-item.tsx`
- `LoadingState`, `ErrorState` (message + retry button) -> molecules -> `app/src/presentation/common/components/molecules/`
  (first app view that needs them creates them there, per the invariants)
- `ProfileSummary` (avatar + full name + email) -> molecule -> `app/src/presentation/profile/components/molecules/profile-summary.tsx`
- `TrainerCard` (card + one list item with avatar) -> molecule -> `app/src/presentation/profile/components/molecules/trainer-card.tsx`
- `ProfileMenu` (card + five static list items, translated labels by props from the view) -> molecule -> `app/src/presentation/profile/components/molecules/profile-menu.tsx`
- `useProfile` (loads the profile, then the trainer; holds both, the status and the last error code; `retry`) ->
  screen hook -> `app/src/presentation/profile/hooks/use-profile.ts`
- `useLogout` (in-flight flag and last error code around `logoutUseCase`, then `session.reload()`) -> screen hook ->
  `app/src/presentation/profile/hooks/use-logout.ts`
- `ProfileView` (title row, the molecules, logout button; replaces the placeholder) -> view -> `app/src/presentation/profile/views/profile-view.tsx`
- `HomeView` (loses the logout button and its handling) -> view -> `app/src/presentation/common/views/home-view.tsx`
- Initials helper (first letters of name and surname, upper-cased) -> presentation formatting ->
  `app/src/presentation/common/format/initials.ts` (display formatting, not a business rule)
- `profile.*` keys -> i18n -> new `profile` namespace in `app/src/i18n/{es,en}/profile.json`, registered in
  `app/src/i18n/resources.ts`; `home.logout` removed from `common.json`

Backoffice:

- `getUserTrainerUseCase` registration -> composition root -> `backoffice/src/infrastructure/container.ts`

## Canonical Template Mapping

- `getTrainer` contract and implementation -> `UserRepository.getMe` in `core/src/infrastructure/repositories/user-repository.ts`
  -> same repository and endpoint family; the `204` -> `null` branch has no reference: **this change establishes it**.
- `GetUserTrainerUseCase` -> `core/src/application/use-cases/users/get-my-profile-use-case.ts` -> same area, single
  repository call with a parameter like `GetTrainerAthletesUseCase`.
- Core tests -> `core/test/infrastructure/repositories/user-repository.test.ts` and
  `core/test/application/use-cases/users/get-my-profile-use-case.test.ts`.
- Container registrations -> the `getMyProfileUseCase` entry in each front's `container.ts`; the key added to
  `app/test/infrastructure/container.test.ts` (and its backoffice counterpart if it lists keys).
- Atoms -> `app/src/presentation/common/components/atoms/button.tsx` and `text.tsx` -> thin Paper wrappers.
- Molecules -> no app molecule exists: **this change establishes the first ones**, following the invariants'
  definition (a few atoms, local UI state at most, no use cases).
- `useProfile`, `useLogout` -> the logout handling in the app `home-view.tsx` (`add-logout`): use case through the
  container, `Result` to state, `errors:<code>` on failure, no navigation (the gate follows the session).
- `ProfileView` -> `app/src/presentation/common/views/home-view.tsx` (view calling a use case and showing a
  translated error).
- View test -> `app/test/presentation/common/views/home-view.test.tsx` (fakes through the container, success and
  failures).

The backoffice is not a UI reference. `.stpr/template/` and its README are not updated.

## Screens And Navigation

- app -> `/profile` -> `ProfileView` in the Perfil tab -> tapping "Perfil" -> stays on the screen; the cog, the trainer
  item and the menu items do nothing yet.
- Logout success -> no navigation from the screen; the gate shows login. Logout failure -> stays and shows the error.
- Profile load failure -> stays, shows the error state; retry loads again.

No new route.

## API Contract

- `GET /users/me` -> in `core/openapi/v1.json`: yes -> response `UserProfileResponse` (`id`, `email`, `name`,
  `surname`, `role`).
- `GET /users/{userId}/trainer` -> in `core/openapi/v1.json`: yes (added by the API; the snapshot was refreshed with
  `openapi:refresh` + `openapi:generate` on 2026-11-08, and only this path was added) -> `200` `UserProfileResponse`
  of the trainer; `204` when the user has no active trainer; `401`, `403` (a user may ask only about themselves),
  `404`.

The refreshed `core/openapi/v1.json` and `core/src/contract/generated/api.ts` are committed as a step of their own
before this change.

## Held State

- `useProfile` -> profile, trainer (`UserProfile | null`), status (`loading` / `ready` / `error`), last error code ->
  `ProfileView` (first load and retry) -> reset when the screen unmounts. Tab screens stay mounted, so it loads once
  per signed-in session, on first opening the tab; logout unmounts `(app)` and clears it.
- `useLogout` -> `loggingOut`, `errorCode` -> the logout button -> reset on screen exit.
- Stored tokens -> removed by `LogoutUseCase` -> unchanged from `add-logout`.

## Forms And Structural Validation

None.

## Errors And Copy

- Profile or trainer fails to load -> any `DomainError` code (`NETWORK`, `UNEXPECTED`, `FORBIDDEN`, `NOT_FOUND`, ...)
  -> error state with `errors:<code>` and "Reintentar". The screen does not show a partial profile.
- Logout fails in storage -> `UNEXPECTED` -> `errors:UNEXPECTED` under the logout button.
- No trainer (`204`) -> not an error -> the trainer card is not rendered.

No new error code (`403` and `404` are already mapped).

New `profile` namespace (Spanish from the request; English proposed):

- `title` -> "Perfil" -> "Profile"
- `settings` -> "Ajustes" -> "Settings" (accessible label of the cog)
- `trainer.title` -> "Mi entrenador" -> "My trainer"
- `menu.myData.title` -> "Mis Datos" -> "My data"
- `menu.myData.description` -> "Medidas, pesos, edad..." -> "Measurements, weights, age..."
- `menu.goals.title` -> "Mis Objetivos" -> "My goals"
- `menu.goals.description` -> "Grasa, Músculo, Rendimiento..." -> "Fat, Muscle, Performance..."
- `menu.limitations.title` -> "Limitaciones" -> "Limitations"
- `menu.limitations.description` -> "Lesiones, molestias, observaciones..." -> "Injuries, discomfort, notes..."
- `menu.notifications` -> "Notificaciones" -> "Notifications"
- `menu.about` -> "Acerca de Trainera" -> "About Trainera"
- `logout` -> "Cerrar sesión" -> "Log out" (moves from `common:home.logout`)
- `retry` -> "Reintentar" -> "Retry"

## Platform Capabilities And Dependencies

None. Icons come from Paper's Material Community Icons (`cog-outline`, `dumbbell`, `target`, `heart-outline`,
`bell-outline`, `information-outline`), already available through `@expo/vector-icons`.

## Out Of Scope

- Avatar pictures (initials only).
- Any behaviour for the cog, the trainer item and the menu items: no routes, no screens.
- Editing the profile, refreshing it on tab focus or pull-to-refresh.
- The trainer's contact details or anything beyond the name.
- Must not change: the API, the access gate, `LogoutUseCase`, the login and signup screens, the tab bar, backoffice
  screens.

## Notes

- **Snapshot refresh is already done** (see API Contract). It must be committed alone, before the change's own
  commits, as `AGENTS.md` requires.
- **Load order.** `useProfile` calls `getMyProfileUseCase`, then `getUserTrainerUseCase(profile.id)`. Either failure
  ends in the error state; retry repeats both.
- **Specs.** `app-logout` changes: the button moves from home to the profile screen (its requirements name "home").
  A new `app-profile` capability describes the screen. `account-profile` (core) gains the trainer read.
- **Test helpers.** Both fronts' `test/support/render-with-providers.tsx` list every repository method and use case
  key with no-op stubs, so `getTrainer` and `getUserTrainerUseCase` must be added there. This is the only helper
  change and follows the existing pattern.
- **Verification tiers:**
  - core unit: `user-repository.test.ts` gains `getTrainer` (200 -> profile, 204 -> `null`, 401, 403, 404, network);
    `get-user-trainer-use-case.test.ts` (passes the id, returns the repository result).
  - app component: `profile-view.test.tsx` replaces the placeholder test: loading state; identity row with initials,
    full name and email; trainer card with initials and name; no trainer card on `null` (assert it is absent); the
    five menu items with their subtitles; tapping a menu item calls no use case; logout calls the use case and
    reloads the session; logout disabled while in flight; logout `UNEXPECTED` shows the message; load failure shows
    the error state and retry loads again.
  - app component: `home-view.test.tsx` drops its logout cases and asserts the logout button is gone.
  - app: `container.test.ts` resolves the new key. Translation parity covers the `profile` namespace.
  - backoffice: container registration compiles and its container test (if it lists keys) resolves the new key. No
    backoffice E2E.
  - Manual check in Expo Go: layout and order as above, with and without a trainer.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.
- No conflict with a stop case. Where a reference is missing (first molecules, the `204` branch), this change
  establishes it as described above; no other deviation is allowed.
