"use client";

import { useState } from "react";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import Typography from "@/presentation/common/components/atoms/typography";
import useEndTrainerAthlete from "@/presentation/common/hooks/use-end-trainer-athlete";

/** The relationship to end: whose trainer, which athlete, and the name the confirmation shows. */
export interface EndRelationshipTarget {
  trainerId: string;
  athleteId: string;
  athleteName: string;
  /** Whether the trainer ending it is the one asking, so the confirmation speaks to them directly. */
  ownRelationship?: boolean;
}

interface EndTrainerAthleteDialogProps {
  /** The relationship to end; the dialog is open while there is one. */
  target: EndRelationshipTarget | null;
  /** Called on Cancelar, Escape or a click outside. `stale` means the list no longer matches the API. */
  onClose: (stale: boolean) => void;
  /** Called once the API has ended the relationship. */
  onEnded: () => void;
}

/** These failures mean the relationship already ended, so the list on screen is out of date. */
const staleCodes = new Set(["CONFLICT", "NOT_FOUND"]);

interface EndConfirmationProps {
  target: EndRelationshipTarget;
  onCancel: () => void;
  onEnded: () => void;
  /** Reports whether the last failure left the list out of date. */
  onFailed: (stale: boolean) => void;
}

/** Lives inside the dialog, which unmounts it on close: every opening starts without an error. */
function EndConfirmation({ target, onCancel, onEnded, onFailed }: EndConfirmationProps) {
  const { t } = useTranslation();
  const { submitting, errorCode, end } = useEndTrainerAthlete();

  async function handleConfirm() {
    const failure = await end({ trainerId: target.trainerId, athleteId: target.athleteId });

    if (failure === null) {
      onEnded();
    } else {
      onFailed(staleCodes.has(failure));
    }
  }

  return (
    <>
      <DialogTitle id="end-trainer-athlete-title">
        {t("endRelationship.title", { name: target.athleteName })}
      </DialogTitle>
      <DialogContent>
        {errorCode && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {t(`endRelationship.errors.${errorCode}`, { defaultValue: t(`errors:${errorCode}`) })}
          </Alert>
        )}
        <Typography>
          {t(target.ownRelationship ? "endRelationship.ownBody" : "endRelationship.body")}
        </Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={submitting}>
          {t("endRelationship.cancel")}
        </Button>
        <Button
          variant="contained"
          color="error"
          disabled={submitting}
          onClick={() => void handleConfirm()}
        >
          {t("endRelationship.confirm")}
        </Button>
      </DialogActions>
    </>
  );
}

/** Asks before ending a trainer–athlete relationship. Escape, a click outside and Cancelar close it without a request. */
export default function EndTrainerAthleteDialog({
  target,
  onClose,
  onEnded,
}: EndTrainerAthleteDialogProps) {
  // Keeps showing the last target while the dialog fades out after `target` becomes null.
  const [shown, setShown] = useState(target);
  const [stale, setStale] = useState(false);

  if (target !== null && target !== shown) {
    setShown(target);
    setStale(false);
  }

  return (
    <Dialog
      open={target !== null}
      onClose={() => onClose(stale)}
      fullWidth
      maxWidth="xs"
      aria-labelledby="end-trainer-athlete-title"
    >
      {shown && (
        <EndConfirmation
          key={`${shown.trainerId}-${shown.athleteId}`}
          target={shown}
          onCancel={() => onClose(stale)}
          onEnded={onEnded}
          onFailed={setStale}
        />
      )}
    </Dialog>
  );
}
