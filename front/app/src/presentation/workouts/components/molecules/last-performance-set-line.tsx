import { StyleSheet, View } from "react-native";
import Icon from "@/presentation/common/components/atoms/icon";
import Text from "@/presentation/common/components/atoms/text";
import { useAppTheme } from "@/presentation/common/theme";

interface LastPerformanceSetLineProps {
  /** The set's number, from 1. */
  number: number;
  /** Already translated, such as "10 reps" or "45 s"; left out for a set with neither reps nor seconds. */
  amount?: string;
  /** Already translated, such as "20 kg" or "sin peso". */
  weight: string;
  /** Marks the set as the one with the highest weight: a star before the weight and this text under it. */
  highestLabel?: string;
}

/**
 * One set the athlete recorded last time, as a row of a table: its number, big and orange like the numbers of the
 * register list, its reps or seconds, and its weight, big too.
 */
export default function LastPerformanceSetLine({
  number,
  amount,
  weight,
  highestLabel,
}: LastPerformanceSetLineProps) {
  const theme = useAppTheme();

  return (
    <View style={styles.line}>
      <Text variant="headlineMedium" style={[styles.number, { color: theme.colors.secondary }]}>
        {number}
      </Text>
      <Text variant="bodyMedium" style={styles.amount}>
        {amount}
      </Text>
      <View style={styles.weight}>
        <View style={styles.weightLine}>
          {highestLabel !== undefined && <Icon source="star" tone="primary" />}
          <Text
            variant="headlineMedium"
            numberOfLines={1}
            adjustsFontSizeToFit
            minimumFontScale={0.6}
            style={styles.weightText}
          >
            {weight}
          </Text>
        </View>
        {highestLabel !== undefined && (
          <Text variant="labelSmall" style={{ color: theme.colors.primary }}>
            {highestLabel}
          </Text>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  line: {
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  number: {
    width: 36,
  },
  // The middle column takes the room left, so the amounts line up across rows whatever the weights are.
  amount: {
    flex: 1,
    textAlign: "center",
  },
  weight: {
    minWidth: 96,
    alignItems: "flex-end",
  },
  weightLine: {
    flexDirection: "row",
    alignItems: "center",
    gap: 4,
  },
  // Shrinks to fit when the text is long, such as "sin peso", rather than push the amount aside.
  weightText: {
    flexShrink: 1,
    textAlign: "right",
  },
});
