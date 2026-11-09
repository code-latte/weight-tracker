import { StyleSheet } from "react-native";
import Text from "@/presentation/common/components/atoms/text";
import { useTranslation } from "react-i18next";
import SafeArea from "@/presentation/common/components/atoms/safe-area";

export default function WorkoutsView() {
  const { t } = useTranslation("common");

  return (
    <SafeArea style={styles.container}>
      <Text variant="headlineMedium" role="heading" aria-level={1}>
        {t("tabs.workouts")}
      </Text>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
  },
});
