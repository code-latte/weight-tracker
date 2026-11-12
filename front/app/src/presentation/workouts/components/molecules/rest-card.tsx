import { StyleSheet, View } from "react-native";
import Card from "@/presentation/common/components/atoms/card";
import Icon from "@/presentation/common/components/atoms/icon";
import Text from "@/presentation/common/components/atoms/text";

interface RestCardProps {
  /** The rest time as the athlete reads it, or that there is none. */
  label: string;
}

/** The rest between sets of the exercise: a full-width band kept against the tab bar. */
export default function RestCard({ label }: RestCardProps) {
  return (
    <Card mode="contained" style={styles.card}>
      <View style={styles.row}>
        <Icon source="timer-outline" tone="primary" />
        <Text variant="titleMedium">{label}</Text>
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
});
