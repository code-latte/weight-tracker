import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, WorkoutTemplate } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type WorkoutTemplateStatus = "loading" | "loaded" | "failed";

interface WorkoutTemplateState {
  status: WorkoutTemplateStatus;
  template: WorkoutTemplate | null;
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

/**
 * Reads one template when the screen opens or its id changes. Without an id nothing is read and the template counts as
 * not found, so the screen has a single not-found path. Held by the screen only, so it resets on exit.
 */
export default function useWorkoutTemplate(id: string | null): WorkoutTemplateState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<WorkoutTemplateStatus>(id === null ? "failed" : "loading");
  const [template, setTemplate] = useState<WorkoutTemplate | null>(null);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(
    id === null ? "NOT_FOUND" : null,
  );
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; `reload` sets "loading" beforehand.
  const request = useCallback(() => {
    const current = ++generation.current;
    if (id === null) {
      return;
    }

    void runWithSession(() =>
      container.resolve("getWorkoutTemplateByIdUseCase").execute({ id }),
    ).then((result) => {
      if (current !== generation.current) {
        return;
      }
      if (result.ok) {
        setTemplate(result.value);
        setStatus("loaded");
      } else {
        setErrorCode(result.error.code);
        setStatus("failed");
      }
    });
  }, [container, runWithSession, id]);

  const reload = useCallback(() => {
    if (id === null) {
      return;
    }
    setStatus("loading");
    setErrorCode(null);
    request();
  }, [id, request]);

  useEffect(() => {
    request();
    return () => {
      generation.current += 1;
    };
  }, [request]);

  return { status, template, errorCode, reload };
}
