import { StyleSheet, View } from "react-native";
import Card from "@/presentation/common/components/atoms/card";
import Chip from "@/presentation/common/components/atoms/chip";
import Text from "@/presentation/common/components/atoms/text";
import WorkoutStatusIcon, {
  type WorkoutProgress,
} from "@/presentation/workouts/components/molecules/workout-status-icon";

interface WorkoutCardProps {
  name: string;
  author: string;
  day: string;
  progress: WorkoutProgress;
  progressLabel: string;
  onPress?: () => void;
}

/** One workout of the week: its status, its name and author, and the day it was done. */
export default function WorkoutCard({
  name,
  author,
  day,
  progress,
  progressLabel,
  onPress,
}: WorkoutCardProps) {
  return (
    <Card mode="outlined" onPress={onPress}>
      <View style={styles.row}>
        <WorkoutStatusIcon progress={progress} label={progressLabel} />
        <View style={styles.body}>
          <Text variant="titleMedium" numberOfLines={1}>
            {name}
          </Text>
          <Text variant="bodyMedium" numberOfLines={1}>
            {author}
          </Text>
        </View>
        <Chip label={day} />
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: 16,
    padding: 16,
  },
  body: {
    flex: 1,
  },
});
