import { View } from "react-native";
import { Icon as PaperIcon, useTheme } from "react-native-paper";

type IconTone = "primary" | "secondary" | "default";

interface IconProps {
  /** A Material Community Icons name. */
  source: string;
  size?: number;
  tone?: IconTone;
  /** Announced by screen readers; Paper hides the bare icon from them. */
  accessibilityLabel?: string;
}

/** The project's Icon: the React Native Paper icon, coloured from the Paper theme by tone. */
export default function Icon({
  source,
  size = 24,
  tone = "default",
  accessibilityLabel,
}: IconProps) {
  const theme = useTheme();
  const colors: Record<IconTone, string> = {
    primary: theme.colors.primary,
    secondary: theme.colors.secondary,
    default: theme.colors.onSurfaceVariant,
  };

  return (
    <View
      accessible={accessibilityLabel !== undefined}
      accessibilityRole={accessibilityLabel !== undefined ? "image" : undefined}
      accessibilityLabel={accessibilityLabel}
    >
      <PaperIcon source={source} size={size} color={colors[tone]} />
    </View>
  );
}
