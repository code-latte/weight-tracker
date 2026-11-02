import { useCallback, useState } from "react";
import type {
  CreatedAthleteInvitation,
  DomainErrorCode,
  InviteAthleteRequest,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface InviteAthleteState {
  inviting: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to the created invitation, or `null` when the API refused it. */
  invite: (request: InviteAthleteRequest) => Promise<CreatedAthleteInvitation | null>;
  reset: () => void;
}

export default function useInviteAthlete(): InviteAthleteState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [inviting, setInviting] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function invite(request: InviteAthleteRequest) {
    setInviting(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("inviteAthleteUseCase").execute(request),
    );

    setInviting(false);
    if (result.ok) {
      return result.value;
    }
    setErrorCode(result.error.code);
    return null;
  }

  const reset = useCallback(() => {
    setInviting(false);
    setErrorCode(null);
  }, []);

  return { inviting, errorCode, invite, reset };
}
