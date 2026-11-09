import { createContext } from "react";

export const themeModes = ["light", "dark"] as const;
export type ThemeMode = (typeof themeModes)[number];

export const languages = ["es", "en"] as const;
export type Language = (typeof languages)[number];

export interface PreferencesState {
  /** The stored colour mode, else the device's. */
  themeMode: ThemeMode;
  /** The language the app shows: the stored one, else the one detected from the device. */
  language: Language;
  /** False until the stored preferences have been read or have failed to be read. */
  ready: boolean;
  setThemeMode: (mode: ThemeMode) => void;
  setLanguage: (language: Language) => void;
}

const PreferencesContext = createContext<PreferencesState>({
  themeMode: "light",
  language: "es",
  ready: false,
  setThemeMode: () => {},
  setLanguage: () => {},
});

export default PreferencesContext;
