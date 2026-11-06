import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, WorkoutById } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type WorkoutStatus = "loading" | "loaded" | "failed";

interface WorkoutState {
  status: WorkoutStatus;
  workout: WorkoutById | null;
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

/**
 * Reads one workout when the screen opens or its id changes. Without an id nothing is read and the workout counts as
 * not found, so the screen has a single not-found path. Held by the screen only, so it resets on exit.
 */
export default function useWorkout(id: string | null): WorkoutState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<WorkoutStatus>(id === null ? "failed" : "loading");
  const [workout, setWorkout] = useState<WorkoutById | null>(null);
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

    void runWithSession(() => container.resolve("getWorkoutByIdUseCase").execute({ id })).then(
      (result) => {
        if (current !== generation.current) {
          return;
        }
        if (result.ok) {
          setWorkout(result.value);
          setStatus("loaded");
        } else {
          setErrorCode(result.error.code);
          setStatus("failed");
        }
      },
    );
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

  return { status, workout, errorCode, reload };
}
