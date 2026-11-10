import type { ComponentProps } from "react";
import type Chip from "@/presentation/common/components/atoms/chip";

type ChipColor = ComponentProps<typeof Chip>["color"];

/** One theme colour per workout status, so a workout's progress reads at a glance; a draft stays grey. */
const colorOf: Record<string, ChipColor> = {
  DRAFT: "default",
  PUBLISHED: "info",
  STARTED: "warning",
  COMPLETED: "success",
  REVIEWED: "secondary",
};

/** The chip colour of a `WorkoutStatus` value, grey for any status it does not know. */
export function workoutStatusColorOf(status: string): ChipColor {
  return colorOf[status] ?? "default";
}
