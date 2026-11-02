import { useCallback, useState } from "react";
import type { AthleteInvitation, DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface RevokeInvitationState {
  submitting: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to `null` once the API has revoked the invitation, or to the error code when it refused. */
  revoke: (invitation: AthleteInvitation) => Promise<DomainErrorCode | null>;
  reset: () => void;
}

export default function useRevokeInvitation(): RevokeInvitationState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [submitting, setSubmitting] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function revoke(invitation: AthleteInvitation) {
    setSubmitting(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      container.resolve("revokeAthleteInvitationUseCase").execute(invitation.id),
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

  return { submitting, errorCode, revoke, reset };
}
