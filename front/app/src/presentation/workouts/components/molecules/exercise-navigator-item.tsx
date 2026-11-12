import Chip from "@/presentation/common/components/atoms/chip";
import ExerciseProgressIcon, {
  type ExerciseProgress,
} from "@/presentation/workouts/components/molecules/exercise-progress-icon";

interface ExerciseNavigatorItemProps {
  name: string;
  progress: ExerciseProgress;
  progressLabel: string;
  selected: boolean;
  onPress: () => void;
}

/** One exercise in the navigator: its progress icon and name, filled when it is the one on screen. */
export default function ExerciseNavigatorItem({
  name,
  progress,
  progressLabel,
  selected,
  onPress,
}: ExerciseNavigatorItemProps) {
  return (
    <Chip
      label={name}
      selected={selected}
      onPress={onPress}
      leading={
        <ExerciseProgressIcon progress={progress} label={progressLabel} onAccent={selected} />
      }
    />
  );
}
