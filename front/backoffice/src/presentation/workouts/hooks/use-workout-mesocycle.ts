import { useEffect, useState } from "react";
import type { Mesocycle } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

/** The mesocycle of a placed workout: still loading, read, failed, or none for a standalone workout. */
export type WorkoutMesocycleState =
  | { status: "none" }
  | { status: "loading" }
  | { status: "loaded"; mesocycle: Mesocycle }
  | { status: "failed" };

/**
 * Reads the mesocycle a workout is placed in, once, to name it beside the workout. A standalone workout reads nothing.
 * The read is informative only, so a failure is not retried. Held by the screen only, so it resets on exit.
 */
export default function useWorkoutMesocycle(mesocycleId: string | null): WorkoutMesocycleState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [state, setState] = useState<WorkoutMesocycleState>(
    mesocycleId === null ? { status: "none" } : { status: "loading" },
  );

  useEffect(() => {
    if (mesocycleId === null) {
      return;
    }
    // Set on unmount or a new id, so an answer that arrives after either is ignored.
    let stale = false;

    void runWithSession(() =>
      container.resolve("getMesocycleByIdUseCase").execute({ id: mesocycleId }),
    ).then((result) => {
      if (stale) {
        return;
      }
      setState(result.ok ? { status: "loaded", mesocycle: result.value } : { status: "failed" });
    });
    return () => {
      stale = true;
    };
  }, [container, runWithSession, mesocycleId]);

  return state;
}
