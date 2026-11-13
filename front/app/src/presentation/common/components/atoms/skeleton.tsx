import { type DimensionValue, type StyleProp, View, type ViewStyle } from "react-native";
import { useAppTheme } from "@/presentation/common/theme";

interface SkeletonProps {
  width?: DimensionValue;
  height: number;
  /** Corner radius; a block with a radius of half its height is a pill, of half its width a circle. */
  radius?: number;
  style?: StyleProp<ViewStyle>;
}

/** The project's Skeleton: a block in a muted surface colour that holds the place of content that is still coming. */
export default function Skeleton({ width = "100%", height, radius = 8, style }: SkeletonProps) {
  const theme = useAppTheme();

  return (
    <View
      style={[
        { width, height, borderRadius: radius, backgroundColor: theme.colors.surfaceVariant },
        style,
      ]}
    />
  );
}
