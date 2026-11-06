import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainError, DomainErrorCode, Result } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

type AssigneesStatus = "loading" | "loaded" | "failed";

/** A person a workout or a mesocycle can be given to or prescribed by, whichever endpoint they came from. */
export interface Assignee {
  id: string;
  name: string;
  surname: string;
  email: string;
}

interface AssigneesState {
  status: AssigneesStatus;
  athletes: Assignee[];
  /** Always empty for a trainer: only an admin chooses the trainer. */
  trainers: Assignee[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

interface Assignees {
  athletes: Assignee[];
  trainers: Assignee[];
}

function toAssignee({ id, name, surname, email }: Assignee): Assignee {
  return { id, name, surname, email };
}

function mapResult<T, U>(
  result: Result<T, DomainError>,
  map: (value: T) => U,
): Result<U, DomainError> {
  return result.ok ? { ok: true, value: map(result.value) } : result;
}

/**
 * Reads the people a create form offers when it opens: a trainer's current athletes, or, for an admin, the
 * active athletes and trainers of one users request. It waits while the role is unknown. Held by the form only, so
 * it resets when the form goes away.
 */
export default function useAssignees(role: "ADMIN" | "TRAINER" | null): AssigneesState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [status, setStatus] = useState<AssigneesStatus>("loading");
  const [assignees, setAssignees] = useState<Assignees>({ athletes: [], trainers: [] });
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives; the caller sets "loading" beforehand if it needs to.
  const request = useCallback(() => {
    const current = ++generation.current;
    // Until the profile says who is asking, there is nothing to request.
    if (role === null) {
      return;
    }
    void runWithSession(async (): Promise<Result<Assignees, DomainError>> => {
      if (role === "ADMIN") {
        const users = await container.resolve("getUsersUseCase").execute();

        return mapResult(users, (list) => ({
          athletes: list.filter((user) => user.role === "USER" && user.isActive).map(toAssignee),
          trainers: list.filter((user) => user.role === "TRAINER" && user.isActive).map(toAssignee),
        }));
      }

      const athletes = await container.resolve("getMyAthletesUseCase").execute();

      return mapResult(athletes, (list) => ({
        athletes: list.map(({ athleteId, ...person }) => toAssignee({ ...person, id: athleteId })),
        trainers: [],
      }));
    }).then((result) => {
      if (current !== generation.current) {
        return;
      }
      if (result.ok) {
        setAssignees(result.value);
        setStatus("loaded");
      } else {
        setErrorCode(result.error.code);
        setStatus("failed");
      }
    });
  }, [container, role, runWithSession]);

  const reload = useCallback(() => {
    setStatus("loading");
    setErrorCode(null);
    request();
  }, [request]);

  // The initial state is already "loading", so opening the dialog only sends the request.
  useEffect(() => {
    request();
    return () => {
      generation.current += 1;
    };
  }, [request]);

  return { status, ...assignees, errorCode, reload };
}
