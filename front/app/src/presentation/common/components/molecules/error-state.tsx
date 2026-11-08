import { StyleSheet, View } from "react-native";
import Button from "../atoms/button";
import Text from "../atoms/text";

interface ErrorStateProps {
  message: string;
  retryLabel: string;
  onRetry: () => void;
}

/** A failure message with a retry button, for a screen section that could not be read. */
export default function ErrorState({ message, retryLabel, onRetry }: ErrorStateProps) {
  return (
    <View style={styles.container}>
      <Text variant="bodyLarge">{message}</Text>
      <Button mode="contained" onPress={onRetry}>
        {retryLabel}
      </Button>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: "center",
    gap: 16,
    paddingVertical: 32,
  },
});
