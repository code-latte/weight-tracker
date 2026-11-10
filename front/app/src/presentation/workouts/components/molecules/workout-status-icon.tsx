import Icon from "@/presentation/common/components/atoms/icon";

/** How the athlete sees a workout's status: done, reviewed by the trainer, under way, or not started. */
export type WorkoutProgress = "completed" | "reviewed" | "started" | "pending";

interface WorkoutStatusIconProps {
  progress: WorkoutProgress;
  label: string;
}

const icons = {
  completed: { source: "check-circle", tone: "secondary" },
  reviewed: { source: "check-circle", tone: "primary" },
  started: { source: "play-circle", tone: "secondary" },
  pending: { source: "circle-outline", tone: "default" },
} as const;

/** A workout's status as an icon: a filled tick when done, a play sign when under way, an empty circle otherwise. */
export default function WorkoutStatusIcon({ progress, label }: WorkoutStatusIconProps) {
  const { source, tone } = icons[progress];

  return <Icon source={source} tone={tone} size={28} accessibilityLabel={label} />;
}
