import { StyleSheet, View } from "react-native";
import { useTheme } from "react-native-paper";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ProgressBar from "@/presentation/common/components/atoms/progress-bar";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import Text from "@/presentation/common/components/atoms/text";

interface WorkoutHeroPanelProps {
  backLabel: string;
  onBack: () => void;
  /** Left out while the workout is not read yet: the panel then only offers the way back. */
  name?: string;
  subtitle?: string;
  progressLabel?: string;
  /** From 0 to 1. */
  progress?: number;
}

/** The head of a workout: the way back, its name and mesocycle week, and how many of its exercises are done. */
export default function WorkoutHeroPanel({
  backLabel,
  onBack,
  name,
  subtitle,
  progressLabel,
  progress,
}: WorkoutHeroPanelProps) {
  const theme = useTheme();
  const onPrimary = { color: theme.colors.onPrimary };

  return (
    <SafeArea edges={["top"]} style={[styles.panel, { backgroundColor: theme.colors.primary }]}>
      <View style={styles.titleRow}>
        <IconButton
          icon="arrow-left"
          iconColor={theme.colors.onPrimary}
          accessibilityLabel={backLabel}
          onPress={onBack}
        />
        {name !== undefined && (
          <Text variant="titleLarge" role="heading" aria-level={1} style={[styles.name, onPrimary]}>
            {name}
          </Text>
        )}
      </View>

      {subtitle !== undefined && (
        <Text variant="bodyMedium" style={[styles.indented, onPrimary]}>
          {subtitle}
        </Text>
      )}

      {progressLabel !== undefined && progress !== undefined && (
        <View style={[styles.progress, styles.indented]}>
          <Text variant="titleMedium" style={onPrimary}>
            {progressLabel}
          </Text>
          <ProgressBar
            progress={progress}
            color={theme.colors.onPrimary}
            style={{ backgroundColor: theme.colors.primaryContainer }}
          />
        </View>
      )}
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  panel: {
    flex: 0,
    gap: 4,
    paddingBottom: 20,
    paddingHorizontal: 8,
  },
  titleRow: {
    flexDirection: "row",
    alignItems: "center",
  },
  name: {
    flex: 1,
  },
  indented: {
    paddingHorizontal: 16,
  },
  progress: {
    gap: 8,
    marginTop: 12,
  },
});
