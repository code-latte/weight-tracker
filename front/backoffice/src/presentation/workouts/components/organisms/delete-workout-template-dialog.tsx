"use client";

import { useId } from "react";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import Typography from "@/presentation/common/components/atoms/typography";

interface DeleteWorkoutTemplateDialogProps {
  open: boolean;
  /** While the deletion is in progress both buttons are disabled and the dialog cannot be dismissed. */
  pending: boolean;
  /** Called on Cancelar, Escape or a click outside. */
  onCancel: () => void;
  onConfirm: () => void;
}

/**
 * Asks before deleting a template. It sends nothing itself: the screen sends the deletion and shows its outcome.
 */
export default function DeleteWorkoutTemplateDialog({
  open,
  pending,
  onCancel,
  onConfirm,
}: DeleteWorkoutTemplateDialogProps) {
  const { t } = useTranslation("workouts");
  const titleId = useId();

  return (
    <Dialog
      open={open}
      onClose={() => {
        if (!pending) {
          onCancel();
        }
      }}
      fullWidth
      maxWidth="xs"
      aria-labelledby={titleId}
    >
      <DialogTitle id={titleId}>{t("workoutTemplate.deleteConfirm.title")}</DialogTitle>
      <DialogContent>
        <Typography>{t("workoutTemplate.deleteConfirm.body")}</Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={pending}>
          {t("workoutTemplate.deleteConfirm.cancel")}
        </Button>
        <Button variant="contained" color="error" disabled={pending} onClick={onConfirm}>
          {t("workoutTemplate.deleteConfirm.confirm")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
