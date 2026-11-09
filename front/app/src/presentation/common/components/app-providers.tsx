import type { ReactNode } from "react";
import { I18nextProvider } from "react-i18next";
import container from "@/infrastructure/container";
import i18n from "@/infrastructure/i18n";
import SessionProvider from "@/presentation/auth/context/session-provider";
import ContainerProvider from "../context/container-provider";
import CurrentUserProvider from "../context/current-user-provider";
import PreferencesProvider from "../context/preferences-provider";
import AppThemeProvider from "./app-theme-provider";

interface AppProvidersProps {
  children: ReactNode;
}

export default function AppProviders({ children }: AppProvidersProps) {
  return (
    <I18nextProvider i18n={i18n}>
      <ContainerProvider container={container}>
        <PreferencesProvider>
          <AppThemeProvider>
            <SessionProvider>
              <CurrentUserProvider>{children}</CurrentUserProvider>
            </SessionProvider>
          </AppThemeProvider>
        </PreferencesProvider>
      </ContainerProvider>
    </I18nextProvider>
  );
}
