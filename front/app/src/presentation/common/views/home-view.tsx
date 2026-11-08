import { StyleSheet, View } from "react-native";
import Text from "@/presentation/common/components/atoms/text";
import { useTranslation } from "react-i18next";

export default function HomeView() {
  const { t } = useTranslation("common");

  return (
    <View style={styles.container}>
      <Text variant="headlineLarge" role="heading" aria-level={1}>
        {t("home.title")}
      </Text>
      <Text variant="bodyLarge">{t("home.signedIn")}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 24,
  },
});
