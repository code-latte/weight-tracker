import { useMemo, type ReactNode } from "react";
import { DarkTheme, DefaultTheme, ThemeProvider, type Theme } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { PaperProvider } from "react-native-paper";
import usePreferences from "../hooks/use-preferences";
import { darkTheme, lightTheme, type AppTheme } from "../theme";

/** Expo Router's navigation theme from the Paper theme, so screens and navigator chrome use the brand colours. */
function navigationTheme(theme: AppTheme, dark: boolean): Theme {
  const base = dark ? DarkTheme : DefaultTheme;

  return {
    ...base,
    colors: {
      ...base.colors,
      primary: theme.colors.primary,
      background: theme.colors.background,
      card: theme.colors.surface,
      text: theme.colors.onSurface,
      border: theme.colors.outline,
      notification: theme.colors.error,
    },
  };
}

interface AppThemeProviderProps {
  children: ReactNode;
}

/** Applies the light or dark brand theme to Paper, the navigators and the status bar. */
export default function AppThemeProvider({ children }: AppThemeProviderProps) {
  const { themeMode } = usePreferences();
  const dark = themeMode === "dark";
  const theme = dark ? darkTheme : lightTheme;
  const navigation = useMemo(() => navigationTheme(theme, dark), [theme, dark]);

  return (
    <PaperProvider theme={theme}>
      <ThemeProvider value={navigation}>
        {children}
        <StatusBar style={dark ? "light" : "dark"} />
      </ThemeProvider>
    </PaperProvider>
  );
}
