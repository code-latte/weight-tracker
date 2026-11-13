import { useCallback, useRef, useState } from "react";
import type { DomainErrorCode, ExerciseLastPerformance } from "@weight-tracker/core/domain";
import useContainer from "@/presentation/common/hooks/use-container";
import useSession from "@/presentation/auth/hooks/use-session";

/** `ready` with `null` is a user who never did the exercise, which is not an error. */
export type LastPerformanceState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; performance: ExerciseLastPerformance | null }
  | { status: "error"; errorCode: DomainErrorCode };

/**
 * The signed-in user's last performance of a catalog exercise, read each time `load` is called and not kept: `reset`
 * returns to idle, and an answer that arrives after a newer `load` or a `reset` is dropped. A failed read leaves the
 * state in `error`, which the screen treats as having nothing to show.
 */
export default function useExerciseLastPerformance() {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [state, setState] = useState<LastPerformanceState>({ status: "idle" });
  const latestRead = useRef(0);

  const load = useCallback(
    async (exerciseId: string) => {
      const read = ++latestRead.current;
      setState({ status: "loading" });

      const result = await runWithSession(() =>
        container.resolve("getMyExerciseLastPerformanceUseCase").execute(exerciseId),
      );
      if (read !== latestRead.current) {
        return;
      }

      setState(
        result.ok
          ? { status: "ready", performance: result.value }
          : { status: "error", errorCode: result.error.code },
      );
    },
    [container, runWithSession],
  );

  const reset = useCallback(() => {
    latestRead.current += 1;
    setState({ status: "idle" });
  }, []);

  return { state, load, reset };
}
