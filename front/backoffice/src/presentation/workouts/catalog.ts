import type { Exercise } from "@weight-tracker/core/domain";

/** The slices of the catalog the user can switch between. */
export type ExerciseSection = "ALL" | "MINE" | "FAVORITES";

/**
 * Body parts whose catalog exercises all work one muscle: the muscle would only repeat what the body part says, so
 * their exercises show no muscle label.
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
