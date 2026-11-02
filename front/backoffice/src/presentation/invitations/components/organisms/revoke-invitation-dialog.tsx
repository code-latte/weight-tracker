"use client";

import { useState } from "react";
import type { AthleteInvitation } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import Typography from "@/presentation/common/components/atoms/typography";
import useRevokeInvitation from "@/presentation/invitations/hooks/use-revoke-invitation";

interface RevokeInvitationDialogProps {
  /** The invitation to revoke; the dialog is open while there is one. */
  invitation: AthleteInvitation | null;
  /** Called on Cancelar, Escape or a click outside. `stale` means the list no longer matches the API. */
  onClose: (stale: boolean) => void;
  /** Called once the API has revoked the invitation. */
  onRevoked: () => void;
}

/** These failures mean the invitation changed or went away first, so the list on screen is out of date. */
const staleCodes = new Set(["CONFLICT", "NOT_FOUND"]);

interface RevokeConfirmationProps {
  invitation: AthleteInvitation;
  onCancel: () => void;
  onRevoked: () => void;
  /** Reports whether the last failure left the list out of date. */
  onFailed: (stale: boolean) => void;
}

/** The confirmation lives inside the dialog, which unmounts it on close: every opening starts without an error. */
function RevokeConfirmation({
  invitation,
  onCancel,
  onRevoked,
  onFailed,
}: RevokeConfirmationProps) {
  const { t } = useTranslation("invitations");
  const { t: tc } = useTranslation();
  const { submitting, errorCode, revoke } = useRevokeInvitation();

  async function handleConfirm() {
    const failure = await revoke(invitation);

    if (failure === null) {
      onRevoked();
    } else {
      onFailed(staleCodes.has(failure));
    }
  }

  return (
    <>
      <DialogTitle id="revoke-invitation-title">
        {t("revoke.title", { email: invitation.inviteEmail })}
      </DialogTitle>
      <DialogContent>
        {errorCode && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {t(`revoke.errors.${errorCode}`, { defaultValue: tc(`errors:${errorCode}`) })}
          </Alert>
        )}
        <Typography>{t("revoke.body")}</Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={submitting}>
          {t("revoke.cancel")}
        </Button>
        <Button
          variant="contained"
          color="error"
          disabled={submitting}
          onClick={() => void handleConfirm()}
        >
          {t("revoke.confirm")}
        </Button>
      </DialogActions>
    </>
  );
}

/** Asks before revoking an invitation. Escape, a click outside and Cancelar close it without a request. */
export default function RevokeInvitationDialog({
  invitation,
  onClose,
  onRevoked,
}: RevokeInvitationDialogProps) {
  // Keeps showing the last invitation while the dialog fades out after `invitation` becomes null.
  const [shown, setShown] = useState(invitation);
  const [stale, setStale] = useState(false);

  if (invitation !== null && invitation !== shown) {
    setShown(invitation);
    setStale(false);
  }

  return (
    <Dialog
      open={invitation !== null}
      onClose={() => onClose(stale)}
      fullWidth
      maxWidth="xs"
      aria-labelledby="revoke-invitation-title"
    >
      {shown && (
        <RevokeConfirmation
          key={shown.id}
          invitation={shown}
          onCancel={() => onClose(stale)}
          onRevoked={onRevoked}
          onFailed={setStale}
        />
      )}
    </Dialog>
  );
}
