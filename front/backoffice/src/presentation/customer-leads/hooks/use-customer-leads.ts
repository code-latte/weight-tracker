import { useCallback, useEffect, useRef, useState } from "react";
import type { CustomerLead, DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type CustomerLeadsStatus = "loading" | "loaded" | "failed";

interface CustomerLeadsState {
  status: CustomerLeadsStatus;
  leads: CustomerLead[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

/** Reads every customer lead when the screen opens. Held by the screen only, so it resets on exit. */
export default function useCustomerLeads(): CustomerLeadsState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<CustomerLeadsStatus>("loading");
  const [leads, setLeads] = useState<CustomerLead[]>([]);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; the caller sets "loading" beforehand if it needs to.
  const request = useCallback(() => {
    const current = ++generation.current;

    void runWithSession(() => container.resolve("getCustomerLeadsUseCase").execute()).then(
      (result) => {
        if (current !== generation.current) {
          return;
        }
        if (result.ok) {
          setLeads(result.value);
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

  return { status, leads, errorCode, reload };
}
