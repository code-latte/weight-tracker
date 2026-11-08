import { type ComponentProps } from "react";
import { Card as PaperCard } from "react-native-paper";

/** The project's Card: the React Native Paper widget, themed by the Paper theme. */
export default function Card(props: ComponentProps<typeof PaperCard>) {
  return <PaperCard {...props} />;
}
