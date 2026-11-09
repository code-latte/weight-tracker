import { use } from "react";
import PreferencesContext, { type PreferencesState } from "../context/preferences-context";

export default function usePreferences(): PreferencesState {
  return use(PreferencesContext);
}
