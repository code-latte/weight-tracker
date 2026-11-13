import { StyleSheet, View } from "react-native";
import Chip from "@/presentation/common/components/atoms/chip";

interface ExerciseTagsProps {
  bodyPart: string;
  /** Left out when the muscle would only repeat the body part. */
  muscle?: string;
}

/** The exercise's body part (filled) and, when it adds something, its muscle (outlined), both in the brand orange. */
export default function ExerciseTags({ bodyPart, muscle }: ExerciseTagsProps) {
  return (
    <View style={styles.row}>
      <Chip small accent="filled" label={bodyPart} />
      {muscle !== undefined && <Chip small accent="outlined" label={muscle} />}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 6,
  },
});
