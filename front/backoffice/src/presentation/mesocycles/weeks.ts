import type { MesocycleWeek } from "@weight-tracker/core/domain";

/** The week's number as a number: the contract may carry it as a numeric string. */
export function weekNumberOf(week: MesocycleWeek): number {
  return Number(week.weekNumber);
}

/** The defined weeks in ascending week number, leaving the given list untouched. */
export function sortedWeeks(weeks: MesocycleWeek[]): MesocycleWeek[] {
  return [...weeks].sort((a, b) => weekNumberOf(a) - weekNumberOf(b));
}

/**
 * The lowest week number from 1 to `duration` that has no week yet, or `null` when every one is defined. It mirrors
 * the loaded duration only; the API still decides whether the week can be defined.
 */
export function nextFreeWeekOf(weeks: MesocycleWeek[], duration: number): number | null {
  const defined = new Set(weeks.map(weekNumberOf));

  for (let number = 1; number <= duration; number += 1) {
    if (!defined.has(number)) {
      return number;
    }
  }
  return null;
}
