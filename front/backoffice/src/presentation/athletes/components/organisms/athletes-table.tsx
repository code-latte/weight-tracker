"use client";

import Box from "@mui/material/Box";
import type { TrainerAthlete } from "@weight-tracker/core/domain";
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

interface AthletesTableProps {
  /** The rows to show, already filtered, in the API's order. */
  athletes: TrainerAthlete[];
  /** Whether rows are missing because of the search, rather than because the trainer has no athletes. */
  filtered: boolean;
  /** Given once the trainer's own id is known; without it the table has no actions column. */
  onEnd?: (athlete: TrainerAthlete) => void;
}

/** One row per athlete: full name, email, the date the relationship started, and an optional actions column. */
export default function AthletesTable({ athletes, filtered, onEnd }: AthletesTableProps) {
  const { t, i18n } = useTranslation("athletes");
  const dateFormat = new Intl.DateTimeFormat(i18n.language, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });

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
            <TableCell>{t("list.columns.since")}</TableCell>
            {onEnd && (
              <TableCell>
                <Box component="span" sx={visuallyHiddenSx}>
                  {t("list.columns.actions")}
                </Box>
              </TableCell>
            )}
          </TableRow>
        </TableHead>
        <TableBody>
          {athletes.map((athlete) => {
            const name = fullNameOf(athlete);

            return (
              <TableRow key={athlete.id}>
                <TableCell>{name ?? "—"}</TableCell>
                <TableCell>{athlete.email}</TableCell>
                <TableCell>{dateFormat.format(new Date(athlete.startedAtUtc))}</TableCell>
                {onEnd && (
                  <TableCell align="right" padding="checkbox">
                    <RowActions
                      label={t("list.actions.open", { name: name ?? athlete.email })}
                      actionLabel={t("list.actions.endRelationship")}
                      onAction={() => onEnd(athlete)}
                    />
                  </TableCell>
                )}
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
