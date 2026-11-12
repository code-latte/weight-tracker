import Button from "@/presentation/common/components/atoms/button";
import Dialog, {
  DialogActions,
  DialogContent,
  DialogTitle,
} from "@/presentation/common/components/atoms/dialog";
import Text from "@/presentation/common/components/atoms/text";

interface ConfirmDialogProps {
  visible: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel: string;
  /** The confirmed action is in flight: the confirm button spins and nothing can be dismissed. */
  loading?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/** Asks the athlete to confirm an action before it happens, with the consequence in a line or two. */
export default function ConfirmDialog({
  visible,
  title,
  message,
  confirmLabel,
  cancelLabel,
  loading = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <Dialog visible={visible} onDismiss={loading ? undefined : onCancel} dismissable={!loading}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Text variant="bodyMedium">{message}</Text>
      </DialogContent>
      <DialogActions>
        <Button onPress={onCancel} disabled={loading}>
          {cancelLabel}
        </Button>
        <Button mode="contained" onPress={onConfirm} loading={loading} disabled={loading}>
          {confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
