import { StyleSheet, View } from "react-native";
import Text from "@/presentation/common/components/atoms/text";
import { useTranslation } from "react-i18next";

export default function WorkoutsView() {
  const { t } = useTranslation("common");

  return (
    <View style={styles.container}>
      <Text variant="headlineMedium" role="heading" aria-level={1}>
        {t("tabs.workouts")}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
  },
});
