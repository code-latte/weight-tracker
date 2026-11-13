"use client";

import CheckCircleOutline from "@mui/icons-material/CheckCircleOutlineOutlined";
import RadioButtonUnchecked from "@mui/icons-material/RadioButtonUncheckedOutlined";
import Box from "@mui/material/Box";
import { alpha } from "@mui/material/styles";
import { minutesToText } from "@weight-tracker/core/application";
import type { SetRecord, WorkoutByIdExercise } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";
import Typography from "@/presentation/common/components/atoms/typography";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import { plannedRowsOf, restOf } from "@/presentation/workouts/workout-review";

interface ReviewSetsTableProps {
  exercise: WorkoutByIdExercise;
}

/** The effort line of the exercise and its planned sets against what the athlete recorded for each. */
export default function ReviewSetsTable({ exercise }: ReviewSetsTableProps) {
  const { t } = useTranslation("workouts");
  const { effortLabel } = useExerciseLabels();
  const rows = plannedRowsOf(exercise);
  const rest = restOf(exercise.restMinutes) ?? t("review.notAvailable");

  function targetText({ target }: (typeof rows)[number]): string {
    const known = ["Reps", "RepsRange", "MaxReps", "Duration", "Minutes"].includes(target.type);

    return t(`review.target.${known ? target.type : "other"}`, {
      value:
        target.type === "Minutes" && target.minutes != null
          ? minutesToText(target.minutes)
          : (target.value ?? ""),
      max: target.maxValue ?? "",
    });
  }

  function recordText(record: SetRecord): string {
    if (record.reps != null && record.weight != null) {
      return t("review.done.weighted", { reps: record.reps, weight: Number(record.weight) });
    }
    if (record.reps != null) {
      return t("review.done.reps", { reps: record.reps });
    }
    return t("review.done.seconds", { seconds: record.durationSeconds ?? "" });
  }

  return (
    <Box>
      {exercise.effort && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {t("workout.exercises.effortLine", {
            effort: effortLabel(exercise.effort, exercise.type),
          })}
        </Typography>
      )}
      <TableContainer>
        <Table size="small">
          <TableHead sx={{ bgcolor: "action.hover" }}>
            <TableRow>
              <TableCell>{t("review.table.set")}</TableCell>
              <TableCell>{t("review.table.target")}</TableCell>
              <TableCell>{t("review.table.done")}</TableCell>
              <TableCell>{t("review.table.rest")}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={row.number}>
                <TableCell>{row.number}</TableCell>
                <TableCell>{targetText(row)}</TableCell>
                <TableCell
                  sx={
                    row.record
                      ? (theme) => ({ bgcolor: alpha(theme.palette.success.main, 0.08) })
                      : undefined
                  }
                >
                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    {row.record ? (
                      <>
                        <CheckCircleOutline fontSize="small" color="success" aria-hidden />
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {recordText(row.record)}
                        </Typography>
                      </>
                    ) : (
                      <>
                        <RadioButtonUnchecked fontSize="small" color="disabled" aria-hidden />
                        <Typography variant="body2" color="text.secondary">
                          {t("review.table.pending")}
                        </Typography>
                      </>
                    )}
                  </Box>
                </TableCell>
                <TableCell>{rest}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
}
