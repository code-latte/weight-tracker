"use client";

import { useId } from "react";
import CloseOutlined from "@mui/icons-material/CloseOutlined";
import { useTranslation } from "react-i18next";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import ExerciseVideo from "@/presentation/workouts/components/molecules/exercise-video";

interface ExerciseVideoDialogProps {
  /** The exercise's name, which titles the dialog and names the player. */
  name: string;
  url: string;
  onClose: () => void;
}

/**
 * Plays an exercise's video. The caller mounts it per opening, so a video never keeps playing behind a closed dialog.
 * Cerrar, Escape and a click outside close it.
 */
export default function ExerciseVideoDialog({ name, url, onClose }: ExerciseVideoDialogProps) {
  const { t } = useTranslation("workouts");
  const titleId = useId();

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="md" aria-labelledby={titleId}>
      <DialogTitle
        id={titleId}
        sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 1 }}
      >
        {name}
        <IconButton edge="end" aria-label={t("workout.catalog.close")} onClick={onClose}>
          <CloseOutlined />
        </IconButton>
      </DialogTitle>
      <DialogContent>
        <ExerciseVideo url={url} title={name} />
      </DialogContent>
    </Dialog>
  );
}
