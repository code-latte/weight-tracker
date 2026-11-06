"use client";

import { type FormEvent, useState } from "react";
import Box from "@mui/material/Box";
import {
  validateWeekWorkoutForm,
  WORKOUT_DESCRIPTION_MAX_LENGTH,
  WORKOUT_NAME_MAX_LENGTH,
  type WeekWorkoutFormValues,
  type WeekWorkoutFormViolations,
} from "@weight-tracker/core/application";
import type { DomainErrorCode } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import TextField from "@/presentation/common/components/atoms/text-field";

const maxLengthOf: Record<keyof WeekWorkoutFormValues, number> = {
  name: WORKOUT_NAME_MAX_LENGTH,
  description: WORKOUT_DESCRIPTION_MAX_LENGTH,
};

interface CreateWeekWorkoutDialogProps {
  weekNumber: number;
  creating: boolean;
  errorCode: DomainErrorCode | null;
  /** Called with valid, trimmed values; a blank description comes as "". */
  onCreate: (values: WeekWorkoutFormValues) => void;
  onClose: () => void;
}

/**
 * Creates a workout in one mesocycle week: only its name and description, since the mesocycle says who it is for and
 * the week dates it. Mounted per opening, so each one starts empty. Escape, a click outside and Cancelar close it
 * without a request.
 */
export default function CreateWeekWorkoutDialog({
  weekNumber,
  creating,
  errorCode,
  onCreate,
  onClose,
}: CreateWeekWorkoutDialogProps) {
  const { t } = useTranslation("mesocycles");
  const { t: tc } = useTranslation();
  const [values, setValues] = useState<WeekWorkoutFormValues>({ name: "", description: "" });
  const [errors, setErrors] = useState<WeekWorkoutFormViolations>({});
  const titleId = `week-${weekNumber}-workout-title`;

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const violations = validateWeekWorkoutForm(values);
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }
    onCreate({ name: values.name.trim(), description: values.description.trim() });
  }

  function field(name: keyof WeekWorkoutFormValues) {
    const violation = errors[name];

    return {
      label: t(`weekWorkout.fields.${name}`),
      value: values[name],
      disabled: creating,
      onChange: (event: { target: { value: string } }) => {
        setValues((current) => ({ ...current, [name]: event.target.value }));
        setErrors((current) => ({ ...current, [name]: undefined }));
      },
      error: Boolean(violation),
      helperText: violation
        ? t(`mesocycle.validation.${violation}`, { max: maxLengthOf[name] })
        : undefined,
    };
  }

  return (
    <Dialog
      open
      onClose={creating ? undefined : onClose}
      fullWidth
      maxWidth="sm"
      aria-labelledby={titleId}
    >
      <Box component="form" noValidate onSubmit={handleSubmit}>
        <DialogTitle id={titleId}>{t("weekWorkout.title", { number: weekNumber })}</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
            {errorCode && <Alert severity="error">{tc(`errors:${errorCode}`)}</Alert>}
            <TextField {...field("name")} required autoFocus />
            <TextField {...field("description")} multiline minRows={2} />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={creating}>
            {t("weekWorkout.actions.cancel")}
          </Button>
          <Button type="submit" variant="contained" disabled={creating}>
            {t("weekWorkout.actions.create")}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}
