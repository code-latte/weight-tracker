import { useCallback, useState } from "react";
import type { DomainErrorCode, TrainerAthleteInput } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface EndTrainerAthleteState {
  submitting: boolean;
  errorCode: DomainErrorCode | null;
  /** Ends the relationship between the trainer and the athlete. Resolves to `null` once ended, or to the error code. */
  end: (input: TrainerAthleteInput) => Promise<DomainErrorCode | null>;
  reset: () => void;
}

export default function useEndTrainerAthlete(): EndTrainerAthleteState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [submitting, setSubmitting] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function end(input: TrainerAthleteInput) {
    setSubmitting(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("endTrainerAthleteUseCase").execute(input),
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

  return { submitting, errorCode, end, reset };
}
