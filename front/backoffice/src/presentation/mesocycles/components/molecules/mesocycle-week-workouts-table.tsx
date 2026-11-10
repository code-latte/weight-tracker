"use client";

import DeleteOutlined from "@mui/icons-material/DeleteOutlined";
import EditOutlined from "@mui/icons-material/EditOutlined";
import NextLink from "next/link";
import Chip from "@/presentation/common/components/atoms/chip";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";
import { workoutStatusColorOf } from "@/presentation/common/workout-status";

/** A workout of the week, ready to show. */
export interface WeekWorkoutRow {
  id: string;
  name: string;
  /** A `WorkoutStatus` value, such as "DRAFT"; it picks the label's colour. */
  status: string;
  /** The status as shown, such as "Borrador". */
  statusLabel: string;
  exerciseCount: number;
  /** The workout's screen, such as "/workout?id=w-1". */
  href: string;
  /** The edit action's accessible name, such as "Editar Pierna". */
  editLabel: string;
  /** The delete action's accessible name, such as "Eliminar Pierna". */
  deleteLabel: string;
}

interface MesocycleWeekWorkoutsTableProps {
  rows: WeekWorkoutRow[];
  /** "Nombre". */
  nameLabel: string;
  /** "Ejercicios". */
  exercisesLabel: string;
  /** Asks to delete the workout with this id; the table sends nothing itself. */
  onDelete: (id: string) => void;
}

/**
 * The week's workouts as a table: each one's name, how many exercises it holds and the ways to edit and delete it.
 */
export default function MesocycleWeekWorkoutsTable({
  rows,
  nameLabel,
  exercisesLabel,
  onDelete,
}: MesocycleWeekWorkoutsTableProps) {
  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>{nameLabel}</TableCell>
            <TableCell align="right">{exercisesLabel}</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row) => (
            <TableRow key={row.id}>
              <TableCell sx={{ overflowWrap: "anywhere" }}>
                {row.name}
                <Chip
                  label={row.statusLabel}
                  color={workoutStatusColorOf(row.status)}
                  size="small"
                  variant="outlined"
                  sx={{ ml: 1 }}
                />
              </TableCell>
              <TableCell align="right">{row.exerciseCount}</TableCell>
              <TableCell align="right" padding="checkbox" sx={{ whiteSpace: "nowrap" }}>
                <IconButton component={NextLink} href={row.href} aria-label={row.editLabel}>
                  <EditOutlined />
                </IconButton>
                <IconButton aria-label={row.deleteLabel} onClick={() => onDelete(row.id)}>
                  <DeleteOutlined />
                </IconButton>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
