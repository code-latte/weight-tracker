import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, Mesocycle } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type MesocycleStatus = "loading" | "loaded" | "failed";

interface MesocycleState {
  status: MesocycleStatus;
  mesocycle: Mesocycle | null;
  errorCode: DomainErrorCode | null;
  reload: () => void;
  /** Takes the mesocycle a write answered with, so the screen never reads it again after a save. */
  replace: (mesocycle: Mesocycle) => void;
}

/** Reads one mesocycle when the screen opens or its id changes. Held by the screen only, so it resets on exit. */
export default function useMesocycle(id: string): MesocycleState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<MesocycleStatus>("loading");
  const [mesocycle, setMesocycle] = useState<Mesocycle | null>(null);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; `reload` sets "loading" beforehand.
  const request = useCallback(() => {
    const current = ++generation.current;

    void runWithSession(() => container.resolve("getMesocycleByIdUseCase").execute({ id })).then(
      (result) => {
        if (current !== generation.current) {
          return;
        }
        if (result.ok) {
          setMesocycle(result.value);
          setStatus("loaded");
        } else {
          setErrorCode(result.error.code);
          setStatus("failed");
        }
      },
    );
  }, [container, runWithSession, id]);

  const reload = useCallback(() => {
    setStatus("loading");
    setErrorCode(null);
    request();
  }, [request]);

  useEffect(() => {
    request();
    return () => {
      generation.current += 1;
    };
  }, [request]);

  return { status, mesocycle, errorCode, reload, replace: setMesocycle };
}
