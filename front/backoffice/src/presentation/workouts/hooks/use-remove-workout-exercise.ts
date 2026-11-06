import { useState } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";
import type { ExercisesOwner } from "@/presentation/workouts/exercises-owner";

interface RemoveWorkoutExerciseState {
  /** The workout entry being removed, while its request is in progress. */
  removingId: string | null;
  errorCode: DomainErrorCode | null;
  /** Resolves to `true` when the API removed the exercise. */
  remove: (workoutId: string, entryId: string, exerciseId: string) => Promise<boolean>;
}

/** Removes through the workout or the template use case, by `owner`; the owner id is passed as `workoutId`. */
export default function useRemoveWorkoutExercise(
  owner: ExercisesOwner = "workout",
): RemoveWorkoutExerciseState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [removingId, setRemovingId] = useState<string | null>(null);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function remove(workoutId: string, entryId: string, exerciseId: string) {
    setRemovingId(entryId);
    setErrorCode(null);

    const result = await runWithSession(() =>
      owner === "template"
        ? container
            .resolve("removeExerciseFromWorkoutTemplateUseCase")
            .execute({ templateId: workoutId, exerciseId })
        : container.resolve("removeExerciseFromWorkoutUseCase").execute({ workoutId, exerciseId }),
    );

    setRemovingId(null);
    if (result.ok) {
      return true;
    }
    setErrorCode(result.error.code);
    return false;
  }

  return { removingId, errorCode, remove };
}
