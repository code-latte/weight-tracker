"use client";

import Box from "@mui/material/Box";
import {
  WORKOUT_DESCRIPTION_MAX_LENGTH,
  WORKOUT_NAME_MAX_LENGTH,
  type WorkoutDetailsFormValues,
  type WorkoutDetailsFormViolations,
} from "@weight-tracker/core/application";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Card from "@/presentation/common/components/atoms/card";
import CardContent from "@/presentation/common/components/atoms/card-content";
import TextField from "@/presentation/common/components/atoms/text-field";
import Typography from "@/presentation/common/components/atoms/typography";
import WeekField from "@/presentation/workouts/components/molecules/week-field";

interface WorkoutDetailsCardProps {
  values: WorkoutDetailsFormValues;
  errors: WorkoutDetailsFormViolations;
  disabled?: boolean;
  /** The week is shown but cannot be changed, as for a workout its mesocycle week dates. */
  weekLocked?: boolean;
  /** Saving is in progress: the save action is disabled. */
  saving: boolean;
  onSave: () => void;
  onChange: (field: keyof WorkoutDetailsFormValues, value: string) => void;
  titleId: string;
}

const maxLengthOf: Partial<Record<keyof WorkoutDetailsFormValues, number>> = {
  name: WORKOUT_NAME_MAX_LENGTH,
  description: WORKOUT_DESCRIPTION_MAX_LENGTH,
};

/**
 * The workout's properties: its own data in editable fields with their save action. The screen owns and saves the
 * values.
 */
export default function WorkoutDetailsCard({
  values,
  errors,
  disabled = false,
  weekLocked = false,
  saving,
  onSave,
  onChange,
  titleId,
}: WorkoutDetailsCardProps) {
  const { t } = useTranslation("workouts");

  function field(name: keyof WorkoutDetailsFormValues) {
    const violation = errors[name];

    return {
      label: t(`createWorkout.fields.${name === "workoutDate" ? "date" : name}`),
      value: values[name],
      disabled,
      onChange: (event: { target: { value: string } }) => onChange(name, event.target.value),
      error: Boolean(violation),
      helperText: violation
        ? t(`createWorkout.validation.${violation}`, { max: maxLengthOf[name] })
        : undefined,
    };
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography id={titleId} variant="h6" component="h2" sx={{ mb: 2 }}>
          {t("workout.properties.title")}
        </Typography>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", sm: "minmax(0, 1fr) auto" },
              gap: 2,
            }}
          >
            <TextField {...field("name")} />
            <WeekField
              value={values.workoutDate}
              onChange={(monday) => onChange("workoutDate", monday)}
              disabled={disabled || weekLocked}
              error={
                errors.workoutDate ? t(`createWorkout.validation.${errors.workoutDate}`) : undefined
              }
            />
          </Box>
          <TextField {...field("description")} multiline minRows={2} />
          <Box sx={{ display: "flex", justifyContent: "flex-end" }}>
            <Button variant="contained" disabled={saving} onClick={onSave}>
              {t("workout.details.save")}
            </Button>
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}
