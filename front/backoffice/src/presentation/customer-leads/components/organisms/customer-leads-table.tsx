"use client";

import type { CustomerLead } from "@weight-tracker/core/domain";
import Table from "@/presentation/common/components/atoms/table";
import TableBody from "@/presentation/common/components/atoms/table-body";
import TableCell from "@/presentation/common/components/atoms/table-cell";
import TableContainer from "@/presentation/common/components/atoms/table-container";
import TableHead from "@/presentation/common/components/atoms/table-head";
import TableRow from "@/presentation/common/components/atoms/table-row";
import Typography from "@/presentation/common/components/atoms/typography";
import { useTranslation } from "react-i18next";

interface CustomerLeadsTableProps {
  /** The rows to show, in the API's order. */
  leads: CustomerLead[];
}

/** One row per lead: its email and when it was registered, in the user's language and local time zone. */
export default function CustomerLeadsTable({ leads }: CustomerLeadsTableProps) {
  const { t, i18n } = useTranslation("customerLeads");
  const dateFormat = new Intl.DateTimeFormat(i18n.language, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });

  if (leads.length === 0) {
    return <Typography color="text.secondary">{t("list.empty")}</Typography>;
  }

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>{t("list.columns.email")}</TableCell>
            <TableCell>{t("list.columns.registeredAt")}</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {leads.map((lead) => (
            <TableRow key={lead.email}>
              <TableCell>{lead.email}</TableCell>
              <TableCell>{dateFormat.format(new Date(lead.registeredAtUtc))}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
