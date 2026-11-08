import { type ComponentProps } from "react";
import { List } from "react-native-paper";

type ListItemProps = ComponentProps<typeof List.Item> & {
  /** A Material Community Icons name shown on the left, when no `left` element is given. */
  icon?: string;
};

/** The project's ListItem: the React Native Paper list item, with an optional icon on the left. */
export default function ListItem({ icon, left, ...props }: ListItemProps) {
  const leftElement =
    left ?? (icon ? (iconProps) => <List.Icon {...iconProps} icon={icon} /> : undefined);

  return <List.Item {...props} left={leftElement} />;
}
