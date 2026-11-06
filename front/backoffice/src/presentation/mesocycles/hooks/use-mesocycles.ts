import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, Mesocycle } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type MesocyclesStatus = "loading" | "loaded" | "failed";

interface MesocyclesState {
  status: MesocyclesStatus;
  mesocycles: Mesocycle[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

/** Reads the caller's mesocycles when the screen opens. Held by the screen only, so it resets on exit. */
export default function useMesocycles(): MesocyclesState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<MesocyclesStatus>("loading");
  const [mesocycles, setMesocycles] = useState<Mesocycle[]>([]);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; the caller sets "loading" beforehand if it needs to.
  const request = useCallback(() => {
    const current = ++generation.current;

    void runWithSession(() => container.resolve("getMesocyclesUseCase").execute()).then(
      (result) => {
        if (current !== generation.current) {
          return;
        }
        if (result.ok) {
          setMesocycles(result.value);
          setStatus("loaded");
        } else {
          setErrorCode(result.error.code);
          setStatus("failed");
        }
      },
    );
  }, [container, runWithSession]);

  const reload = useCallback(() => {
    setStatus("loading");
    setErrorCode(null);
    request();
  }, [request]);

  // The initial state is already "loading", so opening the screen only sends the request.
  useEffect(() => {
    request();
    return () => {
      generation.current += 1;
    };
  }, [request]);

  return { status, mesocycles, errorCode, reload };
}
