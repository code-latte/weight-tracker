"use client";

import { type FormEvent, useState } from "react";
import Box from "@mui/material/Box";
import {
  type InviteAthleteFormViolations,
  validateInviteAthleteForm,
} from "@weight-tracker/core/application";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import TextField from "@/presentation/common/components/atoms/text-field";
import useInviteAthlete from "@/presentation/invitations/hooks/use-invite-athlete";

interface InviteAthleteDialogProps {
  open: boolean;
  onClose: () => void;
  /** Called once the API has created the invitation, before the dialog closes. */
  onInvited: () => void;
}

/** The form lives inside the dialog, which unmounts it on close: every opening starts empty and without errors. */
function InviteAthleteForm({ onClose, onInvited }: Omit<InviteAthleteDialogProps, "open">) {
  const { t } = useTranslation("invitations");
  const { t: tc } = useTranslation();
  const { inviting, errorCode, invite } = useInviteAthlete();
  const [email, setEmail] = useState("");
  const [errors, setErrors] = useState<InviteAthleteFormViolations>({});

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const violations = validateInviteAthleteForm({ email });
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    const invitation = await invite({ email: email.trim() });
    if (invitation) {
      onInvited();
      onClose();
    }
  }

  return (
    <Box component="form" noValidate onSubmit={(event) => void handleSubmit(event)}>
      <DialogTitle id="invite-athlete-title">{t("invite.title")}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          {errorCode && (
            <Alert severity="error">
              {t(`invite.errors.${errorCode}`, { defaultValue: tc(`errors:${errorCode}`) })}
            </Alert>
          )}
          <TextField
            label={t("invite.fields.email")}
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            disabled={inviting}
            error={Boolean(errors.email)}
            helperText={errors.email ? t(`invite.validation.${errors.email}`) : undefined}
            autoComplete="off"
            autoFocus
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={inviting}>
          {t("invite.cancel")}
        </Button>
        <Button type="submit" variant="contained" disabled={inviting}>
          {t("invite.submit")}
        </Button>
      </DialogActions>
    </Box>
  );
}

/** Invites an email to become the trainer's athlete. Escape, a click outside and Cancelar close it without a request. */
export default function InviteAthleteDialog({
  open,
  onClose,
  onInvited,
}: InviteAthleteDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="xs"
      aria-labelledby="invite-athlete-title"
    >
      <InviteAthleteForm onClose={onClose} onInvited={onInvited} />
    </Dialog>
  );
}
