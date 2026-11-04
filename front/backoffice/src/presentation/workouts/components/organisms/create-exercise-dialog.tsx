"use client";

import { type FormEvent, useId, useMemo, useState } from "react";
import Box from "@mui/material/Box";
import {
  EXERCISE_NAME_MAX_LENGTH,
  EXERCISE_NAME_MIN_LENGTH,
  EXERCISE_VIDEO_URL_MAX_LENGTH,
  type ExerciseFormValues,
  type ExerciseFormViolations,
  validateExerciseForm,
} from "@weight-tracker/core/application";
import type { CreateExerciseRequest, Exercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import ListSubheader from "@/presentation/common/components/atoms/list-subheader";
import MenuItem from "@/presentation/common/components/atoms/menu-item";
import TextField from "@/presentation/common/components/atoms/text-field";
import useCreateExercise from "@/presentation/workouts/hooks/use-create-exercise";
import useExerciseLabels, {
  EXERCISE_MUSCLES,
  EXERCISE_TYPES,
  MUSCLE_BODY_PARTS,
} from "@/presentation/workouts/hooks/use-exercise-labels";

interface CreateExerciseDialogProps {
  open: boolean;
  /** Called with the exercise the API answered with, after a successful creation. */
  onCreated: (exercise: Exercise) => void;
  onClose: () => void;
}

const emptyValues: ExerciseFormValues = { name: "", muscle: "", type: "", videoUrl: "" };

/** The request from valid values: trimmed, with the link only when one was typed. */
function toRequest(values: ExerciseFormValues, languageCode: string): CreateExerciseRequest | null {
  const muscle = EXERCISE_MUSCLES.find((code) => code === values.muscle);
  const type = EXERCISE_TYPES.find((code) => code === values.type);
  const videoUrl = values.videoUrl.trim();

  if (!muscle || !type) {
    return null;
  }
  return {
    languageCode,
    name: values.name.trim(),
    muscle,
    type,
    ...(videoUrl !== "" && { videoUrl }),
  };
}

/**
 * The dialog is mounted per opening, so every opening starts with empty fields and no message. Creating sends the
 * exercise in the language the screen is shown in. Cancelar, Escape and a click outside close it without a request,
 * except while the creation is in progress.
 */
function CreateExerciseForm({ onCreated, onClose }: Omit<CreateExerciseDialogProps, "open">) {
  const { t, i18n } = useTranslation("workouts");
  const { t: tc } = useTranslation();
  const { muscleLabel, bodyPartLabel, typeLabel } = useExerciseLabels();
  const { creating, errorCode, create } = useCreateExercise();
  const titleId = useId();
  const [values, setValues] = useState<ExerciseFormValues>(emptyValues);
  const [errors, setErrors] = useState<ExerciseFormViolations>({});

  // The muscles grouped by body part, both sorted by their label; a group's header cannot be chosen.
  const muscleGroups = useMemo(
    () =>
      [...new Set(EXERCISE_MUSCLES.map((code) => MUSCLE_BODY_PARTS[code]))]
        .map((bodyPart) => ({
          bodyPart,
          label: bodyPartLabel(bodyPart),
          muscles: EXERCISE_MUSCLES.filter((code) => MUSCLE_BODY_PARTS[code] === bodyPart)
            .map((code) => ({ code, label: muscleLabel(code) }))
            .sort((a, b) => a.label.localeCompare(b.label)),
        }))
        .sort((a, b) => a.label.localeCompare(b.label)),
    [muscleLabel, bodyPartLabel],
  );

  const limits = {
    min: EXERCISE_NAME_MIN_LENGTH,
    max: EXERCISE_NAME_MAX_LENGTH,
    videoUrlMax: EXERCISE_VIDEO_URL_MAX_LENGTH,
  };

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const violations = validateExerciseForm(values);
    setErrors(violations);
    const request = toRequest(values, i18n.language);
    if (Object.keys(violations).length > 0 || !request) {
      return;
    }

    const exercise = await create(request);
    if (exercise) {
      onCreated(exercise);
    }
  }

  function field(name: keyof ExerciseFormValues) {
    const violation = errors[name];

    return {
      label: t(`exerciseCatalog.create.fields.${name}`),
      value: values[name],
      disabled: creating,
      onChange: (event: { target: { value: string } }) =>
        setValues((current) => ({ ...current, [name]: event.target.value })),
      error: Boolean(violation),
      helperText: violation
        ? t(`exerciseCatalog.create.validation.${violation}`, {
            min: limits.min,
            max: name === "videoUrl" ? limits.videoUrlMax : limits.max,
          })
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
      <Box component="form" noValidate onSubmit={(event) => void handleSubmit(event)}>
        <DialogTitle id={titleId}>{t("exerciseCatalog.create.title")}</DialogTitle>
        <DialogContent>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2, pt: 1 }}>
            {errorCode && <Alert severity="error">{tc(`errors:${errorCode}`)}</Alert>}
            <TextField {...field("name")} autoFocus />
            <TextField {...field("muscle")} select>
              {muscleGroups.flatMap(({ bodyPart, label, muscles }) => [
                <ListSubheader key={bodyPart}>{`— ${label} —`}</ListSubheader>,
                ...muscles.map((muscle) => (
                  <MenuItem key={muscle.code} value={muscle.code}>
                    {muscle.label}
                  </MenuItem>
                )),
              ])}
            </TextField>
            <TextField {...field("type")} select>
              {EXERCISE_TYPES.map((code) => (
                <MenuItem key={code} value={code}>
                  {typeLabel(code)}
                </MenuItem>
              ))}
            </TextField>
            <TextField {...field("videoUrl")} />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={creating}>
            {t("exerciseCatalog.create.actions.cancel")}
          </Button>
          <Button type="submit" variant="contained" disabled={creating}>
            {t("exerciseCatalog.create.actions.create")}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  );
}

/** Creates an exercise owned by the signed-in user. Nothing is mounted while it is closed. */
export default function CreateExerciseDialog({
  open,
  onCreated,
  onClose,
}: CreateExerciseDialogProps) {
  return open ? <CreateExerciseForm onCreated={onCreated} onClose={onClose} /> : null;
}
