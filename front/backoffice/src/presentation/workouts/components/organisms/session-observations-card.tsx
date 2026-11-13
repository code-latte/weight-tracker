"use client";

import AssignmentOutlined from "@mui/icons-material/AssignmentOutlined";
import Box from "@mui/material/Box";
import { useTranslation } from "react-i18next";
import Card from "@/presentation/common/components/atoms/card";
import Chip from "@/presentation/common/components/atoms/chip";
import ObservationItem from "@/presentation/workouts/components/molecules/observation-item";
import ReviewSectionHeader from "@/presentation/workouts/components/molecules/review-section-header";
import { SESSION_OBSERVATIONS_PREVIEW } from "@/presentation/workouts/session-observations.mock";

/** The key points of the session. Sample content until the API returns them, which the "Vista previa" mark says. */
export default function SessionObservationsCard() {
  const { t } = useTranslation("workouts");

  return (
    <Card variant="outlined" sx={{ p: 2, flex: "1 1 360px", minWidth: 0 }}>
      <ReviewSectionHeader
        icon={<AssignmentOutlined />}
        title={t("review.metrics.observations.title")}
        subtitle={t("review.metrics.observations.subtitle")}
        action={
          <Chip size="small" variant="outlined" label={t("review.metrics.observations.preview")} />
        }
      />
      <Box sx={{ display: "grid", gap: 1.5 }}>
        {SESSION_OBSERVATIONS_PREVIEW.map(({ code, severity }) => (
          <ObservationItem
            key={code}
            severity={severity}
            title={t(`review.metrics.observations.${code}.title`)}
            text={t(`review.metrics.observations.${code}.text`)}
          />
        ))}
      </Box>
    </Card>
  );
}
