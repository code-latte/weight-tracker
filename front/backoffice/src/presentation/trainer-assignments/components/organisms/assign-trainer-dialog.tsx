"use client";

import { type FormEvent, useState } from "react";
import Box from "@mui/material/Box";
import type { UserSummary } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Autocomplete from "@/presentation/common/components/atoms/autocomplete";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import { fullNameOf } from "@/presentation/common/full-name";
import { normalized } from "@/presentation/common/search-text";
import useAssignTrainer from "@/presentation/trainer-assignments/hooks/use-assign-trainer";

interface AssignTrainerDialogProps {
  /** The athlete to assign a trainer to; the dialog is open while there is one. */
  athlete: UserSummary | null;
  /** The trainers that can be chosen, in the order they are offered. */
  trainers: UserSummary[];
  /** Called on Cancelar, Escape or a click outside. `stale` means the list no longer matches the API. */
  onClose: (stale: boolean) => void;
  /** Called once the API has linked the athlete to the chosen trainer. */
  onAssigned: () => void;
}

/** These failures mean someone else changed the athlete or the trainer first, so the list on screen is out of date. */
const staleCodes = new Set(["CONFLICT", "NOT_FOUND"]);

/** "Ana García · ana@example.com", or only the email when the trainer has no name. */
function trainerLabel(trainer: UserSummary): string {
  const name = fullNameOf(trainer);

  return name ? `${name} · ${trainer.email}` : trainer.email;
}

/** Keeps the trainers whose full name or email contain the typed text, ignoring case and accents. */
function filterTrainers(trainers: UserSummary[], input: string): UserSummary[] {
  const text = normalized(input.trim());

  return trainers.filter((trainer) =>
    [fullNameOf(trainer) ?? "", trainer.email].some((field) => normalized(field).includes(text)),
  );
}

interface AssignTrainerFormProps {
  athlete: UserSummary;
  trainers: UserSummary[];
  onCancel: () => void;
  onAssigned: () => void;
  /** Reports whether the last failure left the list out of date. */
  onFailed: (stale: boolean) => void;
}

/** Lives inside the dialog, which unmounts it on close: every opening starts without a trainer and without errors. */
function AssignTrainerForm({
  athlete,
  trainers,
  onCancel,
  onAssigned,
  onFailed,
}: AssignTrainerFormProps) {
  const { t } = useTranslation("trainerAssignments");
  const { t: tc } = useTranslation();
  const { submitting, errorCode, assign } = useAssignTrainer();
  const [trainer, setTrainer] = useState<UserSummary | null>(null);
  const [missing, setMissing] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (trainer === null) {
      setMissing(true);
      return;
    }

    const failure = await assign({ trainerId: trainer.id, athleteId: athlete.id });
    if (failure === null) {
      onAssigned();
    } else {
      onFailed(staleCodes.has(failure));
    }
  }

  return (
    <Box component="form" noValidate onSubmit={(event) => void handleSubmit(event)}>
      <DialogTitle id="assign-trainer-title">
        {t("assign.title", { name: fullNameOf(athlete) ?? athlete.email })}
      </DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          {errorCode && (
            <Alert severity="error">
              {t(`assign.errors.${errorCode}`, { defaultValue: tc(`errors:${errorCode}`) })}
            </Alert>
          )}
          <Autocomplete
            label={t("assign.fields.trainer")}
            options={trainers}
            value={trainer}
            onChange={(_event, value) => {
              setTrainer(value);
              setMissing(false);
            }}
            getOptionLabel={trainerLabel}
            getOptionKey={(option) => option.id}
            isOptionEqualToValue={(option, value) => option.id === value.id}
            filterOptions={(options, { inputValue }) => filterTrainers(options, inputValue)}
            noOptionsText={t("assign.noOptions")}
            disabled={submitting}
            error={missing}
            helperText={missing ? t("assign.validation.required") : undefined}
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onCancel} disabled={submitting}>
          {t("assign.cancel")}
        </Button>
        <Button type="submit" variant="contained" disabled={submitting}>
          {t("assign.submit")}
        </Button>
      </DialogActions>
    </Box>
  );
}

/** Assigns a trainer to an athlete. Escape, a click outside and Cancelar close it without a request. */
export default function AssignTrainerDialog({
  athlete,
  trainers,
  onClose,
  onAssigned,
}: AssignTrainerDialogProps) {
  // Keeps showing the last athlete while the dialog fades out after `athlete` becomes null.
  const [shown, setShown] = useState(athlete);
  const [stale, setStale] = useState(false);

  if (athlete !== null && athlete !== shown) {
    setShown(athlete);
    setStale(false);
  }

  return (
    <Dialog
      open={athlete !== null}
      onClose={() => onClose(stale)}
      fullWidth
      maxWidth="sm"
      aria-labelledby="assign-trainer-title"
    >
      {shown && (
        <AssignTrainerForm
          key={shown.id}
          athlete={shown}
          trainers={trainers}
          onCancel={() => onClose(stale)}
          onAssigned={onAssigned}
          onFailed={setStale}
        />
      )}
    </Dialog>
  );
}
