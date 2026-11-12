import { useCallback, useState } from "react";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import useContainer from "@/presentation/common/hooks/use-container";
import { type SetRow, type SetValues, setRecordBodyOf } from "@/presentation/workouts/set-rows";

/**
 * Saves the sets of one exercise: records a set that has no record yet and corrects one that has. It keeps which row
 * is saving and the last error of each row, so a failure stays on its own row. It never throws.
 */
export default function useSaveSetRecord(workoutId: string, exerciseId: string) {
  const container = useContainer();
  const [savingNumber, setSavingNumber] = useState<number | null>(null);
  const [errors, setErrors] = useState<Record<number, DomainErrorCode>>({});

  /** Resolves `true` when the API took the set, so the caller can read the workout again. */
  const save = useCallback(
    async (row: SetRow, values: SetValues): Promise<boolean> => {
      setSavingNumber(row.number);
      setErrors((current) => {
        const next = { ...current };
        delete next[row.number];
        return next;
      });

      const body = setRecordBodyOf(row, values);
      const result =
        row.record === null
          ? await container
              .resolve("addSetRecordUseCase")
              .execute({ workoutId, exerciseId, ...body })
          : await container
              .resolve("updateSetRecordUseCase")
              .execute({ workoutId, exerciseId, setRecordId: row.record.id, ...body });

      setSavingNumber(null);
      if (result.ok) {
        return true;
      }
      setErrors((current) => ({ ...current, [row.number]: result.error.code }));

      return false;
    },
    [container, workoutId, exerciseId],
  );

  return { save, savingNumber, errors };
}
