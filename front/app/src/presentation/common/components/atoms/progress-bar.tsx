import { type ComponentProps } from "react";
import { ProgressBar as PaperProgressBar } from "react-native-paper";

/** The project's ProgressBar: the React Native Paper widget, themed by the Paper theme. */
export default function ProgressBar(props: ComponentProps<typeof PaperProgressBar>) {
  return <PaperProgressBar {...props} />;
}
