import { type ComponentProps } from "react";
import { SegmentedButtons as PaperSegmentedButtons } from "react-native-paper";

/** The project's SegmentedButtons: the React Native Paper widget, themed by the Paper theme. */
export default function SegmentedButtons(props: ComponentProps<typeof PaperSegmentedButtons>) {
  return <PaperSegmentedButtons {...props} />;
}
