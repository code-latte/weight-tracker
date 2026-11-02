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
import UserStatusChip from "@/presentation/users/components/molecules/user-status-chip";
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

interface UsersTableProps {
  /** The rows to show, already filtered, in the API's order. */
  users: UserSummary[];
  /** Whether rows are missing because of the search, the pills or the inactive switch, rather than because there are no users. */
  filtered: boolean;
  /** Given only to those who may change activation; without it the table has no actions column. */
  onToggleActivation?: (user: UserSummary) => void;
  /** The caller's own row gets no action: the API does not let anyone deactivate themselves. */
  currentUserId?: string;
}

/** One row per user: full name, email and profile; inactive users muted and marked, and an optional actions column. */
export default function UsersTable({
  users,
  filtered,
  onToggleActivation,
  currentUserId,
}: UsersTableProps) {
  const { t, i18n } = useTranslation("users");
  const dateFormat = new Intl.DateTimeFormat(i18n.language, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });

  if (users.length === 0) {
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
            <TableCell>{t("list.columns.profile")}</TableCell>
            {onToggleActivation && (
              <TableCell>
                <Box component="span" sx={visuallyHiddenSx}>
                  {t("list.columns.actions")}
                </Box>
              </TableCell>
            )}
          </TableRow>
        </TableHead>
        <TableBody>
          {users.map((user) => {
            const name = fullNameOf(user);

            return (
              <TableRow
                key={user.id}
                sx={user.isActive ? undefined : { "& td": { color: "text.secondary" } }}
              >
                <TableCell>
                  <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    {name ?? "—"}
                    {!user.isActive && (
                      <UserStatusChip
                        label={t("list.status.inactive")}
                        tooltip={
                          user.deactivatedAtUtc
                            ? t("list.status.deactivatedAt", {
                                date: dateFormat.format(new Date(user.deactivatedAtUtc)),
                              })
                            : t("list.status.inactive")
                        }
                      />
                    )}
                  </Box>
                </TableCell>
                <TableCell>{user.email}</TableCell>
                <TableCell>{t(`roles.${user.role}`, { defaultValue: user.role })}</TableCell>
                {onToggleActivation && (
                  <TableCell align="right" padding="checkbox">
                    {user.id !== currentUserId && (
                      <RowActions
                        label={t("list.actions.open", { name: name ?? user.email })}
                        actionLabel={t(
                          user.isActive ? "list.actions.deactivate" : "list.actions.activate",
                        )}
                        onAction={() => onToggleActivation(user)}
                      />
                    )}
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
