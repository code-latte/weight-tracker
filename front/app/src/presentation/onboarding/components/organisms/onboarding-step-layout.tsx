import type { ReactNode } from "react";
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet } from "react-native";
import Button from "@/presentation/common/components/atoms/button";
import HelperText from "@/presentation/common/components/atoms/helper-text";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import Text from "@/presentation/common/components/atoms/text";
import ErrorState from "@/presentation/common/components/molecules/error-state";
import OnboardingHeader from "../molecules/onboarding-header";

interface OnboardingStepLayoutProps {
  header: {
    progressLabel: string;
    progress: number;
    backLabel: string;
    onBack?: () => void;
    logoutLabel: string;
    loggingOut: boolean;
    logoutError: string | null;
    onLogout: () => void;
  };
  title: string;
  /** "Siguiente" or "Finalizar", already chosen by the view. */
  submitLabel: string;
  submitting: boolean;
  submitError: string | null;
  onSubmit: () => void;
  /** When the profile could not be read: its message and retry replace the step. */
  unreadable?: { message: string; retryLabel: string; onRetry: () => void };
  children: ReactNode;
}

/** One onboarding step: header, title, the step's fields and its action, or the unreadable-profile state. */
export default function OnboardingStepLayout({
  header,
  title,
  submitLabel,
  submitting,
  submitError,
  onSubmit,
  unreadable,
  children,
}: OnboardingStepLayoutProps) {
  return (
    <SafeArea edges={["left", "right", "bottom"]}>
      <OnboardingHeader {...header} />
      <KeyboardAvoidingView
        style={styles.flex}
        behavior={Platform.OS === "ios" ? "padding" : "height"}
      >
        {unreadable ? (
          <ErrorState
            message={unreadable.message}
            retryLabel={unreadable.retryLabel}
            onRetry={unreadable.onRetry}
          />
        ) : (
          <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
            <Text variant="headlineMedium" role="heading" aria-level={1}>
              {title}
            </Text>
            {children}
            <HelperText type="error" visible={submitError !== null}>
              {submitError ?? ""}
            </HelperText>
            <Button mode="contained" onPress={onSubmit} loading={submitting} disabled={submitting}>
              {submitLabel}
            </Button>
          </ScrollView>
        )}
      </KeyboardAvoidingView>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  flex: {
    flex: 1,
  },
  content: {
    gap: 16,
    padding: 24,
  },
});
