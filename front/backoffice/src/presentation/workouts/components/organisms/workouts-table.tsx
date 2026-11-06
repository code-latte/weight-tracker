"use client";

import EditOutlined from "@mui/icons-material/EditOutlined";
import type { WorkoutListItem } from "@weight-tracker/core/domain";
import NextLink from "next/link";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";
import Typography from "@/presentation/common/components/atoms/typography";
import { fullNameOf } from "@/presentation/common/full-name";
import useWeekLabel from "@/presentation/workouts/hooks/use-week-label";
import { useTranslation } from "react-i18next";

interface WorkoutsTableProps {
  workouts: WorkoutListItem[];
  /** Whether the trainer column is rendered at all. */
  showTrainer: boolean;
}

/** The person's full name, or their email when they have no name. */
function displayNameOf(person: WorkoutListItem["athlete"]): string {
  return fullNameOf(person) ?? person.email;
}

/** The workouts in the order the API returns them, one row each. */
export default function WorkoutsTable({ workouts, showTrainer }: WorkoutsTableProps) {
  const { t } = useTranslation("workouts");
  const weekLabelOf = useWeekLabel();

  if (workouts.length === 0) {
    return <Typography color="text.secondary">{t("assignedWorkouts.empty")}</Typography>;
  }

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>{t("assignedWorkouts.columns.name")}</TableCell>
            <TableCell>{t("assignedWorkouts.columns.date")}</TableCell>
            <TableCell>{t("assignedWorkouts.columns.athlete")}</TableCell>
            {showTrainer && <TableCell>{t("assignedWorkouts.columns.trainer")}</TableCell>}
            <TableCell align="right">{t("assignedWorkouts.columns.exercises")}</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {workouts.map((workout) => (
            <TableRow key={workout.id}>
              <TableCell>{workout.name}</TableCell>
              <TableCell>{weekLabelOf(workout.workoutDate)}</TableCell>
              <TableCell>{displayNameOf(workout.athlete)}</TableCell>
              {showTrainer && (
                <TableCell>
                  {workout.trainer
                    ? displayNameOf(workout.trainer)
                    : t("assignedWorkouts.noTrainer")}
                </TableCell>
              )}
              <TableCell align="right">{workout.exercises.length}</TableCell>
              <TableCell align="right" padding="checkbox">
                <IconButton
                  component={NextLink}
                  href={`/workout?id=${encodeURIComponent(workout.id)}`}
                  aria-label={t("assignedWorkouts.edit", { name: workout.name })}
                >
                  <EditOutlined />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
