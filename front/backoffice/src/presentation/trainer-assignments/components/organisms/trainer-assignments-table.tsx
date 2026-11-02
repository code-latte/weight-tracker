"use client";

import Box from "@mui/material/Box";
import type { UserSummary } from "@weight-tracker/core/domain";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";
import Typography from "@/presentation/common/components/atoms/typography";
import RowActions from "@/presentation/common/components/molecules/row-actions";
import { fullNameOf } from "@/presentation/common/full-name";
import { useTranslation } from "react-i18next";

/** Kept for screen readers only: the actions column needs a name, but not a visible one. */
const visuallyHiddenSx = {
  border: 0,
  clip: "rect(0 0 0 0)",
  height: "1px",
  margin: "-1px",
  overflow: "hidden",
  padding: 0,
  position: "absolute",
  whiteSpace: "nowrap",
  width: "1px",
} as const;

interface TrainerAssignmentsTableProps {
  /** The rows to show, already filtered, in the API's order. */
  athletes: UserSummary[];
  /** Whether rows are missing because of the search or the pills, rather than because there are no athletes. */
  filtered: boolean;
  /** Offered on the rows whose athlete has no trainer. */
  onAssign: (athlete: UserSummary) => void;
  /** Offered on the rows whose athlete has a trainer. */
  onEnd: (athlete: UserSummary) => void;
}

/**
 * One row per athlete: full name, email and current trainer, and the one action that fits: assigning a trainer, or
 * ending the relationship with the current one. Only the trainer the API returned decides which.
 */
export default function TrainerAssignmentsTable({
  athletes,
  filtered,
  onAssign,
  onEnd,
}: TrainerAssignmentsTableProps) {
  const { t } = useTranslation("trainerAssignments");

  if (athletes.length === 0) {
    return (
      <Typography color="text.secondary">
        {filtered ? t("list.noMatches") : t("list.empty")}
      </Typography>
    );
  }

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>{t("list.columns.fullName")}</TableCell>
            <TableCell>{t("list.columns.email")}</TableCell>
            <TableCell>{t("list.columns.trainer")}</TableCell>
            <TableCell>
              <Box component="span" sx={visuallyHiddenSx}>
                {t("list.columns.actions")}
              </Box>
            </TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {athletes.map((athlete) => {
            const name = fullNameOf(athlete);
            const trainer = athlete.trainer;

            return (
              <TableRow key={athlete.id}>
                <TableCell>{name ?? "—"}</TableCell>
                <TableCell>{athlete.email}</TableCell>
                <TableCell sx={trainer ? undefined : { color: "text.secondary" }}>
                  {trainer ? (fullNameOf(trainer) ?? trainer.email) : t("list.noTrainer")}
                </TableCell>
                <TableCell align="right" padding="checkbox">
                  <RowActions
                    label={t("list.actions.open", { name: name ?? athlete.email })}
                    actionLabel={t(
                      trainer ? "list.actions.endRelationship" : "list.actions.assign",
                    )}
                    onAction={() => (trainer ? onEnd(athlete) : onAssign(athlete))}
                  />
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
