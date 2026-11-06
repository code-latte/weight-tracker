"use client";

import Box from "@mui/material/Box";
import { useTranslation } from "react-i18next";
import PersonCard, {
  type PersonCardProps,
} from "@/presentation/common/components/molecules/person-card";

interface MesocycleContextStripProps {
  athlete: PersonCardProps;
  /** `null` leaves the trainer out. */
  trainer: PersonCardProps | null;
}

/** Who the mesocycle is for, between the page header and the cards. */
export default function MesocycleContextStrip({ athlete, trainer }: MesocycleContextStripProps) {
  const { t } = useTranslation("mesocycles");

  return (
    <Box
      component="section"
      aria-label={t("mesocycle.context.label")}
      sx={{ display: "flex", flexWrap: "wrap", alignItems: "stretch", gap: 2, mb: 3 }}
    >
      <PersonCard {...athlete} />
      {trainer !== null && <PersonCard {...trainer} />}
    </Box>
  );
}
