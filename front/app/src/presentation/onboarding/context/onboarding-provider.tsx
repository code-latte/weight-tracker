import { useCallback, useMemo, useState, type ReactNode } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import defaultSteps, {
  type OnboardingErrors,
  type OnboardingField,
  type OnboardingStep,
  type OnboardingValues,
} from "../steps";
import OnboardingContext from "./onboarding-context";

interface OnboardingProviderProps {
  children: ReactNode;
  /** The ordered steps; tests pass their own list. */
  steps?: OnboardingStep[];
}

/** What the onboarding steps collect, from the first step to the final save. Never navigates. */
export default function OnboardingProvider({
  children,
  steps = defaultSteps,
}: OnboardingProviderProps) {
  const container = useContainer();
  const { runWithSession } = useSession();
  const { profile, setProfile } = useCurrentUser();
  const [values, setValues] = useState<OnboardingValues>({ name: "", surname: "" });
  const [errors, setErrors] = useState<OnboardingErrors>({});
  const [saving, setSaving] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  const [seededFrom, setSeededFrom] = useState<string | null>(null);

  // Seeds the fields once from the first profile read, during render rather than in an effect.
  if (profile && seededFrom !== profile.id) {
    setSeededFrom(profile.id);
    setValues({ name: profile.name, surname: profile.surname });
  }

  const change = useCallback((field: OnboardingField, value: string) => {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  }, []);

  const validateStep = useCallback(
    (stepId: string) => {
      const step = steps.find((candidate) => candidate.id === stepId);
      const found = step ? step.validate(values) : {};
      setErrors(found);
      return Object.keys(found).length === 0;
    },
    [steps, values],
  );

  const finish = useCallback(
    async (stepId: string) => {
      if (saving || !validateStep(stepId)) {
        return;
      }

      setSaving(true);
      setErrorCode(null);
      const request = { name: values.name.trim(), surname: values.surname.trim() };
      const result = await runWithSession(() =>
        container.resolve("updateMyProfileUseCase").execute(request),
      );

      if (result.ok) {
        // The access gate follows the completed profile; nothing navigates here.
        setProfile(result.value);
        return;
      }

      setErrorCode(result.error.code);
      setSaving(false);
    },
    [saving, validateStep, values, runWithSession, container, setProfile],
  );

  const value = useMemo(
    () => ({ steps, values, errors, saving, errorCode, change, validateStep, finish }),
    [steps, values, errors, saving, errorCode, change, validateStep, finish],
  );

  return <OnboardingContext value={value}>{children}</OnboardingContext>;
}
