import { StyleSheet, View } from "react-native";
import Skeleton from "@/presentation/common/components/atoms/skeleton";

interface ExerciseSetsSkeletonProps {
  /** How many set rows to hold the place of, so the list does not change height when it arrives. */
  rows: number;
}

/**
 * Holds the place of what the exercise screen shows between its navigator and its rest card, the previous sets link
 * and the list of sets, while the athlete's last session of the exercise is read.
 */
export default function ExerciseSetsSkeleton({ rows }: ExerciseSetsSkeletonProps) {
  return (
    <View
      testID="exercise-skeleton"
      accessibilityElementsHidden
      importantForAccessibility="no-hide-descendants"
      style={styles.container}
    >
      <Skeleton width={150} height={20} />
      {Array.from({ length: rows }, (_, index) => (
        <View key={index} style={styles.row}>
          <Skeleton width={28} height={28} radius={14} />
          <Skeleton height={56} radius={12} style={styles.card} />
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
    padding: 16,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
  },
  card: {
    flex: 1,
  },
});
