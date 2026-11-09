import { StyleSheet } from "react-native";
import Button from "@/presentation/common/components/atoms/button";
import Text from "@/presentation/common/components/atoms/text";
import { Link } from "expo-router";
import { useTranslation } from "react-i18next";
import SafeArea from "@/presentation/common/components/atoms/safe-area";

export default function NotFoundView() {
  const { t } = useTranslation("common");

  return (
    <SafeArea style={styles.container}>
      <Text variant="headlineMedium" role="heading" aria-level={1}>
        {t("notFound.title")}
      </Text>
      <Link href="/" asChild>
        <Button mode="contained">{t("notFound.goHome")}</Button>
      </Link>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "center",
    gap: 16,
    padding: 24,
  },
});
