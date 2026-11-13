import { useCallback, useState } from "react";
import type { DomainErrorCode, WorkoutStatus } from "@weight-tracker/core/domain";
import useContainer from "@/presentation/common/hooks/use-container";

/** Moves a workout to another status (start, finish). Holds whether the change is in flight and its last error. It never throws. */
export default function useChangeWorkoutStatus(workoutId: string) {
  const container = useContainer();
  const [changing, setChanging] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  /** Resolves `true` when the API made the change, so the caller can read the workout again. */
  const change = useCallback(
    async (status: WorkoutStatus): Promise<boolean> => {
      setChanging(true);
      setErrorCode(null);

      const result = await container
        .resolve("changeWorkoutStatusUseCase")
        .execute({ workoutId, status });

      setChanging(false);
      if (result.ok) {
        return true;
      }
      setErrorCode(result.error.code);

      return false;
    },
    [container, workoutId],
  );

  return { change, changing, errorCode };
}
