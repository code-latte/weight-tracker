"use client";

import type { ReactNode } from "react";
import AddOutlined from "@mui/icons-material/AddOutlined";
import DescriptionOutlined from "@mui/icons-material/DescriptionOutlined";
import Box from "@mui/material/Box";
import { useTranslation } from "react-i18next";
import SectionList from "@/presentation/common/components/molecules/section-list";

/** Where a workout starts from: today's form, or one of the trainer's templates. */
export type CreateWorkoutSection = "SCRATCH" | "TEMPLATE";

/** The section list on the left and the section's fields beside it, stacked on narrow screens. */
export function SectionsLayout({
  sections,
  children,
}: {
  sections: ReactNode;
  children: ReactNode;
}) {
  return (
    <Box
      sx={{
        display: "grid",
        gridTemplateColumns: { xs: "1fr", sm: "200px minmax(0, 1fr)" },
        gap: 3,
        pt: 1,
      }}
    >
      <Box>{sections}</Box>
      <Box sx={{ display: "flex", flexDirection: "column", gap: 2, minWidth: 0 }}>{children}</Box>
    </Box>
  );
}

/** "Desde cero" and "Desde plantilla", in that order; the dialog holds which one is selected. */
export default function CreateWorkoutSections({
  selected,
  onChange,
}: {
  selected: CreateWorkoutSection;
  onChange: (section: CreateWorkoutSection) => void;
}) {
  const { t } = useTranslation();

  return (
    <SectionList
      aria-label={t("workoutTemplatePicker.sectionsLabel")}
      sections={[
        {
          value: "SCRATCH",
          label: t("workoutTemplatePicker.sections.scratch"),
          icon: <AddOutlined />,
        },
        {
          value: "TEMPLATE",
          label: t("workoutTemplatePicker.sections.template"),
          icon: <DescriptionOutlined />,
        },
      ]}
      selected={selected}
      onChange={onChange}
    />
  );
}
