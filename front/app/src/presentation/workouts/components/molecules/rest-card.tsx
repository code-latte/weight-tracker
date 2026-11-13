import { StyleSheet, View } from "react-native";
import Card from "@/presentation/common/components/atoms/card";
import Icon from "@/presentation/common/components/atoms/icon";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Text from "@/presentation/common/components/atoms/text";

interface RestCardProps {
  /** The rest time as the athlete reads it, or that there is none. */
  label: string;
  /** The countdown is over: the card reads as done. */
  finished?: boolean;
  /** Play / pause and reset for the countdown. Left out when the exercise has no rest. */
  controls?: {
    running: boolean;
    canPlay: boolean;
    canReset: boolean;
    onPlay: () => void;
    onPause: () => void;
    onReset: () => void;
    playLabel: string;
    pauseLabel: string;
    resetLabel: string;
  };
}

/** The rest between sets of the exercise: a full-width band kept against the tab bar. */
export default function RestCard({ label, finished = false, controls }: RestCardProps) {
  return (
    <Card mode="contained" style={styles.card}>
      <View style={styles.row}>
        <Icon source={finished ? "timer-off-outline" : "timer-outline"} tone="primary" />
        <Text variant="titleMedium">{label}</Text>
        {controls !== undefined && (
          <View style={styles.controls}>
            <IconButton
              icon={controls.running ? "pause" : "play"}
              onPress={controls.running ? controls.onPause : controls.onPlay}
              disabled={!controls.running && !controls.canPlay}
              accessibilityLabel={controls.running ? controls.pauseLabel : controls.playLabel}
            />
            <IconButton
              icon="restore"
              onPress={controls.onReset}
              disabled={!controls.canReset}
              accessibilityLabel={controls.resetLabel}
            />
          </View>
        )}
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: 0,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
    gap: 12,
    padding: 16,
  },
  controls: {
    flexDirection: "row",
  },
});
