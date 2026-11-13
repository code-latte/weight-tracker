import { useCallback, useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import type { ContainerInterface, Dependencies } from "@weight-tracker/core/application";
import type {
  DomainErrorCode,
  WorkoutById,
  WorkoutByIdExercise,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import type { SessionState } from "@/presentation/auth/context/session-context";
import useContainer from "@/presentation/common/hooks/use-container";
import type { ExerciseProgress } from "@/presentation/workouts/components/molecules/exercise-progress-icon";

/** One exercise of the workout as the screen shows it. An exercise without names leaves the name empty. */
export interface WorkoutExerciseRowData {
  id: string;
  name: string | null;
  type: string;
  bodyPart: string;
  muscle: string;
  progress: ExerciseProgress;
}

type WorkoutState =
  | { status: "loading" }
  | { status: "ready"; workout: WorkoutById }
  | { status: "error"; errorCode: DomainErrorCode };

/** The workout carries its exercises' catalog data and its mesocycle, so one read is enough. */
async function readWorkout(
  container: ContainerInterface<Dependencies>,
  runWithSession: SessionState["runWithSession"],
  id: string,
): Promise<WorkoutState> {
  const result = await runWithSession(() =>
    container.resolve("getWorkoutByIdUseCase").execute({ id }),
  );
  if (!result.ok) {
    return { status: "error", errorCode: result.error.code };
  }

  return { status: "ready", workout: result.value };
}

/**
 * Whether an exercise is done, under way or not started, from the set records against the prescribed sets. Each
 * prescription is `count × target`, so the sets are the sum of the counts.
 */
export function exerciseProgressOf(exercise: WorkoutByIdExercise): ExerciseProgress {
  const records = exercise.setRecords.length;
  if (records === 0) {
    return "notStarted";
  }
  const sets = exercise.sets.reduce((total, set) => total + Number(set.count), 0);

  return records >= sets ? "done" : "inProgress";
}

/** One workout with its exercises and mesocycle, read when the screen mounts and on retry. */
export default function useWorkout(id: string) {
  const container = useContainer();
  const { runWithSession } = useSession();
  const { i18n } = useTranslation();
  const [state, setState] = useState<WorkoutState>({ status: "loading" });

  useEffect(() => {
    let active = true;

    void readWorkout(container, runWithSession, id).then((next) => {
      if (active) {
        setState(next);
      }
    });

    return () => {
      active = false;
    };
  }, [container, runWithSession, id]);

  const retry = useCallback(() => {
    setState({ status: "loading" });
    void readWorkout(container, runWithSession, id).then(setState);
  }, [container, runWithSession, id]);

  /** Reads the workout again without leaving the ready state, so what is on screen does not flash. */
  const reload = useCallback(() => {
    void readWorkout(container, runWithSession, id).then((next) => {
      if (next.status === "ready") {
        setState(next);
      }
    });
  }, [container, runWithSession, id]);

  const rows = useMemo<WorkoutExerciseRowData[]>(() => {
    if (state.status !== "ready") {
      return [];
    }

    return state.workout.exercises.map((exercise) => {
      const translation =
        exercise.translations.find((candidate) => candidate.languageCode === i18n.language) ??
        exercise.translations[0];

      return {
        id: exercise.id,
        name: translation?.name ?? null,
        type: exercise.type,
        bodyPart: exercise.bodyPart,
        muscle: exercise.muscle,
        progress: exerciseProgressOf(exercise),
      };
    });
  }, [state, i18n.language]);

  const workout = state.status === "ready" ? state.workout : null;

  return {
    state,
    retry,
    reload,
    rows,
    done: rows.filter((row) => row.progress === "done").length,
    total: rows.length,
    mesocycleName: workout?.mesocycle?.name ?? null,
    mesocycleWeekNumber:
      workout?.mesocycleWeekNumber != null ? Number(workout.mesocycleWeekNumber) : null,
  };
}
