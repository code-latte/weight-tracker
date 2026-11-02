import type { UserSummary } from "@weight-tracker/core/domain";
import { fullNameOf } from "@/presentation/common/full-name";
import { normalized } from "@/presentation/common/search-text";

/** Which athletes the pills keep: those with a trainer, those without one, or both when none is selected. */
export type AssignmentFilterValue = "with" | "without";

/**
 * The athletes the table shows: with or without a trainer, if a pill is selected; whose full name, email or trainer
 * full name contain the search text. A view filter over the loaded list; it sends nothing and keeps the API's order.
 */
export function filterTrainerAssignments(
  athletes: UserSummary[],
  search: string,
  assignment: AssignmentFilterValue | null,
): UserSummary[] {
  const text = normalized(search.trim());

  return athletes.filter(
    (athlete) =>
      (assignment === null || (assignment === "with") === (athlete.trainer !== null)) &&
      (text === "" ||
        [
          fullNameOf(athlete),
          athlete.email,
          athlete.trainer ? fullNameOf(athlete.trainer) : null,
        ].some((field) => field !== null && normalized(field).includes(text))),
  );
}
