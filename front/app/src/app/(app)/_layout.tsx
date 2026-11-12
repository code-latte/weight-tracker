import { View } from "react-native";
import { Stack } from "expo-router";
import { useTranslation } from "react-i18next";
import BrandBar from "@/presentation/common/components/molecules/brand-bar";

export default function AppLayout() {
  const { t } = useTranslation("common");

  // The brand bar takes the top inset; the screens below it are laid out under it, so their own top insets are nil.
  return (
    <View style={{ flex: 1 }}>
      <BrandBar name={t("appName")} />
      <Stack screenOptions={{ headerShown: false }}>
        <Stack.Screen name="(tabs)" />
      </Stack>
    </View>
  );
}
