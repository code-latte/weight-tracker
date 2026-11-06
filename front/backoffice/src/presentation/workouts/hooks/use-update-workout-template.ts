import { useState } from "react";
import type {
  DomainErrorCode,
  UpdateWorkoutTemplateInput,
  WorkoutTemplate,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

interface UpdateWorkoutTemplateState {
  saving: boolean;
  errorCode: DomainErrorCode | null;
  saved: boolean;
  /** Resolves to the saved template, or `null` when the API refused it. */
  save: (input: UpdateWorkoutTemplateInput) => Promise<WorkoutTemplate | null>;
  clearSaved: () => void;
}

export default function useUpdateWorkoutTemplate(): UpdateWorkoutTemplateState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [saving, setSaving] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);
  const [saved, setSaved] = useState(false);

  async function save(input: UpdateWorkoutTemplateInput) {
    setSaving(true);
    setErrorCode(null);
    setSaved(false);

    const result = await runWithSession(() =>
      container.resolve("updateWorkoutTemplateUseCase").execute(input),
    );

    setSaving(false);
    if (result.ok) {
      setSaved(true);
      return result.value;
    }
    setErrorCode(result.error.code);
    return null;
  }

  return { saving, errorCode, saved, save, clearSaved: () => setSaved(false) };
}
