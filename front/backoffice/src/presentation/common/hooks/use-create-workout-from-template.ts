import { useCallback, useState } from "react";
import type {
  CreateWorkoutFromTemplateInput,
  DomainErrorCode,
  Workout,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface CreateWorkoutFromTemplateState {
  creating: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to the created workout, or `null` when the API refused it. */
  create: (input: CreateWorkoutFromTemplateInput) => Promise<Workout | null>;
  reset: () => void;
}

export default function useCreateWorkoutFromTemplate(): CreateWorkoutFromTemplateState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [creating, setCreating] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function create(input: CreateWorkoutFromTemplateInput) {
    setCreating(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("createWorkoutFromTemplateUseCase").execute(input),
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
