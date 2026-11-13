import type { WorkoutById } from "@weight-tracker/core/domain";
import { fullNameOf } from "@/presentation/common/full-name";
import { initialsOf } from "@/presentation/common/initials";
import type { PersonCardProps } from "@/presentation/common/components/molecules/person-card";

/** Where "Volver" leads: the mesocycle of a placed workout, or the list of workouts outside any mesocycle. */
export function backHrefOf(workout: WorkoutById): string {
  return workout.mesocycleId
    ? `/mesocycle?id=${encodeURIComponent(workout.mesocycleId)}`
    : "/assigned-workouts";
}

/** The person's full name, or their email when they have no name. */
export function displayNameOf(person: WorkoutById["athlete"]): string {
  return fullNameOf(person) ?? person.email;
}

/** The person as their card in the strip shows them. */
export function personCardOf(person: WorkoutById["athlete"], role: string): PersonCardProps {
  return { name: displayNameOf(person), initials: initialsOf(person), role };
}
