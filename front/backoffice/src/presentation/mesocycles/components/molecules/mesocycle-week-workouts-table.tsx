"use client";

import EditOutlined from "@mui/icons-material/EditOutlined";
import NextLink from "next/link";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";

/** A workout of the week, ready to show. */
export interface WeekWorkoutRow {
  id: string;
  name: string;
  exerciseCount: number;
  /** The workout's screen, such as "/workout?id=w-1". */
  href: string;
  /** The edit action's accessible name, such as "Editar Pierna". */
  editLabel: string;
}

interface MesocycleWeekWorkoutsTableProps {
  rows: WeekWorkoutRow[];
  /** "Nombre". */
  nameLabel: string;
  /** "Ejercicios". */
  exercisesLabel: string;
}

/** The week's workouts as a table: each one's name, how many exercises it holds and the way to edit it. */
export default function MesocycleWeekWorkoutsTable({
  rows,
  nameLabel,
  exercisesLabel,
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
              <TableCell sx={{ overflowWrap: "anywhere" }}>{row.name}</TableCell>
              <TableCell align="right">{row.exerciseCount}</TableCell>
              <TableCell align="right" padding="checkbox">
                <IconButton component={NextLink} href={row.href} aria-label={row.editLabel}>
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
