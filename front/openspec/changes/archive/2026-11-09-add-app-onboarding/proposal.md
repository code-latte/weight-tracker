# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-add-app-onboarding.md`.

## Why

App signup asks only for email and password, so every new athlete reaches the tabs with an empty name and surname:
the profile shows an empty identity row and trainers see nameless athletes. The backoffice solves this with its
onboarding dialog; the app needs the mobile equivalent now, built to take more steps later.

## What Changes

- A signed-in athlete is **in onboarding** when their profile has an empty name or surname (trimmed), **or when the
  profile cannot be read**. The tabs open only with a profile known to be complete.
- New `(onboarding)` route group, a sibling of `(app)` and `(auth)`, guarded in the root layout. It requires a session
  exactly like `(app)`: without one only `(auth)` renders. While in onboarding no tab, tab bar or `(app)` screen is
  reachable, even through a deep link; with a complete profile no onboarding screen is.
- Onboarding is a stack with one screen per step. Each step shows "Paso n de total" with a progress bar, its title and
  fields, and "Siguiente" (not last) or "Finalizar" (last). The first step cannot be left by back. Values persist
  across steps.
- First and only step: "Dinos cómo te llamas" with "Nombre" and "Apellidos" (3 to 100 characters after trimming),
  pre-filled with existing values. "Finalizar" sends one `PUT /users/me` with everything collected; on success the
  gate moves the athlete to Inicio.
- An unreadable profile shows, inside onboarding, the translated error with "Reintentar" instead of the step.
- "Cerrar sesión" is available in the onboarding header.
- The splash also waits for the profile read when a session is restored, so the tabs never flash.
- **BREAKING (spec):** after signup or login, an athlete with an incomplete profile sees onboarding, not Inicio; the
  gate now reads the profile from the API once signed in.
- `STPR_INVARIANTS.md`: the splash bullet gains the profile read as a wait condition (amended, not deviated from).

Fronts and roles: app only, role `USER`, no role difference. The backoffice keeps its dialog and does not change.

API: `GET /users/me` and `PUT /users/me`, both in `core/openapi/v1.json`. No snapshot refresh, no API change.

## Capabilities

### New Capabilities

- `app-onboarding`: when a signed-in athlete is in onboarding, the step flow (progress, actions, back, kept values),
  the name step, saving on Finalizar, the unreadable-profile state with retry, and logout from onboarding.

### Modified Capabilities

- `app-access-gate`: a third access state (signed in with onboarding pending); onboarding requires a session; the
  splash waits for the profile read; the gate reads the profile once signed in; signed-in destinations become "home
  or onboarding".
- `app-navigation`: Inicio is the default tab only for an athlete whose profile is known to be complete.

No core capability changes (`account-profile` already covers reading and updating the own profile).

## Canonical References

- Current-user state hook -> `app/src/presentation/auth/context/session-provider.tsx` (app provider shape); value
  reference only: `backoffice/src/presentation/common/components/current-user-provider.tsx` and `use-current-user.ts`.
- Root guards and splash -> `STPR_INVARIANTS.md` 4.3 "Root layout and auth gate" and the routing tree (sibling group
  for a state the user must leave before `(app)`).
- `(onboarding)` flow group with its provider -> routing tree `(<flow>)/_layout.tsx` -> no app flow exists yet:
  **this change establishes it**.
- Step list and flow hook -> `backoffice/src/presentation/onboarding/hooks/use-onboarding.ts`, value reference only.
- Atom `ProgressBar` (new) -> `app/src/presentation/common/components/atoms/icon-button.tsx`.
- Molecules `OnboardingHeader`, `ProfileNameFields` (new) -> `app/src/presentation/common/components/molecules/section-header.tsx`
  and the fields of `app/src/presentation/auth/components/organisms/signup-form.tsx`.
- Organism `OnboardingStepLayout` (new) and view `NameStepView` (new) -> `signup-form.tsx` and
  `app/src/presentation/auth/views/signup-view.tsx`; error with retry -> `profile-view.tsx`.
- Reused atoms: `SafeArea`, `Text`, `TextInput`, `HelperText`, `Button`, `IconButton`. Reused molecule: `ErrorState`.
- Tests -> `app/test/presentation/auth/views/signup-view.test.tsx`, `app/test/presentation/auth/context/session-provider.test.tsx`.

The backoffice is not a UI reference.

## Conflicts With The Template

- The splash rule names session, fonts and preferences only. Resolved by amending it in this change (profile read as
  a fourth condition, only when signed in).
- `app/test/support/render-with-providers.tsx` must mount the current-user provider with a complete profile by
  default; the exception is documented in design.md.

No business rule enters the front beyond the onboarding-pending rule, which mirrors the backoffice's and lives in one
place; form validation is structural and uses the shared core rules.

## Impact

- App routes: new `(onboarding)/_layout.tsx` and `(onboarding)/name.tsx`; root `_layout.tsx` guards and splash.
- App presentation: current-user state hook (common), onboarding area (step list, flow state hook, molecules,
  organism, view), `ProgressBar` atom, `useLogout` moved to `common/hooks`, `AppProviders` mounts the new provider.
- Translations: new `onboarding` namespace in `es` and `en`.
- Tests and the test helper in `app/test`.
- `.stpr/template/STPR_INVARIANTS.md`: splash bullet amended.
- Core, backoffice, API and `container.ts`: unchanged. No new dependency.
