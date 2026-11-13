import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, ExerciseHistorySession } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

/** How many of the latest sessions the evolution charts show. */
const HISTORY_LIMIT = 5;

type ExerciseHistoryStatus = "idle" | "loading" | "loaded" | "failed";

interface ExerciseHistoryState {
  status: ExerciseHistoryStatus;
  sessions: ExerciseHistorySession[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

type Answer =
  | { key: string; sessions: ExerciseHistorySession[]; errorCode: null }
  | { key: string; sessions: []; errorCode: DomainErrorCode };

/**
 * Reads the latest sessions of one exercise for one user, every time the exercise changes. Without an exercise nothing
 * is read. The answer is kept with the exercise it belongs to, so while a new exercise is read the state is "loading"
 * and never shows the previous exercise's sessions. Held by the screen only, so it resets on exit.
 */
export default function useExerciseHistory(
  userId: string,
  exerciseId: string | null,
): ExerciseHistoryState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [answer, setAnswer] = useState<Answer | null>(null);
  // Bumped by every read and on cleanup, so an answer that arrives after either is ignored.
  const generation = useRef(0);
  const key = exerciseId === null ? null : `${userId}/${exerciseId}`;

  const request = useCallback(() => {
    const current = ++generation.current;

    if (key === null || exerciseId === null) {
      return;
    }
    void runWithSession(() =>
      container
        .resolve("getUserExerciseHistoryUseCase")
        .execute({ userId, exerciseId, limit: HISTORY_LIMIT }),
    ).then((result) => {
      if (current !== generation.current) {
        return;
      }
      setAnswer(
        result.ok
          ? { key, sessions: result.value, errorCode: null }
          : { key, sessions: [], errorCode: result.error.code },
      );
    });
  }, [container, runWithSession, userId, exerciseId, key]);

  const reload = useCallback(() => {
    setAnswer(null);
    request();
  }, [request]);

  useEffect(() => {
    request();
    return () => {
      generation.current += 1;
    };
  }, [request]);

  const current = answer !== null && answer.key === key ? answer : null;
  let status: ExerciseHistoryStatus = "loading";

  if (key === null) {
    status = "idle";
  } else if (current !== null) {
    status = current.errorCode === null ? "loaded" : "failed";
  }

  return {
    status,
    sessions: current?.sessions ?? [],
    errorCode: current?.errorCode ?? null,
    reload,
  };
}
