import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, WorkoutSummary } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type WorkoutSummaryStatus = "idle" | "loading" | "loaded" | "failed";

interface WorkoutSummaryState {
  status: WorkoutSummaryStatus;
  summary: WorkoutSummary | null;
  errorCode: DomainErrorCode | null;
  /** Reads the summary the first time it is called, and does nothing afterwards. */
  load: () => void;
  /** Reads the summary again, after a failure. */
  reload: () => void;
}

/**
 * Reads the summary of one workout, only when `load` is first called (the metrics tab opening), and keeps it while the
 * screen lives. Held by the screen only, so it resets on exit.
 */
export default function useWorkoutSummary(workoutId: string): WorkoutSummaryState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<WorkoutSummaryStatus>("idle");
  const [summary, setSummary] = useState<WorkoutSummary | null>(null);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);
  const started = useRef(false);

  const request = useCallback(() => {
    const current = ++generation.current;

    setStatus("loading");
    setErrorCode(null);
    void runWithSession(() =>
      container.resolve("getWorkoutSummaryUseCase").execute({ workoutId }),
    ).then((result) => {
      if (current !== generation.current) {
        return;
      }
      if (result.ok) {
        setSummary(result.value);
        setStatus("loaded");
      } else {
        setErrorCode(result.error.code);
        setStatus("failed");
      }
    });
  }, [container, runWithSession, workoutId]);

  const load = useCallback(() => {
    if (started.current) {
      return;
    }
    started.current = true;
    request();
  }, [request]);

  useEffect(
    () => () => {
      generation.current += 1;
    },
    [],
  );

  return { status, summary, errorCode, load, reload: request };
}
