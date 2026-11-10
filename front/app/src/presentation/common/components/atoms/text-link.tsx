import { StyleSheet } from "react-native";
import { Button as PaperButton } from "react-native-paper";

interface TextLinkProps {
  label: string;
  onPress: () => void;
  accessibilityLabel?: string;
}

/** The project's TextLink: a compact React Native Paper text button that reads as a link. */
export default function TextLink({ label, onPress, accessibilityLabel }: TextLinkProps) {
  return (
    <PaperButton
      mode="text"
      compact
      onPress={onPress}
      accessibilityRole="link"
      accessibilityLabel={accessibilityLabel}
      style={styles.link}
    >
      {label}
    </PaperButton>
  );
}

const styles = StyleSheet.create({
  link: {
    alignSelf: "flex-start",
    marginHorizontal: -4,
  },
});
