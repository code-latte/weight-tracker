import { createContext } from "react";
import type { DomainErrorCode, UserProfile } from "@weight-tracker/core/domain";

export type CurrentUserStatus = "idle" | "loading" | "loaded" | "error";

export interface CurrentUserContextValue {
  /** `idle` while signed out; `loading` until the first read of a session ends. */
  status: CurrentUserStatus;
  profile: UserProfile | null;
  errorCode: DomainErrorCode | null;
  /** True while `reload` runs; the previous status stays until its result arrives. */
  reloading: boolean;
  reload: () => Promise<void>;
  /** Stores the profile returned by a successful update. */
  setProfile: (profile: UserProfile) => void;
}

const CurrentUserContext = createContext<CurrentUserContextValue>({
  status: "idle",
  profile: null,
  errorCode: null,
  reloading: false,
  reload: async () => {},
  setProfile: () => {},
});

export default CurrentUserContext;
