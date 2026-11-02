import type { TrainerAthlete } from "@weight-tracker/core/domain";
import { fullNameOf } from "@/presentation/common/full-name";
import { normalized } from "@/presentation/common/search-text";

/**
 * The athletes the table shows: those whose full name or email contain the search text. A view filter over the loaded
 * list; it sends nothing and keeps the API's order.
 */
export function filterAthletes(athletes: TrainerAthlete[], search: string): TrainerAthlete[] {
  const text = normalized(search.trim());

  if (text === "") {
    return athletes;
  }
  return athletes.filter((athlete) =>
    [fullNameOf(athlete) ?? "", athlete.email].some((field) => normalized(field).includes(text)),
  );
}
