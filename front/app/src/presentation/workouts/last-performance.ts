import type { LastPerformanceSet } from "@weight-tracker/core/domain";

/**
 * The position of the set with the highest weight, or `null` when no set has a weight above 0. A weight that was not
 * tracked does not count. When the highest weight is repeated, the last of those sets is the one.
 */
export function highestWeightIndexOf(sets: readonly LastPerformanceSet[]): number | null {
  let highest: number | null = null;
  let highestWeight = 0;

  sets.forEach(({ weight }, index) => {
    if (weight == null) {
      return;
    }
    const value = Number(weight);

    if (value > 0 && value >= highestWeight) {
      highest = index;
      highestWeight = value;
    }
  });

  return highest;
}

/** Whether any set has a weight. A weight that was not tracked is `null`, which is different from 0 kg. */
export function hasWeightIn(sets: readonly LastPerformanceSet[]): boolean {
  return sets.some(({ weight }) => weight != null);
}
