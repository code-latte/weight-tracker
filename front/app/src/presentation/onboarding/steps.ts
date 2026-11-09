import type { Href } from "expo-router";
import { validateProfileName, type ProfileNameViolation } from "@weight-tracker/core/application";

/** Everything the onboarding collects; each step fills some of these fields. */
export interface OnboardingValues {
  name: string;
  surname: string;
}

export type OnboardingField = keyof OnboardingValues;
export type OnboardingErrors = Partial<Record<OnboardingField, ProfileNameViolation>>;

export interface OnboardingStep {
  id: string;
  /** The step's route, an absolute typed literal. */
  href: Href;
  /** Key of the step title in the `onboarding` namespace. */
  titleKey: string;
  /** Violations of this step's own fields, leaving out the valid ones. */
  validate: (values: OnboardingValues) => OnboardingErrors;
}

function validateNameStep(values: OnboardingValues): OnboardingErrors {
  const errors: OnboardingErrors = {};
  const name = validateProfileName(values.name);
  const surname = validateProfileName(values.surname);

  if (name) {
    errors.name = name;
  }
  if (surname) {
    errors.surname = surname;
  }
  return errors;
}

/** The onboarding steps, in order. A new step is one more entry here, plus its route and view. */
const steps: OnboardingStep[] = [
  { id: "name", href: "/name", titleKey: "steps.name.title", validate: validateNameStep },
];

export default steps;
