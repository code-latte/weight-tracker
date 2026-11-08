# Change Proposal: Set App Theme

Source brief for `/opsx:propose`. Artifacts go to `openspec/changes/set-app-theme/`.

## Change

Replace the app's stock `MD3LightTheme` with the Trainera brand theme for React Native Paper: the equivalent of the
backoffice MUI theme (colours, typography, shape and custom tokens), with the Inter and Plus Jakarta Sans fonts
loaded in the app.

## Objective

The app renders with Paper's default purple palette and system fonts, so it looks unrelated to the backoffice. The
athlete should see the same brand as the trainer: teal primary, orange secondary, slate surfaces, Plus Jakarta Sans
headings and Inter body text. Doing it now, with only a few screens, means every later screen is built on the final
theme instead of being restyled.

## Capabilities

- The app uses the brand theme (appendix) as its Paper theme: brand colours on every Paper widget and on the tab bar,
  no purple-tinted elevation surfaces.
- Typography uses Plus Jakarta Sans for display, headline and `titleLarge` variants and Inter for the rest, with the
  sizes, line heights and letter spacing of the appendix.
- The fonts are loaded at launch; the splash screen stays up until they are ready, so no screen renders first in the
  system font and then jumps.
- The theme exposes custom tokens (`custom.*`: spacing, radius, semantic, exercise, mood, chart, gradients, ...) and a
  typed `useAppTheme()` hook for components that need them.
- The theme is built for both modes (`buildTheme("light" | "dark")`), but the app applies the light theme only.

## Fronts And Roles

- backoffice -> lands here: no -> its MUI theme is the source of the values and does not change.
- app -> lands here: yes -> roles: `USER` -> every screen (login, signup, tabs, not-found) renders with the brand theme.

No role difference. A signed-out athlete sees the theme on login and signup as well.

## Classification

App:

- Brand theme (`buildTheme`, `lightTheme`, `darkTheme`, `AppTheme`, `useAppTheme`, default `lightTheme`) -> theme ->
  `app/src/presentation/common/theme.ts` (replaces its content)
- Font map (font family name from the theme -> `@expo-google-fonts` asset) -> theme support ->
  `app/src/presentation/common/fonts.ts`
- `AppProviders` (passes `lightTheme` to `PaperProvider`; the default import keeps working) -> provider composition ->
  `app/src/presentation/common/components/app-providers.tsx`, likely unchanged
- Root layout (loads the fonts with `useFonts` from `expo-font` and holds the splash until both the session and the
  fonts are known) -> layout -> `app/src/app/_layout.tsx`
- `app/app.json` -> config -> `userInterfaceStyle` stays `"light"`

No atom, molecule, organism or view changes: atoms already read colours with `useTheme()`, so they pick the new
theme up. Core: none.

## Canonical Template Mapping

- Theme -> `app/src/presentation/common/theme.ts` (current) and `STPR_INVARIANTS.md` 4.3 ("The theme is the single
  `presentation/common/theme.ts`, built from Paper's `MD3LightTheme`") -> same file, still built from
  `MD3LightTheme` for the applied light theme. Values mirror `backoffice/src/presentation/common/theme.ts`, which is
  a value reference only, not a UI reference.
- Root layout splash hold -> `app/src/app/_layout.tsx` (current `SplashScreen.preventAutoHideAsync` /
  `SplashScreen.hide` around the session) -> extended with the font condition. Font loading has no reference:
  **this change establishes it**.
- Tests -> `app/test/presentation/common/views/home-view.test.tsx` and the other view tests keep rendering through
  `test/support/render-with-providers.tsx`, which imports the default theme.

## Screens And Navigation

No new screen or route. Every existing screen changes appearance only. Launch: splash until the session is restored
and the fonts are loaded, then the gate decides as today.

## API Contract

None. No snapshot refresh.

## Held State

- Fonts loaded flag -> `useFonts` in the root layout -> set once at launch -> never resets.

No provider is added. The theme mode is fixed to light; there is no mode state.

## Forms And Structural Validation

None.

## Errors And Copy

- Fonts fail to load -> no `DomainError` (not an API interaction) -> the app hides the splash and renders with the
  system font, like a slow network font on the web. Nothing is shown to the user.

No new error code, no new copy.

## Platform Capabilities And Dependencies

- `@expo-google-fonts/inter`, `@expo-google-fonts/plus-jakarta-sans` -> `app` -> no existing dependency ships these
  fonts (the backoffice gets them from `next/font/google`). Add with `pnpm exec expo install`. Loaded at runtime with
  `expo-font`'s `useFonts` (already installed): works in Expo Go, no development build needed.

No new port or adapter.

## Out Of Scope

- Dark mode in the app: no mode toggle, no stored preference, no system colour scheme. `darkTheme` is built and
  exported but not applied; `userInterfaceStyle` stays `"light"`. Wiring it (reusing the backoffice's
  `getPreferredThemeModeUseCase` / `setPreferredThemeModeUseCase` from core or following the system scheme) is a later
  change.
- Restyling atoms or views to use the custom tokens (`custom.radius`, `custom.button`, `custom.card`, ...). They are
  available; adopting them is per component, in later changes.
- Embedding the fonts at build time with the `expo-font` config plugin (it would need a development build).
- Must not change: the backoffice theme, core, the API, routes, copy, the `color as string` cast in the tabs layout.

## Notes

- **Code is given.** The appendix is the theme to apply. Keep its values and structure; only adapt it to the repo
  style (Prettier, double quotes) and to TypeScript strictness if `tsc` asks for it. The `custom.navigation.drawer*`
  tokens are kept for parity even though the app has no drawer.
- **Invariant extension, needs approval before apply.** `STPR_INVARIANTS.md` 4.3 says the root layout holds the
  splash "once the session state is known". This change makes it "once the session state is known and the fonts are
  loaded (or failed)". Either accept it as an allowed deviation recorded here, or update that invariant line in the
  same change. No other deviation is allowed.
- **Font names.** The theme refers to `Inter-Regular`, `Inter-Medium`, `Inter-SemiBold`, `Inter-Bold`,
  `PlusJakartaSans-Regular`, `PlusJakartaSans-SemiBold`, `PlusJakartaSans-Bold`, `PlusJakartaSans-ExtraBold`.
  `fonts.ts` maps exactly those keys to `Inter_400Regular`, `Inter_500Medium`, `Inter_600SemiBold`, `Inter_700Bold`,
  `PlusJakartaSans_400Regular`, `PlusJakartaSans_600SemiBold`, `PlusJakartaSans_700Bold`,
  `PlusJakartaSans_800ExtraBold`, so `useFonts(fonts)` registers them under the theme's names.
- **Expo SDK 58.** Read the `expo-font` / fonts guide for SDK 58 (`https://docs.expo.dev/versions/v58.0.0/`) before
  wiring `useFonts`, and check that jest-expo mocks font loading in tests.
- **Verification tiers:**
  - app component: no new test. All existing view tests and the translation parity test must still pass with the new
    theme.
  - Manual check in Expo Go: teal primary button on login, slate background, Plus Jakarta Sans headings and Inter
    body text, teal selected tab, white tab bar, no purple anywhere, no font jump after the splash.
  - `pnpm dlx expo-doctor` reports no new issue.
- Done means `pnpm typecheck && pnpm lint && pnpm test` passes from `front/`.

## Appendix: Theme

```ts
import { MD3DarkTheme, MD3LightTheme, useTheme } from "react-native-paper";
import type { MD3Theme } from "react-native-paper";

/** React Native Paper (Material Design 3) equivalent of the MUI backoffice theme. */
const brand = {
  teal: "#0F766E",
  tealLight: "#14B8A6",
  tealBright: "#2DD4BF",
  pine: "#115E59",
  orange: "#F97316",
  burnt: "#C2410C",
  ink: "#0F172A",
  white: "#FFFFFF",
} as const;

const surfaces = {
  light: {
    bg: "#F8FAFC",
    paper: "#FFFFFF",
    fog: "#F1F5F9",
    border: "#E2E8F0",
    input: "#FFFFFF",
    text: "#0F172A",
    textSecondary: "#475569",
    textDisabled: "#94A3B8",
    sidebar: "#0F172A",
  },
  dark: {
    bg: "#0B1120",
    paper: "#131C2E",
    fog: "#1B2740",
    border: "#263450",
    input: "#1B2740",
    text: "#F1F5F9",
    textSecondary: "#94A3B8",
    textDisabled: "#64748B",
    sidebar: "#070B14",
  },
} as const;

const moodColors = ["#DC2626", "#F97316", "#EAB308", "#84CC16", "#16A34A"];
const chartColors = ["#0F766E", "#F97316", "#6366F1", "#0EA5E9", "#64748B"];

/** #RRGGBB + opacity, so styles work in React Native without MUI alpha(). */
function alpha(hex: string, opacity: number): string {
  const raw = hex.replace("#", "");
  const r = parseInt(raw.slice(0, 2), 16);
  const g = parseInt(raw.slice(2, 4), 16);
  const b = parseInt(raw.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${opacity})`;
}

/** Equivalent to MUI lighten(main, .88) / darken(main, .7). */
function mix(hex: string, target: string, ratio: number): string {
  const channels = (value: string) => [1, 3, 5].map((i) => parseInt(value.slice(i, i + 2), 16));
  const a = channels(hex);
  const b = channels(target);
  return `#${a
    .map((v, i) =>
      Math.round(v * (1 - ratio) + b[i] * ratio)
        .toString(16)
        .padStart(2, "0"),
    )
    .join("")
    .toUpperCase()}`;
}

/** Font names must match the names registered through Expo Font / native assets. */
const body = {
  regular: "Inter-Regular",
  medium: "Inter-Medium",
  semiBold: "Inter-SemiBold",
  bold: "Inter-Bold",
};
const heading = {
  regular: "PlusJakartaSans-Regular",
  semiBold: "PlusJakartaSans-SemiBold",
  bold: "PlusJakartaSans-Bold",
  extraBold: "PlusJakartaSans-ExtraBold",
};

function buildFonts(base: MD3Theme["fonts"]): MD3Theme["fonts"] {
  const withFont = <K extends keyof MD3Theme["fonts"]>(
    key: K,
    fontFamily: string,
    fontSize: number,
    lineHeight: number,
    letterSpacing = 0,
  ) => ({ ...base[key], fontFamily, fontSize, lineHeight, letterSpacing });

  return {
    ...base,
    displayLarge: withFont("displayLarge", heading.extraBold, 40, 48, -1.2),
    displayMedium: withFont("displayMedium", heading.extraBold, 32, 40, -0.64),
    displaySmall: withFont("displaySmall", heading.bold, 28, 36, -0.56),
    headlineLarge: withFont("headlineLarge", heading.bold, 28, 36, -0.56),
    headlineMedium: withFont("headlineMedium", heading.bold, 24, 32, -0.24),
    headlineSmall: withFont("headlineSmall", heading.bold, 20, 28),
    titleLarge: withFont("titleLarge", heading.bold, 20, 28),
    titleMedium: withFont("titleMedium", body.semiBold, 16, 24),
    titleSmall: withFont("titleSmall", body.semiBold, 14, 20),
    bodyLarge: withFont("bodyLarge", body.regular, 16, 24),
    bodyMedium: withFont("bodyMedium", body.regular, 14, 22),
    bodySmall: withFont("bodySmall", body.regular, 12, 16),
    labelLarge: withFont("labelLarge", body.bold, 15, 22, 0.15),
    labelMedium: withFont("labelMedium", body.semiBold, 12, 16),
    labelSmall: withFont("labelSmall", body.semiBold, 11, 16),
    default: { ...base.default, fontFamily: body.regular },
  };
}

export function buildTheme(mode: "light" | "dark") {
  const dark = mode === "dark";
  const base = dark ? MD3DarkTheme : MD3LightTheme;
  const s = surfaces[mode];
  const primary = dark ? brand.tealLight : brand.teal;
  const primaryStrong = dark ? brand.tealLight : brand.pine;

  const exercise = (main: string) => ({
    main,
    pastel: mix(main, dark ? "#000000" : "#FFFFFF", dark ? 0.7 : 0.88),
    onMain: "#FFFFFF",
  });

  return {
    ...base,
    roundness: 4, // MD3 component-specific curvature; use custom radius tokens below for exact MUI shapes.
    fonts: buildFonts(base.fonts),
    colors: {
      ...base.colors,
      primary,
      onPrimary: dark ? s.bg : brand.white,
      primaryContainer: dark ? "#134E4A" : "#CCFBF1",
      onPrimaryContainer: dark ? "#CCFBF1" : brand.pine,
      secondary: brand.orange,
      onSecondary: brand.ink,
      secondaryContainer: dark ? "#78350F" : "#FFEDD5",
      onSecondaryContainer: dark ? "#FFEDD5" : "#7C2D12",
      tertiary: "#6366F1",
      onTertiary: "#FFFFFF",
      background: s.bg,
      onBackground: s.text,
      surface: s.paper,
      onSurface: s.text,
      surfaceVariant: s.fog,
      onSurfaceVariant: s.textSecondary,
      surfaceDisabled: alpha(s.textSecondary, 0.12),
      onSurfaceDisabled: s.textDisabled,
      outline: s.border,
      outlineVariant: s.border,
      error: "#DC2626",
      onError: "#FFFFFF",
      errorContainer: dark ? "#531B20" : "#FEE2E2",
      onErrorContainer: dark ? "#FECACA" : "#991B1B",
      inverseSurface: dark ? "#F1F5F9" : brand.ink,
      inverseOnSurface: dark ? brand.ink : "#F1F5F9",
      inversePrimary: dark ? brand.pine : brand.tealLight,
      shadow: brand.ink,
      scrim: "#000000",
      backdrop: alpha("#000000", 0.5),
      // Avoid MD3's default purple-tinted elevation surfaces.
      elevation: {
        level0: "transparent",
        level1: s.paper,
        level2: s.paper,
        level3: s.paper,
        level4: s.paper,
        level5: s.paper,
      },
    },
    custom: {
      brand,
      surfaces: s,
      spacing: (factor: number) => factor * 8,
      radius: { xs: 8, sm: 10, input: 12, button: 12, card: 16, dialog: 20, pill: 999 },
      button: { minHeight: 44, smallHeight: 36, largeHeight: 52 },
      card: { padding: 24, borderWidth: 1 },
      input: { background: s.input, border: s.border },
      navigation: {
        drawer: s.sidebar,
        drawerText: alpha(brand.white, 0.72),
        drawerSelected: alpha(brand.tealLight, 0.16),
        drawerSelectedText: brand.tealLight,
        divider: s.border,
      },
      action: {
        hover: alpha(primary, dark ? 0.08 : 0.06),
        selected: alpha(primary, dark ? 0.14 : 0.1),
        focus: alpha(primary, dark ? 0.16 : 0.12),
      },
      semantic: {
        success: "#16A34A",
        warning: "#D97706",
        error: "#DC2626",
        info: "#2563EB",
      },
      exercise: {
        strength: exercise("#2F80ED"),
        cardio: exercise("#E5484D"),
        stretching: exercise("#27AE60"),
      },
      mood: [...moodColors],
      chart: [...chartColors],
      gradient: {
        hero: [brand.teal, brand.pine] as const,
        energy: [brand.orange, brand.burnt] as const,
      },
      typography: { heading, body },
      primaryHover: dark ? brand.tealBright : brand.pine,
      primaryStrong,
    },
  };
}

export const lightTheme = buildTheme("light");
export const darkTheme = buildTheme("dark");
export type AppTheme = typeof lightTheme;
export const useAppTheme = () => useTheme<AppTheme>();
export default lightTheme;
```
