"use client";

import AddOutlined from "@mui/icons-material/AddOutlined";
import Box from "@mui/material/Box";
import type { MesocycleWeek } from "@weight-tracker/core/domain";
import { useTranslation } from "react-i18next";
import Button from "@/presentation/common/components/atoms/button";
import Card from "@/presentation/common/components/atoms/card";
import CardContent from "@/presentation/common/components/atoms/card-content";
import List from "@/presentation/common/components/atoms/list";
import Typography from "@/presentation/common/components/atoms/typography";
import MesocycleWeekListItem from "@/presentation/mesocycles/components/molecules/mesocycle-week-list-item";
import { weekNumberOf } from "@/presentation/mesocycles/weeks";

interface MesocycleWeeksCardProps {
  titleId: string;
  /** The defined weeks, already in the order to show them. */
  weeks: MesocycleWeek[];
  /** The mesocycle does not exist yet: nothing can be planned. */
  locked: boolean;
  /** Whether a week is left to define. */
  canAdd: boolean;
  onAdd: () => void;
  onEdit: (week: MesocycleWeek) => void;
}

/** The mesocycle's weeks under a "Planificación semanal" header, with the action that defines the next one. */
export default function MesocycleWeeksCard({
  titleId,
  weeks,
  locked,
  canAdd,
  onAdd,
  onEdit,
}: MesocycleWeeksCardProps) {
  const { t } = useTranslation("mesocycles");

  function body() {
    if (locked) {
      return <Typography color="text.secondary">{t("weeks.saveFirst")}</Typography>;
    }
    if (weeks.length === 0) {
      return <Typography color="text.secondary">{t("weeks.empty")}</Typography>;
    }
    return (
      <List sx={{ display: "flex", flexDirection: "column", gap: 1, p: 0 }}>
        {weeks.map((week) => {
          const number = weekNumberOf(week);

          return (
            <MesocycleWeekListItem
              key={number}
              type={week.trainingWeekType}
              title={t("weeks.week", { number })}
              objective={week.objective}
              editLabel={t("weeks.edit", { number })}
              onEdit={() => onEdit(week)}
            />
          );
        })}
      </List>
    );
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: 1,
            mb: 2,
          }}
        >
          <Box>
            <Typography id={titleId} variant="h6" component="h2">
              {t("weeks.title")}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {t("weeks.subtitle")}
            </Typography>
          </Box>
          <Button
            variant="contained"
            startIcon={<AddOutlined />}
            onClick={onAdd}
            disabled={locked || !canAdd}
          >
            {t("weeks.add")}
          </Button>
        </Box>
        {body()}
      </CardContent>
    </Card>
  );
}
