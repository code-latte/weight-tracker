import { type ReactNode } from "react";
import { StyleSheet } from "react-native";
import { Chip as PaperChip } from "react-native-paper";
import { useAppTheme } from "@/presentation/common/theme";

interface ChipProps {
  label: string;
  /** Makes the chip pressable. Without it the chip only shows its label. */
  onPress?: () => void;
  /** Filled instead of outlined, and announced as selected. */
  selected?: boolean;
  /** Shown before the label. */
  leading?: ReactNode;
  /** In the brand orange: filled with white text, or outlined with orange text. */
  accent?: "filled" | "outlined";
  /** A tag-sized chip: shorter, with smaller text. */
  small?: boolean;
}

/** The project's Chip: a compact React Native Paper chip, themed by the Paper theme. */
export default function Chip({
  label,
  onPress,
  selected = false,
  leading,
  accent,
  small = false,
}: ChipProps) {
  const theme = useAppTheme();

  return (
    <PaperChip
      compact
      mode={selected || accent === "filled" ? "flat" : "outlined"}
      selected={selected}
      showSelectedCheck={false}
      onPress={onPress}
      icon={leading === undefined ? undefined : () => leading}
      style={[
        small && styles.small,
        accent === "outlined" && { borderColor: theme.colors.secondary },
      ]}
      textStyle={[
        small && styles.smallText,
        accent === "outlined" && { color: theme.colors.secondary },
      ]}
    >
      {label}
    </PaperChip>
  );
}

const styles = StyleSheet.create({
  small: {
    height: 22,
  },
  smallText: {
    fontSize: 11,
    lineHeight: 14,
    marginVertical: 3,
    marginHorizontal: 8,
  },
});
