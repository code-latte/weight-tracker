# Design

## Context

See `proposal.md` for motivation and `specs/` for the required behaviour.

Today `app/src/presentation/common/theme.ts` default-exports `MD3LightTheme`. `AppProviders` passes it to
`PaperProvider`, and `test/support/render-with-providers.tsx` imports the same default. Atoms are thin wrappers over
Paper widgets, so they follow whatever theme `PaperProvider` gets. The root layout (`app/src/app/_layout.tsx`) calls
`SplashScreen.preventAutoHideAsync()` at module scope and `SplashScreen.hide()` once the session status leaves
`restoring`, returning `null` until then. `app.json` sets `userInterfaceStyle: "light"`. `expo-font` (`~58.0.11`) is
already a direct dependency with its config plugin; no custom font is loaded.

Screens themselves have no background: views render a plain `View`, so the screen colour comes from Expo Router's
navigation theme (React Navigation's default, `rgb(242, 242, 242)`), not from Paper. `expo-router` 58 exports
`ThemeProvider` and `DefaultTheme`; Paper 5 exports `adaptNavigationTheme`.

## Goals / Non-Goals

**Goals:**

- Apply the brand theme exactly as given, including on screen backgrounds and navigator chrome.
- Load the brand fonts at runtime (Expo Go compatible) with no font jump and no hard failure.

**Non-Goals:**

- Dark mode wiring, a mode provider or state.
- Using `custom.*` tokens in existing atoms or views.

## Decisions

### Mapping to core and fronts

- **Core:** none. No alias, port, repository or use case.
- **App `container.ts`:** no new key in `Dependencies`, no registration.
- **Backoffice:** none. Its theme and translations stay independent; nothing is shared or imported across fronts.
- **Theme (`app/src/presentation/common/theme.ts`):** the brief's appendix, formatted with Prettier. It keeps
  `buildTheme`, `lightTheme`, `darkTheme`, `AppTheme`, `useAppTheme` and the default `lightTheme`, and nothing else.
  The `alpha` and `mix` helpers stay private. Colour literals stay in this
  file only.
- **Fonts (`app/src/presentation/common/fonts.ts`):** default-exports a map from the theme's family names to the
  `@expo-google-fonts` assets:

  | Theme name                  | Asset                          |
  | --------------------------- | ------------------------------ |
  | `Inter-Regular`             | `Inter_400Regular`             |
  | `Inter-Medium`              | `Inter_500Medium`              |
  | `Inter-SemiBold`            | `Inter_600SemiBold`            |
  | `Inter-Bold`                | `Inter_700Bold`                |
  | `PlusJakartaSans-Regular`   | `PlusJakartaSans_400Regular`   |
  | `PlusJakartaSans-SemiBold`  | `PlusJakartaSans_600SemiBold`  |
  | `PlusJakartaSans-Bold`      | `PlusJakartaSans_700Bold`      |
  | `PlusJakartaSans-ExtraBold` | `PlusJakartaSans_800ExtraBold` |

  It is data, not a component, so it sits beside `theme.ts` and has no atomic level.

- **Root layout:** `RootNavigator` calls `useFonts(fonts)` from `expo-font` and derives
  `fontsSettled = loaded || error !== null`. The splash effect hides when `status !== "restoring" && fontsSettled`, and
  the component returns `null` until both hold. The font error is not shown or stored: it only settles the wait. The
  gate (`Stack.Protected`) is unchanged.
- **`AppProviders`:** keeps `PaperProvider theme={theme}`, builds a module-level `navigationTheme` from the theme's
  colours and wraps its children in `ThemeProvider` from `expo-router` with it (see below). No other change.
- **State:** only the font loading tuple inside `useFonts` in the root layout, set once at launch, never reset. No
  provider holds theme or mode state.
- **Components:** no atom, molecule, organism or view is created or edited. Imports still flow downward and only atoms
  import Paper widgets; the tabs layout keeps its allowed `Icon`.
- **Translations:** none. **Error codes:** none (font loading is not an API interaction and returns no `Result`).

### Navigation theme from the Paper theme

Without it, every screen keeps React Navigation's grey background and the brand background `#F8FAFC` required by
`app-appearance` never shows. `AppProviders` builds `navigationTheme` by hand from `expo-router`'s `DefaultTheme`
(keeping its `fonts` and `dark`) with six colours from the Paper theme: `primary`, `background`, `card` (`surface`),
`text` (`onSurface`), `border` (`outline`) and `notification` (`error`), and provides it through `expo-router`'s
`ThemeProvider`. No colour literal is added.

Found during apply:

- Paper's `adaptNavigationTheme` does not accept `expo-router`'s vendored `DefaultTheme` type, so the object is built
  by hand with the same mappings Paper would use.
- It lives in `AppProviders`, not in `theme.ts`: importing `expo-router` from `theme.ts` broke every view test,
  because `render-with-providers.tsx` imports the theme and Jest cannot load an ES module `expo-router` depends on
  (`standard-navigation`). `AppProviders` is not imported by tests, so the test setup stays untouched (decided with
  the user).

Alternatives rejected: setting `backgroundColor` in every view (repeats itself in each screen and misses navigator
chrome), `contentStyle` in each layout (spreads theme reads across layouts), and adding `standard-navigation` to
Jest's `transformIgnorePatterns` (changes the test setup).

### Runtime `useFonts`, not the config plugin

`useFonts` works in Expo Go and keeps the change free of a development build. The `expo-font` config plugin embeds
fonts at build time and avoids the async wait, but requires a development build; it is out of scope. Font packages
are added with `pnpm exec expo install @expo-google-fonts/inter @expo-google-fonts/plus-jakarta-sans`.

`useFonts` is imported from `expo-font` (not from the `@expo-google-fonts` packages) so the font map can register the
theme's own names. `SplashScreen` stays the one from `expo-router`, as the invariant requires; `expo-splash-screen` is
not added.

### Invariant update

`.stpr/template/STPR_INVARIANTS.md` 4.3, splash bullet, becomes: the root layout keeps the native splash up while the
session is being restored or the fonts are loading, with `SplashScreen.preventAutoHideAsync()` at module scope and
`SplashScreen.hide()` once the session state is known and the fonts are loaded or have failed, both from
`expo-router`; there is no loading route. The theme bullet ("built from Paper's `MD3LightTheme`") still holds for the
applied theme and is left as is.

### Tests

- No test helper or pattern changes. `render-with-providers.tsx` keeps importing the default theme, now `lightTheme`,
  and keeps rendering without the navigation `ThemeProvider` (views do not read it).
- No new automated test: there is no root layout test in the app, and the theme is configuration. Existing view tests
  assert roles and text, not colours or fonts, so they must still pass unchanged.
- Visual behaviour (colours, fonts, no font jump, fallback) is checked manually in Expo Go.

## Risks / Trade-offs

- [The given theme's object shape fails `tsc` against `MD3Theme` (e.g. `PaperProvider`'s `theme` prop or `fonts`
  typing)] → fix types only (annotations, `as const` placement), never values; record any such edit in tasks.
- [Font names mismatch between `fonts.ts` and the theme] → `fonts.ts` uses the exact keys the theme declares; the
  manual check looks at a heading and a body text on device. A mismatch silently falls back to the system font.
- [Launch is slower by the font load] → eight local font assets load from the bundle; acceptable. The config plugin
  is the later fix if it is ever noticeable.
- [`expo-router`'s vendored React Navigation types diverge from Paper's] → resolved during apply with the hand-built
  theme in `AppProviders`, described above.
- [`custom.navigation.drawer*` tokens are dead in the app] → kept for parity with the backoffice, as the brief
  decides.
