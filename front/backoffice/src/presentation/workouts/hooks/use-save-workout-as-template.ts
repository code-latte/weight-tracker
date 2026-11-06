import { useState } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface SaveWorkoutAsTemplateState {
  saving: boolean;
  errorCode: DomainErrorCode | null;
  /** The template the last save created, until `clear` is called. */
  createdId: string | null;
  /** Creates a template from the workout as stored; resolves to its id, or `null` when the API refused it. */
  save: (workoutId: string) => Promise<string | null>;
  clear: () => void;
}

export default function useSaveWorkoutAsTemplate(): SaveWorkoutAsTemplateState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [saving, setSaving] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  const [createdId, setCreatedId] = useState<string | null>(null);

  async function save(workoutId: string) {
    setSaving(true);
    setErrorCode(null);
    setCreatedId(null);

    const result = await runWithSession(() =>
      container.resolve("createWorkoutTemplateFromWorkoutUseCase").execute({ workoutId }),
    );

    setSaving(false);
    if (result.ok) {
      setCreatedId(result.value.id);
      return result.value.id;
    }
    setErrorCode(result.error.code);
    return null;
  }

  function clear() {
    setCreatedId(null);
    setErrorCode(null);
  }

  return { saving, errorCode, createdId, save, clear };
}
