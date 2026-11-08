# Design

## Context

See `proposal.md` for motivation and `specs/` for the required behaviour.

- **Core.** `UserRepository` (`core/src/infrastructure/repositories/user-repository.ts`) wraps `HttpClientInterface`
  and maps errors through the shared HTTP error mapper. `FetchHttpClient` returns `undefined` for an empty body, so a
  `204` reaches the repository as `undefined`. Use case keys are declared in `core/src/application/dependencies.ts`
  (`getMyProfileUseCase: UseCaseInterface<void, UserProfile>`), and both fronts' containers and test helpers list every
  key. The trainer is a `UserProfile` (`UserProfileResponse` in the contract), already aliased.
- **App.** The Perfil tab renders a placeholder `ProfileView` (heading only). `HomeView` holds the temporary logout:
  `logoutUseCase` through the container, then `session.reload()`, error code in `useState`. Atoms today: `button`,
  `text`, `text-input`, `helper-text`; no molecules exist. i18n namespaces are `auth`, `common`, `errors`, registered
  in `app/src/i18n/resources.ts`. Paper icons resolve through `@expo/vector-icons`. Tab screens stay mounted once
  opened.

## Goals / Non-Goals

**Goals:**

- Establish the first app molecules and the shared loading and error states as references for later screens.
- Keep the profile and trainer reads screen-local; no app-wide provider.

**Non-Goals:**

- Any action for the cog, the trainer item or the menu items.
- Refetching on tab focus, pull-to-refresh, avatar pictures.

## Decisions

### Core

- `UserRepositoryInterface.getTrainer(userId: string): Promise<Result<UserProfile | null, DomainError>>`.
- `UserRepository.getTrainer` sends `GET /users/{userId}/trainer` (path built the way the existing `/users/{id}/...`
  methods build theirs) and returns `ok(response ?? null)`: `undefined` from a `204` becomes `null`; any thrown error
  goes through the mapper like `getMe`.
- `GetUserTrainerUseCase implements UseCaseInterface<string, UserProfile | null>` in
  `core/src/application/use-cases/users/get-user-trainer-use-case.ts`, exported from the area's `index.ts`; it only
  calls the repository.
- `dependencies.ts`: `getUserTrainerUseCase: UseCaseInterface<string, UserProfile | null>`.
- No new domain alias, port, error code or mapper branch.

### Container registrations

- `app/src/infrastructure/container.ts` and `backoffice/src/infrastructure/container.ts`:
  `getUserTrainerUseCase: (c) => new GetUserTrainerUseCase(c.resolve("userRepository"))`, beside
  `getMyProfileUseCase`.
- Both `test/support/render-with-providers.tsx` gain `getTrainer` in the `userRepository` stub and
  `getUserTrainerUseCase` in the use case stubs, same no-op pattern. Both `container.test.ts` key lists gain
  `"getUserTrainerUseCase"`. These are additions in the existing pattern, not a change to it.

### App components (atomic level and folder)

Atoms, `app/src/presentation/common/components/atoms/` (only atoms import Paper widgets; each reads colours through
Paper, no hooks of their own beyond what Paper does, text by props):

- `avatar.tsx` -> Paper `Avatar.Text`, props `label` and optional `size`. Used by `profile-summary` and `trainer-card`.
- `icon-button.tsx` -> Paper `IconButton`, props `icon`, `accessibilityLabel`, optional `onPress`.
- `card.tsx` -> Paper `Card` (`mode="outlined"`), children. Used by `trainer-card` and `profile-menu`.
- `list-item.tsx` -> Paper `List.Item`, props `title`, optional `description`, optional `icon` (rendered through
  `List.Icon` as `left`), optional `left` element (for the avatar), optional `onPress`.

Molecules:

- `common/components/molecules/loading-state.tsx` -> centred Paper `ActivityIndicator`. It is a widget used by one
  file, so the molecule imports it directly, as the invariants allow for a single-use widget.
- `common/components/molecules/error-state.tsx` -> `text` atom with the message and `button` atom "retry", props
  `message`, `retryLabel`, `onRetry`.
- `profile/components/molecules/profile-summary.tsx` -> `avatar` + `text` (name, `titleMedium`) + `text` (email,
  `bodyMedium`, `onSurfaceVariant`), props `name`, `surname`, `email`; computes initials with the helper.
- `profile/components/molecules/trainer-card.tsx` -> `card` + `list-item` with `avatar` on the left, props `title`,
  `name`, `surname`.
- `profile/components/molecules/profile-menu.tsx` -> `card` + five `list-item`s, props: a list of `{ icon, title,
description? }` built and translated by the view. Items get no `onPress`.

Molecules call no use case and no `useTranslation`; the view passes translated text. Imports flow view -> molecule ->
atom.

- Initials helper `app/src/presentation/common/format/initials.ts`: `initials(name, surname)` -> first character of
  each trimmed value, upper-cased, joined (`"Ana", "López"` -> `"AL"`). Display formatting only.
- Icons: `cog-outline`, `dumbbell`, `target`, `heart-outline`, `bell-outline`, `information-outline`.

### Screen hooks and view

- `profile/hooks/use-profile.ts` -> `useState` for `status` (`"loading" | "ready" | "error"`), `profile`, `trainer`,
  `errorCode`; a `load` function: `getMyProfileUseCase`, then on success `getUserTrainerUseCase(profile.id)`; any
  failure sets `error` with its code and keeps no partial data; `useEffect` runs `load` on mount; returns `retry =
load`. A stale-response guard (an `active` flag in the effect cleanup) avoids setting state after unmount.
- `profile/hooks/use-logout.ts` -> the logic moved from `HomeView`: `loggingOut`, `errorCode`, `logout()` calling
  `logoutUseCase`, then `session.reload()` on success, or setting the code and clearing `loggingOut` on failure. No
  navigation.
- `ProfileView` (`profile/views/profile-view.tsx`) -> `useTranslation("profile")`, both hooks. Layout: a `ScrollView`
  with the title row (`text` heading `headlineMedium` + `icon-button`), then by status: `loading-state`, `error-state`
  with `errors:<code>`, or `profile-summary`, `trainer-card` (only when `trainer` is not `null`), `profile-menu`. The
  logout `button` (`mode="outlined"`, `loading`, `disabled` while in flight) and its `helper-text` sit at the bottom in
  every status, so logout stays reachable when the profile cannot load. Styles in a bottom `StyleSheet.create`; spacing
  numbers only, no colour literals.
- `HomeView` loses the logout button, `HelperText`, the state and the handler, keeping title and signed-in text.

### State

- `useProfile`: profile, trainer, status, error code -> changed by its own load and retry -> reset when `ProfileView`
  unmounts (logout unmounts `(app)`; tab switches do not).
- `useLogout`: `loggingOut`, `errorCode` -> changed by the logout button -> reset on unmount.
- No provider is added.

### Errors and translations

- Codes involved: any `DomainError` code from the two reads (`NETWORK`, `UNEXPECTED`, `UNAUTHORIZED`, `FORBIDDEN`,
  `NOT_FOUND`) and `UNEXPECTED` from logout. All exist in `errors.json` in `es` and `en`; none is new.
- New `profile` namespace, `app/src/i18n/{es,en}/profile.json`, keys as listed in the brief (`title`, `settings`,
  `trainer.title`, `menu.*`, `logout`, `retry`), registered in `resources.ts`. `common:home.logout` removed from both
  languages.
- The backoffice gets no translation, style or presentation change.

### Tests

- Core: `user-repository.test.ts` gains a `getTrainer` block (route and method sent; `200` -> profile; `204` (client
  returns `undefined`) -> `null`; `401`, `403`, `404`, `500`, network); new `get-user-trainer-use-case.test.ts` modelled
  on `get-my-profile-use-case.test.ts`.
- App: `profile-view.test.tsx` rewritten from the placeholder, modelled on `home-view.test.tsx`, faking
  `getMyProfileUseCase`, `getUserTrainerUseCase`, `logoutUseCase` and `getSessionUseCase` through the container,
  covering every scenario in `specs/app-profile` and `specs/app-logout`, asserting absence for the no-trainer case.
- App: `home-view.test.tsx` drops its logout cases and asserts no "Cerrar sesión".
- Containers: both `container.test.ts` resolve the new key. Translation parity covers `profile`.
- Arrange / Act / Assert labels with one Act, as in the existing tests.
- Found during apply: Paper icons load their font through `expo-font`, which has no assets under Jest, so
  `profile-view.test.tsx` mocks `expo-font` (`isLoaded: () => true`, no-op `loadAsync`) at the top of the file, the
  same per-file `jest.mock` pattern the app tests use for `expo-router` and `expo-sqlite`. The shared Jest setup is not
  changed.

## Risks / Trade-offs

- [Two sequential requests make the first paint slower] → acceptable for one screen; the trainer read needs the id
  from the first. A future `/users/me/trainer` would remove the hop.
- [A trainer failure hides the whole profile] → chosen for clarity (brief); retry repeats both.
- [Profile goes stale after edits made elsewhere] → no edit exists in the app yet; refetch on focus is out of scope.
- [Paper `List.Item` and `Card` default paddings differ from the backoffice look] → theme-driven; fine-tuning with
  `custom.*` tokens is a later change.
- [`FetchHttpClient` returning `undefined` for `204` is implicit] → the repository test pins it with a `204` case.
