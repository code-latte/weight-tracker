import { useCallback, useState } from "react";
import type {
  CreateWorkoutTemplateRequest,
  DomainErrorCode,
  WorkoutTemplate,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface CreateWorkoutTemplateState {
  creating: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to the created template, or `null` when the API refused it. */
  create: (request: CreateWorkoutTemplateRequest) => Promise<WorkoutTemplate | null>;
  reset: () => void;
}

export default function useCreateWorkoutTemplate(): CreateWorkoutTemplateState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [creating, setCreating] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function create(request: CreateWorkoutTemplateRequest) {
    setCreating(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("createWorkoutTemplateUseCase").execute(request),
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
