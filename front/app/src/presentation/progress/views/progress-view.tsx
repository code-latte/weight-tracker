import { StyleSheet } from "react-native";
import Text from "@/presentation/common/components/atoms/text";
import { useTranslation } from "react-i18next";
import SafeArea from "@/presentation/common/components/atoms/safe-area";

export default function ProgressView() {
  const { t } = useTranslation("common");

  return (
    <SafeArea edges={["left", "right"]} style={styles.container}>
      <Text variant="headlineMedium" role="heading" aria-level={1}>
        {t("tabs.progress")}
      </Text>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
    paddingTop: 0,
  },
});
