import { StyleSheet, View } from "react-native";
import { useTheme } from "react-native-paper";
import ProgressBar from "@/presentation/common/components/atoms/progress-bar";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import Text from "@/presentation/common/components/atoms/text";

interface WeekProgressCardProps {
  title: string;
  subtitle: string;
  progressLabel: string;
  completed: number;
  total: number;
}

/** The viewed week, as the screen's jumbo panel: its title and subtitle, and how many of its workouts are done. */
export default function WeekProgressCard({
  title,
  subtitle,
  progressLabel,
  completed,
  total,
}: WeekProgressCardProps) {
  const theme = useTheme();
  const onPrimary = { color: theme.colors.onPrimary };

  return (
    <SafeArea edges={[]} style={[styles.panel, { backgroundColor: theme.colors.primary }]}>
      <View style={styles.row}>
        <View style={styles.week}>
          <Text variant="titleLarge" role="heading" aria-level={1} style={onPrimary}>
            {title}
          </Text>
          <Text variant="bodyMedium" style={onPrimary}>
            {subtitle}
          </Text>
        </View>
        <View style={styles.progress}>
          <Text variant="titleMedium" style={[styles.progressLabel, onPrimary]}>
            {progressLabel}
          </Text>
          <ProgressBar
            progress={total === 0 ? 0 : completed / total}
            color={theme.colors.secondary}
            style={{ backgroundColor: "rgba(255, 255, 255, 0.3)" }}
          />
        </View>
      </View>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  panel: {
    flex: 0,
    paddingBottom: 20,
    paddingHorizontal: 24,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: 16,
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
