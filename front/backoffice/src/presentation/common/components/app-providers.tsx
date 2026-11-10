"use client";

import { useEffect, type ReactNode } from "react";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v16-appRouter";
import { I18nextProvider } from "react-i18next";
import container from "@/infrastructure/container";
import i18n, { detectLanguage, supportedLanguages } from "@/infrastructure/i18n";
import SessionProvider from "@/presentation/auth/components/session-provider";
import ContainerProvider from "../context/container-provider";
import CurrentUserProvider from "./current-user-provider";
import ExerciseCatalogProvider from "./exercise-catalog-provider";
import ThemeModeProvider from "./theme-mode-provider";
import NavigationDrawerProvider from "./navigation-drawer-provider";

interface AppProvidersProps {
  children: ReactNode;
}

export default function AppProviders({ children }: AppProvidersProps) {
  useEffect(() => {
    let active = true;
    const syncDocumentLanguage = (language: string) => {
      document.documentElement.lang = language;
    };
    i18n.on("languageChanged", syncDocumentLanguage);

    // A language the user chose on this device wins; otherwise follow the browser.
    void container
      .resolve("getPreferredLanguageUseCase")
      .execute()
      .then((result) => {
        if (!active) {
          return;
        }
        const preferred = result.ok ? result.value : null;
        const supported = supportedLanguages.find((language) => language === preferred);
        void i18n.changeLanguage(supported ?? detectLanguage());
      });

    return () => {
      active = false;
      i18n.off("languageChanged", syncDocumentLanguage);
    };
  }, []);

  return (
    <AppRouterCacheProvider>
      <I18nextProvider i18n={i18n}>
        <ContainerProvider container={container}>
          <ThemeModeProvider>
            <SessionProvider>
              <CurrentUserProvider>
                <ExerciseCatalogProvider>
                  <NavigationDrawerProvider>{children}</NavigationDrawerProvider>
                </ExerciseCatalogProvider>
              </CurrentUserProvider>
            </SessionProvider>
          </ThemeModeProvider>
        </ContainerProvider>
      </I18nextProvider>
    </AppRouterCacheProvider>
  );
}
