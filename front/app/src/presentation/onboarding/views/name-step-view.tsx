import { router } from "expo-router";
import { useTranslation } from "react-i18next";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import useLogout from "@/presentation/common/hooks/use-logout";
import ProfileNameFields from "../components/molecules/profile-name-fields";
import OnboardingStepLayout from "../components/organisms/onboarding-step-layout";
import useOnboarding from "../hooks/use-onboarding";

const STEP_ID = "name";

export default function NameStepView() {
  const { t } = useTranslation("onboarding");
  const { steps, values, errors, saving, errorCode, change, validateStep, finish } =
    useOnboarding();
  const currentUser = useCurrentUser();
  const { loggingOut, errorCode: logoutErrorCode, logout } = useLogout();

  const index = steps.findIndex((step) => step.id === STEP_ID);
  const step = steps[index];
  const isFirst = index === 0;
  const isLast = index === steps.length - 1;

  function submit() {
    if (isLast) {
      void finish(STEP_ID);
      return;
    }
    if (validateStep(STEP_ID)) {
      router.push(steps[index + 1].href);
    }
  }

  const fieldError = (field: "name" | "surname") =>
    errors[field] ? t(`fieldErrors.${errors[field]}`) : null;

  return (
    <OnboardingStepLayout
      header={{
        progressLabel: t("progress", { current: index + 1, total: steps.length }),
        progress: (index + 1) / steps.length,
        backLabel: t("common:back"),
        onBack: isFirst ? undefined : () => router.back(),
        logoutLabel: t("profile:logout"),
        loggingOut,
        logoutError: logoutErrorCode ? t(`errors:${logoutErrorCode}`) : null,
        onLogout: () => void logout(),
      }}
      title={t(step.titleKey)}
      submitLabel={isLast ? t("finish") : t("next")}
      submitting={saving}
      submitError={errorCode ? t(`errors:${errorCode}`) : null}
      onSubmit={submit}
      unreadable={
        currentUser.status === "error"
          ? {
              message: t(`errors:${currentUser.errorCode}`),
              retryLabel: t("retry"),
              onRetry: () => void currentUser.reload(),
            }
          : undefined
      }
    >
      <ProfileNameFields
        disabled={saving}
        name={{
          label: t("fields.name"),
          value: values.name,
          error: fieldError("name"),
          onChange: (value) => change("name", value),
        }}
        surname={{
          label: t("fields.surname"),
          value: values.surname,
          error: fieldError("surname"),
          onChange: (value) => change("surname", value),
        }}
      />
    </OnboardingStepLayout>
  );
}
