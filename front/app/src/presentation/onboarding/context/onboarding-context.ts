import { createContext } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import type { OnboardingErrors, OnboardingField, OnboardingStep, OnboardingValues } from "../steps";

export interface OnboardingState {
  steps: OnboardingStep[];
  values: OnboardingValues;
  errors: OnboardingErrors;
  saving: boolean;
  /** Code of the last failed save, for the view to translate. */
  errorCode: DomainErrorCode | null;
  change: (field: OnboardingField, value: string) => void;
  /** Validates the step's own fields and keeps their errors; true when they are valid. */
  validateStep: (stepId: string) => boolean;
  /** Validates the step, then saves what every step collected. */
  finish: (stepId: string) => Promise<void>;
}

const OnboardingContext = createContext<OnboardingState | null>(null);

export default OnboardingContext;
