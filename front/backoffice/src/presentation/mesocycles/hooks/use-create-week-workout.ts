import { useCallback, useState } from "react";
import type { CreateWorkoutRequest, DomainErrorCode, Workout } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface CreateWeekWorkoutState {
  creating: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to the created workout, or `null` when the API refused it. */
  create: (request: CreateWorkoutRequest) => Promise<Workout | null>;
  reset: () => void;
}

/** Creates a workout placed in a mesocycle week. The API dates it from the week and checks the placement. */
export default function useCreateWeekWorkout(): CreateWeekWorkoutState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [creating, setCreating] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function create(request: CreateWorkoutRequest) {
    setCreating(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("createWorkoutUseCase").execute(request),
    );

    setCreating(false);
    if (result.ok) {
      return result.value;
    }
    setErrorCode(result.error.code);
    return null;
  }

  const reset = useCallback(() => {
    setCreating(false);
    setErrorCode(null);
  }, []);

  return { creating, errorCode, create, reset };
}
