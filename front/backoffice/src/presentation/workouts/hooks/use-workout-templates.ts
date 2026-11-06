import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, WorkoutTemplate } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type WorkoutTemplatesStatus = "loading" | "loaded" | "failed";

interface WorkoutTemplatesState {
  status: WorkoutTemplatesStatus;
  templates: WorkoutTemplate[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

/**
 * Reads the templates the caller may see when the screen opens: a trainer's own, every template for an admin. Held by
 * the screen only, so it resets on exit.
 */
export default function useWorkoutTemplates(): WorkoutTemplatesState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<WorkoutTemplatesStatus>("loading");
  const [templates, setTemplates] = useState<WorkoutTemplate[]>([]);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; the caller sets "loading" beforehand if it needs to.
  const request = useCallback(() => {
    const current = ++generation.current;

    void runWithSession(() => container.resolve("getWorkoutTemplatesUseCase").execute({})).then(
      (result) => {
        if (current !== generation.current) {
          return;
        }
        if (result.ok) {
          setTemplates(result.value);
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

  return { status, templates, errorCode, reload };
}
