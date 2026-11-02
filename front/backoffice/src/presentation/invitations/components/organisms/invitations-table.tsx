"use client";

import Box from "@mui/material/Box";
import type { AthleteInvitation } from "@weight-tracker/core/domain";
import Button from "@/presentation/common/components/atoms/button";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";
import Typography from "@/presentation/common/components/atoms/typography";
import InvitationStatusChip from "@/presentation/invitations/components/molecules/invitation-status-chip";
import { trainerNameOf } from "@/presentation/invitations/filter-invitations";
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

interface InvitationsTableProps {
  /** The rows to show, already filtered, in the API's order. */
  invitations: AthleteInvitation[];
  /** Whether rows are missing because of the search or the pills, rather than because there are no invitations. */
  filtered: boolean;
  /** Adds the trainer column, for those who see every trainer's invitations. */
  showTrainer: boolean;
  /** Given only to those who may revoke; without it the table has no actions column. */
  onRevoke?: (invitation: AthleteInvitation) => void;
}

/** One row per invitation: optionally the trainer, then the invited email, status and dates, and an optional action. */
export default function InvitationsTable({
  invitations,
  filtered,
  showTrainer,
  onRevoke,
}: InvitationsTableProps) {
  const { t, i18n } = useTranslation("invitations");
  const dateFormat = new Intl.DateTimeFormat(i18n.language, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
  const dateOf = (value: string) => dateFormat.format(new Date(value));

  if (invitations.length === 0) {
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
            {showTrainer && <TableCell>{t("list.columns.trainer")}</TableCell>}
            <TableCell>
              {t(showTrainer ? "list.columns.athleteEmail" : "list.columns.email")}
            </TableCell>
            <TableCell>{t("list.columns.status")}</TableCell>
            <TableCell>{t("list.columns.sentAt")}</TableCell>
            <TableCell>{t("list.columns.expiresAt")}</TableCell>
            {onRevoke && (
              <TableCell>
                <Box component="span" sx={visuallyHiddenSx}>
                  {t("list.columns.actions")}
                </Box>
              </TableCell>
            )}
          </TableRow>
        </TableHead>
        <TableBody>
          {invitations.map((invitation) => (
            <TableRow key={invitation.id}>
              {showTrainer && <TableCell>{trainerNameOf(invitation) ?? "—"}</TableCell>}
              <TableCell>{invitation.inviteEmail}</TableCell>
              <TableCell>
                <InvitationStatusChip
                  label={t(`status.${invitation.status}`, { defaultValue: invitation.status })}
                  pending={invitation.status === "PENDING"}
                />
              </TableCell>
              <TableCell>{dateOf(invitation.createdAtUtc)}</TableCell>
              <TableCell>{dateOf(invitation.expiresAtUtc)}</TableCell>
              {onRevoke && (
                <TableCell align="right">
                  {invitation.status === "PENDING" && (
                    <Button size="small" color="error" onClick={() => onRevoke(invitation)}>
                      {t("list.revoke")}
                    </Button>
                  )}
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
