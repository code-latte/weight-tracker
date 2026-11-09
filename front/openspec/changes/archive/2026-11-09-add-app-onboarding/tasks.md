# Tasks

Core has no work in this change (design.md, Non-Goals): the profile use cases and `validateProfileName` exist and are
registered. Tasks start at the app.

## 1. Invariants

- [x] 1.1 Amend the splash bullet of `.stpr/template/STPR_INVARIANTS.md` (4.3, "Root layout and auth gate"): with a
      stored session the splash also waits for the current user's profile read to end, a failure only ends the wait
      and the gate shows the onboarding. Verify the bullet reads as in design.md decision 2 and the drift check in
      `AGENTS.md` prints nothing.

## 2. Translations

- [x] 2.1 Add `app/src/i18n/{es,en}/onboarding.json` with the keys of design.md decision 6 (values from the brief) and
      register the namespace in `app/src/i18n/resources.ts`; verify the translation parity test passes.

## 3. Current-user state hook

- [x] 3.1 Create `current-user-context.ts`, `current-user-provider.tsx` (common `context/`) and `use-current-user.ts`
      (common `hooks/`) as in design.md decision 1: read through `runWithSession` on sign-in, reset on sign-out,
      `reload` keeping the previous status, `setProfile`, derived `onboardingPending` and `settled`. Verify
      `pnpm typecheck` passes.
- [x] 3.2 Mount `CurrentUserProvider` inside `SessionProvider` in `AppProviders` and in
      `app/test/support/render-with-providers.tsx` (documented helper exception); verify all existing app tests pass.
- [x] 3.3 Add `test/presentation/common/context/current-user-provider.test.tsx` with the cases of design.md decision 8;
      verify it passes.

## 4. Root guards and splash

- [x] 4.1 In `app/src/app/_layout.tsx`, add the profile condition to `ready` and the three guards of design.md decision
      2 (`(auth)`, `(onboarding)`, `(app)`); verify `pnpm typecheck` passes.

## 5. Onboarding flow state

- [x] 5.1 Create `app/src/presentation/onboarding/steps.ts` with the name step (`id`, `href: "/name"`, `titleKey`,
      `validate` with `validateProfileName`); verify `pnpm typecheck` passes.
- [x] 5.2 Create `onboarding-context.ts`, `onboarding-provider.tsx` and `use-onboarding.ts` (onboarding area) as in
      design.md decision 4; add `test/presentation/onboarding/context/onboarding-provider.test.tsx` with a two-step
      fake list (current-step validation only, values kept, `finish` sends every step's values trimmed). Verify it
      passes.

## 6. Components

- [x] 6.1 Create the `ProgressBar` atom (`app/src/presentation/common/components/atoms/progress-bar.tsx`); verify
      `pnpm typecheck` passes.
- [x] 6.2 Move `useLogout` to `app/src/presentation/common/hooks/use-logout.ts` and update `ProfileView`'s import;
      verify `profile-view.test.tsx` passes.
- [x] 6.3 Create the `OnboardingHeader` and `ProfileNameFields` molecules (onboarding area) as in design.md decision 5;
      verify `pnpm typecheck` and `pnpm lint` pass.
- [x] 6.4 Create the `OnboardingStepLayout` organism (onboarding area), including the unreadable-profile branch with
      `ErrorState`; verify `pnpm typecheck` and `pnpm lint` pass.

## 7. Name step and routes

- [x] 7.1 Create `NameStepView` (`app/src/presentation/onboarding/views/name-step-view.tsx`), the group layout
      `app/src/app/(onboarding)/_layout.tsx` (`OnboardingProvider` around a `Stack`, no header, first step without
      gesture back) and the route `app/src/app/(onboarding)/name.tsx` (two statements). Regenerate typed routes with
      a brief `expo start` and verify `pnpm typecheck` passes.
- [x] 7.2 Add `test/presentation/onboarding/views/name-step-view.test.tsx` (template: `signup-view.test.tsx`) with the
      cases of design.md decision 8; verify it passes.

## 8. Review

- [x] 8.1 Review the app against the mapped references (proposal "Canonical References"): provider shapes against
      `session-provider.tsx`, the group layout and route against the routing tree, atoms against `icon-button.tsx`,
      molecules against `section-header.tsx`, organism and view against `signup-form.tsx` / `signup-view.tsx`;
      confirm only atoms import Paper widgets, presentation imports no `src/infrastructure` outside
      `app-providers.tsx`, providers do not navigate, and every string is in `es` and `en`. Record and fix findings.
- [x] 8.2 Confirm core, backoffice and both `container.ts` are unchanged: this command prints nothing,
      `git diff --stat -- core backoffice app/src/infrastructure/container.ts`.
- [x] 8.3 Manual check in Expo Go: signup -> name step -> Inicio; launch with an incomplete profile -> name step
      without a tabs flash; deep link to `/workouts` while in onboarding -> name step; deep link to `/name` signed out
      -> login; launch with the API stopped -> onboarding error with "Reintentar" and no tabs; start the API and retry
      -> Inicio (complete) or the pre-filled name step (incomplete); "Cerrar sesión" from onboarding -> login.
- [x] 8.4 Run `pnpm typecheck && pnpm lint && pnpm test` from `front/` and verify it passes. No E2E is planned.
