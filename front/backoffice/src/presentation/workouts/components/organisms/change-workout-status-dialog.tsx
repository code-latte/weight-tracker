"use client";

import { useId } from "react";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import Typography from "@/presentation/common/components/atoms/typography";

interface ChangeWorkoutStatusDialogProps {
  open: boolean;
  /** The status the workout moves to: it picks the dialog's copy. */
  target: "PUBLISHED" | "DRAFT";
  /** While the change is in progress both buttons are disabled and the dialog cannot be dismissed. */
  pending: boolean;
  /** Called on Cancelar, Escape or a click outside. */
  onCancel: () => void;
  onConfirm: () => void;
}

/**
 * Asks before publishing a workout or moving it back to draft. It sends nothing itself: the screen sends the change
 * and shows its outcome.
 */
export default function ChangeWorkoutStatusDialog({
  open,
  target,
  pending,
  onCancel,
  onConfirm,
}: ChangeWorkoutStatusDialogProps) {
  const { t } = useTranslation("workouts");
  const titleId = useId();
  const copy =
    target === "PUBLISHED" ? "workout.status.publishConfirm" : "workout.status.backToDraftConfirm";

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
      <DialogTitle id={titleId}>{t(`${copy}.title`)}</DialogTitle>
      <DialogContent>
        <Typography>{t(`${copy}.body`)}</Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={pending}>
          {t("workout.status.cancel")}
        </Button>
        <Button variant="contained" disabled={pending} onClick={onConfirm}>
          {t(`${copy}.confirm`)}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
