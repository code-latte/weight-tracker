import { StyleSheet, View } from "react-native";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Text from "@/presentation/common/components/atoms/text";

interface WeekNavigatorProps {
  range: string;
  previousLabel: string;
  nextLabel: string;
  onPrevious: () => void;
  onNext: () => void;
}

/** Moves between weeks: a caret each side of the viewed week's range. */
export default function WeekNavigator({
  range,
  previousLabel,
  nextLabel,
  onPrevious,
  onNext,
}: WeekNavigatorProps) {
  return (
    <View style={styles.row}>
      <IconButton icon="chevron-left" accessibilityLabel={previousLabel} onPress={onPrevious} />
      <Text variant="titleSmall">{range}</Text>
      <IconButton icon="chevron-right" accessibilityLabel={nextLabel} onPress={onNext} />
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
  },
});
