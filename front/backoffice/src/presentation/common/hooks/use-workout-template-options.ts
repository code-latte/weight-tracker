import { useCallback, useEffect, useRef, useState } from "react";
import type { DomainErrorCode, WorkoutTemplate } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

/** `idle`: nothing is read yet, because the list is not shown or an admin has not chosen the trainer. */
export type WorkoutTemplateOptionsStatus = "idle" | "loading" | "loaded" | "failed";

export interface WorkoutTemplateOptionsState {
  status: WorkoutTemplateOptionsStatus;
  templates: WorkoutTemplate[];
  errorCode: DomainErrorCode | null;
  reload: () => void;
}

interface WorkoutTemplateOptionsQuery {
  /** Whether the list is shown; nothing is read while it is not. */
  enabled: boolean;
  /** Whose templates to read; omitted for a trainer, who gets their own. */
  trainerId?: string | null;
  /** An admin must name the trainer: without one nothing is read. */
  needsTrainer?: boolean;
}

/**
 * Reads the templates a workout can be created from, once the list is shown and again when the trainer changes. Held
 * by the dialog only, so it resets when the dialog closes.
 */
export default function useWorkoutTemplateOptions({
  enabled,
  trainerId = null,
  needsTrainer = false,
}: WorkoutTemplateOptionsQuery): WorkoutTemplateOptionsState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const ready = enabled && (!needsTrainer || Boolean(trainerId));
  // The query an answer belongs to: a stored answer for another trainer counts as still loading.
  const query = trainerId ?? "";
  const [answer, setAnswer] = useState<{
    query: string;
    templates: WorkoutTemplate[];
    errorCode: DomainErrorCode | null;
  } | null>(null);
  // Bumped by every read and on unmount, so an answer that arrives after either is ignored.
  const generation = useRef(0);

  // State changes only when the answer arrives.
  const request = useCallback(() => {
    const current = ++generation.current;
    if (!ready) {
      return;
    }

    void runWithSession(() =>
      container.resolve("getWorkoutTemplatesUseCase").execute(trainerId ? { trainerId } : {}),
    ).then((result) => {
      if (current !== generation.current) {
        return;
      }
      setAnswer(
        result.ok
          ? { query, templates: result.value, errorCode: null }
          : { query, templates: [], errorCode: result.error.code },
      );
    });
  }, [container, runWithSession, ready, trainerId, query]);

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

  const current = ready && answer?.query === query ? answer : null;
  let status: WorkoutTemplateOptionsStatus = "idle";
  if (ready) {
    status = current === null ? "loading" : current.errorCode ? "failed" : "loaded";
  }

  return {
    status,
    templates: current?.templates ?? [],
    errorCode: current?.errorCode ?? null,
    reload,
  };
}
