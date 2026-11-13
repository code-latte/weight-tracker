import { type ComponentProps } from "react";
import { Divider as PaperDivider } from "react-native-paper";

/** The project's Divider: the React Native Paper widget, a thin line that separates two blocks of a surface. */
export default function Divider(props: ComponentProps<typeof PaperDivider>) {
  return <PaperDivider {...props} />;
}
