# Change Proposal: Add App Onboarding

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/add-app-onboarding/`.

## Change

Make a signed-in athlete with an incomplete profile, or whose profile cannot be read, go through a step-by-step
onboarding flow, as a stack of full-screen steps without the tab bar, before reaching the tabs. The first and only
step for now asks for name and surname. The flow is built so later steps are added by declaring them, not by
rewriting it.

## Objective

App signup asks only for email and password, so every new athlete reaches the tabs with an empty name and surname:
the profile screen shows an empty identity row and trainers see nameless athletes. The backoffice already solves
this with its onboarding dialog. The app needs the equivalent, in a mobile form (a stack of screens), and with room
for the steps that will follow (measurements, goals, ...).

## Capabilities

1. **Onboarding state.** A signed-in athlete is _in onboarding_ when their profile, as `GET /users/me` returns it,
   has an empty name or an empty surname (after trimming), **or when the profile cannot be read** (any failure). The
   athlete is never let into the tabs without a profile known to be complete. This rule lives in one place in the
   app, as it does in the backoffice (`use-current-user.ts`).
2. **Blocking flow, behind the session.** Onboarding is reachable only with a valid session, exactly like `(app)`:
   without one, `(auth)` is rendered (login), whatever the link. While in onboarding, the athlete sees only the
   onboarding screens: no tab bar, no tab screen, no deep link into `(app)`. When onboarding ends, the app moves to
   the Inicio tab by itself (the access gate follows the state; nothing navigates). Logging out or losing the session
   from onboarding shows login.
3. **One screen per step, in a stack.** Each step is its own screen. Every step screen shows, from top to bottom:
   - a progress header: "Paso <n> de <total>" and a progress bar;
   - the step title and its fields;
   - the actions at the bottom: "Siguiente" on every step but the last, "Finalizar" on the last one only.

   Going back to the previous step uses the stack's back (caret in the header, swipe or Android back) and is not
   offered on the first step, which cannot be left. Values typed in earlier steps are kept when going back and forth.

4. **Name step** (the only step now), titled "Dinos cómo te llamas": fields "Nombre" and "Apellidos", each validated
   with the shared core rules (`validateProfileName`: required, 3 to 100 characters after trimming), error under the
   field. Pre-filled with the current values when one of them is already set.
5. **Finalizar** validates the current step, then sends the trimmed values collected by every step in one profile
   update (`PUT /users/me`), with the button loading and disabled while pending. On success, the stored profile is
   updated, onboarding is no longer pending and the athlete lands on Inicio. On failure, the screen stays, keeps the
   values and shows the translated error.
6. **Profile not readable.** When the profile read fails, the onboarding flow shows, instead of the step, the
   translated error and a "Reintentar" button (the progress header and "Cerrar sesión" stay). "Reintentar" reads the
   profile again: complete -> Inicio; incomplete -> the first step, pre-filled; another failure -> the error stays.
   The athlete cannot leave this screen except by a successful retry or by logging out.
7. **Log out from onboarding.** The progress header has a "Cerrar sesión" text action, so an athlete who signed in
   with the wrong account, or cannot reach the API, is not trapped (the backoffice keeps logout available during
   onboarding too).
8. **Launch.** When a session is restored, the splash also waits for the profile read to end (loaded or failed), so
   an athlete in onboarding never sees the tabs flash first.

Future steps are added by: one entry in the ordered step list (id, route, title key, validation of its own fields),
one route file, one step view, its fields in the collected values and its keys in `onboarding`. The header, the
actions, the progress, the error state and the final save do not change.

## Fronts And Roles

- backoffice -> lands here: no -> it keeps its onboarding dialog. No change.
- app -> lands here: yes -> roles: `USER` -> an athlete with an empty name or surname, or an unreadable profile,
  completes onboarding before using the app.

No role difference (only `USER` signs in to the app). An athlete in onboarding must NOT see the tab bar, any tab screen
or any `(app)` screen. An athlete with a complete profile must NOT see any onboarding screen, even through a deep link.
A signed-out visitor must NOT see any onboarding or `(app)` screen: only `(auth)`.

## Classification

Core: none. `GetMyProfileUseCase`, `UpdateMyProfileUseCase` and `validateProfileName` exist; both use cases are already
registered in `app/src/infrastructure/container.ts`.

App:

- `CurrentUserProvider` / `useCurrentUser` (profile, status `loading` / `loaded` / `error`, error code, `reload`,
  `setProfile`; `onboardingPending` derived in the hook, the only place the rule lives) -> state hook (app-wide) ->
  `app/src/presentation/common/context/current-user-context.ts`, `current-user-provider.tsx`,
  `app/src/presentation/common/hooks/use-current-user.ts`; mounted in `AppProviders` inside `SessionProvider`
- Root layout (splash also waits for the profile read when signed in; one `Stack.Protected` per access state:
  `(auth)` guard `!signedIn`; `(onboarding)` guard `signedIn && onboardingPending`; `(app)` guard
  `signedIn && !onboardingPending`) -> layout -> `app/src/app/_layout.tsx`
- Onboarding group layout (mounts `OnboardingProvider` around a `Stack`; header through options) -> layout ->
  `app/src/app/(onboarding)/_layout.tsx`, a sibling of `(app)` and `(auth)`
- Name step route -> route -> `app/src/app/(onboarding)/name.tsx` (two statements)
- Step list (ordered: id, route, title key, `validate(values)`) -> presentation config ->
  `app/src/presentation/onboarding/steps.ts`
- `OnboardingProvider` / `useOnboarding` (collected values seeded from the profile, per-step errors, `saving`, last
  error code, `change`, `validateStep(stepId)`, `finish()` validating and saving) -> flow state hook ->
  `app/src/presentation/onboarding/context/onboarding-context.ts`, `onboarding-provider.tsx`,
  `app/src/presentation/onboarding/hooks/use-onboarding.ts`. Navigation (`router.push` of the next step's route) stays
  in the views, per the invariants; the hook only validates and saves.
- `ProgressBar` (Paper `ProgressBar`, progress by props) -> atom ->
  `app/src/presentation/common/components/atoms/progress-bar.tsx`
- `OnboardingHeader` (top safe area, "Paso n de total", `ProgressBar`, back caret when not first, "Cerrar sesión"
  text button) -> molecule -> `app/src/presentation/onboarding/components/molecules/onboarding-header.tsx`
- `ProfileNameFields` (two `TextInput` + `HelperText`, values, errors and labels by props) -> molecule ->
  `app/src/presentation/onboarding/components/molecules/profile-name-fields.tsx`
- `OnboardingStepLayout` (step title, body slot, error helper, "Siguiente" / "Finalizar" button by `isLast`; shows the
  existing `ErrorState` molecule with "Reintentar" instead of the body while the profile is unreadable) -> organism ->
  `app/src/presentation/onboarding/components/organisms/onboarding-step-layout.tsx`
- `NameStepView` (step layout + `ProfileNameFields`) -> view ->
  `app/src/presentation/onboarding/views/name-step-view.tsx`
- Logout from the header -> reuses `useLogout` (`app/src/presentation/profile/hooks/use-logout.ts`); it moves to
  `common/hooks` because a second area now uses it
- `onboarding.*` keys -> i18n -> new `onboarding` namespace in `app/src/i18n/{es,en}/onboarding.json`, registered in
  `app/src/i18n/resources.ts`

Reused atoms: `SafeArea`, `Text`, `TextInput`, `HelperText`, `Button`, `IconButton`. Reused molecules: `ErrorState`.

## Canonical Template Mapping

- `CurrentUserProvider` -> `app/src/presentation/auth/context/session-provider.tsx` (app provider shape) and, as a
  value reference only, `backoffice/src/presentation/common/components/current-user-provider.tsx` and
  `use-current-user.ts` (status, derived `onboardingPending`, rule in one place).
- Root guards -> `STPR_INVARIANTS.md` 4.3 "Root layout and auth gate" (`Stack.Protected` per access state, no
  `Redirect`, splash hold) and the routing tree ("a state a signed-in user must leave before reaching `(app)`
  (completing a profile, for example) is its own sibling group with its own guard").
- Flow group with provider -> routing tree `(<flow>)/_layout.tsx` "mounts the flow's provider around a Stack" -> no
  flow exists yet in the app: **this change establishes it**.
- `useOnboarding` and the step list -> `backoffice/src/presentation/onboarding/hooks/use-onboarding.ts` as a value
  reference only (ordered steps, per-step `validate`, last step saves everything); adapted to one screen per step.
- Atoms -> `app/src/presentation/common/components/atoms/icon-button.tsx`.
- Molecules -> `app/src/presentation/common/components/molecules/section-header.tsx` (header with safe area) and
  `app/src/presentation/auth/components/organisms/signup-form.tsx` (fields with `HelperText`).
- Organism and view -> `app/src/presentation/auth/components/organisms/signup-form.tsx` and
  `app/src/presentation/auth/views/signup-view.tsx` (form view, `KeyboardAvoidingView`, submit button loading);
  error with retry -> `app/src/presentation/profile/views/profile-view.tsx`.
- Tests -> `app/test/presentation/auth/views/signup-view.test.tsx` (form view),
  `app/test/presentation/auth/context/session-provider.test.tsx` (provider).

The backoffice is not a UI reference.

## Screens And Navigation

- app -> `/name` -> `NameStepView` in the `(onboarding)` stack -> the gate shows it when a signed-in athlete is in
  onboarding (after login, after signup, at launch) -> Finalizar success: the profile is updated, the guard flips and
  the app shows Inicio; failure: stays with the error. "Cerrar sesión": the session ends and the gate shows login.
- Profile read fails -> the gate shows `/name` with the error and "Reintentar" -> success with a complete profile:
  Inicio; success with an incomplete one: the name step, pre-filled.
- Deep link to any `(app)` route while in onboarding -> the guard sends the athlete to the first onboarding step.
- Deep link to `/name` with a complete profile -> the guard sends the athlete to Inicio.
- Deep link to `/name` (or any onboarding or `(app)` route) without a session -> login.
- Future steps: "Siguiente" pushes the next step's route from the step list; back pops to the previous step.

## API Contract

- `GET /users/me` -> in `core/openapi/v1.json`: yes -> `UserProfileResponse` (`id`, `email`, `name`, `surname`,
  `role`).
- `PUT /users/me` (`UserRepository.updateMe`) -> in `core/openapi/v1.json`: yes -> `UpdateProfileRequest`
  (`name`, `surname`, both required) -> `UserProfileResponse`.

No snapshot refresh.

## Held State

- `CurrentUserProvider` -> profile, status, last error code -> loaded when the session becomes signed in, replaced by
  `setProfile` after a successful onboarding update, reloaded by `reload` ("Reintentar") -> cleared when the session
  becomes signed out (logout or failed refresh).
- `OnboardingProvider` -> collected values (`name`, `surname`, seeded from the profile when it is read), per-step
  errors, `saving`, last error code -> changed by the step views -> reset when the flow unmounts (onboarding
  completed, logout), since it is mounted in the `(onboarding)` group layout.
- `useProfile` in the profile screen is unchanged (it still reads its own profile; reusing `useCurrentUser` there is a
  later change).

## Forms And Structural Validation

- Name step -> `name` -> required, 3 to 100 characters after trimming (`validateProfileName`, mirroring the API).
- Name step -> `surname` -> same.

Validation runs on "Siguiente" and "Finalizar" for the current step's fields only; errors clear as the field changes.

## Errors And Copy

- Profile read fails (at launch, after login, or on "Reintentar") -> any `DomainError` code (`NETWORK`, `UNEXPECTED`,
  ...) -> the onboarding error state with `errors:<code>` and "Reintentar". `UNAUTHORIZED` is handled by the session
  (refresh, else login).
- Profile update fails -> `NETWORK`, `UNEXPECTED`, `VALIDATION`, ... -> `errors:<code>` above the button; values kept.
- Logout fails -> `UNEXPECTED` -> `errors:UNEXPECTED` under the header action, as on the profile screen.

No new error code.

New `onboarding` namespace (Spanish from the backoffice where it exists; English proposed):

- `progress` -> "Paso {{current}} de {{total}}" -> "Step {{current}} of {{total}}"
- `next` -> "Siguiente" -> "Next"
- `finish` -> "Finalizar" -> "Finish"
- `retry` -> "Reintentar" -> "Retry"
- `back` -> reuse `common:back` ("Volver" / "Back")
- `logout` -> reuse `profile:logout` ("Cerrar sesión" / "Log out")
- `steps.name.title` -> "Dinos cómo te llamas" -> "Tell us your name"
- `fields.name` -> "Nombre" -> "Name"
- `fields.surname` -> "Apellidos" -> "Surname"
- `fieldErrors.required` -> "Este campo es obligatorio." -> "This field is required."
- `fieldErrors.minLength` -> "Debe tener al menos 3 caracteres." -> "Must be at least 3 characters."
- `fieldErrors.maxLength` -> "No puede superar los 100 caracteres." -> "Cannot exceed 100 characters."

## Platform Capabilities And Dependencies

None. Paper's `ProgressBar` is part of `react-native-paper`; no development build needed.

## Out Of Scope

- Any step other than the name step.
- Any offline handling beyond locking in onboarding: no offline banner, no connectivity detection, no letting the
  athlete into the tabs when the profile cannot be read. That is its own change.
- Reusing `useCurrentUser` in the profile screen, or editing the profile from Perfil.
- Skipping onboarding, or saving after each step (all values are saved once, on Finalizar).
- Must not change: core, the API, the backoffice, the tabs and their screens, login and signup screens, the settings
  and preferences of `add-app-settings-screen`.

## Notes

- **Decided: where the flow lives.** `app/src/app/(onboarding)/`, a sibling of `(app)` and `(auth)`, guarded in the
  root layout, as the invariants describe. It requires a valid session exactly like `(app)`: both guards include
  `signedIn`, so without a session only `(auth)` renders.
- **Decided: an unreadable profile locks the athlete in onboarding.** `onboardingPending` is true while signed in and
  the profile status is `error`, so the tabs are reachable only with a profile known to be complete. The flow shows
  the error with "Reintentar" rather than an empty name step, so an athlete who already has a name is not asked to
  type it again just because the read failed.
- **Splash.** Extends the root-layout wait of the invariants with a fourth condition (profile read ended, only when
  signed in). Like `add-app-settings-screen`, the splash bullet of `STPR_INVARIANTS.md` is amended in this change.
- **`useLogout` moves** from `profile/hooks` to `common/hooks` (second area that uses it), with its test.
- **Specs.** New `app-onboarding` capability. `app-access-gate` changes (third access state; onboarding behind the
  session). `app-navigation` changes: the tabs need a profile known to be complete.
- **Test helper.** `render-with-providers.tsx` mounts `CurrentUserProvider` (with `getMyProfileUseCase` defaulting to
  a complete profile) so existing view tests do not see onboarding; documented exception.
- **Verification tiers:**
  - core unit: none.
  - app component: `current-user-provider.test.tsx` (loads on sign-in; pending for empty name, empty surname,
    whitespace only; pending on any read failure; not pending when complete; `reload` success with a complete
    profile ends pending; cleared on sign-out).
  - app component: `name-step-view.test.tsx` (progress "Paso 1 de 1"; only "Finalizar", no back caret on the first
    step; pre-filled values; each validation error per field and no update sent; trimmed values sent; button disabled
    while pending; success updates the current user; failure keeps values and shows the message; unreadable profile
    shows the error and "Reintentar", and no fields; "Reintentar" reads the profile again; "Cerrar sesión" calls the
    logout use case).
  - app component: an onboarding-provider test with a two-step fake list (Siguiente validates only the current step;
    values kept when going back; Finalizar sends what every step collected), so the multi-step path is covered before
    a second real step exists.
  - app: manual check in Expo Go: signup -> name step -> Inicio; launch with an incomplete profile -> name step
    without a tabs flash; deep link to `/workouts` while in onboarding -> name step; deep link to `/name` signed out
    -> login; launch with the API stopped -> onboarding error with "Reintentar", no tabs; start the API and retry ->
    Inicio (complete profile) or the name step (incomplete).
  - No backoffice tests, no E2E (app E2E is a known gap).
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.
