"use client";

import CheckOutlined from "@mui/icons-material/CheckOutlined";
import { useTranslation } from "react-i18next";
import Chip from "@/presentation/common/components/atoms/chip";
import Typography from "@/presentation/common/components/atoms/typography";
import { percentOf } from "@/presentation/workouts/workout-review";

interface ComplianceChipProps {
  inTarget: number | string;
  evaluated: number | string;
}

/** The share of evaluated sets that met their target: success with a check at 100%, warning below it. */
export default function ComplianceChip({ inTarget, evaluated }: ComplianceChipProps) {
  const { t } = useTranslation("workouts");
  const percent = percentOf(inTarget, evaluated);

  if (percent === null) {
    return <Typography variant="body2">{t("review.notAvailable")}</Typography>;
  }
  return (
    <Chip
      size="small"
      color={percent === 100 ? "success" : "warning"}
      icon={percent === 100 ? <CheckOutlined /> : undefined}
      label={t("review.metrics.percentValue", { value: percent })}
    />
  );
}
