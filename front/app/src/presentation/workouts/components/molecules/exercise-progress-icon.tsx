import Icon from "@/presentation/common/components/atoms/icon";

/** How far along the athlete is with one exercise of the workout. */
export type ExerciseProgress = "done" | "inProgress" | "notStarted";

interface ExerciseProgressIconProps {
  progress: ExerciseProgress;
  label: string;
  /** On a selected, orange control: white instead of the progress colour, which would clash with it. */
  onAccent?: boolean;
}

const icons = {
  done: { source: "check-circle", tone: "success" },
  inProgress: { source: "play-circle", tone: "primary" },
  notStarted: { source: "circle-outline", tone: "default" },
} as const;

/** An exercise's progress as an icon: a green tick when done, a play sign when under way, an empty circle otherwise. */
export default function ExerciseProgressIcon({
  progress,
  label,
  onAccent = false,
}: ExerciseProgressIconProps) {
  const { source, tone } = icons[progress];

  return (
    <Icon
      source={source}
      tone={onAccent ? "onAccent" : tone}
      size={28}
      accessibilityLabel={label}
    />
  );
}
