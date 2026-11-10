import { StyleSheet, View } from "react-native";
import Card from "@/presentation/common/components/atoms/card";
import Icon from "@/presentation/common/components/atoms/icon";
import Text from "@/presentation/common/components/atoms/text";
import TextLink from "@/presentation/common/components/atoms/text-link";
import ExerciseProgressIcon, {
  type ExerciseProgress,
} from "@/presentation/workouts/components/molecules/exercise-progress-icon";
import ExerciseTypeIcon from "@/presentation/workouts/components/molecules/exercise-type-icon";

interface WorkoutExerciseRowProps {
  name: string;
  progress: ExerciseProgress;
  progressLabel: string;
  type: string | null;
  typeLabel: string;
  detailsLabel: string;
  /** Shown only when the exercise has a video. */
  video?: { label: string; onPress: () => void };
}

/** One exercise of a workout: its progress and type, its name and video, and the way to its detail. */
export default function WorkoutExerciseRow({
  name,
  progress,
  progressLabel,
  type,
  typeLabel,
  detailsLabel,
  video,
}: WorkoutExerciseRowProps) {
  return (
    <Card mode="outlined">
      <View style={styles.row}>
        <ExerciseProgressIcon progress={progress} label={progressLabel} />
        <ExerciseTypeIcon type={type} label={typeLabel} />
        <View style={styles.body}>
          <Text variant="titleMedium">{name}</Text>
          {video && <TextLink label={video.label} onPress={video.onPress} />}
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
    gap: 2,
  },
});
