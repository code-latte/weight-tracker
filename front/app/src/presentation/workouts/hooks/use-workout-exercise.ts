import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import type { ExerciseProgress } from "@/presentation/workouts/components/molecules/exercise-progress-icon";
import useWorkout, { exerciseProgressOf } from "@/presentation/workouts/hooks/use-workout";
import {
  englishSubtitleOf,
  expandSets,
  nameOf,
  type SetRow,
} from "@/presentation/workouts/set-rows";

/** One entry of the navigator: an exercise of the workout with its progress. */
export interface ExerciseNavigatorEntry {
  id: string;
  name: string | null;
  progress: ExerciseProgress;
}

/** The exercise the screen shows, with everything it needs already derived. */
export interface WorkoutExerciseData {
  /** The catalog exercise id, the one the set record routes take. */
  exerciseId: string;
  name: string | null;
  subtitle: string | null;
  bodyPart: string;
  muscle: string;
  videoUrl: string | null;
  restMinutes: number | null;
  rows: SetRow[];
}

/**
 * One exercise of a workout, read through the workout so a deep link works. An id that is not in the workout is a
 * `NOT_FOUND`, shown like any failed read.
 */
export default function useWorkoutExercise(workoutId: string, workoutExerciseId: string) {
  const { i18n } = useTranslation();
  const { state, retry, reload } = useWorkout(workoutId);
  const language = i18n.language;

  const data = useMemo(() => {
    if (state.status !== "ready") {
      return null;
    }
    const { workout } = state;
    const current = workout.exercises.find((exercise) => exercise.id === workoutExerciseId);
    const items: ExerciseNavigatorEntry[] = workout.exercises.map((exercise) => ({
      id: exercise.id,
      name: nameOf(exercise, language),
      progress: exerciseProgressOf(exercise),
    }));

    if (current === undefined) {
      return { workout, items, exercise: null };
    }
    const name = nameOf(current, language);
    const exercise: WorkoutExerciseData = {
      exerciseId: current.exerciseId,
      name,
      subtitle: englishSubtitleOf(current, language, name),
      bodyPart: current.bodyPart,
      muscle: current.muscle,
      videoUrl: current.videoUrl,
      restMinutes: current.restMinutes === null ? null : Number(current.restMinutes),
      rows: expandSets(current),
    };

    return { workout, items, exercise };
  }, [state, workoutExerciseId, language]);

  const errorCode: DomainErrorCode | null =
    state.status === "error"
      ? state.errorCode
      : data !== null && data.exercise === null
        ? "NOT_FOUND"
        : null;

  return {
    status:
      errorCode !== null
        ? ("error" as const)
        : state.status === "ready"
          ? ("ready" as const)
          : ("loading" as const),
    errorCode,
    retry,
    reload,
    workoutName: data?.workout.name ?? null,
    workoutStatus: data?.workout.status ?? null,
    items: data?.items ?? [],
    exercise: data?.exercise ?? null,
  };
}
