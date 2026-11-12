import { StyleSheet, View } from "react-native";
import Card from "@/presentation/common/components/atoms/card";
import Icon from "@/presentation/common/components/atoms/icon";
import Text from "@/presentation/common/components/atoms/text";
import ExerciseProgressIcon, {
  type ExerciseProgress,
} from "@/presentation/workouts/components/molecules/exercise-progress-icon";
import ExerciseTags from "@/presentation/workouts/components/molecules/exercise-tags";
import ExerciseTypeIcon from "@/presentation/workouts/components/molecules/exercise-type-icon";

interface WorkoutExerciseRowProps {
  name: string;
  progress: ExerciseProgress;
  progressLabel: string;
  type: string | null;
  typeLabel: string;
  detailsLabel: string;
  /** The body part and, when it says more than the body part, the muscle. */
  chips?: { bodyPart: string; muscle?: string };
  /** Opens the exercise. */
  onPress?: () => void;
}

/** One exercise of a workout: its progress and type, its name and body part and muscle, and the way to its detail. */
export default function WorkoutExerciseRow({
  name,
  progress,
  progressLabel,
  type,
  typeLabel,
  detailsLabel,
  chips,
  onPress,
}: WorkoutExerciseRowProps) {
  return (
    <Card mode="outlined" onPress={onPress}>
      <View style={styles.row}>
        <ExerciseProgressIcon progress={progress} label={progressLabel} />
        <ExerciseTypeIcon type={type} label={typeLabel} />
        <View style={styles.body}>
          <Text variant="titleMedium">{name}</Text>
          {chips && <ExerciseTags bodyPart={chips.bodyPart} muscle={chips.muscle} />}
        </View>
        <Icon source="chevron-right" accessibilityLabel={detailsLabel} />
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    padding: 16,
  },
  body: {
    flex: 1,
    gap: 6,
  },
});
