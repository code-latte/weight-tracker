# Design

## Context

See `proposal.md` for motivation and `specs/app-navigation/spec.md` for the required behaviour.

Today `app/src/app/(app)/` holds `_layout.tsx` (a bare `Stack`, `headerShown: false`) and `index.tsx`, which
re-exports `HomeView`. The root layout guards `(app)` with `Stack.Protected` on the session state, so anything under
`(app)` is already hidden from a signed-out athlete. `STPR_INVARIANTS.md` 4.3 "Routing" already fixes where tabs go
(`(app)/(tabs)/_layout.tsx`, first tab at `(tabs)/index.tsx`) and what a nested layout may contain. No tabs layout
exists in the app yet, so this change establishes that reference.

Expo SDK 58 (`expo` `^58.0.17`, `expo-router` `~58.0.25`), React Native Paper `^5.15.3`. `@expo/vector-icons` does
not resolve from `app/`, and Paper's `Icon` renders Material Community Icons through it.

## Goals / Non-Goals

**Goals:**

- Establish the app's tabs layout as the reference for later tabs and tab content.
- Keep `/` as home so the access gate, the not-found link and logout keep working untouched.

**Non-Goals:**

- Tab folders with their own `Stack`, screens pushed over the tabs, headers, badges.
- Any change to core, `container.ts`, the session state, the root layout or `HomeView`.

## Decisions

### Mapping to core and fronts

- **Core:** none. No domain alias, port, repository or use case.
- **App container (`container.ts`):** no new key in `Dependencies`, no registration.
- **Backoffice:** none.
- **Routes and layouts (app):**

  ```
  app/src/app/(app)/
    _layout.tsx         AppLayout: Stack, headerShown false, <Stack.Screen name="(tabs)" /> first
    (tabs)/
      _layout.tsx       TabsLayout: Tabs, one Tabs.Screen per tab
      index.tsx         HomeView           -> /
      workouts.tsx      WorkoutsView       -> /workouts
      progress.tsx      ProgressView       -> /progress
      profile.tsx       ProfileView        -> /profile
  ```

  `(app)/index.tsx` is deleted (moved, not copied: two files resolving `/` would break the "one route file per URL"
  rule). Each route file is the two-statement re-export of its view.

- **Views (app), page level of atomic design:**
  - `app/src/presentation/workouts/views/workouts-view.tsx` -> `WorkoutsView`
  - `app/src/presentation/progress/views/progress-view.tsx` -> `ProgressView`
  - `app/src/presentation/profile/views/profile-view.tsx` -> `ProfileView`

  Each one is a `View` with a heading through the existing `Text` atom (`variant="headlineMedium"`, `role="heading"`,
  `aria-level={1}`) and `t("tabs.<tab>")` from the `common` namespace, styled with `StyleSheet` like
  `not-found-view.tsx`. No new atom, molecule or organism. Imports flow downward (view -> atom); views do not import
  Paper.

- **State hooks / providers:** none. The selected tab is navigator state owned by Expo Router; nothing resets it
  explicitly. On logout the guard unmounts `(app)` and the tabs with it.
- **Translations:** `app/src/i18n/{es,en}/common.json` get a `tabs` object: `home` "Inicio"/"Home", `workouts`
  "Entrenamientos"/"Workouts", `progress` "Progreso"/"Progress", `profile` "Perfil"/"Profile". Backoffice
  translations are not touched.
- **Error codes:** none involved. No mapper change.

### Single-file tabs, not a group folder per tab

The invariants use `(tabs)/index.tsx` for the first tab and a `<tab>/` folder only for a tab that pushes screens.
None does yet, so `workouts.tsx`, `progress.tsx` and `profile.tsx` are plain files. Planea's `(home)/`, `(agenda)/`
group folders were considered and rejected: they add a `_layout.tsx` per tab with nothing to stack, and they diverge
from the invariants. A tab that later needs pushed screens becomes `<tab>/_layout.tsx` + `<tab>/index.tsx` with the
same URL.

### `TabsLayout` contents

Following "Nested layouts and route files": one `Tabs` navigator, only `useTranslation("common")` for labels and
`useTheme()` for colours, Paper's `Icon` in `tabBarIcon`.

- `screenOptions`: `headerShown: false`, `tabBarActiveTintColor: theme.colors.primary`,
  `tabBarInactiveTintColor: theme.colors.onSurfaceVariant`, `tabBarStyle: { backgroundColor: theme.colors.surface }`.
- `Tabs.Screen` in order `index`, `workouts`, `progress`, `profile`, each with `title: t("tabs.<tab>")` and
  `tabBarIcon: ({ color, size }) => <Icon source="<name>" color={color} size={size} />`.
- Icons (Material Community Icons names): `home`, `dumbbell`, `chart-bar`, `account`.

Planea's iOS `position: "absolute"` tab bar and pathname-based hiding are not copied: the first overlaps content
without a blur view, the second is forbidden by the invariants.

Inicio is default because `index` is the first route of `(tabs)` and `(tabs)` the first screen of `(app)`; the gate
lands on `/` with no navigation call, as the invariants require. No `initialRouteName` is needed.

### `@expo/vector-icons` through `expo install`

Added with `pnpm exec expo install @expo/vector-icons` from `app/` so the version matches SDK 58. It is JS plus fonts
loaded through `expo-font`, its required peer, which `expo-doctor` asks to install directly
(`pnpm exec expo install expo-font`). `expo-font` is a native module that Expo Go bundles, so Expo Go needs no
development build; an existing development build must be rebuilt to include it. `expo install` also adds its
config plugin to `app/app.json` (`"expo-font"` in `plugins`). Alternative rejected: another icon
library, since the invariants allow Paper only and Paper's `Icon` already targets this package.

### Tests

- One component test per new view, modelled on `not-found-view.test.tsx`: render with `renderWithProviders`, assert
  the heading in `es`, `// Arrange & Act` / `// Assert` as that test does. These views use no use case, so the
  container needs no fakes; `expo-router` needs no mock since they use no `Link`.
- `home-view.test.tsx` stays as is.
- The translation parity test covers the new keys.
- No layout test: no app layout has one and the test patterns are not changed. The tab bar is checked manually in
  Expo Go. No E2E (app E2E is a known gap).

## Risks / Trade-offs

- [`@expo/vector-icons` version or font loading misbehaves on SDK 58] → install with `expo install`, run
  `pnpm dlx expo-doctor`, and check the icons render in Expo Go before closing.
- [Tab bar colours differ from React Navigation defaults in dark mode] → colours come from the Paper theme, so they
  follow it; checked manually in both schemes if the theme defines a dark variant.
- [Headerless tab screens sit under the status bar] → no change from today's home; content padding is left to each
  view and is out of scope here.
- [Typed routes still list `/(app)` paths for the removed `index.tsx`] → regenerated by `expo start`; `pnpm typecheck`
  catches stale hrefs (only `/` is used, which stays valid).
- [No automated test of the tab bar itself] → accepted, consistent with the app's current test tiers; the manual
  check in tasks covers order, icons, default tab and logout.
