"use client";

import { type FormEvent, useState } from "react";
import Box from "@mui/material/Box";
import {
  MESOCYCLE_OBJECTIVE_MAX_LENGTH,
  type MesocycleWeekFormValues,
  type MesocycleWeekFormViolations,
  validateMesocycleWeekForm,
} from "@weight-tracker/core/application";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import MenuItem from "@/presentation/common/components/atoms/menu-item";
import TextField from "@/presentation/common/components/atoms/text-field";

/** The API's week types, in the order a block usually runs through them. */
const TRAINING_WEEK_TYPES = [
  "ADAPTATION",
  "ACCUMULATION",
  "INTENSIFICATION",
  "REALIZATION",
  "DELOAD",
  "RECOVERY",
] as const;

interface MesocycleWeekDialogProps {
  weekNumber: number;
  /** The week's stored values, or empty ones for a new week. */
  initial: MesocycleWeekFormValues;
  saving: boolean;
  errorCode: DomainErrorCode | null;
  /** Called with valid, trimmed values. */
  onSave: (values: MesocycleWeekFormValues) => void;
  onClose: () => void;
}

/**
 * Defines or redefines one week: its type and objective. Mounted per opening, so each one starts from `initial`.
 * Escape, a click outside and Cancelar close it without a request.
 */
export default function MesocycleWeekDialog({
  weekNumber,
  initial,
  saving,
  errorCode,
  onSave,
  onClose,
}: MesocycleWeekDialogProps) {
  const { t } = useTranslation("mesocycles");
  const { t: tc } = useTranslation();
  const [values, setValues] = useState<MesocycleWeekFormValues>(initial);
  const [errors, setErrors] = useState<MesocycleWeekFormViolations>({});
  const titleId = `mesocycle-week-${weekNumber}-title`;

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const violations = validateMesocycleWeekForm(values);
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }
    onSave({ trainingWeekType: values.trainingWeekType, objective: values.objective.trim() });
  }

  function change(name: keyof MesocycleWeekFormValues, value: string) {
    setValues((current) => ({ ...current, [name]: value }));
    setErrors((current) => ({ ...current, [name]: undefined }));
  }

  function messageOf(name: keyof MesocycleWeekFormValues): string | undefined {
    const violation = errors[name];

    return violation
      ? t(`mesocycle.validation.${violation}`, { max: MESOCYCLE_OBJECTIVE_MAX_LENGTH })
      : undefined;
  }

  return (
    <Dialog
      open
      onClose={saving ? undefined : onClose}
      fullWidth
      maxWidth="sm"
      aria-labelledby={titleId}
    >
      <Box component="form" noValidate onSubmit={handleSubmit}>
        <DialogTitle id={titleId}>{t("weeks.week", { number: weekNumber })}</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
            {errorCode && <Alert severity="error">{tc(`errors:${errorCode}`)}</Alert>}
            <TextField
              select
              required
              label={t("weeks.dialog.type")}
              value={values.trainingWeekType}
              disabled={saving}
              onChange={(event) => change("trainingWeekType", event.target.value)}
              error={Boolean(errors.trainingWeekType)}
              helperText={messageOf("trainingWeekType")}
            >
              {TRAINING_WEEK_TYPES.map((type) => (
                <MenuItem key={type} value={type}>
                  {t(`trainingWeekType.${type}`)}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              required
              multiline
              minRows={3}
              label={t("weeks.dialog.objective")}
              value={values.objective}
              disabled={saving}
              onChange={(event) => change("objective", event.target.value)}
              error={Boolean(errors.objective)}
              helperText={
                messageOf("objective") ??
                `${values.objective.length} / ${MESOCYCLE_OBJECTIVE_MAX_LENGTH}`
              }
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={saving}>
            {t("weeks.dialog.cancel")}
          </Button>
          <Button type="submit" variant="contained" disabled={saving}>
            {t("weeks.dialog.save")}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
