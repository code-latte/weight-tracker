import type { Exercise } from "@weight-tracker/core/domain";

/**
 * Body parts whose catalog exercises all work one muscle: the muscle would only repeat what the body part says, so
 * their exercises show the body part alone.
 */
export function singleMuscleBodyPartsOf(exercises: readonly Exercise[]): Set<string> {
  const musclesOf = new Map<string, Set<string>>();

  for (const { bodyPart, muscle } of exercises) {
    musclesOf.set(bodyPart, (musclesOf.get(bodyPart) ?? new Set()).add(muscle));
  }
  return new Set(
    [...musclesOf].filter(([, muscles]) => muscles.size === 1).map(([bodyPart]) => bodyPart),
  );
}
