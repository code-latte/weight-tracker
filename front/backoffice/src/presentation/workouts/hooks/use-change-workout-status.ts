import { useState } from "react";
import type { DomainErrorCode, WorkoutDetail, WorkoutStatus } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface ChangeWorkoutStatusState {
  changing: boolean;
  errorCode: DomainErrorCode | null;
  /** The status the last accepted change left the workout in, until cleared. */
  changed: WorkoutDetail["status"] | null;
  /** Resolves to the status the API returned, or `null` when it refused the change. */
  change: (workoutId: string, status: WorkoutStatus) => Promise<WorkoutDetail["status"] | null>;
  clear: () => void;
}

export default function useChangeWorkoutStatus(): ChangeWorkoutStatusState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [changing, setChanging] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  const [changed, setChanged] = useState<WorkoutDetail["status"] | null>(null);

  async function change(workoutId: string, status: WorkoutStatus) {
    setChanging(true);
    setErrorCode(null);
    setChanged(null);

    const result = await runWithSession(() =>
      container.resolve("changeWorkoutStatusUseCase").execute({ workoutId, status }),
    );

    setChanging(false);
    if (result.ok) {
      setChanged(result.value.status);
      return result.value.status;
    }
    setErrorCode(result.error.code);
    return null;
  }

  return {
    changing,
    errorCode,
    changed,
    change,
    clear: () => {
      setChanged(null);
      setErrorCode(null);
    },
  };
}
