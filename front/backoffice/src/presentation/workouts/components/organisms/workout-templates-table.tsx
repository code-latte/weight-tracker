"use client";

import DeleteOutlined from "@mui/icons-material/DeleteOutlined";
import EditOutlined from "@mui/icons-material/EditOutlined";
import type { WorkoutTemplate } from "@weight-tracker/core/domain";
import NextLink from "next/link";
import IconButton from "@/presentation/common/components/atoms/icon-button";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";
import Typography from "@/presentation/common/components/atoms/typography";
import { useTranslation } from "react-i18next";

interface WorkoutTemplatesTableProps {
  templates: WorkoutTemplate[];
  /** Whether the trainer column is rendered at all. */
  showTrainer: boolean;
  /** Asks to delete this template; the table sends nothing itself. */
  onDelete: (template: WorkoutTemplate) => void;
  /** The trainer's display name by id; a template only carries the id. */
  trainerNameOf: (trainerId: string) => string | null;
}

/** The templates in the order the API returns them, one row each. */
export default function WorkoutTemplatesTable({
  templates,
  showTrainer,
  trainerNameOf,
  onDelete,
}: WorkoutTemplatesTableProps) {
  const { t } = useTranslation("workouts");

  if (templates.length === 0) {
    return <Typography color="text.secondary">{t("workoutTemplates.empty")}</Typography>;
  }

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>{t("workoutTemplates.columns.name")}</TableCell>
            <TableCell>{t("workoutTemplates.columns.description")}</TableCell>
            {showTrainer && <TableCell>{t("workoutTemplates.columns.trainer")}</TableCell>}
            <TableCell align="right">{t("workoutTemplates.columns.exercises")}</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {templates.map((template) => (
            <TableRow key={template.id}>
              <TableCell>{template.name}</TableCell>
              <TableCell>{template.description ?? ""}</TableCell>
              {showTrainer && (
                <TableCell>
                  {trainerNameOf(template.trainerId) ?? t("workoutTemplates.unknownTrainer")}
                </TableCell>
              )}
              <TableCell align="right">{template.exercises.length}</TableCell>
              <TableCell align="right" padding="checkbox" sx={{ whiteSpace: "nowrap" }}>
                <IconButton
                  component={NextLink}
                  href={`/workout-template?id=${encodeURIComponent(template.id)}`}
                  aria-label={t("workoutTemplates.edit", { name: template.name })}
                >
                  <EditOutlined />
                </IconButton>
                <IconButton
                  aria-label={t("workoutTemplates.delete", { name: template.name })}
                  onClick={() => onDelete(template)}
                >
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
