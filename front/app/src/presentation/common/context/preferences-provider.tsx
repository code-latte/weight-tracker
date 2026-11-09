import { useCallback, useEffect, useEffectEvent, useMemo, useState, type ReactNode } from "react";
import { useColorScheme } from "react-native";
import { useTranslation } from "react-i18next";
import useContainer from "../hooks/use-container";
import PreferencesContext, {
  languages,
  themeModes,
  type Language,
  type ThemeMode,
} from "./preferences-context";

interface PreferencesProviderProps {
  children: ReactNode;
}

/**
 * Holds the colour mode and language chosen on this device. Until one is chosen, the colour mode follows the device
 * and the language stays the one the i18n instance detected.
 */
export default function PreferencesProvider({ children }: PreferencesProviderProps) {
  const container = useContainer();
  const { i18n } = useTranslation();
  const deviceScheme = useColorScheme();
  const [storedThemeMode, setStoredThemeMode] = useState<ThemeMode | null>(null);
  const [ready, setReady] = useState(false);

  // An effect event, so a language change (which gives a new i18n reference) does not read the preferences again.
  const applyStoredLanguage = useEffectEvent(async (language: Language | undefined) => {
    if (language) {
      await i18n.changeLanguage(language);
    }
  });

  useEffect(() => {
    let active = true;

    // Read both stored preferences once on mount. A failed read or an unknown value counts as nothing stored.
    void Promise.all([
      container.resolve("getPreferredThemeModeUseCase").execute(),
      container.resolve("getPreferredLanguageUseCase").execute(),
    ]).then(async ([themeResult, languageResult]) => {
      const mode = themeModes.find(
        (candidate) => themeResult.ok && candidate === themeResult.value,
      );
      const language = languages.find(
        (candidate) => languageResult.ok && candidate === languageResult.value,
      );

      await applyStoredLanguage(language);
      if (active) {
        setStoredThemeMode(mode ?? null);
        setReady(true);
      }
    });

    return () => {
      active = false;
    };
  }, [container]);

  const setThemeMode = useCallback(
    (mode: ThemeMode) => {
      setStoredThemeMode(mode);
      // A failed write only means the next launch starts from what was stored before.
      void container.resolve("setPreferredThemeModeUseCase").execute(mode);
    },
    [container],
  );

  const setLanguage = useCallback(
    (language: Language) => {
      void i18n.changeLanguage(language);
      // A failed write only means the next launch starts from what was stored before.
      void container.resolve("setPreferredLanguageUseCase").execute(language);
    },
    [container, i18n],
  );

  const themeMode: ThemeMode = storedThemeMode ?? (deviceScheme === "dark" ? "dark" : "light");
  const language: Language =
    languages.find((candidate) => candidate === i18n.resolvedLanguage) ?? "es";

  const value = useMemo(
    () => ({ themeMode, language, ready, setThemeMode, setLanguage }),
    [themeMode, language, ready, setThemeMode, setLanguage],
  );

  return <PreferencesContext value={value}>{children}</PreferencesContext>;
}
