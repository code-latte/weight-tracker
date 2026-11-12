import { StyleSheet, View } from "react-native";
import Button from "@/presentation/common/components/atoms/button";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ProgressBar from "@/presentation/common/components/atoms/progress-bar";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import Text from "@/presentation/common/components/atoms/text";
import { useAppTheme } from "@/presentation/common/theme";

interface WorkoutHeroPanelProps {
  backLabel: string;
  onBack: () => void;
  /** Left out while the workout is not read yet: the panel then only offers the way back. */
  name?: string;
  subtitle?: string;
  progressLabel?: string;
  /** From 0 to 1. */
  progress?: number;
  /** Beside the progress: starts the workout, or says it is started once `disabled`. Left out when there is none. */
  start?: {
    label: string;
    onPress: () => void;
    disabled: boolean;
    loading: boolean;
    /** A Material Community Icons name, shown before the label. */
    icon?: string;
  };
}

/** The head of a workout: the way back, its name and mesocycle week, and how many of its exercises are done. */
export default function WorkoutHeroPanel({
  backLabel,
  onBack,
  name,
  subtitle,
  progressLabel,
  progress,
  start,
}: WorkoutHeroPanelProps) {
  const theme = useAppTheme();
  const onPrimary = { color: theme.colors.onPrimary };

  return (
    <SafeArea edges={[]} style={[styles.panel, { backgroundColor: theme.colors.primary }]}>
      <View style={styles.titleRow}>
        <IconButton
          icon="chevron-left"
          iconColor={theme.colors.onPrimary}
          accessibilityLabel={backLabel}
          onPress={onBack}
          style={styles.back}
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
          <View style={styles.progressRow}>
            <Text variant="titleMedium" style={[styles.progressLabel, onPrimary]}>
              {progressLabel}
            </Text>
            {start && (
              <Button
                mode="contained-tonal"
                compact
                onPress={start.onPress}
                disabled={start.disabled}
                loading={start.loading}
                icon={start.icon}
                accessibilityLabel={start.label}
              >
                {start.label}
              </Button>
            )}
          </View>
          <ProgressBar
            progress={progress}
            color={theme.colors.secondary}
            style={[styles.bar, { backgroundColor: "rgba(255, 255, 255, 0.3)" }]}
          />
        </View>
      )}
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  // No vertical margin, so the caret is centred on the title's line and the jumbo sits right under the brand bar.
  back: {
    marginVertical: 0,
  },
  bar: {
    height: 14,
    borderRadius: 7,
  },
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
  progressRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
  },
  progressLabel: {
    flex: 1,
  },
});
