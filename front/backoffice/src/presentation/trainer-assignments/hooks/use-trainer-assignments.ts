import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, UserSummary } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type TrainerAssignmentsStatus = "loading" | "loaded" | "failed";

interface TrainerAssignmentsState {
  status: TrainerAssignmentsStatus;
  /** The users with role `USER`, in the API's order, each with their current trainer. */
  athletes: UserSummary[];
  /** The active users with role `TRAINER`, in the API's order: the ones offered when assigning. */
  trainers: UserSummary[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

/**
 * Reads the users once when the screen opens and splits them by role. Held by the screen only, so it resets on exit.
 */
export default function useTrainerAssignments(): TrainerAssignmentsState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<TrainerAssignmentsStatus>("loading");
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; the caller sets "loading" beforehand if it needs to.
  const request = useCallback(() => {
    const current = ++generation.current;

    void runWithSession(() => container.resolve("getUsersUseCase").execute()).then((result) => {
      if (current !== generation.current) {
        return;
      }
      if (result.ok) {
        setUsers(result.value);
        setStatus("loaded");
      } else {
        setErrorCode(result.error.code);
        setStatus("failed");
      }
    });
  }, [container, runWithSession]);

  const reload = useCallback(() => {
    setStatus("loading");
    setErrorCode(null);
    request();
  }, [request]);

  // The initial state is already "loading", so opening the screen only sends the request.
  useEffect(() => {
    request();
    return () => {
      generation.current += 1;
    };
  }, [request]);

  const athletes = users.filter((user) => user.role === "USER");
  const trainers = users.filter((user) => user.role === "TRAINER" && user.isActive);

  return { status, athletes, trainers, errorCode, reload };
}
