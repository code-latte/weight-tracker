# Proposal

Source brief: `.stpr/plan/openspec/proposal-change-set-app-theme.md` (its appendix holds the theme source).

## Why

The app renders with Paper's stock purple `MD3LightTheme` and system fonts, so it looks unrelated to the backoffice.
The athlete should see the same brand as the trainer, and every later screen should be built on the final theme
instead of being restyled.

## What Changes

- `app/src/presentation/common/theme.ts` is replaced by the brand theme in the brief's appendix: the React Native
  Paper equivalent of the backoffice MUI theme (teal primary, orange secondary, slate surfaces, flat elevation
  surfaces, MD3 typography scale on Plus Jakarta Sans and Inter, `roundness: 4`, and `custom.*` tokens). Values and
  structure are kept as given; only repo formatting and `tsc` strictness may adjust it.
- It exports `buildTheme(mode)`, `lightTheme`, `darkTheme`, `AppTheme`, `useAppTheme()` and `lightTheme` as default.
  The app applies `lightTheme`; `darkTheme` is built but not applied.
- `AppProviders` provides Expo Router with a navigation theme built from the Paper theme's colours, so screen
  backgrounds and navigator chrome use the brand colours instead of React Navigation's default grey.
- The Inter and Plus Jakarta Sans fonts are loaded at launch under the names the theme uses, and the native splash
  stays up until the session is known and the fonts are loaded or have failed. On failure the app renders with the
  system font.
- `STPR_INVARIANTS.md` 4.3 ("Root layout and auth gate") is updated: the splash hides once the session state is known
  and the fonts are loaded or have failed.
- New dependencies in `app`: `@expo-google-fonts/inter`, `@expo-google-fonts/plus-jakarta-sans` (JS + font assets,
  loaded at runtime; Expo Go works, no development build).

Fronts and roles:

- **app** -> `USER` -> every screen (login, signup, tabs, not-found) renders with the brand theme, signed in or not.
  No role difference; nothing is hidden from anyone.
- **backoffice** -> not affected. Its MUI theme is the value reference and does not change.

Components (atomic design): no atom, molecule or organism is added or changed. The existing atoms (`button`, `text`,
`text-input`, `helper-text` in `app/src/presentation/common/components/atoms/`) already read the theme through Paper
and pick it up. The tabs layout reads `primary`, `onSurfaceVariant` and `surface` and also picks it up.

Canonical references:

- Theme -> the current `app/src/presentation/common/theme.ts` and `STPR_INVARIANTS.md` 4.3 ("The theme is the single
  `presentation/common/theme.ts`, built from Paper's `MD3LightTheme`"). The applied light theme is still built from
  `MD3LightTheme`. `backoffice/src/presentation/common/theme.ts` is a value reference only, not a UI reference.
- Splash hold and font loading -> the current `app/src/app/_layout.tsx` (`SplashScreen` from `expo-router` around the
  session). Font loading has no reference: **this change establishes it**.
- Tests -> existing app view tests through `app/test/support/render-with-providers.tsx`, which imports the default
  theme.

API: none. No endpoint, no snapshot refresh. No business rule, no form, no new error code, no new copy.

Conflicts with the template, the invariants or a stop case: the font condition on the splash goes beyond the current
invariant text; resolved by updating the invariant in this change (decided with the user). No other deviation.

## Capabilities

### New Capabilities

- `app-appearance`: how the app looks across screens: the brand colours, the typography and its fonts, the light
  mode, and how launch waits for the fonts.

### Modified Capabilities

- `app-access-gate`: its requirement "Nothing shows until the session is restored" now also waits for the fonts
  before any screen renders.

## Impact

- `app/src/presentation/common/theme.ts`: replaced.
- `app/src/presentation/common/fonts.ts`: new font map.
- `app/src/app/_layout.tsx`: loads the fonts and extends the splash condition.
- `app/package.json`, `pnpm-lock.yaml`: the two font packages.
- `.stpr/template/STPR_INVARIANTS.md`: the splash bullet of 4.3.
- `app/src/presentation/common/components/app-providers.tsx`: wraps its children in Expo Router's `ThemeProvider`
  with the navigation theme.
- `app/test/support/render-with-providers.tsx`: no change (it keeps importing the default theme).
- Not affected: core, the API, the snapshot, the backoffice, routes, copy, atoms, views.

Out of scope: dark mode in the app (toggle, stored preference or system scheme; `userInterfaceStyle` stays
`"light"`), adopting `custom.*` tokens in components, embedding fonts with the `expo-font` config plugin.

Acceptance: brand colours and fonts on every screen with no purple left; no font jump after the splash; the app
still starts if fonts fail; `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.
