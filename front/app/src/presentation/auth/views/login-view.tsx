import { useState } from "react";
import {
  Keyboard,
  KeyboardAvoidingView,
  Platform,
  StyleSheet,
  TouchableWithoutFeedback,
} from "react-native";
import { useTranslation } from "react-i18next";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useContainer from "@/presentation/common/hooks/use-container";
import LoginForm from "../components/organisms/login-form";
import useSession from "../hooks/use-session";
import SafeArea from "@/presentation/common/components/atoms/safe-area";

const LOGIN_ERROR_CODES: readonly DomainErrorCode[] = ["UNAUTHORIZED"];

export default function LoginView() {
  const { t } = useTranslation();
  const container = useContainer();
  const session = useSession();
  const [submitting, setSubmitting] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function handleSubmit(credentials: { email: string; password: string }) {
    setSubmitting(true);
    setErrorCode(null);

    const result = await container.resolve("loginUseCase").execute(credentials);

    if (result.ok) {
      // The session gate reacts to the reloaded state; nothing navigates here.
      await session.reload();
      return;
    }

    setErrorCode(result.error.code);
    setSubmitting(false);
  }

  const errorMessage = errorCode
    ? LOGIN_ERROR_CODES.includes(errorCode)
      ? t(`auth:login.errors.${errorCode}`)
      : t(`errors:${errorCode}`)
    : null;

  return (
    <SafeArea>
      <TouchableWithoutFeedback onPress={Keyboard.dismiss} accessible={false}>
        <KeyboardAvoidingView
          style={styles.container}
          behavior={Platform.OS === "ios" ? "padding" : "height"}
        >
          <LoginForm
            submitting={submitting}
            errorMessage={errorMessage}
            onSubmit={(credentials) => void handleSubmit(credentials)}
          />
        </KeyboardAvoidingView>
      </TouchableWithoutFeedback>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "center",
    padding: 24,
  },
});
