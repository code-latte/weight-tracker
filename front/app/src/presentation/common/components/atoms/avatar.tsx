import { type ComponentProps } from "react";
import { Avatar as PaperAvatar } from "react-native-paper";

/** The project's Avatar: the React Native Paper text avatar, themed by the Paper theme. */
export default function Avatar(props: ComponentProps<typeof PaperAvatar.Text>) {
  return <PaperAvatar.Text {...props} />;
}
