"use client";

import type { ComponentProps } from "react";
import Chip from "@/presentation/common/components/atoms/chip";
import { useTranslation } from "react-i18next";

type ChipColor = ComponentProps<typeof Chip>["color"];

/** One theme colour per week type, so the phases of a block read at a glance. */
const colorOf: Record<string, ChipColor> = {
  ADAPTATION: "info",
  ACCUMULATION: "success",
  INTENSIFICATION: "warning",
  REALIZATION: "error",
  DELOAD: "secondary",
  RECOVERY: "default",
};

interface TrainingWeekTypeChipProps {
  /** A `TrainingWeekType` value, such as "ADAPTATION". */
  type: string;
}

/** A small chip with the week type's translated name, in the type's colour. */
export default function TrainingWeekTypeChip({ type }: TrainingWeekTypeChipProps) {
  const { t } = useTranslation("mesocycles");

  return (
    <Chip
      label={t(`trainingWeekType.${type}`)}
      color={colorOf[type] ?? "default"}
      size="small"
      variant="outlined"
    />
  );
}
