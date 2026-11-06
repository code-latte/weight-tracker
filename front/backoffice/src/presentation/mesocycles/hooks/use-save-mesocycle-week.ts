import { useCallback, useState } from "react";
import type {
  DefineMesocycleWeekInput,
  DomainErrorCode,
  Mesocycle,
  RedefineMesocycleWeekInput,
} from "@weight-tracker/core/domain";
import useSession from "@/presentation/auth/hooks/use-session";
import useContainer from "@/presentation/common/hooks/use-container";

/** A new week is defined; an existing one, already numbered, is redefined. */
export type SaveMesocycleWeekInput =
  | { kind: "define"; input: DefineMesocycleWeekInput }
  | { kind: "redefine"; input: RedefineMesocycleWeekInput };

interface SaveMesocycleWeekState {
  saving: boolean;
  errorCode: DomainErrorCode | null;
  /** Resolves to the mesocycle with the saved week, or `null` when the API refused it. */
  save: (week: SaveMesocycleWeekInput) => Promise<Mesocycle | null>;
  reset: () => void;
}

export default function useSaveMesocycleWeek(): SaveMesocycleWeekState {
  const container = useContainer();
  const { runWithSession } = useSession();
  const [saving, setSaving] = useState(false);
  const [errorCode, setErrorCode] = useState<DomainErrorCode | null>(null);

  async function save(week: SaveMesocycleWeekInput) {
    setSaving(true);
    setErrorCode(null);

    const result = await runWithSession(() =>
      week.kind === "define"
        ? container.resolve("defineMesocycleWeekUseCase").execute(week.input)
        : container.resolve("redefineMesocycleWeekUseCase").execute(week.input),
    );

    setSaving(false);
    if (result.ok) {
      return result.value;
    }
    setErrorCode(result.error.code);
    return null;
  }

  const reset = useCallback(() => {
    setSaving(false);
    setErrorCode(null);
  }, []);

  return { saving, errorCode, save, reset };
}
