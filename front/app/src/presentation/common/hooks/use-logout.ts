import { useState } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

/** Ends the session. On success the access gate follows the reloaded session; nothing navigates here. */
export default function useLogout() {
  const container = useContainer();
  const session = useSession();
  const [loggingOut, setLoggingOut] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function logout() {
    setLoggingOut(true);
    setErrorCode(null);

    const result = await container.resolve("logoutUseCase").execute();

    if (result.ok) {
      await session.reload();
      return;
    }

    setErrorCode(result.error.code);
    setLoggingOut(false);
  }

  return { loggingOut, errorCode, logout };
}
