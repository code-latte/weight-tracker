import { StyleSheet, View } from "react-native";
import { useAppTheme } from "@/presentation/common/theme";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import Text from "@/presentation/common/components/atoms/text";
import TextLink from "@/presentation/common/components/atoms/text-link";
import ExerciseTags from "@/presentation/workouts/components/molecules/exercise-tags";

interface ExerciseHeroPanelProps {
  backLabel: string;
  onBack: () => void;
  /** Left out while the exercise is not read yet: the panel then only offers the way back. */
  workoutName?: string;
  name?: string;
  /** The English name, greyed, when it adds something to `name`. */
  subtitle?: string;
  /** The body part and, when it says more than the body part, the muscle. */
  chips?: { bodyPart: string; muscle?: string };
  /** Shown only when the exercise has a video. */
  video?: { label: string; onPress: () => void };
}

/** The head of an exercise: the way back, the workout's name, the exercise's name and its English name and video. */
export default function ExerciseHeroPanel({
  backLabel,
  onBack,
  workoutName,
  name,
  subtitle,
  chips,
  video,
}: ExerciseHeroPanelProps) {
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
        {workoutName !== undefined && (
          <Text variant="bodyMedium" numberOfLines={1} style={[styles.workout, onPrimary]}>
            {workoutName}
          </Text>
        )}
      </View>

      {/* A fixed-height block filled from the top, so the panel measures the same on every exercise. */}
      <View style={styles.details}>
        <View style={styles.titleLine}>
          {name !== undefined && (
            <Text
              variant="headlineSmall"
              role="heading"
              aria-level={1}
              numberOfLines={2}
              style={[styles.name, onPrimary]}
            >
              {name}
            </Text>
          )}
          {video && (
            <TextLink
              icon="youtube"
              label={video.label}
              accessibilityLabel={video.label}
              onPress={video.onPress}
              color={theme.custom.brand.white}
            />
          )}
        </View>
        {subtitle !== undefined && (
          <Text variant="bodyMedium" numberOfLines={1} style={[onPrimary, styles.subtitle]}>
            {subtitle}
          </Text>
        )}
        {chips && (
          <View style={styles.chips}>
            <ExerciseTags bodyPart={chips.bodyPart} muscle={chips.muscle} />
          </View>
        )}
      </View>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  // No vertical margin, so the caret is centred on the title's line and the jumbo sits right under the brand bar.
  back: {
    marginVertical: 0,
  },
  panel: {
    flex: 0,
    gap: 4,
    paddingBottom: 16,
    paddingHorizontal: 8,
  },
  titleRow: {
    flexDirection: "row",
    alignItems: "center",
  },
  workout: {
    flex: 1,
  },
  // Two lines of headlineSmall (32 each), one of bodyMedium (20) and the chips (28). A longer name is cut.
  details: {
    height: 112,
    paddingHorizontal: 16,
  },
  // The name takes the width; the video link sits at the right, level with the name's first line.
  titleLine: {
    flexDirection: "row",
    alignItems: "flex-start",
    gap: 8,
  },
  name: {
    flex: 1,
  },
  subtitle: {
    opacity: 0.7,
  },
  chips: {
    marginTop: 6,
  },
});
