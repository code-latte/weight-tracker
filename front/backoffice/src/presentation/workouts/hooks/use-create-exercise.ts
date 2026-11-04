import { useCallback, useState } from "react";
import type { CreateExerciseRequest, DomainErrorCode, Exercise } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface CreateExerciseState {
  creating: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to the created exercise, or `null` when the API refused it. */
  create: (request: CreateExerciseRequest) => Promise<Exercise | null>;
  reset: () => void;
}

export default function useCreateExercise(): CreateExerciseState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [creating, setCreating] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function create(request: CreateExerciseRequest) {
    setCreating(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("createExerciseUseCase").execute(request),
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
