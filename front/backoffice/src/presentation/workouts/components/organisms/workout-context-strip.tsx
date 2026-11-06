"use client";

import Box from "@mui/material/Box";
import Breadcrumbs from "@mui/material/Breadcrumbs";
import { useTranslation } from "react-i18next";
import Card from "@/presentation/common/components/atoms/card";
import Skeleton from "@/presentation/common/components/atoms/skeleton";
import Typography from "@/presentation/common/components/atoms/typography";
import PersonCard, {
  type PersonCardProps,
} from "@/presentation/common/components/molecules/person-card";

/** The mesocycle entry: still loading, or the mesocycle's name and the "Semana 2" label of the workout's week. */
export type WorkoutContextMesocycle = "loading" | { name: string; week: string };

interface WorkoutContextStripProps {
  athlete: PersonCardProps;
  /** `null` leaves the trainer out. */
  trainer: PersonCardProps | null;
  /** `null` leaves the mesocycle out. */
  mesocycle: WorkoutContextMesocycle | null;
}

/** Where the workout sits, as "Hipertrofia / Semana 2" in a card the size of a person card. It leads nowhere. */
function MesocycleCard({ mesocycle }: { mesocycle: WorkoutContextMesocycle }) {
  const { t } = useTranslation("workouts");

  return (
    <Card
      variant="outlined"
      sx={{
        display: "inline-flex",
        flexDirection: "column",
        justifyContent: "center",
        py: 1,
        px: 1.5,
      }}
    >
      <Typography variant="caption" color="text.secondary" component="p">
        {t("workout.context.mesocycle")}
      </Typography>
      {mesocycle === "loading" ? (
        <Skeleton width={160} />
      ) : (
        <Breadcrumbs aria-label={t("workout.context.breadcrumb")}>
          <Typography variant="body2" sx={{ fontWeight: 600 }}>
            {mesocycle.name}
          </Typography>
          <Typography variant="body2" sx={{ fontWeight: 600 }} aria-current="page">
            {mesocycle.week}
          </Typography>
        </Breadcrumbs>
      )}
    </Card>
  );
}

/** Whose workout it is and, when it belongs to one, the mesocycle week it sits in. */
export default function WorkoutContextStrip({
  athlete,
  trainer,
  mesocycle,
}: WorkoutContextStripProps) {
  const { t } = useTranslation("workouts");

  return (
    <Box
      component="section"
      aria-label={t("workout.context.label")}
      sx={{ display: "flex", flexWrap: "wrap", alignItems: "stretch", gap: 2, mb: 3 }}
    >
      <PersonCard {...athlete} />
      {trainer !== null && <PersonCard {...trainer} />}
      {mesocycle !== null && <MesocycleCard mesocycle={mesocycle} />}
    </Box>
  );
}
