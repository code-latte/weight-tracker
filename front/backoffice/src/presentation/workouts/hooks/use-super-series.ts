import { useState } from "react";
import type { DomainErrorCode, SuperSeriesId } from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";
import type { ExercisesOwner } from "@/presentation/workouts/exercises-owner";

/** Why the last create or break failed, and which of the two it was. */
export interface SuperSeriesError {
  action: "create" | "break";
  code: DomainErrorCode;
}

interface SuperSeriesState {
  /** Whether a create or break request is in progress. */
  pending: boolean;
  error: SuperSeriesError | null;
  /** Groups the given workout entries; resolves to the new superseries id, or `null` when the API refused it. */
  create: (workoutId: string, exerciseIds: string[]) => Promise<SuperSeriesId | null>;
  /** Ungroups a superseries; resolves to `true` when the API accepted it. */
  breakUp: (workoutId: string, superSeriesId: SuperSeriesId) => Promise<boolean>;
}

/** Groups and breaks through the workout or the template use cases, by `owner`; the owner id is passed as `workoutId`. */
export default function useSuperSeries(owner: ExercisesOwner = "workout"): SuperSeriesState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<SuperSeriesError | null>(null);

  async function create(workoutId: string, exerciseIds: string[]) {
    setPending(true);
    setError(null);

    const result = await runWithSession(() =>
      owner === "template"
        ? container
            .resolve("createWorkoutTemplateSuperSeriesUseCase")
            .execute({ templateId: workoutId, exerciseIds })
        : container.resolve("createSuperSeriesUseCase").execute({ workoutId, exerciseIds }),
    );

    setPending(false);
    if (result.ok) {
      return result.value.id;
    }
    setError({ action: "create", code: result.error.code });
    return null;
  }

  async function breakUp(workoutId: string, superSeriesId: SuperSeriesId) {
    setPending(true);
    setError(null);

    const result = await runWithSession(() =>
      owner === "template"
        ? container
            .resolve("breakWorkoutTemplateSuperSeriesUseCase")
            .execute({ templateId: workoutId, superSeriesId })
        : container.resolve("breakSuperSeriesUseCase").execute({ workoutId, superSeriesId }),
    );

    setPending(false);
    if (result.ok) {
      return true;
    }
    setError({ action: "break", code: result.error.code });
    return false;
  }

  return { pending, error, create, breakUp };
}
