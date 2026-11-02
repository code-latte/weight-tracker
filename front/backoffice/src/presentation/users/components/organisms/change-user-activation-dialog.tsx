"use client";

import { useState } from "react";
import type { UserSummary } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import Typography from "@/presentation/common/components/atoms/typography";
import { fullNameOf } from "@/presentation/common/full-name";
import useChangeUserActivation from "@/presentation/users/hooks/use-change-user-activation";

export type ActivationChange = "activated" | "deactivated";

interface ChangeUserActivationDialogProps {
  /** The user to activate or deactivate; the dialog is open while there is one. */
  user: UserSummary | null;
  /** Called on Cancelar, Escape or a click outside. `stale` means the list no longer matches the API. */
  onClose: (stale: boolean) => void;
  /** Called once the API has changed the user. */
  onChanged: (change: ActivationChange) => void;
}

/** These failures mean someone else changed the user first, so the list on screen is out of date. */
const staleCodes = new Set(["CONFLICT", "NOT_FOUND"]);

/**
 * The confirmation lives inside the dialog, which unmounts it on close: every opening starts without an error.
 */
interface ActivationConfirmationProps {
  user: UserSummary;
  onCancel: () => void;
  onChanged: (change: ActivationChange) => void;
  /** Reports whether the last failure left the list out of date. */
  onFailed: (stale: boolean) => void;
}

function ActivationConfirmation({
  user,
  onCancel,
  onChanged,
  onFailed,
}: ActivationConfirmationProps) {
  const { t } = useTranslation("users");
  const { t: tc } = useTranslation();
  const { submitting, errorCode, change } = useChangeUserActivation();
  const action = user.isActive ? "deactivate" : "activate";
  const name = fullNameOf(user) ?? user.email;

  async function handleConfirm() {
    const failure = await change(user);

    if (failure === null) {
      onChanged(user.isActive ? "deactivated" : "activated");
    } else {
      onFailed(staleCodes.has(failure));
    }
  }

  return (
    <>
      <DialogTitle id="change-user-activation-title">
        {t(`activation.${action}.title`, { name })}
      </DialogTitle>
      <DialogContent>
        {errorCode && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {t(`activation.errors.${errorCode}`, { defaultValue: tc(`errors:${errorCode}`) })}
          </Alert>
        )}
        <Typography>{t(`activation.${action}.body`)}</Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={submitting}>
          {t("activation.cancel")}
        </Button>
        <Button
          variant="contained"
          color={user.isActive ? "error" : "primary"}
          disabled={submitting}
          onClick={() => void handleConfirm()}
        >
          {t(`activation.${action}.confirm`)}
        </Button>
      </DialogActions>
    </>
  );
}

/** Asks before activating or deactivating a user. Escape, a click outside and Cancelar close it without a request. */
export default function ChangeUserActivationDialog({
  user,
  onClose,
  onChanged,
}: ChangeUserActivationDialogProps) {
  // Keeps showing the last user while the dialog fades out after `user` becomes null.
  const [shown, setShown] = useState(user);
  const [stale, setStale] = useState(false);

  if (user !== null && user !== shown) {
    setShown(user);
    setStale(false);
  }

  return (
    <Dialog
      open={user !== null}
      onClose={() => onClose(stale)}
      fullWidth
      maxWidth="xs"
      aria-labelledby="change-user-activation-title"
    >
      {shown && (
        <ActivationConfirmation
          key={shown.id}
          user={shown}
          onCancel={() => onClose(stale)}
          onChanged={onChanged}
          onFailed={setStale}
        />
      )}
    </Dialog>
  );
}
