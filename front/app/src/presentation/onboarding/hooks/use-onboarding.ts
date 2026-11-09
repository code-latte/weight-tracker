import { use } from "react";
import OnboardingContext, { type OnboardingState } from "../context/onboarding-context";

export default function useOnboarding(): OnboardingState {
  const context = use(OnboardingContext);

  if (context === null) {
    throw new Error("useOnboarding must be used inside OnboardingProvider");
  }
  return context;
}
