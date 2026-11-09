import { type ComponentProps } from "react";
import { StyleSheet } from "react-native";
import { useTheme } from "react-native-paper";
import { SafeAreaView } from "react-native-safe-area-context";

type SafeAreaProps = ComponentProps<typeof SafeAreaView>;

/** Keeps its content clear of the status bar, notch and rounded corners, on the theme background. */
export default function SafeArea({
  edges = ["top", "left", "right"],
  style,
  ...props
}: SafeAreaProps) {
  const theme = useTheme();

  return (
    <SafeAreaView
      edges={edges}
      style={[styles.container, { backgroundColor: theme.colors.background }, style]}
      {...props}
    />
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
});
