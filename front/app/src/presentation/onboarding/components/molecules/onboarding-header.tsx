import { StyleSheet, View } from "react-native";
import Button from "@/presentation/common/components/atoms/button";
import HelperText from "@/presentation/common/components/atoms/helper-text";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ProgressBar from "@/presentation/common/components/atoms/progress-bar";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import Text from "@/presentation/common/components/atoms/text";

interface OnboardingHeaderProps {
  /** "Paso n de total", already translated. */
  progressLabel: string;
  /** From 0 to 1. */
  progress: number;
  backLabel: string;
  /** No back caret when absent: the first step cannot be left. */
  onBack?: () => void;
  logoutLabel: string;
  loggingOut: boolean;
  logoutError: string | null;
  onLogout: () => void;
}

/** Top of every onboarding screen: back caret, step position with its progress bar, and logout. */
export default function OnboardingHeader({
  progressLabel,
  progress,
  backLabel,
  onBack,
  logoutLabel,
  loggingOut,
  logoutError,
  onLogout,
}: OnboardingHeaderProps) {
  return (
    <SafeArea edges={["top", "left", "right"]} style={styles.area}>
      <View style={styles.row}>
        <View style={styles.back}>
          {onBack && (
            <IconButton icon="chevron-left" accessibilityLabel={backLabel} onPress={onBack} />
          )}
        </View>
        <Button mode="text" onPress={onLogout} loading={loggingOut} disabled={loggingOut}>
          {logoutLabel}
        </Button>
      </View>
      <HelperText type="error" visible={logoutError !== null} style={styles.logoutError}>
        {logoutError ?? ""}
      </HelperText>
      <View style={styles.progress}>
        <Text variant="labelLarge">{progressLabel}</Text>
        <ProgressBar progress={progress} accessibilityLabel={progressLabel} />
      </View>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  area: {
    flex: 0,
    paddingHorizontal: 16,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    minHeight: 48,
  },
  back: {
    minWidth: 48,
  },
  logoutError: {
    textAlign: "right",
  },
  progress: {
    gap: 8,
    paddingHorizontal: 8,
  },
});
