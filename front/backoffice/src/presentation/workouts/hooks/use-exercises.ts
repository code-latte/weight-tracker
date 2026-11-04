import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, Exercise } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type ExercisesStatus = "loading" | "loaded" | "failed";

interface ExercisesState {
  status: ExercisesStatus;
  exercises: Exercise[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
  /** Appends an exercise created on this screen, so it is listed without reading the catalog again. */
  add: (exercise: Exercise) => void;
}

/** Reads the exercise catalog when the screen opens. Held by the screen only, so it resets on exit. */
export default function useExercises(): ExercisesState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<ExercisesStatus>("loading");
  const [exercises, setExercises] = useState<Exercise[]>([]);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; `reload` sets "loading" beforehand.
  const request = useCallback(() => {
    const current = ++generation.current;

    void runWithSession(() => container.resolve("getExercisesUseCase").execute()).then((result) => {
      if (current !== generation.current) {
        return;
      }
      if (result.ok) {
        setExercises(result.value);
        setStatus("loaded");
      } else {
        setErrorCode(result.error.code);
        setStatus("failed");
      }
    });
  }, [container, runWithSession]);

  const reload = useCallback(() => {
    setStatus("loading");
    setErrorCode(null);
    request();
  }, [request]);

  const add = useCallback((exercise: Exercise) => {
    setExercises((current) => [...current, exercise]);
  }, []);

  useEffect(() => {
    request();
    return () => {
      generation.current += 1;
    };
  }, [request]);

  return { status, exercises, errorCode, reload, add };
}
