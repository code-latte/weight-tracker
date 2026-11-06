"use client";

import { type FormEvent, useState } from "react";
import Box from "@mui/material/Box";
import {
  type CreateWorkoutTemplateFormValues,
  type CreateWorkoutTemplateFormViolations,
  validateCreateWorkoutTemplateForm,
  WORKOUT_TEMPLATE_DESCRIPTION_MAX_LENGTH,
  WORKOUT_TEMPLATE_NAME_MAX_LENGTH,
} from "@weight-tracker/core/application";
import type { CreateWorkoutTemplateRequest } from "@weight-tracker/core/domain";
import { useRouter } from "next/navigation";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import TextField from "@/presentation/common/components/atoms/text-field";
import PersonPicker from "@/presentation/common/components/molecules/person-picker";
import useAssignees from "@/presentation/common/hooks/use-assignees";
import useCurrentUser from "@/presentation/common/hooks/use-current-user";
import useCreateWorkoutTemplate from "@/presentation/workouts/hooks/use-create-workout-template";

interface CreateWorkoutTemplateDialogProps {
  open: boolean;
  onClose: () => void;
}

const emptyValues: CreateWorkoutTemplateFormValues = { name: "", description: "", trainerId: "" };

const maxLengthOf: Partial<Record<keyof CreateWorkoutTemplateFormValues, number>> = {
  name: WORKOUT_TEMPLATE_NAME_MAX_LENGTH,
  description: WORKOUT_TEMPLATE_DESCRIPTION_MAX_LENGTH,
};

/** Builds the request from valid values: trimmed, and without the optional keys that do not apply. */
function toRequest(
  values: CreateWorkoutTemplateFormValues,
  withTrainer: boolean,
): CreateWorkoutTemplateRequest {
  const request: CreateWorkoutTemplateRequest = { name: values.name.trim() };
  const description = values.description.trim();

  if (description !== "") {
    request.description = description;
  }
  if (withTrainer) {
    request.trainerId = values.trainerId.trim();
  }
  return request;
}

/** The form lives inside the dialog, which unmounts it on close: every opening starts empty and without errors. */
function CreateWorkoutTemplateForm({ onClose }: { onClose: () => void }) {
  const { t } = useTranslation("workouts");
  const { t: tc } = useTranslation();
  const router = useRouter();
  const { profile } = useCurrentUser();
  const { creating, errorCode, create } = useCreateWorkoutTemplate();
  const [values, setValues] = useState<CreateWorkoutTemplateFormValues>(emptyValues);
  const [errors, setErrors] = useState<CreateWorkoutTemplateFormViolations>({});
  // Mirrors whom the API requires a trainer from: an admin never owns a template. The API keeps the rule.
  const withTrainer = profile?.role === "ADMIN";
  // Only an admin needs the people: a trainer is never asked for anyone.
  const assignees = useAssignees(withTrainer ? "ADMIN" : null);
  const optionsLoaded = !withTrainer || assignees.status === "loaded";

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const violations = validateCreateWorkoutTemplateForm(values, { withTrainer });
    setErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }

    const template = await create(toRequest(values, withTrainer));
    if (template) {
      onClose();
      router.push(`/workout-template?id=${encodeURIComponent(template.id)}`);
    }
  }

  function field(name: "name" | "description") {
    const violation = errors[name];

    return {
      label: t(`createTemplate.fields.${name}`),
      value: values[name],
      disabled: creating,
      onChange: (event: { target: { value: string } }) =>
        setValues((current) => ({ ...current, [name]: event.target.value })),
      error: Boolean(violation),
      helperText: violation
        ? t(`createWorkout.validation.${violation}`, { max: maxLengthOf[name] })
        : undefined,
    };
  }

  return (
    <Box component="form" noValidate onSubmit={(event) => void handleSubmit(event)}>
      <DialogTitle id="create-workout-template-title">{t("createTemplate.title")}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
          {withTrainer && assignees.status === "failed" && assignees.errorCode && (
            <Alert
              severity="error"
              action={
                <Button color="inherit" size="small" onClick={assignees.reload}>
                  {t("createWorkout.retry")}
                </Button>
              }
            >
              {tc(`errors:${assignees.errorCode}`)}
            </Alert>
          )}
          {errorCode && <Alert severity="error">{tc(`errors:${errorCode}`)}</Alert>}
          <TextField {...field("name")} autoFocus />
          <TextField {...field("description")} multiline minRows={2} />
          {withTrainer && (
            <PersonPicker
              label={t("createTemplate.fields.trainerId")}
              value={values.trainerId}
              onChange={(id) => {
                setValues((current) => ({ ...current, trainerId: id }));
                setErrors((current) => ({ ...current, trainerId: undefined }));
              }}
              disabled={creating || !optionsLoaded}
              error={errors.trainerId ? t("createWorkout.validation.trainerRequired") : undefined}
              options={assignees.trainers}
              noOptionsText={t("createWorkout.noTrainers")}
            />
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={creating}>
          {t("createTemplate.actions.cancel")}
        </Button>
        <Button type="submit" variant="contained" disabled={creating || !optionsLoaded}>
          {t("createTemplate.actions.create")}
        </Button>
      </DialogActions>
    </Box>
  );
}

/** Creates a template and opens its screen. Escape, a click outside and Cancelar close it without a request. */
export default function CreateWorkoutTemplateDialog({
  open,
  onClose,
}: CreateWorkoutTemplateDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="sm"
      aria-labelledby="create-workout-template-title"
    >
      <CreateWorkoutTemplateForm onClose={onClose} />
    </Dialog>
  );
}
