"use client";

import { useId } from "react";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import Typography from "@/presentation/common/components/atoms/typography";

interface BreakSuperSeriesDialogProps {
  open: boolean;
  /** While the break is in progress both buttons are disabled and the dialog cannot be dismissed. */
  pending: boolean;
  /** Called on Cancelar, Escape or a click outside. */
  onCancel: () => void;
  onConfirm: () => void;
}

/**
 * Asks before breaking a superseries. It sends nothing itself: the screen sends the break and shows its outcome.
 */
export default function BreakSuperSeriesDialog({
  open,
  pending,
  onCancel,
  onConfirm,
}: BreakSuperSeriesDialogProps) {
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
      <DialogTitle id={titleId}>{t("workout.superSeries.breakDialog.title")}</DialogTitle>
      <DialogContent>
        <Typography>{t("workout.superSeries.breakDialog.message")}</Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={pending}>
          {t("workout.superSeries.breakDialog.cancel")}
        </Button>
        <Button variant="contained" color="error" disabled={pending} onClick={onConfirm}>
          {t("workout.superSeries.breakDialog.confirm")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
