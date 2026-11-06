import { useState } from "react";
import type {
  CreateMesocycleRequest,
  DomainErrorCode,
  Mesocycle,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface CreateMesocycleState {
  creating: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to the created mesocycle, or `null` when the API refused it. */
  create: (request: CreateMesocycleRequest) => Promise<Mesocycle | null>;
}

export default function useCreateMesocycle(): CreateMesocycleState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [creating, setCreating] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function create(request: CreateMesocycleRequest) {
    setCreating(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("createMesocycleUseCase").execute(request),
    );

    setCreating(false);
    if (result.ok) {
      return result.value;
    }
    setErrorCode(result.error.code);
    return null;
  }

  return { creating, errorCode, create };
}
