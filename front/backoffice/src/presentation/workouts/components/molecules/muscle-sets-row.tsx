"use client";

import Box from "@mui/material/Box";
import { useTranslation } from "react-i18next";
import LinearProgress from "@/presentation/common/components/atoms/linear-progress";
import Typography from "@/presentation/common/components/atoms/typography";

interface MuscleSetsRowProps {
  /** The muscle's name, already translated. */
  label: string;
  sets: number;
  /** The largest count of the card, which the bar is measured against. */
  max: number;
}

/** One muscle of the distribution: its name, a bar relative to the largest count, and its sets. */
export default function MuscleSetsRow({ label, sets, max }: MuscleSetsRowProps) {
  const { t } = useTranslation("workouts");
  const count = t("review.metrics.muscles.sets", { count: sets });

  return (
    <Box
      sx={{
        display: "grid",
        gridTemplateColumns: { xs: "1fr auto", sm: "minmax(110px, 1fr) 2fr auto" },
        alignItems: "center",
        columnGap: 2,
        rowGap: 0.5,
      }}
    >
      <Typography variant="body2">{label}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ order: { xs: 0, sm: 2 } }}>
        {count}
      </Typography>
      <LinearProgress
        variant="determinate"
        value={max === 0 ? 0 : (sets / max) * 100}
        aria-label={`${label}: ${count}`}
        sx={{ height: 10, borderRadius: 5, gridColumn: { xs: "1 / -1", sm: "auto" } }}
      />
    </Box>
  );
}
