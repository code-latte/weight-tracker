import { ScrollView, StyleSheet, View } from "react-native";
import { router } from "expo-router";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import HelperText from "@/presentation/common/components/atoms/helper-text";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import Text from "@/presentation/common/components/atoms/text";
import ErrorState from "@/presentation/common/components/molecules/error-state";
import LoadingState from "@/presentation/common/components/molecules/loading-state";
import ProfileMenu, {
  type ProfileMenuItem,
} from "@/presentation/profile/components/molecules/profile-menu";
import ProfileSummary from "@/presentation/profile/components/molecules/profile-summary";
import TrainerCard from "@/presentation/profile/components/molecules/trainer-card";
import useLogout from "@/presentation/common/hooks/use-logout";
import useProfile from "@/presentation/profile/hooks/use-profile";

export default function ProfileView() {
  const { t } = useTranslation("profile");
  const { state, retry } = useProfile();
  const { loggingOut, errorCode, logout } = useLogout();

  const menuItems: ProfileMenuItem[] = [
    {
      icon: "dumbbell",
      title: t("menu.myData.title"),
      description: t("menu.myData.description"),
    },
    { icon: "target", title: t("menu.goals.title"), description: t("menu.goals.description") },
    {
      icon: "heart-outline",
      title: t("menu.limitations.title"),
      description: t("menu.limitations.description"),
    },
    { icon: "bell-outline", title: t("menu.notifications") },
    { icon: "information-outline", title: t("menu.about") },
  ];

  return (
    <SafeArea>
      <ScrollView contentContainerStyle={styles.container}>
        <View style={styles.titleRow}>
          <Text variant="headlineMedium" role="heading" aria-level={1}>
            {t("title")}
          </Text>
          <IconButton
            icon="cog-outline"
            accessibilityLabel={t("settings")}
            onPress={() => router.push("/profile/settings")}
          />
        </View>

        {state.status === "loading" && <LoadingState />}

        {state.status === "error" && (
          <ErrorState
            message={t(`errors:${state.errorCode}`)}
            retryLabel={t("retry")}
            onRetry={retry}
          />
        )}

        {state.status === "ready" && (
          <>
            <ProfileSummary
              name={state.profile.name}
              surname={state.profile.surname}
              email={state.profile.email}
            />
            {state.trainer && (
              <TrainerCard
                title={t("trainer.title")}
                name={state.trainer.name}
                surname={state.trainer.surname}
              />
            )}
            <ProfileMenu items={menuItems} />
          </>
        )}

        <View>
          <Button
            mode="outlined"
            onPress={() => void logout()}
            loading={loggingOut}
            disabled={loggingOut}
          >
            {t("logout")}
          </Button>
          <HelperText type="error" visible={errorCode !== null}>
            {errorCode ? t(`errors:${errorCode}`) : ""}
          </HelperText>
        </View>
      </ScrollView>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 24,
    padding: 24,
  },
  titleRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
  },
});
