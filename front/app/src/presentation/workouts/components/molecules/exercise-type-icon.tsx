import Icon from "@/presentation/common/components/atoms/icon";

interface ExerciseTypeIconProps {
  type: string | null;
  label: string;
}

const icons: Record<string, string> = {
  STRENGTH: "dumbbell",
  CARDIO: "heart",
  STRETCHING: "meditation",
};

/** An exercise's type as an icon, as the backoffice shows it; nothing for a type it does not know. */
export default function ExerciseTypeIcon({ type, label }: ExerciseTypeIconProps) {
  const source = type === null ? undefined : icons[type];
  if (source === undefined) {
    return null;
  }

  return <Icon source={source} accessibilityLabel={label} />;
}
