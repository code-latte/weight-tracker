"use client";

import { useTranslation } from "react-i18next";
import Chip from "@/presentation/common/components/atoms/chip";
import Typography from "@/presentation/common/components/atoms/typography";
import type { WeightChange } from "@/presentation/workouts/workout-review";

interface WeightChangeChipProps {
  /** `null` when there is nothing to compare with. */
  change: WeightChange | null;
}

/** How the top weight moved since the previous execution: up in success, down in warning, equal or not comparable plain. */
export default function WeightChangeChip({ change }: WeightChangeChipProps) {
  const { t, i18n } = useTranslation("workouts");

  if (change === null) {
    return <Typography variant="body2">{t("review.notAvailable")}</Typography>;
  }
  if (change.direction === "equal") {
    return <Typography variant="body2">{t("review.metrics.comparison.equal")}</Typography>;
  }
  const value = new Intl.NumberFormat(i18n.language, {
    signDisplay: "always",
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
  }).format(change.percent);
  const arrow = change.direction === "up" ? "▲" : "▼";

  return (
    <Chip
      size="small"
      color={change.direction === "up" ? "success" : "warning"}
      label={`${arrow} ${t("review.metrics.comparison.changeValue", { value })}`}
    />
  );
}
