import { type ComponentProps } from "react";
import { IconButton as PaperIconButton } from "react-native-paper";

/** The project's IconButton: the React Native Paper widget, themed by the Paper theme. */
export default function IconButton(props: ComponentProps<typeof PaperIconButton>) {
  return <PaperIconButton {...props} />;
}
