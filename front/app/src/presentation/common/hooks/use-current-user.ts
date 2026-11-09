import { use } from "react";
import CurrentUserContext, { type CurrentUserContextValue } from "../context/current-user-context";

interface CurrentUser extends CurrentUserContextValue {
  /** The profile read has ended, with a profile or a failure. */
  settled: boolean;
  /**
   * The athlete still has to complete onboarding: the profile could not be read, or its name or surname is empty.
   * The only place this rule lives.
   */
  onboardingPending: boolean;
}

export default function useCurrentUser(): CurrentUser {
  const context = use(CurrentUserContext);
  const { status, profile } = context;

  const settled = status === "loaded" || status === "error";
  const onboardingPending =
    status === "error" ||
    (status === "loaded" &&
      profile !== null &&
      (profile.name.trim() === "" || profile.surname.trim() === ""));

  return { ...context, settled, onboardingPending };
}
