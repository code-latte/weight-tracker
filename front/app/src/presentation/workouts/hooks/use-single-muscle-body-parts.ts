import { useEffect, useState } from "react";
import useContainer from "@/presentation/common/hooks/use-container";
import { singleMuscleBodyPartsOf } from "@/presentation/workouts/catalog";

/**
 * The body parts that have a single muscle in the exercise catalog, read once when the screen mounts. Until it is read,
 * and when it cannot be, the set is empty, so every exercise shows its muscle too.
 */
export default function useSingleMuscleBodyParts(): ReadonlySet<string> {
  const container = useContainer();
  const [bodyParts, setBodyParts] = useState<ReadonlySet<string>>(new Set());

  useEffect(() => {
    let active = true;

    void container
      .resolve("getExercisesUseCase")
      .execute()
      .then((result) => {
        if (active && result.ok) {
          setBodyParts(singleMuscleBodyPartsOf(result.value));
        }
      });

    return () => {
      active = false;
    };
  }, [container]);

  return bodyParts;
}
