import { useCallback, useEffect, useMemo, useState } from "react";
import type { ContainerInterface, Dependencies } from "@weight-tracker/core/application";
import type { DomainErrorCode, WorkoutListItem } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import type { SessionState } from "@/presentation/auth/context/session-context";
import useContainer from "@/presentation/common/hooks/use-container";
import { addWeeks, isInWeek, weekStartOf } from "@/presentation/common/format/week";

type WorkoutsState =
  | { status: "loading" }
  | { status: "ready"; workouts: WorkoutListItem[] }
  | { status: "error"; errorCode: DomainErrorCode };

const DONE_STATUSES = ["COMPLETED", "REVIEWED"];

/** A pull-to-refresh shows its spinner at least this long, so a fast answer does not just flash it. */
export const MIN_REFRESH_MS = 800;

function wait(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function readWorkouts(
  container: ContainerInterface<Dependencies>,
  runWithSession: SessionState["runWithSession"],
): Promise<WorkoutsState> {
  const result = await runWithSession(() => container.resolve("getWorkoutsUseCase").execute({}));
  if (!result.ok) {
    return { status: "error", errorCode: result.error.code };
  }

  return { status: "ready", workouts: result.value };
}

/** Whether a workout counts as done in the week's progress. */
export function isDone(workout: WorkoutListItem): boolean {
  return DONE_STATUSES.includes(workout.status);
}

/**
 * The athlete's workouts, read when the screen mounts, on retry and on pull-to-refresh, seen one week at a time.
 * Changing week filters what was read; it never reads again.
 */
export default function useWeekWorkouts() {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [state, setState] = useState<WorkoutsState>({ status: "loading" });
  const [weekStart, setWeekStart] = useState(() => weekStartOf(new Date()));
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    let active = true;

    void readWorkouts(container, runWithSession).then((next) => {
      if (active) {
        setState(next);
      }
    });

    return () => {
      active = false;
    };
  }, [container, runWithSession]);

  const retry = useCallback(() => {
    setState({ status: "loading" });
    void readWorkouts(container, runWithSession).then(setState);
  }, [container, runWithSession]);

  /** Reads again without any sign of it, for when the screen regains focus. A failed read keeps what is shown. */
  const reload = useCallback(() => {
    void readWorkouts(container, runWithSession).then((next) => {
      if (next.status === "ready") {
        setState(next);
      }
    });
  }, [container, runWithSession]);

  /** Reads again while the list stays on screen; the viewed week is kept. */
  const refresh = useCallback(() => {
    setRefreshing(true);
    void Promise.all([readWorkouts(container, runWithSession), wait(MIN_REFRESH_MS)]).then(
      ([next]) => {
        setState(next);
        setRefreshing(false);
      },
    );
  }, [container, runWithSession]);

  const previousWeek = useCallback(() => setWeekStart((current) => addWeeks(current, -1)), []);
  const nextWeek = useCallback(() => setWeekStart((current) => addWeeks(current, 1)), []);

  const workouts = useMemo(
    () =>
      state.status === "ready"
        ? state.workouts
            .filter((workout) => isInWeek(workout.workoutDate, weekStart))
            .sort((a, b) => a.workoutDate.localeCompare(b.workoutDate))
        : [],
    [state, weekStart],
  );

  const currentWeekStart = weekStartOf(new Date());

  return {
    state,
    retry,
    reload,
    refreshing,
    refresh,
    weekStart,
    workouts,
    completed: workouts.filter(isDone).length,
    total: workouts.length,
    isCurrentWeek: weekStart === currentWeekStart,
    isNextWeek: weekStart === addWeeks(currentWeekStart, 1),
    previousWeek,
    nextWeek,
  };
}
