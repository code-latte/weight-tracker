import { StyleSheet, View } from "react-native";
import { ActivityIndicator } from "react-native-paper";

/** A centred loading indicator for a screen section that is still being read. */
export default function LoadingState() {
  return (
    <View style={styles.container}>
      <ActivityIndicator accessibilityRole="progressbar" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: "center",
    paddingVertical: 32,
  },
});
