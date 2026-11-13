"use client";

import FitnessCenterOutlined from "@mui/icons-material/FitnessCenterOutlined";
import Box from "@mui/material/Box";
import { useTranslation } from "react-i18next";
import Card from "@/presentation/common/components/atoms/card";
import MuscleSetsRow from "@/presentation/workouts/components/molecules/muscle-sets-row";
import ReviewSectionHeader from "@/presentation/workouts/components/molecules/review-section-header";
import useExerciseLabels from "@/presentation/workouts/hooks/use-exercise-labels";
import type { MuscleSets } from "@/presentation/workouts/workout-review";

interface MuscleDistributionCardProps {
  /** The trained muscles, most sets first. */
  distribution: readonly MuscleSets[];
}

/** The sets the athlete did per muscle, one bar each, measured against the most worked muscle. */
export default function MuscleDistributionCard({ distribution }: MuscleDistributionCardProps) {
  const { t } = useTranslation("workouts");
  const { muscleLabel } = useExerciseLabels();
  const max = distribution[0]?.sets ?? 0;

  return (
    <Card variant="outlined" sx={{ p: 2, flex: "1 1 360px", minWidth: 0 }}>
      <ReviewSectionHeader
        icon={<FitnessCenterOutlined />}
        title={t("review.metrics.muscles.title")}
        subtitle={t("review.metrics.muscles.subtitle")}
      />
      <Box sx={{ display: "grid", gap: 2 }}>
        {distribution.map(({ muscle, sets }) => (
          <MuscleSetsRow key={muscle} label={muscleLabel(muscle)} sets={sets} max={max} />
        ))}
      </Box>
    </Card>
  );
}
