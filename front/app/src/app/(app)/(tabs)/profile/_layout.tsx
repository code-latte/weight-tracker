import { Stack } from "expo-router";
import { useTranslation } from "react-i18next";
import SectionHeader from "@/presentation/common/components/molecules/section-header";

export default function ProfileLayout() {
  const { t } = useTranslation();

  return (
    <Stack>
      <Stack.Screen name="index" options={{ headerShown: false }} />
      <Stack.Screen
        name="settings"
        options={{
          title: t("settings:title"),
          header: ({ navigation, options }) => (
            <SectionHeader
              title={options.title ?? ""}
              backLabel={t("common:back")}
              onBack={navigation.goBack}
            />
          ),
        }}
      />
    </Stack>
  );
}
