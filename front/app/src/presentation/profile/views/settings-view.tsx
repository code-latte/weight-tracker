import { ScrollView, StyleSheet } from "react-native";
import { useTranslation } from "react-i18next";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import usePreferences from "@/presentation/common/hooks/use-preferences";
import PreferenceSelector from "@/presentation/profile/components/molecules/preference-selector";

export default function SettingsView() {
  const { t } = useTranslation("settings");
  const { themeMode, language, setThemeMode, setLanguage } = usePreferences();

  return (
    // The section header already keeps clear of the status bar.
    <SafeArea edges={["left", "right"]}>
      <ScrollView contentContainerStyle={styles.container}>
        <PreferenceSelector
          label={t("theme.label")}
          value={themeMode}
          options={[
            { value: "light", label: t("theme.light") },
            { value: "dark", label: t("theme.dark") },
          ]}
          onChange={setThemeMode}
        />
        <PreferenceSelector
          label={t("language.label")}
          value={language}
          options={[
            { value: "es", label: t("language.es") },
            { value: "en", label: t("language.en") },
          ]}
          onChange={setLanguage}
        />
      </ScrollView>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 24,
    padding: 24,
    paddingTop: 0,
  },
});
