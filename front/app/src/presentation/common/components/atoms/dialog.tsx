import { type ComponentProps } from "react";
import { Dialog as PaperDialog, Portal } from "react-native-paper";

/** The project's Dialog: the React Native Paper widget, shown above the screen through a portal. */
export default function Dialog(props: ComponentProps<typeof PaperDialog>) {
  return (
    <Portal>
      <PaperDialog {...props} />
    </Portal>
  );
}

export const DialogTitle = PaperDialog.Title;
export const DialogContent = PaperDialog.Content;
export const DialogActions = PaperDialog.Actions;
