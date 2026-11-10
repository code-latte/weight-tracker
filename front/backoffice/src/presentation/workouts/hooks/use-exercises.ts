import { useEffect } from "react";
import type { DomainErrorCode, Exercise } from "@weight-tracker/core/domain";
import useExerciseCatalog from "@/presentation/common/hooks/use-exercise-catalog";

type ExercisesStatus = "loading" | "loaded" | "failed";

interface ExercisesState {
  status: ExercisesStatus;
  exercises: Exercise[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
  /** Appends an exercise created on this screen, so it is listed without waiting for the catalog to be read again. */
  add: (exercise: Exercise) => void;
  /** Reads the catalog again in the background, after the user created or updated an exercise of their own. */
  refresh: () => void;
}

/**
 * The session's exercise catalog, as a screen that needs it sees it. The catalog is read once per session (in the
 * background after sign-in); a screen opened before that read starts it, and never reads it again on its own.
 */
export default function useExercises(): ExercisesState {
  const { status, exercises, errorCode, load, reload, add, refresh } = useExerciseCatalog();

  // React runs a screen's effects before the shell's, so the screen asks too; only the first ask reads.
  useEffect(() => {
    load();
  }, [load]);

  return {
    status: status === "idle" ? "loading" : status,
    exercises,
    errorCode,
    reload,
    add,
    refresh,
  };
}
