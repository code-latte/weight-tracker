import { StyleSheet, View } from "react-native";
import Card from "@/presentation/common/components/atoms/card";
import Text from "@/presentation/common/components/atoms/text";

interface LastPerformanceStatCardProps {
  /** Already translated, such as "Series realizadas". */
  label: string;
  /** A figure or a short text, such as "3" or "RIR 3"; shown big. */
  value: string;
}

/** One figure of the last session: a small label above its value. Cards in a row share the width. */
export default function LastPerformanceStatCard({ label, value }: LastPerformanceStatCardProps) {
  return (
    <Card mode="contained" style={styles.card} accessible accessibilityLabel={`${label}: ${value}`}>
      <View style={styles.content}>
        <Text variant="labelMedium" numberOfLines={1}>
          {label}
        </Text>
        <Text variant="headlineMedium" numberOfLines={1} adjustsFontSizeToFit>
          {value}
        </Text>
      </View>
    </Card>
  );
}

const styles = StyleSheet.create({
  card: {
    flex: 1,
  },
  content: {
    gap: 4,
    padding: 12,
  },
});
