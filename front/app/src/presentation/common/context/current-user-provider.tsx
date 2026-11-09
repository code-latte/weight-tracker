import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import type { DomainErrorCode, UserProfile } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "../hooks/use-container";
import CurrentUserContext, { type CurrentUserStatus } from "./current-user-context";

interface CurrentUserProviderProps {
  children: ReactNode;
}

interface CurrentUserSnapshot {
  status: CurrentUserStatus;
  profile: UserProfile | null;
  errorCode: DomainErrorCode | null;
}

/** The signed-in athlete's profile, read when the session starts and cleared when it ends. */
export default function CurrentUserProvider({ children }: CurrentUserProviderProps) {
  const container = useContainer();
  const { status: sessionStatus, runWithSession } = useSession();
  // The result of the last read for the current session; null until that read ends.
  const [snapshot, setSnapshot] = useState<CurrentUserSnapshot | null>(null);
  const [reloading, setReloading] = useState(false);
  const [trackedSession, setTrackedSession] = useState(sessionStatus);

  // A session change discards what was read for the previous one, during render rather than in an effect.
  if (trackedSession !== sessionStatus) {
    setTrackedSession(sessionStatus);
    setSnapshot(null);
  }

  const read = useCallback(async (): Promise<CurrentUserSnapshot> => {
    // Through the session, so an expired access token is renewed and a rejected refresh signs out.
    const result = await runWithSession(() => container.resolve("getMyProfileUseCase").execute());

    return result.ok
      ? { status: "loaded", profile: result.value, errorCode: null }
      : { status: "error", profile: null, errorCode: result.error.code };
  }, [container, runWithSession]);

  useEffect(() => {
    if (sessionStatus !== "signedIn") {
      return;
    }

    let active = true;
    void read().then((next) => {
      if (active) {
        setSnapshot(next);
      }
    });

    return () => {
      active = false;
    };
  }, [sessionStatus, read]);

  const reload = useCallback(async () => {
    setReloading(true);
    const next = await read();
    setSnapshot(next);
    setReloading(false);
  }, [read]);

  const setProfile = useCallback((profile: UserProfile) => {
    setSnapshot({ status: "loaded", profile, errorCode: null });
  }, []);

  const value = useMemo(() => {
    const current: CurrentUserSnapshot =
      sessionStatus !== "signedIn"
        ? { status: "idle", profile: null, errorCode: null }
        : (snapshot ?? { status: "loading", profile: null, errorCode: null });

    return { ...current, reloading, reload, setProfile };
  }, [sessionStatus, snapshot, reloading, reload, setProfile]);

  return <CurrentUserContext value={value}>{children}</CurrentUserContext>;
}
