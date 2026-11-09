# Design

## Context

See proposal.md (Why). Current state that shapes the approach:

- `RootNavigator` (`app/src/app/_layout.tsx`) guards `(app)` with `signedIn` and `(auth)` with `!signedIn`, and holds
  the splash until session, fonts and preferences are settled.
- The profile is read only by `useProfile` in the Perfil screen, without `runWithSession`. Nothing app-wide knows the
  profile.
- `GetMyProfileUseCase` and `UpdateMyProfileUseCase` are registered in `app/src/infrastructure/container.ts` and stubbed
  in `app/test/support/render-with-providers.tsx`. `validateProfileName` (core) gives `required` / `minLength` /
  `maxLength`.
- `useLogout` lives in `profile/hooks` and is used only by `ProfileView`.
- Invariant constraints: the root layout is the only place that decides access; a nested layout renders one navigator
  in at most one flow provider and uses only `useTranslation` / `useTheme`; state hooks are three files; navigation
  calls live in views and components, never in providers.

## Goals / Non-Goals

**Goals:**

- The onboarding-pending rule lives in one hook (`useCurrentUser`).
- Adding a step touches only the step list, one route, one view, the collected values and translations.
- No screen navigates to grant or deny access; the root guards decide.

**Non-Goals:**

- No core, backoffice, API or `container.ts` change; no new `Dependencies` key.
- No offline handling (own change), no reuse of `useCurrentUser` in `ProfileView` (it keeps `useProfile`).

## Decisions

### 1. Current-user state hook (common, app-wide)

Files: `app/src/presentation/common/context/current-user-context.ts`, `current-user-provider.tsx`,
`app/src/presentation/common/hooks/use-current-user.ts`. Mounted in `AppProviders` inside `SessionProvider` (it reads
the session).

Provider state:

| Field                 | Value                                                                              |
| --------------------- | ---------------------------------------------------------------------------------- |
| `status`              | `"idle"` (signed out) / `"loading"` / `"loaded"` / `"error"`                       |
| `profile`             | `UserProfile \| null`                                                              |
| `errorCode`           | `DomainErrorCode \| null`                                                          |
| `reloading`           | `true` while `reload` runs; the previous status stays until the new result arrives |
| `reload()`            | reads again through `runWithSession(getMyProfileUseCase)`                          |
| `setProfile(profile)` | stores the profile returned by a successful update (`status` -> `loaded`)          |

- When the session status becomes `signedIn`, it reads the profile through `session.runWithSession`, so an expired
  access token is refreshed and a rejected refresh signs out (spec "Session presence is enough").
- When the session becomes `signedOut`, it resets to `idle`.
- `reload` keeps the previous `status` while reading, so a retry from the error state never flips the guard to `(app)`
  for a moment.

`useCurrentUser` returns the context plus two derived values, the only place these rules live:

- `onboardingPending = status === "error" || (status === "loaded" && (name.trim() === "" || surname.trim() === ""))`;
- `settled = status === "loaded" || status === "error"`.

Alternative considered: reuse `useProfile`. Rejected: it is screen state (reads the trainer too, no session refresh)
and the gate needs app-wide state.

### 2. Root guards and splash

`RootNavigator`:

- launch `ready` adds `(status !== "signedIn" || currentUser.settled)` to the existing conditions (session, fonts,
  preferences);
- guards: `(auth)` -> `!signedIn || !settled`; `(onboarding)` -> `signedIn && settled && onboardingPending`; `(app)` ->
  `signedIn && settled && !onboardingPending`.

`(auth)` stays mounted while the first profile read after login or signup is in flight, so the athlete keeps seeing the
form (its button still loading) instead of a blank screen or a flash of the wrong group. At launch the splash covers
the same window. Exactly one group is open at any time, and every non-`(auth)` guard includes `signedIn`, so without a
session only `(auth)` renders.

Invariant amendment (`.stpr/template/STPR_INVARIANTS.md`, 4.3 "Root layout and auth gate", splash bullet): with a
stored session, the splash also waits for the current user's profile read to end (read or failed); a failure only ends
the wait, and the gate then shows the onboarding. The routing tree already names the sibling group for this case; no
other rule changes.

Found while implementing:

- Every guard carries `redirectTo` set to the entry of the open group (`/login`, the first step's `href`, or `/`). A
  closed group otherwise leaves its URL in place, and only `+not-found` matches it (the reason the `(app)` guard
  already had `redirectTo="/login"` since `add-logout`).
- The splash wait holds only the launch: `RootNavigator` latches a `launched` flag the first time everything is
  settled and never returns `null` again, so a profile read after login or signup keeps `(auth)` on screen instead of
  unmounting the navigator.

### 3. `(onboarding)` group and step list

- `app/src/app/(onboarding)/_layout.tsx` -> `OnboardingLayout`: `OnboardingProvider` around a `Stack` with
  `headerShown: false`; the first step's screen sets `gestureEnabled: false`. It uses no hook. (The brief planned
  the header through navigator options; see below for why it is screen content instead.)
- `app/src/app/(onboarding)/name.tsx` -> two statements, `NameStepView`.
- `app/src/presentation/onboarding/steps.ts`: ordered list, each entry `{ id, href, titleKey, validate(values) }`
  (`href` a typed absolute literal, e.g. `"/name"`). `validate` returns the errors of that step's fields only. The name
  step validates `name` and `surname` with `validateProfileName`.

The progress header is screen content (part of the step organism), not a navigator header: it needs the step position
and the logout handler, which a layout may not read. Back is a caret in that header calling `router.back()`, shown when
the step is not the first.

### 4. Onboarding flow state hook

Files: `app/src/presentation/onboarding/context/onboarding-context.ts`, `onboarding-provider.tsx`,
`app/src/presentation/onboarding/hooks/use-onboarding.ts`. Mounted in the group layout, so it resets when the group
unmounts (onboarding ended, logout).

- `values` (`{ name, surname }` now; later steps add fields), seeded once from `useCurrentUser().profile` when it is
  loaded (and again after a successful `reload` that brings the first profile).
- `errors` per field, `saving`, `errorCode`.
- `change(field, value)` clears that field's error.
- `validateStep(stepId)` runs the step's `validate`, stores the errors, returns whether it is valid. The view then
  pushes `steps[index + 1].href` (navigation stays in the view).
- `finish(stepId)` validates the step, then calls `runWithSession(() => updateMyProfileUseCase.execute(trimmed))`. On
  success it calls `currentUser.setProfile(result.value)`; the guard flips to `(app)`. On failure it stores
  `errorCode`; values stay.

### 5. Components (atomic levels)

- `ProgressBar` -> atom `app/src/presentation/common/components/atoms/progress-bar.tsx` (Paper `ProgressBar`).
- `OnboardingHeader` -> molecule `app/src/presentation/onboarding/components/molecules/onboarding-header.tsx`:
  `SafeArea` top edge, optional back `IconButton` (`chevron-left`, label `common:back`), `Text` "Paso n de total",
  `ProgressBar`, text `Button` "Cerrar sesión" with its `HelperText` error. Props only.
- `ProfileNameFields` -> molecule `app/src/presentation/onboarding/components/molecules/profile-name-fields.tsx`: two
  `TextInput` with `error` and `HelperText type="error"`, labels and messages by props.
- `OnboardingStepLayout` -> organism `app/src/presentation/onboarding/components/organisms/onboarding-step-layout.tsx`:
  header, title, `children`, submit error `HelperText`, primary `Button` ("Siguiente" / "Finalizar", `loading` and
  disabled while saving); when given an `unreadable` error it renders `ErrorState` with "Reintentar" instead of the
  body and the button. Inside `KeyboardAvoidingView` and `SafeArea` (left/right edges; the header covers the top).
- `NameStepView` -> view `app/src/presentation/onboarding/views/name-step-view.tsx`: reads `useOnboarding`,
  `useCurrentUser`, `useLogout`; translates; wires "Siguiente" / "Finalizar" / back / retry / logout.

Imports flow downward; only atoms import Paper widgets. `useLogout` moves to
`app/src/presentation/common/hooks/use-logout.ts` (second area), and `ProfileView` imports it from there.

### 6. Translations

New `onboarding` namespace (`app/src/i18n/{es,en}/onboarding.json`), registered in `resources.ts`: `progress`,
`next`, `finish`, `retry`, `steps.name.title`, `fields.name`, `fields.surname`, `fieldErrors.required`,
`fieldErrors.minLength`, `fieldErrors.maxLength` (values in the brief). Reused: `common:back`, `profile:logout`,
`errors:<code>`.

### 7. Errors

- Profile read: any code -> onboarding error state, `errors:<code>` + "Reintentar". `UNAUTHORIZED` never reaches it:
  `runWithSession` refreshes or signs out.
- Update: any code -> `errors:<code>` above the button.
- Logout: `UNEXPECTED` -> `errors:UNEXPECTED` under "Cerrar sesión".

No new code, no mapper change.

### 8. Tests and the helper exception

- `test/presentation/common/context/current-user-provider.test.tsx` (template: session provider test): idle when
  signed out; reads on sign-in; pending for empty name, empty surname, whitespace; pending on read failure; not pending
  when complete; `reload` keeps the error status until the result and ends pending on success; `setProfile` ends
  pending; reset to idle on sign-out; read goes through `runWithSession` (an `UNAUTHORIZED` then refreshed read
  succeeds).
- `test/presentation/onboarding/context/onboarding-provider.test.tsx` with a two-step fake list: `validateStep`
  validates only the current step; values kept; `finish` sends what every step collected, trimmed.
- `test/presentation/onboarding/views/name-step-view.test.tsx` (template: `signup-view.test.tsx`): "Paso 1 de 1",
  title, only "Finalizar", no back caret; pre-filled values; each field error and no update sent; trimmed update;
  button disabled while pending; success calls `setProfile` (observed as pending false through a probe); failure keeps
  values and shows the message; unreadable profile shows the error and "Reintentar" and no fields; retry reads again;
  "Cerrar sesión" calls the logout use case.
- `profile-view.test.tsx` keeps passing with the moved `useLogout`.

Helper exception: `render-with-providers.tsx` mounts `CurrentUserProvider` inside `SessionProvider`. Its default
`getSessionUseCase` returns no session, so the provider stays `idle` and existing view tests make no profile read; tests
that need a signed-in athlete pass a session and a profile. No other helper change.

The guards themselves are covered by the manual check in tasks (no root-layout test harness exists in the app).

## Risks / Trade-offs

- [A slow first profile read after login keeps the login form visible] -> the form already shows its button loading;
  acceptable versus a blank screen or a flash of the wrong group.
- [An athlete with a valid profile is locked in onboarding while the API is unreachable] -> intended by the request;
  "Reintentar" and "Cerrar sesión" are always offered. Offline handling is its own change.
- [Two reads of `GET /users/me` (current user and Perfil)] -> accepted for now; reusing `useCurrentUser` in Perfil is
  a later change.
- [Android hardware back on the first step backgrounds the app] -> standard behaviour at a navigator root; it never
  reaches `(app)`.

## Migration Plan

No data migration. Existing athletes with a complete profile see no change; those without one see onboarding on the
next launch. Rollback is a code revert.
