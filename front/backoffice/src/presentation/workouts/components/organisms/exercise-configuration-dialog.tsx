"use client";

import { useId, useState } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import DeleteOutlined from "@mui/icons-material/DeleteOutlined";
import Box from "@mui/material/Box";
import {
  type ExerciseConfigurationViolations,
  type SetFormValues,
  type SetFormViolations,
  minutesFromText,
  minutesToText,
  targetFieldsOf,
  validateExerciseConfiguration,
  validateSetForm,
} from "@weight-tracker/core/application";
import type {
  AddExerciseRequest,
  SetPrescription,
  WorkoutExerciseDetail,
} from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
import Button from "@/presentation/common/components/atoms/button";
import Chip from "@/presentation/common/components/atoms/chip";
import Dialog from "@/presentation/common/components/atoms/dialog";
import DialogActions from "@/presentation/common/components/atoms/dialog-actions";
import DialogContent from "@/presentation/common/components/atoms/dialog-content";
import DialogTitle from "@/presentation/common/components/atoms/dialog-title";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import List from "@/presentation/common/components/atoms/list";
import ListItem from "@/presentation/common/components/atoms/list-item";
import MenuItem from "@/presentation/common/components/atoms/menu-item";
import Switch from "@/presentation/common/components/atoms/switch";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import ExerciseVideo from "@/presentation/workouts/components/molecules/exercise-video";
import type { WorkoutExerciseItem } from "@/presentation/workouts/components/organisms/workout-exercises-list";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import useSaveWorkoutExercise from "@/presentation/workouts/hooks/use-save-workout-exercise";
import type { ExercisesOwner } from "@/presentation/workouts/exercises-owner";

type Effort = NonNullable<AddExerciseRequest["effort"]>;

/** The API's `Effort` values, lightest first. */
const EFFORTS: Effort[] = ["LOW", "MIDLOW", "MID", "MIDHIGH", "HIGH"];

function isEffort(value: string): value is Effort {
  return (EFFORTS as string[]).includes(value);
}
/** The API's set target types. */
const TARGET_TYPES = ["Reps", "Duration", "MaxReps", "RepsRange", "Minutes"];

const emptyDraft: SetFormValues = { count: "", type: "Reps", value: "", maxValue: "" };

interface ExerciseConfigurationDialogProps {
  open: boolean;
  item: WorkoutExerciseItem;
  /** The item was chosen from the catalog and is not in the workout yet: Guardar adds it instead of replacing it. */
  adding: boolean;
  /** The id of the workout, or of the template when `owner` is `template`. */
  workoutId: string;
  /** What the exercise is saved to; a workout unless stated. */
  owner?: ExercisesOwner;
  /** Called with the entry the API answered with, after a successful save. */
  onSaved: (entry: WorkoutExerciseDetail) => void;
  onClose: () => void;
}

/** A minutes target is shown as `M` or `M:SS`, never as the API's decimal. */
function toFormValues({ count, target }: WorkoutExerciseDetail["sets"][number]): SetFormValues {
  let value = target.value == null ? "" : String(target.value);
  if (target.type === "Minutes") {
    value = target.minutes == null ? "" : minutesToText(target.minutes);
  }
  return {
    count: String(count),
    type: target.type,
    value,
    maxValue: target.maxValue == null ? "" : String(target.maxValue),
  };
}

/**
 * A listed set as the API expects it: numbers, and only the target fields its type needs. Minutes go as a decimal
 * with at most two decimals.
 */
function toPrescription(set: SetFormValues): SetPrescription {
  const needs = targetFieldsOf(set.type);
  const prescription: SetPrescription = { count: Number(set.count), targetType: set.type };

  if (set.type === "Minutes") {
    prescription.targetMinutes = minutesFromText(set.value);
  } else if (needs.value) {
    prescription.target = Number(set.value);
  }
  if (needs.maxValue) {
    prescription.maxTarget = Number(set.maxValue);
  }
  return prescription;
}

/**
 * Configures a workout exercise: its effort, rest, per side and its list of sets, built with the set form. Guardar
 * adds an exercise chosen from the catalog to the workout, or replaces a listed one's prescription. Cancelar, Escape and a click
 * outside close it without a request, except while the save is in progress. The caller keys it by the item, so every
 * opening starts from the stored values.
 */
export default function ExerciseConfigurationDialog({
  open,
  item,
  adding,
  workoutId,
  owner = "workout",
  onSaved,
  onClose,
}: ExerciseConfigurationDialogProps) {
  const { t } = useTranslation("workouts");
  const { t: tc } = useTranslation();
  const {
    nameOf,
    muscleLabel,
    bodyPartLabel,
    bodyPartColor,
    effortLabel,
    targetTypeLabel,
    setSummary,
  } = useExerciseLabels();
  const { saving, errorCode, save } = useSaveWorkoutExercise(owner);
  const titleId = useId();
  const perSideLabelId = useId();
  const [effort, setEffort] = useState(item.workoutExercise.effort);
  const [rest, setRest] = useState(
    item.workoutExercise.restMinutes == null ? "" : minutesToText(item.workoutExercise.restMinutes),
  );
  const [perSide, setPerSide] = useState(item.workoutExercise.perSide ?? false);
  const [sets, setSets] = useState<SetFormValues[]>(item.workoutExercise.sets.map(toFormValues));
  const [draft, setDraft] = useState<SetFormValues>(emptyDraft);
  const [draftErrors, setDraftErrors] = useState<SetFormViolations>({});
  const [errors, setErrors] = useState<ExerciseConfigurationViolations>({});

  const { workoutExercise, exercise } = item;
  const name = exercise ? nameOf(exercise) : workoutExercise.exerciseId;
  const efforts: string[] = isEffort(effort) || effort === "" ? EFFORTS : [...EFFORTS, effort];
  const draftNeeds = targetFieldsOf(draft.type);
  const positiveInteger = t("workout.configuration.validation.positiveInteger");
  const minutesFormat = t("workout.configuration.validation.minutesFormat");
  const draftIsMinutes = draft.type === "Minutes";

  function changeDraft(field: keyof SetFormValues, value: string) {
    setDraft((current) => ({ ...current, [field]: value }));
  }

  function addSet() {
    const violations = validateSetForm(draft);
    setDraftErrors(violations);
    if (Object.keys(violations).length > 0) {
      return;
    }
    setSets((current) => [...current, draft]);
    setDraft(emptyDraft);
    setErrors((current) => ({ ...current, sets: undefined }));
  }

  function removeSet(index: number) {
    setSets((current) => current.filter((_, position) => position !== index));
  }

  async function handleSave() {
    // An effort the API no longer offers counts as missing: it has to be chosen again.
    const violations = validateExerciseConfiguration({
      effort: isEffort(effort) ? effort : "",
      setCount: sets.length,
      rest,
    });
    setErrors(violations);
    if (Object.keys(violations).length > 0 || !isEffort(effort)) {
      return;
    }

    const entry = await save(workoutId, adding, {
      exerciseId: workoutExercise.exerciseId,
      effort,
      sets: sets.map(toPrescription),
      perSide,
      // An empty rest means none, so it is left out.
      ...(rest.trim() === "" ? {} : { restMinutes: minutesFromText(rest) }),
    });
    if (entry) {
      onSaved(entry);
    }
  }

  return (
    <Dialog
      open={open}
      // A save in progress keeps the dialog open, so its outcome is never lost.
      onClose={saving ? undefined : onClose}
      fullWidth
      maxWidth="md"
      aria-labelledby={titleId}
    >
      <DialogTitle id={titleId}>{t("workout.configuration.title")}</DialogTitle>
      <DialogContent>
        {/* The exercise on a narrow left column, so the video stays small; its configuration on the right. */}
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "minmax(0, 1fr)", md: "minmax(0, 3fr) minmax(0, 7fr)" },
            gap: 3,
            alignItems: "start",
            pt: 1,
          }}
        >
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            {exercise?.videoUrl && (
              <Box component="section" aria-label={t("workout.configuration.video")}>
                <ExerciseVideo url={exercise.videoUrl} title={name} />
              </Box>
            )}
            <Box>
              <Typography
                variant="subtitle1"
                component="h3"
                sx={{ fontWeight: 600, overflowWrap: "anywhere" }}
              >
                {name}
              </Typography>
              {exercise && (
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5, mt: 0.5 }}>
                  <Chip
                    label={bodyPartLabel(exercise.bodyPart)}
                    size="small"
                    variant="filled"
                    color={bodyPartColor(exercise.bodyPart)}
                  />
                  <Chip
                    label={muscleLabel(exercise.muscle)}
                    size="small"
                    variant="outlined"
                    color={bodyPartColor(exercise.bodyPart)}
                  />
                </Box>
              )}
            </Box>
          </Box>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            {errorCode && <Alert severity="error">{tc(`errors:${errorCode}`)}</Alert>}
            <TextField
              select
              label={t("workout.configuration.effort")}
              value={effort}
              disabled={saving}
              onChange={(event) => {
                setEffort(event.target.value);
                setErrors((current) => ({ ...current, effort: undefined }));
              }}
              error={Boolean(errors.effort)}
              helperText={errors.effort ? t("workout.configuration.validation.effort") : undefined}
            >
              {efforts.map((code) => (
                <MenuItem key={code} value={code}>
                  {effortLabel(code, exercise?.type)}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label={t("workout.configuration.rest")}
              value={rest}
              disabled={saving}
              onChange={(event) => {
                setRest(event.target.value);
                setErrors((current) => ({ ...current, rest: undefined }));
              }}
              error={Boolean(errors.rest)}
              helperText={errors.rest ? minutesFormat : t("workout.configuration.restHint")}
            />
            <Box>
              <Typography variant="subtitle2" component="h4">
                {t("workout.configuration.sets")}
              </Typography>
              {sets.length === 0 ? (
                <Typography
                  variant="body2"
                  color={errors.sets ? "error" : "text.secondary"}
                  sx={{ mt: 0.5 }}
                >
                  {errors.sets
                    ? t("workout.configuration.validation.sets")
                    : t("workout.configuration.noSets")}
                </Typography>
              ) : (
                <List dense aria-label={t("workout.configuration.sets")}>
                  {sets.map((set, index) => (
                    <ListItem
                      // Sets have no id of their own; their position is their identity in this list.
                      key={index}
                      divider
                      disableGutters
                      secondaryAction={
                        <IconButton
                          edge="end"
                          size="small"
                          aria-label={t("workout.configuration.removeSet")}
                          disabled={saving}
                          onClick={() => removeSet(index)}
                        >
                          <DeleteOutlined fontSize="small" />
                        </IconButton>
                      }
                    >
                      <Typography variant="body2">{setSummary({ ...set, perSide })}</Typography>
                    </ListItem>
                  ))}
                </List>
              )}
            </Box>
            <Box
              role="group"
              aria-label={t("workout.configuration.addSet")}
              sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 1 }}
            >
              <TextField
                label={t("workout.configuration.setCount")}
                type="number"
                size="small"
                value={draft.count}
                disabled={saving}
                onChange={(event) => changeDraft("count", event.target.value)}
                error={Boolean(draftErrors.count)}
                helperText={draftErrors.count ? positiveInteger : undefined}
              />
              <TextField
                select
                label={t("workout.configuration.targetType")}
                size="small"
                value={draft.type}
                disabled={saving}
                onChange={(event) => changeDraft("type", event.target.value)}
              >
                {TARGET_TYPES.map((type) => (
                  <MenuItem key={type} value={type}>
                    {targetTypeLabel(type)}
                  </MenuItem>
                ))}
              </TextField>
              {draftNeeds.value && (
                <TextField
                  label={t("workout.configuration.targetValue")}
                  // Minutes take `M:SS`, which a number field would refuse.
                  type={draftIsMinutes ? "text" : "number"}
                  size="small"
                  value={draft.value}
                  disabled={saving}
                  onChange={(event) => changeDraft("value", event.target.value)}
                  error={Boolean(draftErrors.value)}
                  helperText={
                    draftErrors.value === "minutesFormat"
                      ? minutesFormat
                      : draftErrors.value
                        ? positiveInteger
                        : draftIsMinutes
                          ? t("workout.configuration.minutesHint")
                          : undefined
                  }
                />
              )}
              {draftNeeds.maxValue && (
                <TextField
                  label={t("workout.configuration.targetMaxValue")}
                  type="number"
                  size="small"
                  value={draft.maxValue}
                  disabled={saving}
                  onChange={(event) => changeDraft("maxValue", event.target.value)}
                  error={Boolean(draftErrors.maxValue)}
                  helperText={draftErrors.maxValue ? positiveInteger : undefined}
                />
              )}
              {/* Per side applies to every set of the exercise; it sits in the free cell beside Valor. */}
              <Box sx={{ display: "flex", alignItems: "center", minHeight: 40 }}>
                <Switch
                  checked={perSide}
                  disabled={saving}
                  onChange={(event) => setPerSide(event.target.checked)}
                  slotProps={{ input: { "aria-labelledby": perSideLabelId } }}
                />
                <Typography id={perSideLabelId} variant="body2">
                  {t("workout.configuration.perSide")}
                </Typography>
              </Box>
            </Box>
            <Button
              variant="outlined"
              startIcon={<AddOutlined />}
              disabled={saving}
              onClick={addSet}
              sx={{ alignSelf: "flex-start" }}
            >
              {t("workout.configuration.addSet")}
            </Button>
          </Box>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={saving}>
          {t("workout.configuration.cancel")}
        </Button>
        <Button variant="contained" disabled={saving} onClick={() => void handleSave()}>
          {t("workout.configuration.save")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
