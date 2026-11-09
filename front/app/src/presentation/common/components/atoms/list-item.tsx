import { type ComponentProps } from "react";
import { List } from "react-native-paper";

type ListItemProps = ComponentProps<typeof List.Item> & {
  /** A Material Community Icons name shown on the left, when no `left` element is given. */
  icon?: string;
  /** Shows a right caret, for an item that opens another screen. */
  chevron?: boolean;
};

/** The project's ListItem: the React Native Paper list item, with an optional left icon and right caret. */
export default function ListItem({ icon, chevron, left, right, ...props }: ListItemProps) {
  const leftElement =
    left ?? (icon ? (iconProps) => <List.Icon {...iconProps} icon={icon} /> : undefined);
  const rightElement =
    right ??
    (chevron ? (iconProps) => <List.Icon {...iconProps} icon="chevron-right" /> : undefined);

  return <List.Item {...props} left={leftElement} right={rightElement} />;
}
