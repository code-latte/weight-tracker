"use client";

import type { ReactNode } from "react";
import Box from "@mui/material/Box";
import {
  WORKOUT_DESCRIPTION_MAX_LENGTH,
  WORKOUT_NAME_MAX_LENGTH,
  type WorkoutDetailsFormValues,
  type WorkoutDetailsFormViolations,
} from "@weight-tracker/core/application";
import { useTranslation } from "react-i18next";
import Alert from "@/presentation/common/components/atoms/alert";
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
  /** The card title; the workout's properties unless stated. */
  title?: string;
  /** Whether the week field is shown: a template has no week. */
  showWeek?: boolean;
  /** An action shown right before the save button, such as saving the workout as a template. */
  secondaryAction?: ReactNode;
  /** An action shown right after the save button, such as publishing the workout. */
  statusAction?: ReactNode;
  /** The athlete started the workout: the fields are disabled and a notice replaces the save and status actions. */
  locked?: boolean;
}

const maxLengthOf: Partial<Record<keyof WorkoutDetailsFormValues, number>> = {
  name: WORKOUT_NAME_MAX_LENGTH,
  description: WORKOUT_DESCRIPTION_MAX_LENGTH,
};

/**
 * The properties of a workout, or of a template without its week: its own data in editable fields with their save action. The screen owns and saves the
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
  title,
  showWeek = true,
  secondaryAction,
  statusAction,
  locked = false,
}: WorkoutDetailsCardProps) {
  const { t } = useTranslation("workouts");

  function field(name: keyof WorkoutDetailsFormValues) {
    const violation = errors[name];

    return {
      label: t(`createWorkout.fields.${name === "workoutDate" ? "date" : name}`),
      value: values[name],
      disabled: disabled || locked,
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
          {title ?? t("workout.properties.title")}
        </Typography>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", sm: showWeek ? "minmax(0, 1fr) auto" : "1fr" },
              gap: 2,
            }}
          >
            <TextField {...field("name")} />
            {showWeek && (
              <WeekField
                value={values.workoutDate}
                onChange={(monday) => onChange("workoutDate", monday)}
                disabled={disabled || weekLocked || locked}
                error={
                  errors.workoutDate
                    ? t(`createWorkout.validation.${errors.workoutDate}`)
                    : undefined
                }
              />
            )}
          </Box>
          <TextField {...field("description")} multiline minRows={2} />
          <Box sx={{ display: "flex", justifyContent: "flex-end", alignItems: "center", gap: 1 }}>
            {secondaryAction}
            {locked ? (
              <Alert severity="info" sx={{ py: 0 }}>
                {t("workout.status.locked")}
              </Alert>
            ) : (
              <>
                <Button variant="contained" disabled={saving} onClick={onSave}>
                  {t("workout.details.save")}
                </Button>
                {statusAction}
              </>
            )}
          </Box>
        </Box>
      </CardContent>
    </Card>
  );
}
