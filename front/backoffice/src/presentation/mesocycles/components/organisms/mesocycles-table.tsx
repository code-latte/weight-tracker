"use client";

import EditOutlined from "@mui/icons-material/EditOutlined";
import type { Mesocycle } from "@weight-tracker/core/domain";
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
import useMesocycleDate from "@/presentation/mesocycles/hooks/use-mesocycle-date";
import { useTranslation } from "react-i18next";

interface MesocyclesTableProps {
  mesocycles: Mesocycle[];
  /** Whether the trainer column is rendered at all. */
  showTrainer: boolean;
}

/** The person's full name, or their email when they have no name. */
function displayNameOf(person: Mesocycle["athlete"]): string {
  return fullNameOf(person) ?? person.email;
}

/** The mesocycles in the order the API returns them, one row each. */
export default function MesocyclesTable({ mesocycles, showTrainer }: MesocyclesTableProps) {
  const { t } = useTranslation("mesocycles");
  const dateOf = useMesocycleDate();

  if (mesocycles.length === 0) {
    return <Typography color="text.secondary">{t("assignedPlans.empty")}</Typography>;
  }

  return (
    <TableContainer>
      <Table>
        <TableHead>
          <TableRow>
            <TableCell>{t("assignedPlans.columns.name")}</TableCell>
            <TableCell>{t("assignedPlans.columns.athlete")}</TableCell>
            {showTrainer && <TableCell>{t("assignedPlans.columns.trainer")}</TableCell>}
            <TableCell>{t("assignedPlans.columns.startDate")}</TableCell>
            <TableCell>{t("assignedPlans.columns.duration")}</TableCell>
            <TableCell align="right">{t("assignedPlans.columns.weeks")}</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {mesocycles.map((mesocycle) => {
            const duration = Number(mesocycle.durationInWeeks);

            return (
              <TableRow key={mesocycle.id}>
                <TableCell>{mesocycle.name}</TableCell>
                <TableCell>{displayNameOf(mesocycle.athlete)}</TableCell>
                {showTrainer && <TableCell>{displayNameOf(mesocycle.trainer)}</TableCell>}
                <TableCell>{dateOf(mesocycle.startDate)}</TableCell>
                <TableCell>{t("assignedPlans.duration", { count: duration })}</TableCell>
                <TableCell align="right">
                  {t("assignedPlans.definedWeeks", {
                    defined: mesocycle.weeks.length,
                    total: duration,
                  })}
                </TableCell>
                <TableCell align="right" padding="checkbox">
                  <IconButton
                    component={NextLink}
                    href={`/mesocycle?id=${encodeURIComponent(mesocycle.id)}`}
                    aria-label={t("assignedPlans.edit", { name: mesocycle.name })}
                  >
                    <EditOutlined />
                  </IconButton>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
