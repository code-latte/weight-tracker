import { StyleSheet, View } from "react-native";
import SegmentedButtons from "@/presentation/common/components/atoms/segmented-buttons";
import Text from "@/presentation/common/components/atoms/text";

export interface PreferenceOption<T extends string> {
  value: T;
  label: string;
}

interface PreferenceSelectorProps<T extends string> {
  label: string;
  value: T;
  options: PreferenceOption<T>[];
  onChange: (value: T) => void;
}

/** One device setting: its label above a choice of options, the current one selected. */
export default function PreferenceSelector<T extends string>({
  label,
  value,
  options,
  onChange,
}: PreferenceSelectorProps<T>) {
  return (
    <View style={styles.container}>
      <Text variant="titleMedium">{label}</Text>
      <SegmentedButtons
        value={value}
        onValueChange={(next) => {
          const option = options.find((candidate) => candidate.value === next);
          if (option) {
            onChange(option.value);
          }
        }}
        buttons={options.map((option) => ({
          value: option.value,
          label: option.label,
          accessibilityLabel: option.label,
        }))}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: 12,
  },
});
