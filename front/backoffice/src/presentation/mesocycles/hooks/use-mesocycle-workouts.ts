import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { DomainErrorCode, WorkoutListItem } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type MesocycleWorkoutsStatus = "loading" | "loaded" | "failed";

export interface MesocycleWorkoutsState {
  status: MesocycleWorkoutsStatus;
  /** The workouts under their week number, each week keeping the API's order. Empty until they load. */
  byWeek: ReadonlyMap<number, WorkoutListItem[]>;
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

/** Groups the workouts by the week they carry; which workouts belong to the mesocycle is the API's answer. */
function byWeekOf(workouts: WorkoutListItem[]): Map<number, WorkoutListItem[]> {
  const groups = new Map<number, WorkoutListItem[]>();

  for (const workout of workouts) {
    // The contract may carry the week as a numeric string.
    const week = Number(workout.mesocycleWeekNumber);
    groups.set(week, [...(groups.get(week) ?? []), workout]);
  }
  return groups;
}

/**
 * Reads the workouts of one mesocycle when the mesocycle is shown. Held by the screen only, so it resets on exit.
 */
export default function useMesocycleWorkouts(mesocycleId: string): MesocycleWorkoutsState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<MesocycleWorkoutsStatus>("loading");
  const [workouts, setWorkouts] = useState<WorkoutListItem[]>([]);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; `reload` sets "loading" beforehand.
  const request = useCallback(() => {
    const current = ++generation.current;

    void runWithSession(() =>
      container.resolve("getMesocycleWorkoutsUseCase").execute({ mesocycleId }),
    ).then((result) => {
      if (current !== generation.current) {
        return;
      }
      if (result.ok) {
        setWorkouts(result.value);
        setStatus("loaded");
      } else {
        setErrorCode(result.error.code);
        setStatus("failed");
      }
    });
  }, [container, runWithSession, mesocycleId]);

  const reload = useCallback(() => {
    setStatus("loading");
    setErrorCode(null);
    request();
  }, [request]);

  // The initial state is already "loading", so showing the mesocycle only sends the request.
  useEffect(() => {
    request();
    return () => {
      generation.current += 1;
    };
  }, [request]);

  const byWeek = useMemo(() => byWeekOf(workouts), [workouts]);

  return { status, byWeek, errorCode, reload };
}
