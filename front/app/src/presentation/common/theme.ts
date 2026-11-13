import { MD3DarkTheme, MD3LightTheme, useTheme } from "react-native-paper";
import type { MD3Theme, MD3TypescaleKey } from "react-native-paper";

/** React Native Paper (Material Design 3) equivalent of the MUI backoffice theme. */
const brand = {
  teal: "#0F766E",
  tealLight: "#14B8A6",
  tealBright: "#2DD4BF",
  pine: "#115E59",
  orange: "#F97316",
  // The dark theme's orange: a pastel of the brand orange, so it does not glare on dark surfaces.
  orangeSoft: "#F57736",
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
  // Paper types the typescale keys as an enum; its string values let the calls below pass plain names.
  const withFont = (
    key: `${MD3TypescaleKey}`,
    fontFamily: string,
    fontSize: number,
    lineHeight: number,
    letterSpacing = 0,
  ) => ({
    ...base[key as MD3TypescaleKey],
    fontFamily,
    fontSize,
    lineHeight,
    letterSpacing,
  });

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
      secondary: dark ? brand.orangeSoft : brand.orange,
      // White on the brand orange, in both modes, for text and icons on tonal buttons, selected chips and segments.
      onSecondary: brand.white,
      // Selected chips, segmented buttons and tonal buttons paint with these: the brand orange (softer in the dark theme), not a tint of it.
      secondaryContainer: dark ? brand.orangeSoft : brand.orange,
      onSecondaryContainer: brand.white,
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
