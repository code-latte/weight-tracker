import { useCallback, useState } from "react";
import type { DomainErrorCode, TrainerAthleteInput } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface AssignTrainerState {
  submitting: boolean;
  errorCode: DomainErrorCode | null;
  /** Links the athlete to the trainer. Resolves to `null` once linked, or to the error code when the API refused. */
  assign: (input: TrainerAthleteInput) => Promise<DomainErrorCode | null>;
  reset: () => void;
}

export default function useAssignTrainer(): AssignTrainerState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [submitting, setSubmitting] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function assign(input: TrainerAthleteInput) {
    setSubmitting(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("linkAthleteToTrainerUseCase").execute(input),
    );

    setSubmitting(false);
    if (result.ok) {
      return null;
    }
    setErrorCode(result.error.code);
    return result.error.code;
  }

  const reset = useCallback(() => {
    setSubmitting(false);
    setErrorCode(null);
  }, []);

  return { submitting, errorCode, assign, reset };
}
