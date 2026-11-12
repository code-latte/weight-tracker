import { StyleSheet } from "react-native";
import { Button as PaperButton, Icon as PaperIcon } from "react-native-paper";

interface TextLinkProps {
  label: string;
  onPress: () => void;
  accessibilityLabel?: string;
  /** The text colour, for a link on a coloured surface where the primary would not show. */
  color?: string;
  /** A Material Community Icons name, shown before the label. */
  icon?: string;
}

/** The project's TextLink: a compact React Native Paper text button that reads as a link. */
export default function TextLink({
  label,
  onPress,
  accessibilityLabel,
  color,
  icon,
}: TextLinkProps) {
  return (
    <PaperButton
      mode="text"
      compact
      onPress={onPress}
      accessibilityRole="link"
      accessibilityLabel={accessibilityLabel}
      textColor={color}
      icon={
        icon === undefined
          ? undefined
          : ({ color: iconColor }) => <PaperIcon source={icon} size={22} color={iconColor} />
      }
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
