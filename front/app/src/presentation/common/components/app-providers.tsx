import type { ReactNode } from "react";
import { DefaultTheme, ThemeProvider, type Theme } from "expo-router";
import { PaperProvider } from "react-native-paper";
import { I18nextProvider } from "react-i18next";
import container from "@/infrastructure/container";
import i18n from "@/infrastructure/i18n";
import SessionProvider from "@/presentation/auth/context/session-provider";
import ContainerProvider from "../context/container-provider";
import theme from "../theme";

/** Expo Router's navigation theme from the Paper theme, so screens and navigator chrome use the brand colours. */
const navigationTheme: Theme = {
  ...DefaultTheme,
  colors: {
    ...DefaultTheme.colors,
    primary: theme.colors.primary,
    background: theme.colors.background,
    card: theme.colors.surface,
    text: theme.colors.onSurface,
    border: theme.colors.outline,
    notification: theme.colors.error,
  },
};

interface AppProvidersProps {
  children: ReactNode;
}

export default function AppProviders({ children }: AppProvidersProps) {
  return (
    <PaperProvider theme={theme}>
      <ThemeProvider value={navigationTheme}>
        <I18nextProvider i18n={i18n}>
          <ContainerProvider container={container}>
            <SessionProvider>{children}</SessionProvider>
          </ContainerProvider>
        </I18nextProvider>
      </ThemeProvider>
    </PaperProvider>
  );
}
