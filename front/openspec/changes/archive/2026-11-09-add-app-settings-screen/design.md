# Design

## Context

See proposal.md (Why). Current state that shapes the approach:

- Every app view pads 24 points and none reads the safe area. Expo Router already mounts a `SafeAreaProvider`, and
  `react-native-safe-area-context` is an `app` dependency.
- The Perfil tab is a single route, `(tabs)/profile.tsx`. No tab has a stack yet.
- `AppProviders` passes the fixed `lightTheme` to `PaperProvider` and derives the navigation theme from it once, at
  module scope. The root layout renders `<StatusBar style="auto" />`, which follows the device, not the app.
- `theme.ts` already exports `buildTheme(mode)`, `lightTheme` and `darkTheme` (dark built from `MD3DarkTheme`).
- `i18n.ts` initialises with `detectLanguage()` (first supported device language, else `es`).
- Core has `Get/SetPreferredThemeModeUseCase` (key `preferences.themeMode`) and `Get/SetPreferredLanguageUseCase`, all
  registered in `app/src/infrastructure/container.ts` and stubbed in `app/test/support/render-with-providers.tsx`
  (getters return `ok(null)`). They go through `keyValueStore`, the SQLite adapter that also holds the tokens.
- Invariant constraints: only `app-providers.tsx` imports `src/infrastructure` (container and i18n); a state hook is
  three files (`context/<name>-context.ts`, `context/<name>-provider.tsx`, `hooks/use-<name>.ts`); composition
  providers that wrap `PaperProvider` sit at the root of `components/`; nested layouts use only `useTranslation` and
  `useTheme`, and headers are presentation components passed through options.

## Goals / Non-Goals

**Goals:**

- One place applies the safe area (`SafeArea` atom); views do not read insets.
- The tab-stack and section-header pattern is established once, so later section screens only add a route, a view
  and a `Stack.Screen`.
- Theme and language are resolved once, at the top of the tree, and the splash covers the read.

**Non-Goals:**

- No core, backoffice or API change. No new `Dependencies` key, so no `container.ts` change in either front.
- No storage change (SQLite stays, unencrypted, as for the tokens).
- No other section screen and no wiring of the other menu items.

## Decisions

### 1. Core and containers: reuse, nothing new

The four preference use cases already exist and are registered in both fronts. The app resolves them through the
container: `getPreferredThemeModeUseCase`, `setPreferredThemeModeUseCase`, `getPreferredLanguageUseCase`,
`setPreferredLanguageUseCase`. No new domain alias, port, repository, use case or `Dependencies` key.

### 2. Preferences state hook (common area)

Three files, following the invariant and the shape of `auth/context/session-provider.tsx`:

- `app/src/presentation/common/context/preferences-context.ts` -> `createContext`.
- `app/src/presentation/common/context/preferences-provider.tsx` -> `PreferencesProvider`.
- `app/src/presentation/common/hooks/use-preferences.ts` -> `usePreferences` (throws outside the provider, as the
  wiring guard pattern of `useContainer`/`useSession` does).

Holds:

| Field                 | Value                                                             |
| --------------------- | ----------------------------------------------------------------- |
| `storedThemeMode`     | `"light"` / `"dark"` / `null`                                     |
| `themeMode` (derived) | `storedThemeMode ?? useColorScheme() ?? "light"`                  |
| `language` (derived)  | `i18n.resolvedLanguage` (`"es"` / `"en"`)                         |
| `ready`               | `false` until both reads settle (success or failure)              |
| `setThemeMode(mode)`  | sets `storedThemeMode`, then `setPreferredThemeModeUseCase(mode)` |
| `setLanguage(lang)`   | `i18n.changeLanguage(lang)`, then `setPreferredLanguageUseCase`   |

- On mount it runs both getters in parallel. A stored value is kept only if it is in the known list
  (`themeModes`, `supportedLanguages` mirrored in presentation as a constant list `["es", "en"]`); anything else, or a
  failed read, means "nothing stored". A stored language is applied with `i18n.changeLanguage` before `ready` turns
  true.
- With nothing stored it never calls `changeLanguage`: the i18n instance already started with `detectLanguage()`. This
  keeps the presentation free of `src/infrastructure` and keeps tests in `es`.
- `i18n` is reached through `useTranslation()` (the `I18nextProvider` sits above), not by import.
- A failed write is ignored, as in the backoffice `theme-mode-provider.tsx`: the choice already applied; the next
  launch starts from what was stored before.
- Resets: never during the app's lifetime. It is mounted in `AppProviders`, above `SessionProvider`, so logout does
  not clear it. Stored values are never removed by this change.

Why a state hook and not `useState` in the view: the root layout (splash), the theme composition and `SettingsView`
all read it.

Alternative considered: one provider that also wraps `PaperProvider` (as the backoffice does). Rejected because the
invariants place state-hook providers in `context/` and theme composition at the root of `components/`; splitting
keeps each in its folder.

### 3. Theme composition

New composition provider `app/src/presentation/common/components/app-theme-provider.tsx` (`AppThemeProvider`, root of
`components/`, not an atomic level). It reads `usePreferences().themeMode` and renders:

- `PaperProvider` with `lightTheme` or `darkTheme`;
- Expo Router's `ThemeProvider` with the navigation theme derived from the active Paper theme (`DarkTheme` base for
  dark), moved here from `AppProviders` module scope and memoised by mode;
- `<StatusBar style={themeMode === "dark" ? "light" : "dark"} />` from `expo-status-bar`, replacing the one in the
  root layout.

`AppProviders` order becomes: `I18nextProvider` > `ContainerProvider` > `PreferencesProvider` > `AppThemeProvider` >
`SessionProvider` > children. `PreferencesProvider` needs the container and i18n above it.

`app.json` `userInterfaceStyle` changes from `"light"` to `"automatic"` so `useColorScheme()` reports the device
appearance. Works in Expo Go; no development build.

### 4. Splash hold (invariant amendment)

`app/src/app/_layout.tsx` `RootNavigator` adds `usePreferences().ready` to its `ready` condition:
`status !== "restoring" && (fontsLoaded || fontsError !== null) && preferencesReady`. A failed read sets `ready` too,
so it only ends the wait. The `StatusBar` leaves the root layout (now in `AppThemeProvider`).

`.stpr/template/STPR_INVARIANTS.md` amendments (the template has no copied code files, so no drift check is affected):

- Section 4.3, "Root layout and auth gate", splash bullet: the root layout keeps the splash until the session state is
  known, the fonts are loaded or have failed, **and the device preferences (colour mode, language) are read or have
  failed**; a failure only ends the wait.
- "Internationalisation": the initial language is the one stored on the device when there is one; otherwise it is
  detected from the device or browser, falling back to `es`.
- Section 4.3, theme bullet: the theme is the single `presentation/common/theme.ts`, built with `buildTheme` from
  Paper's `MD3LightTheme` and `MD3DarkTheme`; the active mode is the stored one, otherwise the device's.

### 5. Safe area

New atom `app/src/presentation/common/components/atoms/safe-area.tsx` (`SafeArea`): wraps `SafeAreaView` from
`react-native-safe-area-context` with `style={{ flex: 1, backgroundColor: theme.colors.background }}` and `edges` by
props (default `["top", "left", "right"]`; the bottom edge stays with the tab bar or the home indicator). It is the
only file importing `SafeAreaView`, since two files use it (views and `SectionHeader`).

Each view (`login`, `signup`, `home`, `workouts`, `progress`, `not-found`, `profile`, `settings`) wraps its current root
(`View`, `ScrollView`, `KeyboardAvoidingView`) in `SafeArea`; its 24-point padding stays in its own `StyleSheet`.
Section screens use `SafeArea` with `edges={["left", "right"]}` because the header already covers the top inset.

Alternative considered: a `Screen` atom that also owns the padding and scrolling. Rejected: it would mix layout
decisions of different views into one atom and grow props for scroll, keyboard and gap.

### 6. Profile tab stack and section header

Routes:

- `app/src/app/(app)/(tabs)/profile/_layout.tsx` -> `ProfileLayout`: a `Stack`. `index` with `headerShown: false`;
  `settings` with `title: t("settings:title")` and
  `header: ({ navigation, options }) => <SectionHeader title={options.title ?? ""} backLabel={t("common:back")} onBack={navigation.goBack} />`.
  Only `useTranslation` is used. Content background comes from the navigation theme.
- `app/src/app/(app)/(tabs)/profile/index.tsx` -> two statements, `ProfileView` (moved from `(tabs)/profile.tsx`, which
  is deleted). The `profile` tab name in `(tabs)/_layout.tsx` still matches the folder, so that file is unchanged.
- `app/src/app/(app)/(tabs)/profile/settings.tsx` -> two statements, `SettingsView`.

`SectionHeader` -> molecule in `app/src/presentation/common/components/molecules/section-header.tsx` (common because
every later section screen and potentially other tabs reuse it): `SafeArea edges={["top"]}` with the surface
background, a row with `IconButton` (`chevron-left`, `accessibilityLabel={backLabel}`, `onPress={onBack}`) and `Text`
(`titleLarge`, `role="heading"`). Props only, no hooks beyond the atoms'.

Navigation from the cog: `ProfileView` calls `router.push("/profile/settings")` (typed absolute href) from
`expo-router`'s `useRouter`. Re-tapping the Perfil tab pops to the stack root (default tab behaviour).

### 7. Settings view and its components

- `SegmentedButtons` -> atom `app/src/presentation/common/components/atoms/segmented-buttons.tsx`: Paper
  `SegmentedButtons`, `value`, `onValueChange`, `buttons` (`value`, `label`) and `accessibilityLabel` by props.
- `PreferenceSelector` -> molecule `app/src/presentation/profile/components/molecules/preference-selector.tsx`:
  `Text` label (`titleMedium`) above a `SegmentedButtons`; props `label`, `value`, `options`, `onChange`.
- `SettingsView` -> view `app/src/presentation/profile/views/settings-view.tsx`: `SafeArea` (no top edge) around a
  `ScrollView` with the 24-point padding; two `PreferenceSelector`s fed by `usePreferences()`, labels from the
  `settings` namespace. No loading state: the splash guarantees `ready`.

Imports flow downward (view -> molecule -> atom); only atoms import Paper widgets and `SafeAreaView`. No organism.

### 8. Translations

New `settings` namespace, `app/src/i18n/{es,en}/settings.json`, registered in `app/src/i18n/resources.ts`:

| Key              | es      | en       |
| ---------------- | ------- | -------- |
| `title`          | Ajustes | Settings |
| `theme.label`    | Tema    | Theme    |
| `theme.light`    | Claro   | Light    |
| `theme.dark`     | Oscuro  | Dark     |
| `language.label` | Idioma  | Language |
| `language.es`    | Español | Español  |
| `language.en`    | English | English  |

`back` goes to `common` (`Volver` / `Back`), not `settings` as the brief said, because `SectionHeader` serves every
section screen. The parity test covers both.

### 9. Errors

Only `UNEXPECTED` can occur (preference read or write failure). Neither is shown to the user (spec: "A storage failure
is silent"). No new error code, no mapper change, no new `errors.*` key.

### 10. Tests and the helper exception

Tests live in `app/test`, mirroring `src`, Arrange / Act / Assert, rendered in `es`:

- `test/presentation/common/context/preferences-provider.test.tsx` (template: `test/presentation/auth/context/`):
  stored values win over the device; unknown stored value ignored; read failure -> device values and `ready`; `ready`
  only after both reads; `setThemeMode` / `setLanguage` call the set use cases; write failure still applies.
- `test/presentation/profile/views/settings-view.test.tsx` (template: `profile-view.test.tsx`): both settings with the
  current values selected; nothing stored with a mocked dark `useColorScheme` -> "Oscuro" selected; choosing "Oscuro"
  calls the set use case with `"dark"`; choosing "English" calls it with `"en"` and the view re-renders in English
  (the test restores `es` in its own setup, staying deterministic).
- `test/presentation/profile/views/profile-view.test.tsx`: tapping "Ajustes" pushes `/profile/settings` (mocked
  `expo-router` router).
- `test/presentation/common/components/molecules/section-header.test.tsx`: title shown, caret label, `onBack` called.
- Existing view tests keep passing with the `SafeArea` wrapper.

Helper exception (documented here as the rules require): `app/test/support/render-with-providers.tsx` gains a
`SafeAreaProvider` with fixed `initialMetrics` (so `SafeAreaView` renders under Jest) and a `PreferencesProvider`
between `ContainerProvider` and `SessionProvider`. Its `PaperProvider` keeps the fixed light theme; theme switching is
covered in the provider test, not through the helper. No other test pattern changes.

Manual check in Expo Go on a notched iPhone: safe area on every screen, Ajustes from the cog and back, live theme and
language changes, persistence across restart, device appearance followed when nothing is stored.

## Risks / Trade-offs

- [The provider's async reads update state after the first render in tests, producing `act` warnings] -> view tests
  wait with `findBy*`; the provider test awaits `ready`.
- [Switching language re-renders the whole tree, including the tab bar] -> intended; i18next notifies only subscribed
  components, and the change is user-triggered and rare.
- [`useColorScheme` returns `null` on some platforms] -> fallback to `"light"`.
- [Removing "The app uses light mode" changes the look for current users with a dark device] -> accepted by the
  request (system default); dark tokens already exist in `theme.ts` and are covered by the spec.
- [Preferences are not encrypted] -> accepted decision, same as the tokens; nothing sensitive is stored.

## Migration Plan

No data migration: nothing is stored until the athlete chooses. Rollback is a code revert; any stored
`preferences.*` keys are then simply unread.
