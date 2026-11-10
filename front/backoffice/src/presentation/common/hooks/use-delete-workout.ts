import { useState } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface DeleteWorkoutState {
  deleting: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to `true` when the API deleted the workout. */
  remove: (id: string) => Promise<boolean>;
}

export default function useDeleteWorkout(): DeleteWorkoutState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [deleting, setDeleting] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function remove(id: string) {
    setDeleting(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("deleteWorkoutUseCase").execute({ id }),
    );

    setDeleting(false);
    if (result.ok) {
      return true;
    }
    setErrorCode(result.error.code);
    return false;
  }

  return { deleting, errorCode, remove };
}
