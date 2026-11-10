import { StyleSheet, View } from "react-native";
import Card from "@/presentation/common/components/atoms/card";
import ProgressBar from "@/presentation/common/components/atoms/progress-bar";
import Text from "@/presentation/common/components/atoms/text";

interface WeekProgressCardProps {
  title: string;
  subtitle: string;
  progressLabel: string;
  completed: number;
  total: number;
}

/** The viewed week: its title and subtitle, and how many of its workouts are done. */
export default function WeekProgressCard({
  title,
  subtitle,
  progressLabel,
  completed,
  total,
}: WeekProgressCardProps) {
  return (
    <Card mode="contained">
      <View style={styles.row}>
        <View style={styles.week}>
          <Text variant="titleLarge" role="heading" aria-level={1}>
            {title}
          </Text>
          <Text variant="bodyMedium">{subtitle}</Text>
        </View>
        <View style={styles.progress}>
          <Text variant="titleMedium" style={styles.progressLabel}>
            {progressLabel}
          </Text>
          <ProgressBar progress={total === 0 ? 0 : completed / total} />
        </View>
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
  week: {
    maxWidth: "70%",
  },
  progress: {
    flex: 1,
    gap: 8,
  },
  progressLabel: {
    alignSelf: "flex-end",
  },
});
