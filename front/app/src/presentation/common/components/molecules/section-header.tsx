import { StyleSheet, View } from "react-native";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import SafeArea from "@/presentation/common/components/atoms/safe-area";
import Text from "@/presentation/common/components/atoms/text";

interface SectionHeaderProps {
  title: string;
  backLabel: string;
  onBack: () => void;
}

/** Header of a pushed section screen: a back caret and the section title, below the status bar. */
export default function SectionHeader({ title, backLabel, onBack }: SectionHeaderProps) {
  return (
    <SafeArea edges={["top", "left", "right"]} style={styles.area}>
      <View style={styles.row}>
        <IconButton icon="chevron-left" accessibilityLabel={backLabel} onPress={onBack} />
        <Text variant="titleLarge" role="heading" aria-level={1}>
          {title}
        </Text>
      </View>
    </SafeArea>
  );
}

const styles = StyleSheet.create({
  area: {
    flex: 0,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: 8,
    paddingVertical: 4,
  },
});
