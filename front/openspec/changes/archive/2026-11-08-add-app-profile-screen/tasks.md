# Tasks

Prerequisite: the refreshed `core/openapi/v1.json` and `core/src/contract/generated/api.ts` (only
`GET /users/{userId}/trainer` added) are committed on their own before these tasks start.

## 1. Core

- [x] 1.1 Add `getTrainer(userId)` to `UserRepositoryInterface` and implement it in `UserRepository`
      (`GET /users/{userId}/trainer`, `undefined` -> `null`, errors through the mapper); add its block to
      `core/test/infrastructure/repositories/user-repository.test.ts` (route and method, `200`, `204` -> `null`,
      `401`, `403`, `404`, `500`, network); verify the core tests pass.
- [x] 1.2 Add `GetUserTrainerUseCase` in `core/src/application/use-cases/users/get-user-trainer-use-case.ts`, export
      it, and declare `getUserTrainerUseCase` in `core/src/application/dependencies.ts`; add
      `core/test/application/use-cases/users/get-user-trainer-use-case.test.ts` modelled on
      `get-my-profile-use-case.test.ts`; verify `pnpm --filter @weight-tracker/core test` passes.
- [x] 1.3 Review core against `getMe` and `GetMyProfileUseCase` and their tests (naming, error handling, labelled
      Arrange/Act/Assert); verify no domain type was hand-written and the generated contract was not edited.

## 2. Containers and test helpers (both fronts)

- [x] 2.1 Register `getUserTrainerUseCase` in `app/src/infrastructure/container.ts` and
      `backoffice/src/infrastructure/container.ts`; add the key to both `test/infrastructure/container.test.ts`; add
      the `getTrainer` and `getUserTrainerUseCase` no-op stubs to both `test/support/render-with-providers.tsx`;
      verify `pnpm typecheck` passes and both container tests pass.

## 3. App translations

- [x] 3.1 Add `app/src/i18n/{es,en}/profile.json` with the keys and texts in the brief, register the `profile`
      namespace in `app/src/i18n/resources.ts`; verify the translation parity test passes (`common:home.logout` is
      removed in 5.2, with its last user).

## 4. App atoms and molecules

- [x] 4.1 Add the atoms `avatar.tsx`, `icon-button.tsx`, `card.tsx`, `list-item.tsx` in
      `app/src/presentation/common/components/atoms/`, modelled on `button.tsx` / `text.tsx`; verify
      `pnpm --filter app typecheck` passes.
- [x] 4.2 Add `app/src/presentation/common/format/initials.ts`; verify typecheck passes (behaviour is covered by the
      view test's "AL" and "CR" assertions).
- [x] 4.3 Add the molecules `loading-state.tsx` and `error-state.tsx` in `common/components/molecules/`, and
      `profile-summary.tsx`, `trainer-card.tsx`, `profile-menu.tsx` in `profile/components/molecules/`, importing only
      atoms (plus `ActivityIndicator` in `loading-state`); verify typecheck passes and no molecule calls a use case or
      `useTranslation`.

## 5. App hooks and views

- [x] 5.1 Add `app/src/presentation/profile/hooks/use-profile.ts` and `use-logout.ts` as in `design.md`; verify
      typecheck passes.
- [x] 5.2 Remove the logout button, error text, state and handler from `HomeView`, and `home.logout` from both
      `common.json`; update `home-view.test.tsx` to drop its logout cases and assert no "Cerrar sesión"; verify the
      home test and the parity test pass.
- [x] 5.3 Replace `ProfileView` with the screen in `design.md` and rewrite `profile-view.test.tsx` to cover every
      scenario in `specs/app-profile/spec.md` and `specs/app-logout/spec.md` (loading; identity "AL" / "Ana López" /
      email; trainer card "CR" / "Carlos Ruiz"; no trainer card on `null`, asserted absent; five menu items in order
      with three subtitles; tapping "Ajustes" and a menu item calls no use case; load and trainer failures show the
      message and "Reintentar", retry loads again; logout success reloads the session, in-flight disables the button,
      `UNEXPECTED` shows the message; English labels); verify the test passes.
- [x] 5.4 Manual check in Expo Go: sections in order, with and without a trainer; cog and menu do nothing; logout from
      Perfil shows login; home has no logout.

## 6. Review

- [x] 6.1 Review the app against its references: atoms against `button.tsx` / `text.tsx`, hooks and view against the
      former `HomeView` logout handling, test against `home-view.test.tsx`; verify only atoms (and `loading-state`'s
      single `ActivityIndicator`) import Paper widgets, imports flow downward, no colour literal outside `theme.ts`,
      and the AGENTS.md quality checklist holds.
- [x] 6.2 Verify `.stpr/template/` has no drift with the drift loop in `AGENTS.md` (no output).

## 7. Verification

- [x] 7.1 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/`; verify all pass.

## Workflow follow-up

- On archive, after syncing specs, update the `## Purpose` of `openspec/specs/app-logout/spec.md`: logout now lives on
  the profile screen and is no longer provisional.
